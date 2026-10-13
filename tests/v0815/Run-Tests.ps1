param()
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runtime=Join-Path $repo ('.local/tests/v0815-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
foreach($file in @('KianaDesktopPet.exe','KianaDesktopPet.exe.config','KianaHookReceiver.exe','KianaHookReceiver.exe.config')){Copy-Item -LiteralPath (Join-Path $repo $file) -Destination $runtime}
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$executable=Join-Path $runtime 'ConnectionTests.exe'
$arguments=@('/nologo','/target:exe','/platform:x64','/utf8output',('/out:'+$executable),('/reference:'+(Join-Path $runtime 'KianaDesktopPet.exe')))
foreach($ref in @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Xaml.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll')){$arguments+=('/reference:'+(Join-Path $framework $ref))}
$arguments+=(Join-Path $PSScriptRoot 'connection-tests.cs')
& (Join-Path $framework 'csc.exe') $arguments
if($LASTEXITCODE -ne 0){throw 'Connection tests failed to compile'}
$state=Join-Path $runtime 'state'
$process=Start-Process -FilePath $executable -ArgumentList ('"'+$state+'"') -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(30000)){throw "Connection tests timed out: $runtime"}
if($process.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $state 'error.txt');throw "Connection tests failed: $runtime"}
Get-Content -LiteralPath (Join-Path $state 'result.txt')
Write-Output "Isolated reports: $state"
Copy-Item -LiteralPath (Join-Path $repo 'assets') -Destination $runtime -Recurse
$ui=Join-Path $runtime 'ConnectionUiTests.exe'
$uiArguments=@('/nologo','/target:exe','/platform:x64','/utf8output',('/out:'+$ui),('/reference:'+(Join-Path $runtime 'KianaDesktopPet.exe')),('/win32manifest:'+(Join-Path $repo 'source/app.manifest')))
foreach($ref in @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Net.Http.dll','System.Xaml.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll')){$uiArguments+=('/reference:'+(Join-Path $framework $ref))}
$uiArguments+=(Join-Path $PSScriptRoot 'connection-ui-tests.cs')
& (Join-Path $framework 'csc.exe') $uiArguments
if($LASTEXITCODE -ne 0){throw 'Connection UI tests failed to compile'}
$uiState=Join-Path $runtime 'ui-state'
$process=Start-Process -FilePath $ui -ArgumentList ('"'+$uiState+'"') -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(15000)){throw "Connection UI tests timed out: $runtime"}
if($process.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $uiState 'ui-error.json');throw "Connection UI tests failed: $runtime"}
Get-Content -LiteralPath (Join-Path $uiState 'ui-result.json')
Write-Output "UI reports: $uiState"
