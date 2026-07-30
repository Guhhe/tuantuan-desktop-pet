# 本地发布目录

运行根目录的 `build.ps1` 后会在这里生成：

```text
团团桌宠.exe
SHA256SUMS.txt
```

自包含 EXE 超过 GitHub 普通 Git 的 100 MB 单文件限制，因此 `dist/*.exe` 不进入 Git
历史。正式二进制由 GitHub Actions 构建并作为 GitHub Release 附件发布。
