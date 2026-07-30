# 参与贡献

感谢你帮助完善团团桌宠。

## 开始之前

- Bug 请附 Windows 版本、应用版本、复现步骤和预期/实际行为。
- 导入问题请附 `pet.json`、验证错误文本和图集 SHA-256；如果素材不便公开，不要上传原图。
- 大型功能建议先提交 Issue，避免实现方向冲突。
- 请勿提交未经授权的角色、商标或宠物素材。

## 本地开发

需要 Windows 10/11 x64 与 .NET 10 SDK：

```powershell
dotnet restore
dotnet test .\TuantuanDesktopPet.sln -c Release
dotnet build .\src\TuantuanDesktopPet\TuantuanDesktopPet.csproj -c Release -r win-x64
```

生成发布文件：

```powershell
.\build.ps1
```

## Pull Request

1. 从 `main` 创建主题分支。
2. 保持改动聚焦，并为核心逻辑补充测试。
3. 不要提交 `bin/`、`obj/`、`dist/*.exe`、个人设置或导入宠物。
4. 确保 `dotnet test` 和 `build.ps1` 通过。
5. 更新 README 或 CHANGELOG（如果用户可见行为发生变化）。
6. 在 PR 中说明 Windows 实机验收结果；无法实测时明确说明。

## 代码与素材许可

代码贡献默认按仓库 MIT License 提交。宠物和其他美术素材不自动适用 MIT；提交素材时
必须明确你拥有相应授权，并说明许可范围。
