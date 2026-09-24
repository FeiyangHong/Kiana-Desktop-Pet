[CmdletBinding()]
param([string]$InstallRoot=(Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Codex/PetTools/KianaDesktopPet'),[switch]$NoShortcuts,[switch]$SkipChatGPT,[switch]$VerifyOnly)
$ErrorActionPreference='Stop'
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'package-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if($manifest.product -ne 'Kiana Desktop Pet' -or $manifest.schema -ne 1){throw '安装清单无效。'}
$prefix=[IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')+'\'
foreach($entry in $manifest.files){
 if([IO.Path]::IsPathRooted($entry.path) -or $entry.path -match '(^|[\\/])\.\.([\\/]|$)'){throw '清单含无效路径。'}
 $file=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot $entry.path))
 if(-not $file.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -or -not(Test-Path -LiteralPath $file) -or (Get-FileHash -LiteralPath $file).Hash -ine $entry.sha256){throw "文件缺失或校验失败：$($entry.path)"}
}
if($VerifyOnly){Write-Output 'Desktop package integrity OK';return}
$release=(Get-ItemProperty -LiteralPath 'HKLM:/SOFTWARE/Microsoft/NET Framework Setup/NDP/v4/Full' -ErrorAction SilentlyContinue).Release
if(-not $release -or $release -lt 528040){throw '本程序需要 .NET Framework 4.8。请先通过 Windows 更新安装对应运行组件。'}
$appRoot=Join-Path $InstallRoot 'app'
$running=@(Get-CimInstance Win32_Process -Filter "Name='KianaDesktopPet.exe'" -ErrorAction SilentlyContinue | Where-Object {$_.ExecutablePath -eq (Join-Path $appRoot 'KianaDesktopPet.exe')})
if($running.Count){throw '独立桌宠正在运行。请先从它的右键菜单或托盘退出，再运行安装器。'}
$entries=@($manifest.files | Where-Object {$_.path -match '^(assets/|licenses/|docs/demo/|KianaDesktopPet\.exe(?:\.config)?$|README\.md$|CHANGELOG\.md$|ASSET_NOTICE\.md$|LICENSE$|update-worker\.ps1$)'})
$changed=$false
foreach($entry in $entries){$dest=Join-Path $appRoot $entry.path;if(Test-Path -LiteralPath $dest){if((Get-FileHash -LiteralPath $dest).Hash -ine $entry.sha256){$changed=$true;break}}}
if($changed){
 $backup=Join-Path $InstallRoot ('managed-backups/'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
 New-Item -ItemType Directory -Path $backup -Force | Out-Null
 $backupApp=Join-Path $backup 'app'
 Copy-Item -LiteralPath $appRoot -Destination $backupApp -Recurse
 $prefs=Join-Path $InstallRoot 'settings.json';if(Test-Path -LiteralPath $prefs){Copy-Item -LiteralPath $prefs -Destination $backup}
 $oldVersion='0.0.0';$installation=Join-Path $InstallRoot 'installation.json';if(Test-Path -LiteralPath $installation){$oldVersion=(Get-Content -LiteralPath $installation -Raw -Encoding UTF8 | ConvertFrom-Json).version}
 $oldFiles=@(Get-ChildItem -LiteralPath $backupApp -Recurse -File | Where-Object {$_.Name -ne 'runtime-manifest.json'} | ForEach-Object {@{path=$_.FullName.Substring($backupApp.Length+1).Replace('\','/');size=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}})
 @{schema=1;product='Kiana Desktop Pet';version=$oldVersion;files=$oldFiles} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backupApp 'runtime-manifest.json') -Encoding UTF8
 Write-Output "已备份旧版程序和偏好：$backup"
}
foreach($entry in $entries){$dest=Join-Path $appRoot $entry.path;New-Item -ItemType Directory -Path (Split-Path $dest -Parent) -Force | Out-Null;if((Test-Path -LiteralPath $dest) -and (Get-FileHash -LiteralPath $dest).Hash -ieq $entry.sha256){continue};Copy-Item -LiteralPath (Join-Path $PSScriptRoot $entry.path) -Destination $dest -Force}
@{schema=1;product='Kiana Desktop Pet';version=$manifest.version;files=$entries} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $appRoot 'runtime-manifest.json') -Encoding UTF8
if(-not $SkipChatGPT){
 $smoothRoot=Join-Path (Split-Path ([IO.Path]::GetFullPath($InstallRoot).TrimEnd('\')) -Parent) 'KianaSmoothPet'
 $smooth=Join-Path $smoothRoot 'app/scripts/launch.cmd'
 if(Test-Path -LiteralPath $smooth){Write-Output '已发现现有 ChatGPT 平滑联动组件。'}
 elseif(Get-AppxPackage -Name OpenAI.Codex -ErrorAction SilentlyContinue){
  try{$support=Join-Path $InstallRoot ('setup-cache/'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'));Expand-Archive -LiteralPath (Join-Path $PSScriptRoot 'ChatGPT平滑联动组件.zip') -DestinationPath $support; & (Join-Path $support 'install.ps1') -InstallRoot $smoothRoot -NoShortcuts}catch{Write-Warning ('独立桌宠已安装，但 ChatGPT 联动组件未安装成功：'+$_.Exception.Message)}
 }else{Write-Output '未发现对应 ChatGPT 商店应用，独立桌宠仍可使用。以后可单独安装包内联动组件。'}
}
if(-not $NoShortcuts){. (Join-Path $PSScriptRoot 'shortcuts.ps1');Set-KianaShortcuts -InstallRoot $InstallRoot}
@{version=$manifest.version;installedAt=(Get-Date).ToString('o');appRoot=$appRoot} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $InstallRoot 'installation.json') -Encoding UTF8
Write-Output '安装完成！请从桌面双击「琪亚娜桌宠」。双击托盘图标可找回；全局快捷键默认关闭，可在右键菜单的「设置 → 快捷键」中自定义。'
