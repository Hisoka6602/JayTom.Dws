"""Read on-disk pipeline evidence without touching sorting services or databases."""

import argparse
from bisect import bisect_right
import json
import sys
from pathlib import Path


def records(root):
    for folder in ("Pipeline", "Anomalies"):
        for path in sorted((root / folder).glob("*.jsonl*")):
            resolved_path = str(path.resolve())
            with path.open(encoding="utf-8-sig") as stream:
                for number, line in enumerate(stream, 1):
                    if not line.strip():
                        continue
                    try:
                        row = json.loads(line)
                    except json.JSONDecodeError:
                        print(f"Incomplete or invalid JSON: {path}:{number}", file=sys.stderr)
                        continue
                    if isinstance(row, dict):
                        row["evidenceFile"] = resolved_path
                        row["evidenceLine"] = number
                        yield row


def select(root, *, timestamp=None, cart=None, barcode=None, run=None):
    def matches(row):
        return ((run is None or row.get("runId") == run)
                and (timestamp is None or row.get("packageTimestamp") == timestamp)
                and (cart is None or row.get("cartId") == cart or row.get("sequence") == str(cart))
                and (barcode is None or row.get("barcode") == barcode))

    chosen = []
    resets = {}
    for row in records(root):
        if row.get("stage") == "package.pipeline.reset" and row.get("runId") and row.get("eventSequence"):
            resets.setdefault(row["runId"], set()).add(row["eventSequence"])
        if matches(row):
            chosen.append(row)
    resets = {key: sorted(values) for key, values in resets.items()}

    def ordinal_key(row):
        epoch = bisect_right(resets.get(row.get("runId"), []), row.get("eventSequence", 0))
        return row.get("runId"), epoch, row.get("cameraOrdinal")

    packages = {(row.get("runId"), row["packageTimestamp"]) for row in chosen
                if row.get("packageTimestamp", 0) > 0}
    ordinals = {ordinal_key(row) for row in chosen
                if row.get("runId") and row.get("cameraOrdinal", 0) > 0}
    chosen.extend(row for row in records(root)
                  if ((row.get("runId"), row.get("packageTimestamp")) in packages
                      or row.get("packageTimestamp", 0) == 0 and ordinal_key(row) in ordinals))
    unique = {}
    for row in chosen:
        key = (row["runId"], row["eventSequence"]) if row.get("runId") and row.get("eventSequence") else (
            row["evidenceFile"], row["evidenceLine"])
        unique.setdefault(key, row)
    return sorted(unique.values(), key=lambda row: (
        row.get("eventTime", row.get("time", "")), row.get("eventSequence", 0)))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--log-root", required=True, type=Path)
    filters = parser.add_mutually_exclusive_group(required=True)
    filters.add_argument("--package-timestamp", type=int)
    filters.add_argument("--cart-id", type=int)
    filters.add_argument("--barcode")
    parser.add_argument("--run-id")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    if not (args.log_root / "Pipeline").is_dir() and not (args.log_root / "Anomalies").is_dir():
        parser.error("The log root must contain a Pipeline or Anomalies folder.")
    try:
        result = select(args.log_root, timestamp=args.package_timestamp, cart=args.cart_id,
                        barcode=args.barcode, run=args.run_id)
        text = json.dumps(result, ensure_ascii=False, indent=2)
        if args.output:
            args.output.write_text(text, encoding="utf-8")
        else:
            print(text)
        return 0 if result else 1
    except OSError as error:
        print(str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
