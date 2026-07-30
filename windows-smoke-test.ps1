param(
    [Parameter(Mandatory = $false)]
    [string]$ExePath = (
        Join-Path $PSScriptRoot "dist/团团桌宠.exe"
    )
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExePath = (Resolve-Path $ExePath).Path
if (-not [Environment]::Is64BitOperatingSystem) {
    throw "团团桌宠只支持 Windows x64。"
}

$First = Start-Process -FilePath $ExePath -PassThru
Start-Sleep -Seconds 4
if ($First.HasExited) {
    throw "团团桌宠启动后提前退出，退出码：$($First.ExitCode)"
}

$Second = Start-Process -FilePath $ExePath -PassThru
if (-not $Second.WaitForExit(5000)) {
    throw "第二次启动没有按单实例规则退出。"
}

$Running = @(Get-Process -Name "TuantuanDesktopPet" -ErrorAction SilentlyContinue)
if ($Running.Count -ne 1) {
    throw "单实例检查失败，当前进程数：$($Running.Count)"
}

$SettingsDirectory = Join-Path $env:LOCALAPPDATA "TuantuanDesktopPet"
$SettingsPath = Join-Path $SettingsDirectory "settings.json"
if (-not (Test-Path $SettingsPath)) {
    throw "首次运行后没有生成 settings.json。"
}

$PetsDirectory = Join-Path $SettingsDirectory "pets"
$ForbiddenImages = Get-ChildItem -Path $SettingsDirectory -Recurse -File |
    Where-Object {
        $_.Extension -match "^\.(png|jpe?g|gif|bmp|tiff|ico|webp)$" -and
        -not (
            $_.Extension -eq ".webp" -and
            $_.Name -eq "spritesheet.webp" -and
            $_.Directory.Parent.FullName -eq $PetsDirectory
        )
    }
if ($ForbiddenImages) {
    throw "设置目录出现了禁止的图片缓存：$($ForbiddenImages.FullName -join ', ')"
}

$Settings = Get-Content -Raw $SettingsPath | ConvertFrom-Json
foreach ($RequiredSetting in @("selectedPetId", "scale", "mouseFollowEnabled", "walkingEnabled")) {
    if ($null -eq $Settings.PSObject.Properties[$RequiredSetting]) {
        throw "settings.json 缺少新设置项：$RequiredSetting"
    }
}

$RunValue = Get-ItemPropertyValue `
    -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" `
    -Name "TuantuanDesktopPet"
$ExpectedRunValue = '"' + $ExePath + '"'
if ($RunValue -ne $ExpectedRunValue) {
    throw "开机启动路径不正确。实际：$RunValue"
}

Write-Host "自动检查通过：启动、单实例、新设置项、无衍生图片缓存、开机启动。"
Write-Host "请继续人工检查：应用图标、默认宠物切换、鼠标跟随/走动开关、尺寸滑杆与输入、"
Write-Host "待机/点击动作、拖动左右动画、透明区穿透、多屏/DPI、全屏隐藏与恢复。"
