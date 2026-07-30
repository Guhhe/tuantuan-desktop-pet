# 发布流程

## 发布前

1. 更新项目版本与 `CHANGELOG.md`。
2. 在 Windows x64 上运行：

   ```powershell
   .\build.ps1
   .\windows-smoke-test.ps1 -ExePath .\dist\团团桌宠.exe
   ```

3. 人工检查透明背景、托盘图标、导入、待机轮播、点击、拖动方向、多屏/DPI 和全屏隐藏。
4. 确认工作区无 `bin/`、`obj/`、个人设置或未经授权素材。

## 创建 Release

```bash
git switch main
git pull --ff-only
git tag v1.2.1
git push origin v1.2.1
```

`.github/workflows/release.yml` 会在 Windows runner 上重新测试和构建，并创建包含以下附件
的 GitHub Release：

```text
团团桌宠.exe
SHA256SUMS.txt
```

不要把自包含 EXE 直接提交到 Git 历史；它超过 GitHub 普通 Git 的 100 MB 单文件限制。

## 发布后

- 从 Release 页面下载并核对 SHA-256；
- 在未安装 .NET 的 Windows 10/11 x64 账户上运行；
- 确认 README 的 latest 下载链接有效；
- 将验证结果补充到 Release notes。
