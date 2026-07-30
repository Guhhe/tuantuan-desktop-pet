---
name: hatch-desktop-pet
description: Generate a complete import-ready .ttpet package for TuantuanDesktopPet from user-provided character photos, pet photos, illustrations, mascots, brand cues, or an existing Hatch Pet atlas. Use when the user asks to turn a reference image into a selectable Windows desktop pet, make a TuantuanDesktopPet import, package spritesheet.webp plus pet.json, validate the desktop-pet format, or repair a rejected .ttpet package.
---

# Hatch Desktop Pet

Create a visually faithful, fully validated Hatch Pet v2 atlas, normalize only the desktop-reserved cells when required, then package it. Deliver the two-file folder and the single-file `.ttpet` archive.

## Required workflow

1. Read `references/import-format.md` completely.
2. Inspect every user-provided reference image before generating.
3. Invoke and follow `$hatch-pet` completely. This dependency is mandatory for generation and repair:
   - read its `SKILL.md` and required references;
   - use `$imagegen` for visual generation or editing;
   - produce all 11 standard rows; the desktop app uses rows 5–8 as additional stationary animations;
   - complete deterministic assembly, alpha checks, contact sheets, motion-strip QA, and final atlas validation;
   - do not accept a visually weak atlas merely because it passes structural checks.
4. Choose metadata:
   - infer a short user-facing `displayName` from the request;
   - create a stable lowercase ASCII `id` matching `^[a-z0-9][a-z0-9._-]{0,63}$`;
   - never use the reserved id `jindou`;
   - use a concise Chinese description unless the user requests another language.
5. Run `scripts/normalize_desktop_atlas.py` on the approved WebP. This deterministic step:
   - copies an already compatible source byte-for-byte;
   - otherwise clears only cells reserved by the desktop contract;
   - verifies every required cell's decoded RGBA pixels are unchanged;
   - writes no PNG or frame cache.
6. Run `scripts/package_desktop_pet.py` on the normalized WebP. Treat it as the final structural gate.
7. Report the package paths, compatibility report, and packaged WebP SHA-256. State that `.ttpet` imports through `默认宠物 → 导入新宠物…`.

## Compatibility command

```bash
python scripts/normalize_desktop_atlas.py \
  --input /absolute/path/to/approved/spritesheet-extended.webp \
  --output /absolute/path/to/spritesheet-desktop.webp \
  --report /absolute/path/to/desktop-compatibility.json
```

## Packaging command

Use the workspace-bundled Python/Pillow runtime when available:

```bash
python scripts/package_desktop_pet.py \
  --spritesheet /absolute/path/to/spritesheet-desktop.webp \
  --pet-id my-pet \
  --display-name "我的宠物" \
  --description "宠物说明" \
  --output-dir /absolute/path/to/output
```

The command creates:

```text
<output-dir>/
  <pet-id>/
    pet.json
    spritesheet.webp
  <pet-id>.ttpet
  <pet-id>.validation.json
```

Never mutate the normalized WebP during packaging. The packaged folder copy and archive member must have the exact same SHA-256 as the normalized input.

## Repair mode

For an existing package:

1. Unpack only into a temporary working directory.
2. Reject path traversal, nested package files, duplicate entries, or unexpected files.
3. Validate with the packaging script.
4. If only metadata is invalid, repair metadata and repackage without changing the WebP.
5. If cells, alpha, dimensions, or visual quality are invalid, return to the relevant `$hatch-pet` repair and QA stages before packaging.

## Completion criteria

Do not claim completion unless:

- the final atlas is 1536×2288 WebP with Alpha and an 8×11 grid of 192×208 cells;
- every required cell is nonempty and every reserved cell is fully transparent;
- `pet.json` is valid sprite v2 metadata and names `spritesheet.webp`;
- `.ttpet` contains exactly root-level `pet.json` and `spritesheet.webp`;
- the packaging script exits successfully;
- required-cell decoded pixels match the approved source after compatibility normalization;
- normalized input, folder-copy, and archive-member WebP SHA-256 values are identical;
- the final visual QA required by `$hatch-pet` has passed.
