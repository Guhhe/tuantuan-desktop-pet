# 团团桌宠宠物包格式

## 包形式

应用接受：

- `<id>.ttpet`：ZIP 格式，推荐；
- `.zip`：与 `.ttpet` 相同；
- 同一目录下的 `pet.json` 与 `spritesheet.webp`。

压缩包必须恰好包含两个根目录文件：

```text
pet.json
spritesheet.webp
```

不允许子目录、缩略图、README、重复文件或其他条目。

## pet.json

```json
{
  "id": "my-pet",
  "displayName": "我的宠物",
  "description": "一句简短说明。",
  "spriteVersionNumber": 2,
  "spritesheetPath": "spritesheet.webp"
}
```

约束：

- `id`：1–64 个 ASCII 字符，匹配 `^[a-z0-9][a-z0-9._-]{0,63}$`；
- `jindou`：内置团团保留 id，不可用于导入；
- `displayName`：1–64 个字符；
- `description`：不超过 500 个字符；
- `spriteVersionNumber`：必须为 `2`；
- `spritesheetPath`：必须为 `spritesheet.webp`。

## spritesheet.webp

- 格式：可解码且带 Alpha 通道的 WebP；
- 尺寸：1536×2288；
- 网格：8 列×11 行；
- 单元格：192×208；
- 各行有效格数：`6, 8, 8, 4, 5, 8, 6, 6, 6, 8, 8`；
- 每个有效格至少包含一个 Alpha > 0 的像素；
- 每行有效格之后的保留格必须全部 Alpha = 0。

## 动画语义

| 行 | 桌面行为 | 有效列 |
| ---: | --- | --- |
| 0 | 待机 / 眨眼 / 呼吸 | 0–5 |
| 1 | 向右移动 | 0–7 |
| 2 | 向左移动 | 0–7 |
| 3 | 挥爪 | 0–3 |
| 4 | 跳跃 | 0–4 |
| 5 | 委屈 / 困倦待机 | 0–7 |
| 6 | 玩爪待机 | 0–5 |
| 7 | 环顾待机 | 0–5 |
| 8 | 好奇待机 | 0–5 |
| 9 | 000°–157.5° | 0–7 |
| 10 | 180°–337.5° | 0–7 |

方向从正上方 `000` 开始，按屏幕坐标顺时针每 22.5° 一格。`090` 为屏幕右侧，
`180` 为下方，`270` 为屏幕左侧。

## 打包与验证

推荐使用仓库 Skill 中的打包器：

```bash
python skills/hatch-desktop-pet/scripts/normalize_desktop_atlas.py \
  --input /path/to/spritesheet-extended.webp \
  --output /path/to/spritesheet-desktop.webp \
  --report /path/to/desktop-compatibility.json

python skills/hatch-desktop-pet/scripts/package_desktop_pet.py \
  --spritesheet /path/to/spritesheet-desktop.webp \
  --pet-id my-pet \
  --display-name "我的宠物" \
  --description "宠物说明" \
  --output-dir /path/to/output
```

兼容转换只清空桌面端保留格；如果输入已经兼容，则直接复制原始 WebP 字节。发生清理时，
脚本会用无损 WebP 输出，并验证所有有效格的解码 RGBA 像素与输入一致。最终打包器还会
确认文件夹副本和压缩包成员的 SHA-256 与兼容化输入完全一致。

## 安全限制

应用拒绝：

- ZIP 路径穿越、目录条目、重复或额外文件；
- 超过大小限制的 JSON/WebP；
- 不安全 id、保留 id、错误版本或路径；
- 无法解码、尺寸错误或缺少 Alpha 的 WebP；
- 空白必需格或非透明保留格。
