# Codex Skills

- `hatch-desktop-pet`：为团团桌宠生成、兼容化、验证并封装 `.ttpet`。
- `hatch-pet`：生成和视觉检查完整 Hatch Pet v2 动画图集。

安装和使用步骤见 [制作新宠物](../docs/CREATE_PET_WITH_CODEX.md)。

`hatch-pet` 使用其目录中的 Apache License 2.0。`hatch-desktop-pet` 作为本项目的一部分
按仓库 MIT License 分发。Codex 的系统级图片生成 Skill 不复制到本仓库；运行生成流程
时由 Codex 提供。

在普通 Python 环境运行 Skill 脚本或测试时，先安装：

```bash
python -m pip install -r skills/requirements.txt
```
