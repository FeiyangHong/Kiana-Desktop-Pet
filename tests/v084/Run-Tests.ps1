param()
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runtime=Join-Path ([IO.Path]::GetTempPath()) ('Kiana-Gallery-Tests-'+[guid]::NewGuid().ToString('N'))
$state=Join-Path $runtime 'state'
New-Item -ItemType Directory -Path $state -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'KianaDesktopPet.exe') -Destination $runtime
Copy-Item -LiteralPath (Join-Path $repo 'assets') -Destination $runtime -Recurse
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$arguments=@('/nologo','/target:exe','/platform:x64','/utf8output',('/out:'+(Join-Path $runtime 'PreviewGalleryTests.exe')),('/reference:'+(Join-Path $runtime 'KianaDesktopPet.exe')),('/win32manifest:'+(Join-Path $repo 'source/app.manifest')))
foreach($reference in @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Net.Http.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Xaml.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll')){$arguments+=('/reference:'+(Join-Path $framework $reference))}
$arguments+=(Join-Path $PSScriptRoot 'preview-gallery-tests.cs')
& (Join-Path $framework 'csc.exe') $arguments
if($LASTEXITCODE -ne 0){throw 'Gallery test compilation failed'}
$process=Start-Process -FilePath (Join-Path $runtime 'PreviewGalleryTests.exe') -ArgumentList ('"'+$state+'"') -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(60000)){throw ('Gallery test timed out: '+$state)}
if($process.ExitCode -ne 0){throw ('Gallery test failed: '+$state)}
Write-Output ('PASS preview-gallery-tests; reports: '+$state)
