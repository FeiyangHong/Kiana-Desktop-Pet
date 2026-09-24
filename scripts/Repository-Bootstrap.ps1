[CmdletBinding()]
param([ValidateSet('update','build')][string]$Mode='update',[string]$InstallRoot,[switch]$KeepOpen)
$ErrorActionPreference='Stop'
$result=1
try {
 $candidates=@()
 $command=Get-Command pwsh.exe -ErrorAction SilentlyContinue
 if($command){$candidates+=$command.Source}
 $candidates+=(Join-Path $env:ProgramFiles 'PowerShell/7/pwsh.exe')
 $cache=Join-Path ([Environment]::GetFolderPath('UserProfile')) '.cache/codex-runtimes'
 if(Test-Path -LiteralPath $cache){foreach($dir in Get-ChildItem -LiteralPath $cache -Directory){$candidates+=(Join-Path $dir.FullName 'dependencies/native/powershell/pwsh.exe')}}
 $pwsh=$candidates | Where-Object {Test-Path -LiteralPath $_ -PathType Leaf} | Select-Object -First 1
 if(-not $pwsh){throw 'PowerShell 7 is required. Install it, then retry.'}
 $arguments=@('-NoLogo','-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'Repository-Deploy.ps1'),'-Mode',$Mode)
 if($InstallRoot){$arguments+=@('-InstallRoot',$InstallRoot)}
 & $pwsh @arguments
 $result=$LASTEXITCODE
} catch {Write-Host $_.Exception.Message -ForegroundColor Red}
if($KeepOpen){Write-Host ('Exit code: '+$result);Read-Host 'Press Enter to close' | Out-Null}
exit $result
