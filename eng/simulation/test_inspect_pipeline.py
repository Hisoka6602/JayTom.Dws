"""Check offline correlation through cart reuse, recorder resets and duplicated targets."""

import json
import importlib.util
import tempfile
import unittest
from pathlib import Path

spec = importlib.util.spec_from_file_location("pipeline_inspection", Path(__file__).with_name("inspect-pipeline.py"))
inspection = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inspection)
select = inspection.select


class PipelineInspectionTests(unittest.TestCase):
    def test_cart_filter_recovers_events_that_only_carry_the_package_timestamp(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "Pipeline").mkdir()
            rows = [
                {"runId": "run", "eventSequence": 1, "stage": "package.created", "cartId": 38, "packageTimestamp": 101},
                {"runId": "run", "eventSequence": 2, "stage": "camera.paired", "cartId": 38, "packageTimestamp": 101, "cameraOrdinal": 1},
                {"runId": "run", "eventSequence": 3, "stage": "persistence.package.saved", "packageTimestamp": 101},
                {"runId": "other-run", "eventSequence": 1, "stage": "persistence.package.saved", "packageTimestamp": 101},
            ]
            self.write_rows(root / "Pipeline/log.jsonl", rows)
            self.assertEqual([1, 2, 3], [row["eventSequence"] for row in select(root, cart=38)])

    def test_camera_ordinal_does_not_cross_a_stop_reset_and_duplicate_targets_are_collapsed(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "Pipeline").mkdir()
            (root / "Anomalies").mkdir()
            rows = [
                {"runId": "run", "eventSequence": 1, "stage": "camera.result.ordered", "packageTimestamp": 0, "cameraOrdinal": 1},
                {"runId": "run", "eventSequence": 2, "stage": "camera.paired", "packageTimestamp": 101, "cameraOrdinal": 1},
                {"runId": "run", "eventSequence": 3, "stage": "package.pipeline.reset"},
                {"runId": "run", "eventSequence": 4, "stage": "camera.result.ordered", "packageTimestamp": 0, "cameraOrdinal": 1},
                {"runId": "run", "eventSequence": 5, "stage": "camera.pair.rejected", "packageTimestamp": 102, "cameraOrdinal": 1},
            ]
            self.write_rows(root / "Pipeline/log.jsonl", rows)
            self.write_rows(root / "Anomalies/log.jsonl", [rows[-1], rows[2]])
            self.assertEqual([4, 5], [row["eventSequence"] for row in select(root, timestamp=102)])

    @staticmethod
    def write_rows(path, rows):
        path.write_text("".join(json.dumps(row) + "\n" for row in rows), encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
