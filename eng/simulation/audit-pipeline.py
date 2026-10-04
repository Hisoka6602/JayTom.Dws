"""Audit persisted pipeline identities against the independent completed-parcel evidence."""

import argparse
import json
from collections import Counter
from decimal import Decimal
from pathlib import Path


def audit(root):
    report = json.loads((root / "report.json").read_text(encoding="utf-8-sig"))
    expected = {row["timestamp"]: row for row in json.loads((root / "completed.json").read_text(encoding="utf-8-sig"))}
    count = report["config"]["count"]
    stages = Counter()
    parcels = {}
    slots = {}
    paired = {}
    duplicates = set()
    gaps = []
    for path in sorted((root / "logs/Pipeline").glob("*.jsonl*")):
        with path.open(encoding="utf-8-sig") as stream:
            for line_number, line in enumerate(stream, 1):
                try:
                    row = json.loads(line, parse_float=Decimal)
                except json.JSONDecodeError:
                    gaps.append(f"Invalid JSON: {path.name}:{line_number}")
                    continue
                key = row.get("runId"), row.get("eventSequence")
                if key[0] and key[1]:
                    if key in duplicates:
                        continue
                    duplicates.add(key)
                stage = row.get("stage", "legacy-warning")
                stages[stage] += 1
                timestamp = row.get("packageTimestamp", 0)
                if timestamp > 0:
                    parcels.setdefault(timestamp, {})[stage] = row
                if stage == "trigger.slot.queued":
                    slots[timestamp] = row
                elif stage == "camera.paired":
                    paired[timestamp] = row
                elif stage == "diagnostics.loss":
                    gaps.append("Diagnostic loss was reported")

    errors = list(gaps)
    if len(slots) != count or len(paired) != count:
        errors.append(f"Original positions: queued={len(slots)}, paired={len(paired)}, expected={count}")
    for timestamp, pair in paired.items():
        slot = slots.get(timestamp)
        if not slot or pair.get("cartId") != slot.get("cartId") or pair.get("cameraOrdinal") != slot.get("triggerSequence"):
            errors.append(f"Pair does not match original trigger: {timestamp}")
    for stage in ("package.completed", "api.query.completed", "sorting.reply.sent", "persistence.package.saved"):
        actual = {timestamp for timestamp, records in parcels.items() if stage in records}
        if actual != expected.keys():
            errors.append(f"{stage}: missing={len(expected.keys() - actual)}, unexpected={len(actual - expected.keys())}")
    for timestamp, item in expected.items():
        records = parcels.get(timestamp, {})
        for stage in ("camera.paired", "barcode.assignment.selected", "package.completed", "api.query.completed",
                      "sorting.reply.queued", "plc.send.requested", "plc.send.started", "plc.write.attempt", "sorting.reply.sent"):
            record = records.get(stage)
            if not record or record.get("cartId") != item["id"] or record.get("barcode") != item["barcode"]:
                errors.append(f"{stage}: wrong original identity for {item['id']}")
        if "plc.fall.received" not in records:
            errors.append(f"No actual PLC fall evidence for {item['id']}")
        reply = records.get("sorting.reply.sent", {})
        queued = records.get("sorting.reply.queued", {})
        if reply.get("exitId") != queued.get("exitId"):
            errors.append(f"Selected and written chute mismatch for {item['id']}")
    result = {"passed": not errors, "scenario": root.name, "triggers": count, "completed": len(expected),
              "original_pairs": len(paired), "stages": dict(stages), "errors": errors,
              "scope": "Only persisted JSON lines; original cart, barcode, trigger slot, final API response, PLC write and real fall evidence."}
    (root / "pipeline-audit.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("run_root", type=Path)
    args = parser.parse_args()
    cases = sorted(path for path in args.run_root.iterdir() if (path / "report.json").is_file())
    if (args.run_root / "report.json").is_file():
        cases = [args.run_root]
    if not cases:
        parser.error("No simulation reports found")
    failed = False
    for path in cases:
        result = audit(path)
        failed |= not result["passed"]
        print(json.dumps({"passed": result["passed"], "scenario": result["scenario"],
                          "original_pairs": result["original_pairs"], "completed": result["completed"],
                          "error_count": len(result["errors"]), "error_examples": result["errors"][:3]}, ensure_ascii=False))
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
