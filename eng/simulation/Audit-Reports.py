"""Read-only independent audit of retained fixture truth, events, SQLite and images."""
import argparse
import collections
import datetime as dt
import json
import sqlite3
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def audit(report_path, fixture_dir, utc_offset):
    report_path = Path(report_path).resolve()
    root, report = report_path.parent, load(report_path)
    config = report["config"]
    expected = {row["id"]: row for row in load(fixture_dir / (config["name"] + "-truth.json")) if row["accepted"]}
    completed = load(root / "completed.json")
    by_timestamp = {row["timestamp"]: row for row in completed}
    errors = []

    def check(condition, message):
        if not condition:
            errors.append(message)

    check(report["passed"] and report["oracle"]["passed"], "runtime/oracle report failed")
    check(not report["runtime_errors"] and not report["database_errors"], "reported errors")
    check(not report.get("error_log_lines_before_shutdown", []), "Error/Fatal log before shutdown")
    check(len(completed) == len(by_timestamp) == len(expected), "completion identity/count mismatch")
    check({row["id"] for row in completed} == set(expected), "completion cart set mismatch")
    for row in completed:
        original = expected.get(row["id"])
        check(original is not None and row["barcode"] == original["barcode"] and row["scan_time"] == original["scan_time"],
              f"original trigger binding mismatch: {row['id']}")

    for name, value in report["counts"].items():
        multiplier = 3 if name == "Instructions" else 1
        check(value == len(expected) * multiplier, f"reported {name} count mismatch")
    check(report["oracle"]["status"]["created"] == config["count"], "trigger count mismatch")
    check(report["camera_results"] == config["count"] * config["cameras"], "camera result count mismatch")

    with sqlite3.connect((root / "Data.db").as_uri() + "?mode=ro", uri=True) as db:
        check(db.execute("pragma integrity_check").fetchone()[0] == "ok", "SQLite integrity failure")
        if config.get("query_stress"):
            query = report.get("query_test", {})
            check(query.get("passed") and not query.get("errors"), "concurrent query/UI validation failed")
            check(query.get("maximum_page_items", 0) <= 100, "history UI page bound exceeded")
            check(db.execute("SELECT COUNT(*) FROM Data_PackageInfo WHERE PackageTimestamped<0").fetchone()[0]
                  == config["seed_history_count"], "history fixture count changed")
            ns = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
            with zipfile.ZipFile(query["export_path"]) as book:
                with book.open("xl/worksheets/sheet1.xml") as sheet:
                    export_barcodes = []
                    for _, element in ET.iterparse(sheet, events=("end",)):
                        if element.tag != "{" + ns["s"] + "}row":
                            continue
                        if int(element.attrib["r"]) > 2:
                            cell = next(c for c in element if c.attrib.get("r", "").startswith("C"))
                            export_barcodes.append("".join(cell.itertext()))
                        element.clear()
                    check(export_barcodes == [f"HISTORY-EXPORT-{i:08d}" for i in range(1, 5001)],
                          "streaming export barcode order/count/identity mismatch")
        db.row_factory = sqlite3.Row
        rows = db.execute("""
          SELECT p.Id, p.PackageTimestamped, b.Barcode, b.ScanTime, s.Id AS SortingId, s.SortingCode,
                 e.PhysicalExitId, e.TheoreticalExit, u.RequestContent
          FROM Data_PackageInfo p
          LEFT JOIN Data_BarCodeInfo b ON b.PackageId=p.Id
          LEFT JOIN Data_SortingInfo s ON s.PackageId=p.Id
          LEFT JOIN Data_ExitInfo e ON e.PackageId=p.Id
          LEFT JOIN Data_UploadInfo u ON u.PackageId=p.Id
          WHERE p.PackageTimestamped>0
        """).fetchall()
        check(len(rows) == len(expected), "SQLite package count mismatch")
        images = collections.defaultdict(list)
        for row in db.execute("SELECT PackageId, LocalPath FROM Data_ImageInfo"):
            images[row["PackageId"]].append(row["LocalPath"])
        instructions = collections.defaultdict(collections.Counter)
        for row in db.execute("SELECT SortingInfoId, InstructionType FROM Data_InstructionInfo"):
            instructions[row["SortingInfoId"]][row["InstructionType"]] += 1
        physical = load(root / "physical.json")
        physical_by_timestamp = {row["Timestamp"]: row for row in physical}
        check(len(physical) == len(physical_by_timestamp) == len(expected), "physical events duplicated/missing")
        for row in rows:
            source = by_timestamp.get(row["PackageTimestamped"])
            if source is None:
                errors.append("unexpected SQLite identity " + str(row["PackageTimestamped"]))
                continue
            original = expected[source["id"]]
            check(row["Barcode"] == original["barcode"] and row["SortingCode"] == str(original["id"]),
                  f"SQLite barcode/cart mismatch: {original['id']}")
            parsed = dt.datetime.fromisoformat(row["ScanTime"])
            if parsed.tzinfo is None:
                parsed = parsed.replace(tzinfo=dt.timezone(dt.timedelta(hours=utc_offset)))
            parsed = parsed.astimezone(dt.timezone.utc)
            scan_key = parsed.strftime("%Y%m%d%H%M%S") + f"{parsed.microsecond // 1000:03d}"
            check(scan_key == original["scan_time"], f"SQLite full scan time mismatch: {original['id']}")
            check(row["PhysicalExitId"] == original["chute"] and row["TheoreticalExit"] == f"CHUTE-{original['chute']}",
                  f"SQLite chute mismatch: {original['id']}")
            check(original["barcode"] in (row["RequestContent"] or ""), f"upload associated with another barcode: {original['id']}")
            check(instructions[row["SortingId"]] == collections.Counter({1: 1, 2: 1, 3: 1}),
                  f"create/send/callback history mismatch: {original['id']}")
            paths = images[row["Id"]]
            check(len(paths) == 1 and all(Path(path).is_file() and Path(path).stat().st_size > 0 for path in paths),
                  f"image missing/duplicated: {original['id']}")
            event = physical_by_timestamp.get(source["timestamp"])
            check(event is not None and event["ExitId"] == original["chute"] and event["InstructionType"] == 3,
                  f"physical callback associated with another trigger: {original['id']}")
    manifest = load(root / "runtime-manifest.json") if (root / "runtime-manifest.json").is_file() else {}
    return {"scenario": config["name"], "passed": not errors, "triggers": config["count"], "accepted": len(expected),
            "camera_results": report["camera_results"], "elapsed_seconds": report["elapsed_seconds"],
            "peak_working_set_mb": report.get("peak_working_set_mb"),
            "client_dll_sha256": report.get("client_dll_sha256") or manifest.get("client_dll_sha256"),
            "report": str(report_path), "errors": errors}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("reports", nargs="+")
    parser.add_argument("--fixture-dir", type=Path, default=Path(__file__).resolve().parents[2] / "output/simulation/fixture")
    parser.add_argument("--utc-offset", type=float, default=8)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    results = [audit(path, args.fixture_dir, args.utc_offset) for path in args.reports]
    result = {"passed": all(row["passed"] for row in results), "triggers": sum(row["triggers"] for row in results),
              "accepted": sum(row["accepted"] for row in results), "camera_results": sum(row["camera_results"] for row in results),
              "scenarios": results}
    content = json.dumps(result, ensure_ascii=False, indent=2)
    if args.output:
        args.output.write_text(content, encoding="utf-8")
    print(content)
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
