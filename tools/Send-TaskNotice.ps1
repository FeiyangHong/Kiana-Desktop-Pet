[CmdletBinding()]
param(
 [Parameter(Mandatory)][string]$StateRoot,
 [Parameter(Mandatory)][string]$Source,
 [Parameter(Mandatory)][string]$TaskId,
 [Parameter(Mandatory)][string]$Title,
 [Parameter(Mandatory)][ValidateSet('running','succeeded','failed','waiting')][string]$Status,
 [string]$OpenPath
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath($StateRoot)
$exe=Join-Path $root 'app/KianaDesktopPet.exe'
if(-not (Test-Path -LiteralPath $exe -PathType Leaf)){throw "No installed desktop pet at $exe"}
foreach($value in @($Source,$TaskId,$Title,$OpenPath)){if($value -and $value.Contains('"')){throw 'Values cannot contain double quotes.'}}
$arguments=@('--task-notice','--state-dir',$root,'--source',$Source,'--task-id',$TaskId,'--title',$Title,'--status',$Status)
if($OpenPath){$arguments+=@('--open-path',[IO.Path]::GetFullPath($OpenPath))}
# Wait only for the short-lived queue writer, not for the desktop pet or task.
$quoted=$arguments | ForEach-Object {'"'+($_ -replace '(\\+)$','$1$1')+'"'}
$writer=Start-Process -FilePath $exe -ArgumentList $quoted -WindowStyle Hidden -Wait -PassThru
if($writer.ExitCode -ne 0){throw "Task notification rejected (exit $($writer.ExitCode))."}
