param()
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runtime=Join-Path $repo ('.local/tests/v0816-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
foreach($name in @('KianaDesktopPet.exe','KianaDesktopPet.exe.config','assets')){Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $runtime -Recurse}
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$exe=Join-Path $runtime 'SettingsQuickAccessTests.exe'
$arguments=@('/nologo','/target:exe','/platform:x64','/utf8output',('/out:'+$exe),('/reference:'+(Join-Path $runtime 'KianaDesktopPet.exe')),('/win32manifest:'+(Join-Path $repo 'source/app.manifest')))
foreach($ref in @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Net.Http.dll','System.Xaml.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll')){$arguments+=('/reference:'+(Join-Path $framework $ref))}
$arguments+=(Join-Path $PSScriptRoot 'settings-quick-access-tests.cs')
& (Join-Path $framework 'csc.exe') $arguments
if($LASTEXITCODE -ne 0){throw 'Settings quick-access tests failed to compile'}
$state=Join-Path $runtime 'state'
$process=Start-Process -FilePath $exe -ArgumentList ('"'+$state+'"') -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(20000)){throw "Settings quick-access tests timed out: $runtime"}
if($process.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $state 'error.json');throw "Settings quick-access tests failed: $runtime"}
Get-Content -LiteralPath (Join-Path $state 'result.json')
Write-Output "UI reports: $state"
