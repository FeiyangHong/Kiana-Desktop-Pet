[CmdletBinding()]
param(
 [Parameter(Mandatory)][string]$StateRoot,
 [Parameter(Mandatory)][string]$Source,
 [string]$TaskId=([guid]::NewGuid().ToString('N')),
 [Parameter(Mandatory)][string]$Title,
 [Parameter(Mandatory)][string]$FilePath,
 [string[]]$ArgumentList=@(),
 [string]$OpenPath
)
$ErrorActionPreference='Stop'
function Send-Progress([string]$Phase){
 try {
  $parameters=@{StateRoot=$StateRoot;Source=$Source;TaskId=$TaskId;Title=$Title;Status=$Phase}
  if($OpenPath){$parameters.OpenPath=$OpenPath}
  & (Join-Path $PSScriptRoot 'Send-TaskNotice.ps1') @parameters
 } catch {Write-Warning ('Notification was not queued: '+$_.Exception.Message)}
}
Send-Progress 'running'
$taskExit=1
try {
 # Execute only the command explicitly supplied by the caller. Notice contents
 # are never treated as executable commands. Use pwsh -File for a PS script.
 $command=Get-Command -Name $FilePath -CommandType Application -ErrorAction Stop | Select-Object -First 1
 & $command.Source @ArgumentList
 $taskExit=$LASTEXITCODE
} catch {Write-Error $_ -ErrorAction Continue}
if($taskExit -eq 0){Send-Progress 'succeeded'}else{Send-Progress 'failed'}
exit $taskExit
