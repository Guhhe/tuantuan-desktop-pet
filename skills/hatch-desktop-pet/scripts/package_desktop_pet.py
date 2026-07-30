#!/usr/bin/env python3
"""Validate and package a Hatch Pet v2 atlas for TuantuanDesktopPet."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import sys
import zipfile
from pathlib import Path

from PIL import Image


ATLAS_SIZE = (1536, 2288)
CELL_SIZE = (192, 208)
USED_COLUMNS = (6, 8, 8, 4, 5, 8, 6, 6, 6, 8, 8)
ID_PATTERN = re.compile(r"^[a-z0-9][a-z0-9._-]{0,63}$")
RESERVED_IDS = {"jindou"}
MAX_WEBP_BYTES = 128 * 1024 * 1024


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Validate a full Hatch Pet v2 atlas and create an importable .ttpet package."
    )
    parser.add_argument("--spritesheet", required=True, type=Path)
    parser.add_argument("--pet-id", required=True)
    parser.add_argument("--display-name", required=True)
    parser.add_argument("--description", default="")
    parser.add_argument("--output-dir", required=True, type=Path)
    parser.add_argument("--force", action="store_true")
    return parser.parse_args()


def fail(message: str) -> "NoReturn":
    raise ValueError(message)


def validate_metadata(args: argparse.Namespace) -> dict[str, object]:
    pet_id = args.pet_id.strip()
    display_name = args.display_name.strip()
    description = args.description.strip()
    if not ID_PATTERN.fullmatch(pet_id):
        fail("pet id must match ^[a-z0-9][a-z0-9._-]{0,63}$")
    if pet_id in RESERVED_IDS:
        fail("pet id 'jindou' is reserved for the embedded Tuantuan pet")
    if not 1 <= len(display_name) <= 64:
        fail("display name must contain 1–64 characters")
    if len(description) > 500:
        fail("description must not exceed 500 characters")
    return {
        "id": pet_id,
        "displayName": display_name,
        "description": description,
        "spriteVersionNumber": 2,
        "spritesheetPath": "spritesheet.webp",
    }


def alpha_extrema(image: Image.Image, box: tuple[int, int, int, int]) -> tuple[int, int]:
    alpha = image.getchannel("A").crop(box)
    return alpha.getextrema()


def validate_atlas(path: Path) -> tuple[str, list[dict[str, object]]]:
    if not path.is_file():
        fail(f"spritesheet does not exist: {path}")
    if path.suffix.lower() != ".webp":
        fail("spritesheet must use the .webp extension")
    if path.stat().st_size > MAX_WEBP_BYTES:
        fail("spritesheet exceeds 128 MiB")

    original_bytes = path.read_bytes()
    digest = hashlib.sha256(original_bytes).hexdigest()
    with Image.open(path) as image:
        image.load()
        if image.format != "WEBP":
            fail("spritesheet is not a WebP image")
        if image.size != ATLAS_SIZE:
            fail(f"atlas must be exactly {ATLAS_SIZE[0]}x{ATLAS_SIZE[1]}")
        if "A" not in image.getbands():
            fail("atlas must contain an Alpha channel")

        rgba = image.convert("RGBA")
        cell_results: list[dict[str, object]] = []
        for row, used_count in enumerate(USED_COLUMNS):
            for column in range(8):
                left = column * CELL_SIZE[0]
                top = row * CELL_SIZE[1]
                box = (left, top, left + CELL_SIZE[0], top + CELL_SIZE[1])
                minimum, maximum = alpha_extrema(rgba, box)
                required = column < used_count
                if required and maximum == 0:
                    fail(f"required cell row {row}, column {column} is fully transparent")
                if not required and maximum != 0:
                    fail(f"reserved cell row {row}, column {column} is not fully transparent")
                cell_results.append(
                    {
                        "row": row,
                        "column": column,
                        "required": required,
                        "alphaMin": minimum,
                        "alphaMax": maximum,
                    }
                )
    return digest, cell_results


def deterministic_zip(
    archive_path: Path, manifest_bytes: bytes, spritesheet_bytes: bytes
) -> None:
    with zipfile.ZipFile(
        archive_path, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9
    ) as archive:
        for name, payload in (
            ("pet.json", manifest_bytes),
            ("spritesheet.webp", spritesheet_bytes),
        ):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o100644 << 16
            archive.writestr(info, payload)


def package(args: argparse.Namespace) -> dict[str, object]:
    manifest = validate_metadata(args)
    source_hash, cells = validate_atlas(args.spritesheet)
    output_root = args.output_dir.resolve()
    package_dir = output_root / str(manifest["id"])
    archive_path = output_root / f"{manifest['id']}.ttpet"
    report_path = output_root / f"{manifest['id']}.validation.json"

    output_root.mkdir(parents=True, exist_ok=True)
    if package_dir.exists():
        if not args.force:
            fail(f"output package directory already exists: {package_dir}")
        shutil.rmtree(package_dir)
    if (archive_path.exists() or report_path.exists()) and not args.force:
        fail("output archive or validation report already exists; pass --force to replace")

    package_dir.mkdir()
    manifest_bytes = (
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n"
    ).encode("utf-8")
    spritesheet_bytes = args.spritesheet.read_bytes()
    (package_dir / "pet.json").write_bytes(manifest_bytes)
    shutil.copyfile(args.spritesheet, package_dir / "spritesheet.webp")
    deterministic_zip(archive_path, manifest_bytes, spritesheet_bytes)

    folder_hash = hashlib.sha256(
        (package_dir / "spritesheet.webp").read_bytes()
    ).hexdigest()
    with zipfile.ZipFile(archive_path, "r") as archive:
        if archive.namelist() != ["pet.json", "spritesheet.webp"]:
            fail("internal error: archive structure is invalid")
        archive_hash = hashlib.sha256(archive.read("spritesheet.webp")).hexdigest()
    if not source_hash == folder_hash == archive_hash:
        fail("WebP bytes changed during packaging")

    report = {
        "valid": True,
        "contract": "TuantuanDesktopPet Hatch Pet v2",
        "atlasWidth": ATLAS_SIZE[0],
        "atlasHeight": ATLAS_SIZE[1],
        "columns": 8,
        "rows": 11,
        "cellWidth": CELL_SIZE[0],
        "cellHeight": CELL_SIZE[1],
        "usedColumnsByRow": list(USED_COLUMNS),
        "webpSha256": source_hash,
        "webpBytesPreserved": True,
        "packageDirectory": str(package_dir),
        "archive": str(archive_path),
        "cells": cells,
    }
    report_path.write_text(
        json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    return report


def main() -> int:
    try:
        report = package(parse_args())
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1
    print(json.dumps({key: report[key] for key in (
        "valid", "webpSha256", "packageDirectory", "archive"
    )}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
