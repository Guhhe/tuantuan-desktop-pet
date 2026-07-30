# Tuantuan dynamic desktop-pet import format

## Package forms

The Windows app accepts any of:

- `<id>.ttpet`: a ZIP archive with exactly two root-level entries;
- `.zip`: same structure as `.ttpet`;
- `pet.json`: with sibling `spritesheet.webp`;
- `spritesheet.webp`: with sibling `pet.json`.

The archive entries are exactly:

```text
pet.json
spritesheet.webp
```

No directory, thumbnail, README, metadata sidecar, or extra image belongs inside the archive.

## Manifest

```json
{
  "id": "my-pet",
  "displayName": "我的宠物",
  "description": "一句简短说明。",
  "spriteVersionNumber": 2,
  "spritesheetPath": "spritesheet.webp"
}
```

Rules:

- `id`: 1–64 ASCII characters matching `^[a-z0-9][a-z0-9._-]{0,63}$`.
- `jindou` is reserved for the app's embedded Tuantuan pet.
- `displayName`: 1–64 characters.
- `description`: no more than 500 characters.
- `spriteVersionNumber`: exactly `2`.
- `spritesheetPath`: exactly `spritesheet.webp`.

## Atlas

- Format: WebP decodable to a pixel format with Alpha.
- Canvas: exactly 1536×2288.
- Grid: 8 columns × 11 rows.
- Cell: 192×208.
- Effective cell counts by row: `6, 8, 8, 4, 5, 8, 6, 6, 6, 8, 8`.
- All cells after each row's effective count must have Alpha=0 for every pixel.
- Every effective cell must contain at least one pixel with Alpha>0.

Animation semantics:

| Row | Animation | Effective columns |
|---:|---|---:|
| 0 | idle / blink / breathe | 0–5 |
| 1 | move right | 0–7 |
| 2 | move left | 0–7 |
| 3 | wave | 0–3 |
| 4 | jump | 0–4 |
| 5 | stationary mood / sleepy | 0–7 |
| 6 | stationary paw play | 0–5 |
| 7 | stationary look around | 0–5 |
| 8 | stationary curious pose | 0–5 |
| 9 | look directions A | 0–7 |
| 10 | look directions B | 0–7 |

The full visual and animation requirements remain defined by `$hatch-pet`; this document only describes import compatibility.

## Installation behavior

The app copies the imported WebP bytes unchanged to:

```text
%LOCALAPPDATA%\TuantuanDesktopPet\pets\<id>\spritesheet.webp
```

It decodes frames only in memory. Import rejects malformed archives, unsafe ids, the reserved built-in id, oversize files, invalid dimensions, missing Alpha, blank required cells, and nontransparent reserved cells.
