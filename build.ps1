$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$AssetPath = Join-Path $ProjectRoot "assets/tuantuan/spritesheet.webp"
$ManifestPath = Join-Path $ProjectRoot "assets/tuantuan/pet.json"
$ExpectedHash = "C0767ED4B89B19B8B256FFE0FD6B6463E2FE5C3B1C08A40240D96A1E12EE953C"
$ApplicationIcon = Join-Path $ProjectRoot "src/TuantuanDesktopPet/Assets/TuantuanDesktopPet.ico"
$PublishScratch = Join-Path ([System.IO.Path]::GetTempPath()) ("TuantuanDesktopPet-publish-" + [guid]::NewGuid())
$FinalDirectory = Join-Path $ProjectRoot "dist"
$FinalExe = Join-Path $FinalDirectory "团团桌宠.exe"
$ChecksumFile = Join-Path $FinalDirectory "SHA256SUMS.txt"

if ((Get-FileHash -Algorithm SHA256 $AssetPath).Hash -ne $ExpectedHash) {
    throw "团团 spritesheet.webp 的 SHA-256 不符合已批准素材。"
}

$Manifest = Get-Content -Raw -Encoding UTF8 $ManifestPath | ConvertFrom-Json
if (
    $Manifest.id -ne "jindou" -or
    $Manifest.displayName -ne "团团" -or
    $Manifest.spriteVersionNumber -ne 2 -or
    $Manifest.spritesheetPath -ne "spritesheet.webp"
) {
    throw "内置团团 pet.json 不符合预期。"
}

$AllowedImages = @(
    [System.IO.Path]::GetFullPath($AssetPath),
    [System.IO.Path]::GetFullPath($ApplicationIcon)
)
$ForbiddenImages = Get-ChildItem -Path $ProjectRoot -Recurse -File |
    Where-Object {
        $_.FullName -notmatch "[\\/](bin|obj)[\\/]" -and
        $_.Extension -match "^\.(png|jpe?g|gif|bmp|tiff|ico|webp)$" -and
        $_.FullName -notin $AllowedImages
    }
if ($ForbiddenImages) {
    throw "源码目录中出现了禁止新增的图片素材：$($ForbiddenImages.FullName -join ', ')"
}
if (-not (Test-Path $ApplicationIcon)) {
    throw "缺少从团团现有挥爪帧制作的应用图标。"
}

try {
    dotnet test (Join-Path $ProjectRoot "TuantuanDesktopPet.sln") -c Release
    dotnet publish (Join-Path $ProjectRoot "src/TuantuanDesktopPet/TuantuanDesktopPet.csproj") `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:PublishTrimmed=false `
        -o $PublishScratch

    New-Item -ItemType Directory -Force -Path $FinalDirectory | Out-Null
    Copy-Item -Force (Join-Path $PublishScratch "TuantuanDesktopPet.exe") $FinalExe

    if ((Get-FileHash -Algorithm SHA256 $AssetPath).Hash -ne $ExpectedHash) {
        throw "构建后团团素材哈希发生变化。"
    }

    $ReleaseHash = (Get-FileHash -Algorithm SHA256 $FinalExe).Hash.ToLowerInvariant()
    "$ReleaseHash  团团桌宠.exe" |
        Set-Content -Encoding utf8 -NoNewline $ChecksumFile

    Write-Host "构建完成：$FinalExe"
    Write-Host "校验文件：$ChecksumFile"
}
finally {
    if (Test-Path $PublishScratch) {
        Remove-Item -Recurse -Force $PublishScratch
    }
}
