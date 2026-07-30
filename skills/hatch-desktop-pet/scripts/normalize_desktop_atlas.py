#!/usr/bin/env python3
"""Normalize a Hatch Pet v2 WebP to the TuantuanDesktopPet cell contract."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import sys
from pathlib import Path

from PIL import Image, ImageChops


ATLAS_SIZE = (1536, 2288)
CELL_SIZE = (192, 208)
USED_COLUMNS = (6, 8, 8, 4, 5, 8, 6, 6, 6, 8, 8)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Clear only TuantuanDesktopPet reserved cells. Required-cell decoded "
            "pixels are verified unchanged."
        )
    )
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--report", type=Path)
    return parser.parse_args()


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def cell_box(row: int, column: int) -> tuple[int, int, int, int]:
    left = column * CELL_SIZE[0]
    top = row * CELL_SIZE[1]
    return left, top, left + CELL_SIZE[0], top + CELL_SIZE[1]


def validate_source(path: Path) -> Image.Image:
    if not path.is_file():
        raise ValueError(f"input atlas does not exist: {path}")
    if path.suffix.lower() != ".webp":
        raise ValueError("input atlas must use the .webp extension")

    with Image.open(path) as opened:
        opened.load()
        if opened.format != "WEBP":
            raise ValueError("input is not a WebP image")
        if opened.size != ATLAS_SIZE:
            raise ValueError(
                f"atlas must be exactly {ATLAS_SIZE[0]}x{ATLAS_SIZE[1]}"
            )
        if "A" not in opened.getbands():
            raise ValueError("atlas must contain an Alpha channel")
        image = opened.convert("RGBA")

    for row, used_count in enumerate(USED_COLUMNS):
        for column in range(used_count):
            alpha = image.getchannel("A").crop(cell_box(row, column))
            if alpha.getextrema()[1] == 0:
                raise ValueError(
                    f"required cell row {row}, column {column} is fully transparent"
                )
    return image


def reserved_cells_with_content(image: Image.Image) -> list[tuple[int, int]]:
    occupied: list[tuple[int, int]] = []
    alpha = image.getchannel("A")
    for row, used_count in enumerate(USED_COLUMNS):
        for column in range(used_count, 8):
            if alpha.crop(cell_box(row, column)).getextrema()[1] != 0:
                occupied.append((row, column))
    return occupied


def verify_used_cells_equal(before: Image.Image, after: Image.Image) -> None:
    for row, used_count in enumerate(USED_COLUMNS):
        for column in range(used_count):
            box = cell_box(row, column)
            if ImageChops.difference(before.crop(box), after.crop(box)).getbbox():
                raise ValueError(
                    f"required cell row {row}, column {column} changed during normalization"
                )


def normalize(args: argparse.Namespace) -> dict[str, object]:
    source = args.input.resolve()
    output = args.output.resolve()
    if source == output:
        raise ValueError("--output must differ from --input")

    image = validate_source(source)
    occupied = reserved_cells_with_content(image)
    output.parent.mkdir(parents=True, exist_ok=True)

    if not occupied:
        shutil.copyfile(source, output)
        action = "copied-compatible-source"
    else:
        blank = Image.new("RGBA", CELL_SIZE, (0, 0, 0, 0))
        for row, used_count in enumerate(USED_COLUMNS):
            for column in range(used_count, 8):
                image.paste(blank, (column * CELL_SIZE[0], row * CELL_SIZE[1]))
        image.save(output, "WEBP", lossless=True, method=6, exact=True)
        action = "cleared-reserved-cells"

    normalized = validate_source(output)
    if reserved_cells_with_content(normalized):
        raise ValueError("normalized atlas still contains nontransparent reserved cells")
    verify_used_cells_equal(validate_source(source), normalized)

    report = {
        "ok": True,
        "action": action,
        "input": str(source),
        "output": str(output),
        "inputSha256": sha256(source),
        "outputSha256": sha256(output),
        "webpBytesPreserved": sha256(source) == sha256(output),
        "usedCellPixelMatch": True,
        "clearedReservedCells": [
            {"row": row, "column": column} for row, column in occupied
        ],
        "usedColumnsByRow": list(USED_COLUMNS),
    }

    report_path = (
        args.report.resolve()
        if args.report
        else output.with_suffix(".compatibility.json")
    )
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    report["report"] = str(report_path)
    return report


def main() -> int:
    try:
        report = normalize(parse_args())
    except (OSError, ValueError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
