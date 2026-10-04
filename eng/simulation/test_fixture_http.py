"""Exercise real HTTP streams: pooling, chunk trailers, and explicit connection close."""
import asyncio
import json
import unittest

from fixture import Fixture


class FixtureHttpTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.fixture = Fixture()
        self.server = await asyncio.start_server(self.fixture.http_connection, "127.0.0.1", 0)
        port = self.server.sockets[0].getsockname()[1]
        self.reader, self.writer = await asyncio.open_connection("127.0.0.1", port)

    async def asyncTearDown(self):
        self.writer.close()
        await self.writer.wait_closed()
        self.server.close()
        await self.server.wait_closed()

    async def response(self):
        raw = await asyncio.wait_for(self.reader.readuntil(b"\r\n\r\n"), 2)
        headers = {k.strip().lower(): v.strip() for line in raw.decode().split("\r\n")[1:] if ":" in line
                   for k, v in [line.split(":", 1)]}
        body = await self.reader.readexactly(int(headers["content-length"]))
        return headers, json.loads(body)

    async def test_reuses_one_socket_and_preserves_response_boundaries(self):
        self.writer.write(b"GET /health HTTP/1.1\r\nHost: fixture\r\n\r\n" * 2)
        await self.writer.drain()
        for _ in range(2):
            headers, result = await self.response()
            self.assertEqual("keep-alive", headers["connection"])
            self.assertTrue(result["ready"])
        self.assertEqual(1, self.fixture.http_connection_sequence)

    async def test_chunk_trailer_does_not_corrupt_next_request(self):
        self.writer.write(b"POST /health HTTP/1.1\r\nHost: fixture\r\nTransfer-Encoding: chunked\r\n\r\n"
                          b"2\r\n{}\r\n0\r\nX-Fixture: trailer\r\n\r\n"
                          b"GET /health HTTP/1.1\r\nHost: fixture\r\nConnection: close\r\n\r\n")
        await self.writer.drain()
        self.assertTrue((await self.response())[1]["ready"])
        headers, result = await self.response()
        self.assertEqual("close", headers["connection"])
        self.assertTrue(result["ready"])
        self.assertEqual(b"", await asyncio.wait_for(self.reader.read(), 2))


if __name__ == "__main__":
    unittest.main()
