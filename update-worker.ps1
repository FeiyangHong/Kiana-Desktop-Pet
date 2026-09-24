[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$JobFile,[switch]$NoDialogs)
$ErrorActionPreference='Stop'
$job=Get-Content -LiteralPath $JobFile -Raw -Encoding UTF8 | ConvertFrom-Json
$root=[IO.Path]::GetFullPath($job.Root).TrimEnd('\')
$app=Join-Path $root 'app'
$stage=[IO.Path]::GetFullPath($job.Stage)
function Assert-Child([string]$target,[string]$parent){$prefix=[IO.Path]::GetFullPath($parent).TrimEnd('\')+'\';if(-not ([IO.Path]::GetFullPath($target)).StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase)){throw '更新路径超出允许范围。'}}
function Verify-Runtime([string]$folder){
 $manifest=Get-Content -LiteralPath (Join-Path $folder 'runtime-manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
 if($manifest.product -ne 'Kiana Desktop Pet' -or $manifest.schema -ne 1){throw '运行清单无效。'}
 foreach($entry in $manifest.files){if($entry.path -match '(^|[\\/])\.\.([\\/]|$)' -or [IO.Path]::IsPathRooted($entry.path) -or $entry.path.Contains(':')){throw '清单路径无效。'};$file=Join-Path $folder $entry.path;Assert-Child $file $folder;if((Get-FileHash -LiteralPath $file).Hash -ine $entry.sha256){throw ('文件校验失败：'+$entry.path)}}
}
function Start-Pet {Start-Process -FilePath (Join-Path $app 'KianaDesktopPet.exe') -ArgumentList ('--state-dir "'+$root+'"') -WorkingDirectory $app -WindowStyle Hidden}
$maintenanceGate=$null
$moved=$false;$activated=$false;$backup=$null;$settingsBackup=$null
try {
 $maintenanceGate=[IO.File]::Open((Join-Path $root 'maintenance.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
 Assert-Child $app $root
 Assert-Child $stage (Join-Path $root 'update-cache')
 Verify-Runtime $stage
 $parent=Get-Process -Id $job.ParentId -ErrorAction SilentlyContinue
 if($parent -and $parent.Path -eq (Join-Path $app 'KianaDesktopPet.exe') -and -not $parent.WaitForExit(20000)){throw '桌宠没有正常退出，更新已取消。'}
 $other=@(Get-CimInstance Win32_Process -Filter "Name='KianaDesktopPet.exe'" | Where-Object {$_.ExecutablePath -eq (Join-Path $app 'KianaDesktopPet.exe')})
 if($other.Count){throw '桌宠仍在运行，未替换程序。'}
 $backup=Join-Path $root ('managed-backups/'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
 Assert-Child $backup (Join-Path $root 'managed-backups')
 New-Item -ItemType Directory -Path $backup -Force | Out-Null
 $settings=Join-Path $root 'settings.json'
 if(Test-Path -LiteralPath $settings){$settingsBackup=Join-Path $backup 'settings.json';Copy-Item -LiteralPath $settings -Destination $settingsBackup}
 if(-not (Test-Path -LiteralPath (Join-Path $app 'runtime-manifest.json'))){
  $files=@(Get-ChildItem -LiteralPath $app -Recurse -File | Where-Object {$_.Name -ne 'runtime-manifest.json'} | ForEach-Object {@{path=$_.FullName.Substring($app.Length+1).Replace('\','/');size=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}})
  $old=Get-Content -LiteralPath (Join-Path $root 'installation.json') -Raw -Encoding UTF8 | ConvertFrom-Json
  @{schema=1;product='Kiana Desktop Pet';version=$old.version;files=$files} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $app 'runtime-manifest.json') -Encoding UTF8
 }
 Verify-Runtime $app
 $backupApp=Join-Path $backup 'app';Assert-Child $backupApp $backup
 Move-Item -LiteralPath $app -Destination $backupApp
 $moved=$true
 Move-Item -LiteralPath $stage -Destination $app
 $activated=$true
 Verify-Runtime $app
 if($job.PreferenceSource){Assert-Child $job.PreferenceSource (Join-Path $root 'managed-backups');$raw=Get-Content -LiteralPath $job.PreferenceSource -Raw -Encoding UTF8;$parsed=$raw | ConvertFrom-Json;if($parsed.Schema -ne 1 -or -not $parsed.Skin){throw '备份偏好无效。'};Copy-Item -LiteralPath $job.PreferenceSource -Destination $settings -Force}
 @{version=$job.Version;installedAt=(Get-Date).ToString('o');appRoot=$app} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'installation.json') -Encoding UTF8
 @{success=$true;version=$job.Version;backup=$backup;at=(Get-Date).ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'update-result.json') -Encoding UTF8
 Start-Pet
} catch {
 $failure=$_.Exception.Message
 if($moved){
  if($activated -and (Test-Path -LiteralPath $app)){$failed=Join-Path $root ('update-cache/failed-'+[guid]::NewGuid().ToString('N'));Assert-Child $failed (Join-Path $root 'update-cache');Assert-Child $app $root;Move-Item -LiteralPath $app -Destination $failed}
  $backupApp=Join-Path $backup 'app';Assert-Child $backupApp (Join-Path $root 'managed-backups');Move-Item -LiteralPath $backupApp -Destination $app
  if($settingsBackup){Copy-Item -LiteralPath $settingsBackup -Destination (Join-Path $root 'settings.json') -Force}
 }
 @{success=$false;error=$failure;at=(Get-Date).ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'update-result.json') -Encoding UTF8
 if(-not $NoDialogs){Add-Type -AssemblyName PresentationFramework
 [System.Windows.MessageBox]::Show(('更新未完成，原程序已保留。'+[Environment]::NewLine+$failure),'琪亚娜桌宠更新') | Out-Null}
 if($moved){Start-Pet}
} finally {if($maintenanceGate){$maintenanceGate.Dispose()}}
