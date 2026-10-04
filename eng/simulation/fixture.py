"""Independent stdlib device oracle: real Wxkc TCP PLC, camera results and HTTP.
Camera messages deliberately contain no cart Id or reliable trigger counter.
Expected bindings and chutes are calculated independently of the DWS runtime.
"""
import asyncio
import base64
import datetime as dt
import json
import struct
import time
import zlib
from pathlib import Path


def png(width=96, height=64):
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data))
    rows = (b"\x00" + b"\x27\x75\xd0" * width) * height
    return base64.b64encode(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 2, 0, 0, 0))
                            + chunk(b"IDAT", zlib.compress(rows)) + chunk(b"IEND", b"")).decode()


IMAGE = png()


def frame(function, cart, chute=0):
    data = bytes((0xF9, function, cart >> 8, cart & 255, chute >> 8, chute & 255, 1))
    checksum = 0
    for byte in data[1:]:
        checksum ^= byte
    return data + bytes((checksum,))


class Fixture:
    def __init__(self):
        self.config = {}
        self.http_connection_sequence = 0
        self.reset_state()
        self.plc = None
        self.camera = None
        self.plc_lock = asyncio.Lock()
        self.camera_lock = asyncio.Lock()
        self.tasks = set()

    def reset_state(self):
        self.truth, self.scan_lookup = {}, {}
        self.commands, self.queries, self.errors = [], [], []
        self.created = self.final_results = self.callbacks = 0
        self.done = False
        self.started_at = self.finished_at = None
        self.fragmented_writes = self.coalesced_writes = 0
        self.active_queries = self.max_active_queries = 0
        self.api_connection_ids = set()

    def task(self, coroutine):
        task = asyncio.create_task(coroutine)
        self.tasks.add(task)
        def finished(t):
            self.tasks.discard(t)
            if not t.cancelled() and t.exception():
                self.errors.append({"stage": "fixture", "error": repr(t.exception())})
        task.add_done_callback(finished)
        return task

    async def send_plc(self, data, split=False):
        async with self.plc_lock:
            if split:
                self.fragmented_writes += 1
                for part in (data[:3], data[3:6], data[6:]):
                    self.plc.write(part)
                    await self.plc.drain()
                    await asyncio.sleep(.001)
            else:
                self.plc.write(data)
                await self.plc.drain()

    async def plc_connection(self, reader, writer):
        self.plc = writer
        try:
            while True:
                data = await reader.readexactly(8)
                checksum = 0
                for value in data[1:7]:
                    checksum ^= value
                if data[0] != 0xF9 or data[-1] != checksum or data[1] != 0x11:
                    self.errors.append({"stage": "plc", "error": "invalid output frame/function", "hex": data.hex()})
                    continue
                cart = int.from_bytes(data[2:4], "big")
                chute = int.from_bytes(data[4:6], "big")
                row = self.truth.get(cart)
                self.commands.append({"id": cart, "chute": chute, "received_at": time.time()})
                if not row or not row["accepted"] or chute != row["chute"]:
                    self.errors.append({"stage": "plc", "error": "wrong cart/chute or suppressed cart", "id": cart,
                                        "actual": chute, "expected": row})
                elif row.get("command_seen"):
                    self.errors.append({"stage": "plc", "error": "duplicate sort", "id": cart})
                else:
                    row["command_seen"] = True
                self.task(self.callback(cart, chute))
        except (asyncio.IncompleteReadError, ConnectionError):
            pass
        finally:
            if self.plc is writer:
                self.plc = None
            writer.close()

    async def callback(self, cart, chute):
        await asyncio.sleep(.12 + (cart % 7) * .045)
        if not self.plc:
            return
        await self.send_plc(frame(0x21, cart, chute), split=cart % 31 == 0)
        self.callbacks += 1
        if self.config.get("duplicate_callbacks") and cart % 101 == 0:
            await asyncio.sleep(.025)
            await self.send_plc(frame(0x21, cart, chute))
            self.callbacks += 1

    async def camera_connection(self, reader, writer):
        self.camera = writer
        try:
            await reader.read()
        finally:
            if self.camera is writer:
                self.camera = None
            writer.close()

    async def camera_worker(self, queue):
        while True:
            item = await queue.get()
            if item is None:
                return
            due, message = item
            await asyncio.sleep(max(0, due - time.monotonic()))
            async with self.camera_lock:
                self.camera.write(json.dumps(message, separators=(",", ":")).encode() + b"\n")
                await self.camera.drain()
            self.final_results += 1

    async def run(self):
        config = self.config
        count = config["count"]
        interval = config["interval_ms"] / 1000
        cameras = config.get("cameras", 1)
        queues = [asyncio.Queue() for _ in range(cameras)]
        workers = [self.task(self.camera_worker(q)) for q in queues]
        self.started_at = time.time()
        origin = time.monotonic()
        image = png(config.get("image_width", 96), config.get("image_height", 64))
        batch = bytearray()
        for index in range(count):
            await asyncio.sleep(max(0, origin + index * interval - time.monotonic()))
            cart = index + 1
            now = dt.datetime.now(dt.timezone.utc)
            no_read = cart % 5 == 0 or 121 <= cart <= 180
            filtered = cart % 37 == 0
            conflict = cameras > 1 and cart % 113 == 0
            outside = config.get("window", False) and cart % 97 == 0
            scan = now + dt.timedelta(milliseconds=900 if outside else 250)
            scan_key = scan.strftime("%Y%m%d%H%M%S") + f"{scan.microsecond // 1000:03d}"
            # Keep full scan times unique for the independent completion/SQLite oracle.
            # DefaultApi's ScanTimeValue template currently only sends whole seconds.
            while scan_key in self.scan_lookup:
                scan += dt.timedelta(milliseconds=1)
                scan_key = scan.strftime("%Y%m%d%H%M%S") + f"{scan.microsecond // 1000:03d}"
            accepted = not filtered and not conflict and not outside and (config["use_noread"] or not no_read)
            api_fault = config.get("api_faults", False) and not no_read and (cart % 499 == 0 or cart % 997 == 0)
            reply_fault = config.get("reply_faults", False) and not no_read and (
                cart % 131 == 0 or cart % 173 == 0 or cart % 211 == 0 or cart % 8 + 1 in range(2, 8))
            row = {"id": cart, "barcode": "NoRead" if no_read else f"SIM{cart:08d}",
                   "scan_time": scan_key, "accepted": accepted,
                   "chute": 99 if api_fault or reply_fault else 8 if no_read else cart % 8 + 1,
                   "reason": "filtered" if filtered else "conflict" if conflict else "window" if outside
                             else "noread_disabled" if no_read and not config["use_noread"] else "accepted"}
            self.truth[cart] = row
            self.scan_lookup[scan_key] = row
            batch += frame(0x12, cart)
            self.created += 1
            if len(batch) >= 8 * config.get("batch_size", 1) or index == count - 1:
                if len(batch) > 8:
                    self.coalesced_writes += 1
                await self.send_plc(bytes(batch), split=cart % 43 == 0)
                batch.clear()
            for camera_index, queue in enumerate(queues):
                barcode = row["barcode"]
                if cameras > 1 and camera_index == 0 and not no_read and cart % 3 == 0:
                    barcode = "NoRead"
                if conflict:
                    barcode = f"CONFLICT{cart:08d}-{camera_index}"
                response_delay = (.04, .18, .65, .09, .9, .12, .35)[cart % 7] + .025 * camera_index
                message = {"camera": f"SIM-CAMERA-{camera_index + 1}", "barcode": barcode,
                           "scan_time": scan.isoformat(), "filtered": filtered, "image": image}
                queue.put_nowait((time.monotonic() + response_delay, message))
        for queue in queues:
            queue.put_nowait(None)
        await asyncio.gather(*workers)
        async with self.camera_lock:
            self.camera.write(b'{"end":true}\n')
            await self.camera.drain()
        self.done = True
        self.finished_at = time.time()

    def status(self):
        return {"created": self.created, "camera_results": self.final_results, "commands": len(self.commands),
                "queries": len(self.queries), "callbacks": self.callbacks, "done": self.done,
                "expected": sum(row["accepted"] for row in self.truth.values()), "errors": self.errors[:20],
                "fragmented_writes": self.fragmented_writes, "coalesced_writes": self.coalesced_writes,
                "max_active_queries": self.max_active_queries,
                "api_tcp_connections": len(self.api_connection_ids),
                "elapsed_seconds": time.time() - self.started_at if self.started_at else 0}

    async def route(self, method, path, body):
        if path == "/health":
            return {"ready": True, "plc_connected": self.plc is not None, "camera_connected": self.camera is not None}
        if path == "/configure":
            if self.plc or self.camera or self.tasks:
                raise ValueError("previous runtime is still connected/active")
            self.reset_state()
            self.config = body
            return {"configured": body}
        if path == "/start":
            if not self.plc or not self.camera or self.started_at:
                raise ValueError("one PLC and camera connection required; start only once")
            self.task(self.run())
            return {"started": True}
        if path == "/status":
            return {**self.status(), "locked_exits": [6] if self.config.get("reply_faults") else []}
        if path == "/query":
            barcode = body.get("barcode", "")
            cart = int(barcode[3:]) if barcode.startswith("SIM") else 0
            row = self.truth.get(cart)
            self.queries.append(body)
            if barcode != "NoRead" and (not row or not row["accepted"] or barcode != row["barcode"]):
                self.errors.append({"stage": "api", "error": "wrong/suppressed barcode upload", "request": body, "expected": row})
            self.active_queries += 1
            self.max_active_queries = max(self.max_active_queries, self.active_queries)
            try:
                delay = ((cart or len(self.queries)) % 11) * .012
                if cart and self.config.get("api_faults") and cart % 997 == 0:
                    delay = 1.2  # Deliberate HTTP timeout: original cart must use abnormal exit 99.
                await asyncio.sleep(delay)
                if cart and self.config.get("api_faults") and cart % 499 == 0:
                    return {"ok": True, "missing_chute": True}
                if cart and self.config.get("reply_faults"):
                    if cart % 131 == 0:
                        return {"ok": False, "chute": 8}  # Valid target text must not override a failed query.
                    if cart % 173 == 0:
                        return "200 OK", b'{"ok":true,'  # Success-looking malformed JSON must use fallback.
                    if cart % 211 == 0:
                        return "503 Service Unavailable", b'{"ok":true,"chute":8}'
                return {"ok": True, "chute": 8 if barcode == "NoRead" else cart % 8 + 1}
            finally:
                self.active_queries -= 1
        if path == "/verify":
            expected = {cart: row for cart, row in self.truth.items() if row["accepted"]}
            for stage in ("completed", "displayed"):
                actual, seen = body.get(stage, []), set()
                for item in actual:
                    cart = item["id"]
                    row = expected.get(cart)
                    if cart in seen or not row or item["barcode"] != row["barcode"] or item["scan_time"] != row["scan_time"]:
                        self.errors.append({"stage": stage, "error": "duplicate, wrong binding, or suppressed output", "actual": item, "expected": row})
                    seen.add(cart)
                missing = sorted(expected.keys() - seen)
                if missing:
                    self.errors.append({"stage": stage, "error": "missing output", "count": len(missing), "sample": missing[:20]})
            command_ids = [item["id"] for item in self.commands]
            if len(command_ids) != len(expected) or set(command_ids) != set(expected):
                self.errors.append({"stage": "plc", "error": "sort count/set mismatch", "expected": len(expected), "actual": len(command_ids)})
            if len(self.queries) != len(expected):
                self.errors.append({"stage": "api", "error": "HTTP count mismatch", "expected": len(expected), "actual": len(self.queries)})
            if sum(query.get("barcode") == "NoRead" for query in self.queries) != sum(row["barcode"] == "NoRead" for row in expected.values()):
                self.errors.append({"stage": "api", "error": "NoRead HTTP count mismatch"})
            report = {"passed": not self.errors, "config": self.config, "status": self.status(), "errors": self.errors,
                      "expected_counts_by_reason": {reason: sum(row["reason"] == reason for row in self.truth.values())
                        for reason in ("accepted", "noread_disabled", "filtered", "conflict", "window")}}
            directory = Path("/results")
            directory.mkdir(parents=True, exist_ok=True)
            name = self.config.get("name", "run")
            (directory / (name + ".json")).write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
            (directory / (name + "-truth.json")).write_text(json.dumps(list(self.truth.values())), encoding="utf-8")
            return report
        raise ValueError("unknown route")

    async def http_connection(self, reader, writer):
        self.http_connection_sequence += 1
        connection_id = self.http_connection_sequence
        try:
            while True:
                close = False
                try:
                    raw = await asyncio.wait_for(reader.readuntil(b"\r\n\r\n"), 60)
                    lines = raw.decode().split("\r\n")
                    method, path, version = lines[0].split()
                    headers = {k.strip().lower(): v.strip() for line in lines[1:] if ":" in line
                               for k, v in [line.split(":", 1)]}
                    close = version != "HTTP/1.1" or headers.get("connection", "").lower() == "close"
                    length = int(headers.get("content-length", "0"))
                    if "chunked" in headers.get("transfer-encoding", "").lower():
                        chunks = []
                        while True:
                            size = int((await reader.readline()).split(b";", 1)[0].strip(), 16)
                            if size == 0:
                                # Consume optional trailers so the next request starts at its own header.
                                while await reader.readline() not in (b"\r\n", b"\n", b""):
                                    pass
                                break
                            chunks.append(await reader.readexactly(size))
                            if await reader.readexactly(2) != b"\r\n":
                                raise ValueError("invalid chunk terminator")
                        payload = b"".join(chunks)
                    else:
                        payload = await reader.readexactly(length) if length else b"{}"
                    if path == "/query":
                        self.api_connection_ids.add(connection_id)
                    result = await self.route(method, path, json.loads(payload))
                    status = "200 OK"
                except (asyncio.IncompleteReadError, TimeoutError):
                    break
                except Exception as exception:
                    result, status, close = {"error": str(exception)}, "400 Bad Request", True
                if isinstance(result, tuple):
                    status, payload = result
                else:
                    payload = json.dumps(result, ensure_ascii=False, separators=(",", ":")).encode()
                connection = "close" if close else "keep-alive"
                writer.write(f"HTTP/1.1 {status}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {len(payload)}\r\nConnection: {connection}\r\n\r\n".encode() + payload)
                await writer.drain()
                if close:
                    break
        except (ConnectionError, BrokenPipeError):
            pass  # Deliberate timeout closes the client socket before the delayed response.
        finally:
            writer.close()
            try:
                await writer.wait_closed()
            except ConnectionError:
                pass


async def main():
    fixture = Fixture()
    servers = [await asyncio.start_server(fixture.http_connection, "0.0.0.0", 18086),
               await asyncio.start_server(fixture.plc_connection, "0.0.0.0", 19001),
               await asyncio.start_server(fixture.camera_connection, "0.0.0.0", 19002)]
    print("Python fixture ready: HTTP 18086, Wxkc PLC 19001, camera events 19002", flush=True)
    await asyncio.gather(*(server.serve_forever() for server in servers))


if __name__ == "__main__":
    asyncio.run(main())
