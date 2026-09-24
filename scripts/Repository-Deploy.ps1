[CmdletBinding()]
param([ValidateSet('update','build')][string]$Mode='update',[string]$InstallRoot,[switch]$NoZip)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$local=Join-Path $repo '.local'
$lock=$null;$transcribing=$false;$stopped=$false;$deployed=$false
function Start-InstalledPet {
 Start-Process -FilePath $exe -ArgumentList @('--state-dir',('"'+$InstallRoot+'"')) -WorkingDirectory (Split-Path $exe -Parent) -WindowStyle Hidden
}
Push-Location $repo
try {
 if($PSVersionTable.PSVersion.Major -lt 7){throw '请通过根目录的 CMD 入口运行，或使用 PowerShell 7。'}
 New-Item -ItemType Directory -Path $local -Force | Out-Null
 try{$lock=[IO.File]::Open((Join-Path $local 'deploy.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)}catch{throw '已有构建或更新正在进行，请等待那个窗口完成。'}
 Start-Transcript -LiteralPath (Join-Path $local 'last-deploy.log') -Force | Out-Null
 $transcribing=$true
 $bindingFile=Join-Path $local 'repository-binding.json'
 if(-not $InstallRoot -and (Test-Path -LiteralPath $bindingFile)){$InstallRoot=(Get-Content -LiteralPath $bindingFile -Raw -Encoding utf8 | ConvertFrom-Json).InstallRoot}
 if(-not $InstallRoot){$InstallRoot=Join-Path $repo 'runtime/KianaDesktopPet'}
 $InstallRoot=[IO.Path]::GetFullPath($InstallRoot).TrimEnd('\')
 # Refuse destinations that can overwrite tracked source or recurse into the release stage.
 $inside=$InstallRoot.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase)
 $runtimePrefix=(Join-Path $repo 'runtime')+'\'
 if($InstallRoot -ieq $repo -or $inside -and -not $InstallRoot.StartsWith($runtimePrefix,[StringComparison]::OrdinalIgnoreCase) -or $repo.StartsWith($InstallRoot+'\',[StringComparison]::OrdinalIgnoreCase) -or $InstallRoot.Length -le 3){throw '运行目录不能是磁盘根目录、仓库源码目录或仓库的上级目录。仓库内请使用 runtime 子目录。'}
 $exe=Join-Path $InstallRoot 'app/KianaDesktopPet.exe'
 $git=Get-Command git -ErrorAction Stop
 & $git.Source rev-parse --show-toplevel | Out-Null
 if($LASTEXITCODE -ne 0){throw '当前目录不是 Git 仓库。'}
 if($Mode -eq 'update'){
  $dirty=@(& $git.Source status --porcelain)
  if($LASTEXITCODE -ne 0){throw '无法检查 Git 状态。'}
  if($dirty.Count){throw '存在未提交的修改。请先提交或自行处理；要运行本机修改，请选择“构建并运行本机修改”。未停止桌宠。'}
  & $git.Source pull --ff-only
  if($LASTEXITCODE -ne 0){throw '拉取失败：请检查网络、上游分支或分叉。不会自动覆盖、暂存或合并代码，桌宠继续运行。'}
 }
 & (Join-Path $repo 'build-release.ps1') -NoZip:$NoZip
 $recipe=Get-Content -LiteralPath (Join-Path $repo 'release-files.json') -Raw -Encoding utf8 | ConvertFrom-Json
 $stage=Join-Path $repo ('dist/Kiana-Desktop-Pet-'+$recipe.version+'-Windows')
 & (Join-Path $stage 'install.ps1') -VerifyOnly
 $targets=@(Get-CimInstance Win32_Process -Filter "Name='KianaDesktopPet.exe'" | Where-Object {$_.ExecutablePath -ieq $exe})
 $other=@(Get-CimInstance Win32_Process -Filter "Name='KianaDesktopPet.exe'" | Where-Object {$_.ExecutablePath -ine $exe})
 if($other.Count){throw '另一个目录的桌宠仍在运行。请先退出那一份；不会终止其他安装。'}
 if($targets.Count){
  [IO.File]::WriteAllText((Join-Path $InstallRoot 'exit.request'),'exit')
  foreach($target in $targets){$process=Get-Process -Id $target.ProcessId -ErrorAction SilentlyContinue;if($process -and -not $process.WaitForExit(12000)){throw '桌宠尚未保存并退出，未覆盖程序。请从托盘退出后重试。'}}
  $stopped=$true
 }
 & (Join-Path $stage 'install.ps1') -InstallRoot $InstallRoot -SkipChatGPT
 $binding=[ordered]@{RepositoryRoot=$repo;InstallRoot=$InstallRoot}
 $binding | ConvertTo-Json | Set-Content -LiteralPath $bindingFile -Encoding utf8
 $binding | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $InstallRoot 'repository.json') -Encoding utf8
 $record=[ordered]@{version=$recipe.version;commit=(& $git.Source rev-parse HEAD).Trim();installedAt=(Get-Date).ToString('o');runtime=$InstallRoot;localCodeChanges=(@(& $git.Source status --porcelain).Count -gt 0)}
 $record | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $local 'last-deploy.json') -Encoding utf8
 Start-InstalledPet
 $deployed=$true
 Write-Host ('已部署 '+$recipe.version+'，运行目录：'+$InstallRoot) -ForegroundColor Green
} catch {
 # On failure restart only an intact previous/current runtime. Never launch a partial install.
 if($stopped -and -not $deployed){try{
  $app=Join-Path $InstallRoot 'app'
  $manifest=Get-Content -LiteralPath (Join-Path $app 'runtime-manifest.json') -Raw -Encoding utf8 | ConvertFrom-Json
  foreach($entry in $manifest.files){if((Get-FileHash -LiteralPath (Join-Path $app $entry.path)).Hash -ine $entry.sha256){throw 'Runtime is incomplete'}}
  Start-InstalledPet
  Write-Host '已重新启动校验完整的桌宠。'
 }catch{Write-Warning ('未启动不完整的安装。备份位于 '+(Join-Path $InstallRoot 'managed-backups'))}}
 Write-Host $_.Exception.Message -ForegroundColor Red
 exit 1
} finally {if($transcribing){Stop-Transcript | Out-Null};if($lock){$lock.Dispose()};Pop-Location}
