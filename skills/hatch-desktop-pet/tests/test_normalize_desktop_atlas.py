import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw


SKILL_DIR = Path(__file__).resolve().parents[1]
SCRIPT = SKILL_DIR / "scripts" / "normalize_desktop_atlas.py"
USED_COLUMNS = (6, 8, 8, 4, 5, 8, 6, 6, 6, 8, 8)
CELL_WIDTH = 192
CELL_HEIGHT = 208


class NormalizeDesktopAtlasTest(unittest.TestCase):
    def test_clears_only_reserved_cells_and_then_preserves_compatible_bytes(self) -> None:
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            source = root / "source.webp"
            normalized = root / "normalized.webp"
            report = root / "report.json"
            copied = root / "copied.webp"
            copied_report = root / "copied-report.json"

            atlas = Image.new("RGBA", (1536, 2288), (0, 0, 0, 0))
            draw = ImageDraw.Draw(atlas)
            for row, used_count in enumerate(USED_COLUMNS):
                for column in range(used_count):
                    left = column * CELL_WIDTH
                    top = row * CELL_HEIGHT
                    draw.rectangle(
                        (left + 8, top + 8, left + 40, top + 40),
                        fill=(20 + row, 40 + column, 90, 255),
                    )

            reserved_left = 6 * CELL_WIDTH
            draw.rectangle(
                (reserved_left + 8, 8, reserved_left + 40, 40),
                fill=(255, 0, 0, 255),
            )
            atlas.save(source, "WEBP", lossless=True, method=6, exact=True)

            subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT),
                    "--input",
                    str(source),
                    "--output",
                    str(normalized),
                    "--report",
                    str(report),
                ],
                check=True,
                capture_output=True,
                text=True,
            )

            result = json.loads(report.read_text("utf-8"))
            self.assertTrue(result["ok"])
            self.assertTrue(result["usedCellPixelMatch"])
            self.assertEqual(
                [{"row": 0, "column": 6}],
                result["clearedReservedCells"],
            )

            before = Image.open(source).convert("RGBA")
            after = Image.open(normalized).convert("RGBA")
            for row, used_count in enumerate(USED_COLUMNS):
                for column in range(used_count):
                    box = (
                        column * CELL_WIDTH,
                        row * CELL_HEIGHT,
                        (column + 1) * CELL_WIDTH,
                        (row + 1) * CELL_HEIGHT,
                    )
                    self.assertIsNone(
                        ImageChops.difference(
                            before.crop(box),
                            after.crop(box),
                        ).getbbox()
                    )

            reserved_alpha = after.getchannel("A").crop(
                (
                    6 * CELL_WIDTH,
                    0,
                    7 * CELL_WIDTH,
                    CELL_HEIGHT,
                )
            )
            self.assertEqual((0, 0), reserved_alpha.getextrema())

            subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT),
                    "--input",
                    str(normalized),
                    "--output",
                    str(copied),
                    "--report",
                    str(copied_report),
                ],
                check=True,
                capture_output=True,
                text=True,
            )
            self.assertEqual(normalized.read_bytes(), copied.read_bytes())
            self.assertTrue(
                json.loads(copied_report.read_text("utf-8"))["webpBytesPreserved"]
            )


if __name__ == "__main__":
    unittest.main()
