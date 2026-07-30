# 使用 Codex 制作新宠物

## 适用场景

可以从宠物照片、人物/角色设定、插画、吉祥物或已有 Hatch Pet 图集制作可导入的
`.ttpet`。Codex 只参与素材制作；生成结果可以在没有 Codex 的 Windows 电脑上运行。

## 准备参考素材

建议提供：

- 至少一张正面、清晰、无遮挡的参考图；
- 能看清眼睛、脸型、主要配色和标志性花纹；
- 如身份依赖侧面花纹、尾巴或配饰，再补充相应角度；
- 宠物名称，以及希望保留或避免的特征。

请确认你有权使用和生成所提供的图片。不要上传敏感、私密或未经授权的内容。

## 安装仓库 Skill

安装两个目录：

```text
skills/hatch-pet
skills/hatch-desktop-pet
```

Windows PowerShell：

```powershell
$SkillRoot = Join-Path $env:USERPROFILE ".codex\skills"
New-Item -ItemType Directory -Force $SkillRoot | Out-Null
Copy-Item -Recurse -Force .\skills\hatch-pet (Join-Path $SkillRoot "hatch-pet")
Copy-Item -Recurse -Force .\skills\hatch-desktop-pet (Join-Path $SkillRoot "hatch-desktop-pet")
```

macOS / Linux：

```bash
mkdir -p ~/.codex/skills
cp -R skills/hatch-pet ~/.codex/skills/
cp -R skills/hatch-desktop-pet ~/.codex/skills/
```

重启 Codex 或重新打开任务，使 Skill 出现在可用 Skill 列表中。

## 请求示例

上传参考图后：

```text
$hatch-desktop-pet 根据我上传的猫咪照片，制作名为“土豆”的团团桌宠导入包。
保持浅绿色眼睛、灰棕虎斑、白下巴和白胸口。
```

也可以修复已有包：

```text
$hatch-desktop-pet 检查并修复这个被团团桌宠拒绝的 .ttpet，尽量保持 WebP 不变。
```

## Skill 执行内容

1. 分析参考图并确定稳定的身份特征。
2. 生成完整 8×11 Hatch Pet v2 动画图集。
3. 检查 9 组标准动作和 16 个视线方向。
4. 确定性组装、透明度清理和视觉 QA。
5. 只在需要时清空桌面端保留格，并验证有效格像素不变。
6. 生成 `pet.json`、文件夹形式和 `.ttpet`。
7. 输出 SHA-256 与验证报告。

视觉生成可能需要多次迭代。不要仅因为尺寸校验通过就接受身份漂移、错误方向、裁切、
透明底残留、尺度突变或不连贯动作。

## 输出文件

```text
output/
  tudou-tabby.ttpet
  tudou-tabby.validation.json
  tudou-tabby/
    pet.json
    spritesheet.webp
```

将 `.ttpet` 发送到 Windows 电脑，然后选择：

```text
托盘菜单 → 默认宠物 → 导入新宠物…
```

## 不使用 Codex 时

如果已经有符合规范的 1536×2288 WebP，可以手写 `pet.json`，再使用
`normalize_desktop_atlas.py` 和 `package_desktop_pet.py` 完成兼容化、验证及打包。
完整格式见 [PET_PACKAGE.md](PET_PACKAGE.md)。

## 常见问题

### 保留格不透明

先运行 `normalize_desktop_atlas.py`。它只清空保留格，并验证有效格解码像素不变。

### 某个有效格为空

不能靠复制空白帧绕过。返回 `$hatch-pet` 修复整行动画并重新做视觉 QA。

### 左右方向反了

检查第 1/2 行移动方向以及第 9/10 行 `090`、`270` 主方向。主方向错误属于阻断问题。

### 导入后显示名称不对

修改 `pet.json` 的 `displayName` 后重新打包；无需修改 WebP。
