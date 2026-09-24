# Keep one pet entry. Archive only known shortcuts owned by this installation.
function Set-KianaShortcuts {
 param([Parameter(Mandatory=$true)][string]$InstallRoot,
 [string]$DesktopFolder=[Environment]::GetFolderPath('Desktop'),
 [string]$ProgramsFolder=[Environment]::GetFolderPath('Programs'),
 [string]$SmoothRoot=(Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Codex/PetTools/KianaSmoothPet'))
 $appRoot=Join-Path ([IO.Path]::GetFullPath($InstallRoot)) 'app'
 $exe=Join-Path $appRoot 'KianaDesktopPet.exe'
 $shell=New-Object -ComObject WScript.Shell
 $archive=Join-Path ([IO.Path]::GetFullPath($InstallRoot)) ('shortcut-backups/single-entry-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
 $records=@()
 foreach($pair in @(@('Desktop',$DesktopFolder),@('Programs',$ProgramsFolder))){
  $folder=[IO.Path]::GetFullPath($pair[1]);$canonical=Join-Path $folder '琪亚娜桌宠.lnk'
  if(Test-Path -LiteralPath $canonical){$existing=$shell.CreateShortcut($canonical);if($existing.TargetPath -ine $exe){throw ('同名入口指向其他程序，未覆盖：'+$canonical)}}
  $link=$shell.CreateShortcut($canonical);$link.TargetPath=$exe;$link.Arguments='--state-dir "'+[IO.Path]::GetFullPath($InstallRoot)+'"';$link.WorkingDirectory=$appRoot;$link.IconLocation=$exe+',0';$link.Description='琪亚娜桌宠 · 换装、互动、音乐与 ChatGPT';$link.Save()
  foreach($name in @('琪亚娜独立桌宠.lnk','网易云音乐 桌宠联动.lnk','ChatGPT 平滑桌宠.lnk','关闭桌宠平滑采样.lnk')){
   $path=Join-Path $folder $name;if(-not(Test-Path -LiteralPath $path -PathType Leaf)){continue}
   $old=$shell.CreateShortcut($path);$owned=$false
   if($name -eq '琪亚娜独立桌宠.lnk'){$owned=$old.TargetPath -ieq $exe}
   if($name -eq '网易云音乐 桌宠联动.lnk'){$owned=$old.TargetPath -ieq $exe -and $old.Arguments -eq '--music-launch'}
   if($name -eq 'ChatGPT 平滑桌宠.lnk'){$script=Join-Path $SmoothRoot 'app/scripts/launch.cmd';$owned=[IO.Path]::GetFileName($old.TargetPath) -ieq 'cmd.exe' -and $old.Arguments.IndexOf($script,[StringComparison]::OrdinalIgnoreCase) -ge 0}
   if($name -eq '关闭桌宠平滑采样.lnk'){$script=Join-Path $SmoothRoot 'app/scripts/restore-sampling.ps1';$owned=[IO.Path]::GetFileName($old.TargetPath) -ieq 'powershell.exe' -and $old.Arguments.IndexOf($script,[StringComparison]::OrdinalIgnoreCase) -ge 0}
   if(-not $owned){Write-Warning ('保留非本安装创建的入口：'+$path);continue}
   $destination=Join-Path (Join-Path $archive $pair[0]) $name
   New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
   # Copy verified bytes first: some Start-menu links carry EFS attributes that
   # make a filesystem move fail even when both directories are writable.
   $bytes=[IO.File]::ReadAllBytes($path);[IO.File]::WriteAllBytes($destination,$bytes)
   if((Get-FileHash -LiteralPath $path).Hash -ne (Get-FileHash -LiteralPath $destination).Hash){throw ('快捷方式备份校验失败：'+$path)}
   $records+=@{original=$path;backup=$destination}
   $records | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $archive 'restore-map.json') -Encoding UTF8
   Remove-Item -LiteralPath $path
  }
 }
 if($records.Count){$records | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $archive 'restore-map.json') -Encoding UTF8;Write-Output ('旧快捷方式已备份：'+$archive)}
 Write-Output '桌面与开始菜单只保留「琪亚娜桌宠」入口。'
}
