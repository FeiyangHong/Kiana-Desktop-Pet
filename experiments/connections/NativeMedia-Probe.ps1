[CmdletBinding()]
param([ValidateRange(1,300)][int]$DurationSeconds=15,[ValidateRange(1,10)][int]$SampleSeconds=1,[string]$OutputPath,[switch]$ReadThumbnail)
$ErrorActionPreference='Stop'
# Read-only alternate music connection. Run with Windows PowerShell 5.1.
# Does not call playback commands, launch applications, or write production state.
if($PSVersionTable.PSEdition -eq 'Core'){throw 'Run this probe with WindowsPowerShell/v1.0/powershell.exe.'}
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$managerType=[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager,Windows.Media.Control,ContentType=WindowsRuntime]
$mediaType=[Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties,Windows.Media.Control,ContentType=WindowsRuntime]
$streamType=[Windows.Storage.Streams.IRandomAccessStreamWithContentType,Windows.Storage.Streams,ContentType=WindowsRuntime]
$randomStreamType=[Windows.Storage.Streams.IRandomAccessStream,Windows.Storage.Streams,ContentType=WindowsRuntime]
$asTask=[System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {$_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -like 'IAsyncOperation*'} | Select-Object -First 1
function Await-WinRT($operation,[Type]$resultType){
 $task=$asTask.MakeGenericMethod($resultType).Invoke($null,@($operation))
 if(-not $task.Wait(4000)){throw 'Windows media request timed out.'}
 return $task.GetAwaiter().GetResult()
}
if(-not $OutputPath){$repo=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent;$OutputPath=Join-Path $repo ('.local/connection-probes/media-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.jsonl')}
$OutputPath=[IO.Path]::GetFullPath($OutputPath)
New-Item -ItemType Directory -Path (Split-Path $OutputPath -Parent) -Force | Out-Null
if(Test-Path -LiteralPath $OutputPath){throw 'Output already exists; choose a fresh file.'}
$utf8=[Text.UTF8Encoding]::new($false)
$manager=Await-WinRT ($managerType::RequestAsync()) $managerType
$clock=[Diagnostics.Stopwatch]::StartNew();$samples=0;$available=0
do {
 $rows=@();$errors=@()
 foreach($session in @($manager.GetSessions())){
  if($session.SourceAppUserModelId -notmatch '(?i)cloudmusic'){continue}
  try{
   $props=Await-WinRT ($session.TryGetMediaPropertiesAsync()) $mediaType
   $play=$session.GetPlaybackInfo();$timeline=$session.GetTimelineProperties();$controls=$play.Controls
   $thumbnailBytes=$null;$thumbnailError=$null
   if($ReadThumbnail -and $props.Thumbnail){try{$stream=Await-WinRT ($props.Thumbnail.OpenReadAsync()) $streamType;try{$thumbnailBytes=$randomStreamType.GetProperty('Size').GetValue($stream,$null)}finally{if([Runtime.InteropServices.Marshal]::IsComObject($stream)){[void][Runtime.InteropServices.Marshal]::ReleaseComObject($stream)}}}catch{$thumbnailError=$_.Exception.Message}}
   $rows+=([ordered]@{source=$session.SourceAppUserModelId;title=$props.Title;artist=$props.Artist;album=$props.AlbumTitle;thumbnailPresent=($null -ne $props.Thumbnail);thumbnailBytes=$thumbnailBytes;thumbnailError=$thumbnailError;playback=$play.PlaybackStatus.ToString();timelineValid=($timeline.EndTime -gt $timeline.StartTime -and $timeline.LastUpdatedTime.Year -gt 1970);positionSeconds=$timeline.Position.TotalSeconds;startSeconds=$timeline.StartTime.TotalSeconds;endSeconds=$timeline.EndTime.TotalSeconds;timelineUpdatedAt=$timeline.LastUpdatedTime.ToString('o');canPlay=$controls.IsPlayEnabled;canPause=$controls.IsPauseEnabled;canNext=$controls.IsNextEnabled;canPrevious=$controls.IsPreviousEnabled})
  }catch{$errors+=$_.Exception.Message}
 }
 $sample=[ordered]@{at=[DateTime]::UtcNow.ToString('o');available=($rows.Count -gt 0);sessions=$rows;errors=$errors}
 [IO.File]::AppendAllText($OutputPath,($sample | ConvertTo-Json -Depth 5 -Compress)+[Environment]::NewLine,$utf8)
 $samples++;if($rows.Count){$available++}
 if($clock.Elapsed.TotalSeconds -lt $DurationSeconds){Start-Sleep -Seconds $SampleSeconds}
}while($clock.Elapsed.TotalSeconds -lt $DurationSeconds)
[ordered]@{probe='windows-media-session';readOnly=$true;samples=$samples;availableSamples=$available;output=$OutputPath} | ConvertTo-Json -Compress
