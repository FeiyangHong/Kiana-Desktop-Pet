[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compiler=Join-Path $framework 'csc.exe'
$output=Join-Path (Split-Path $PSScriptRoot -Parent) 'KianaDesktopPet.exe'
$refs=@('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Net.Http.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Xaml.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll')
$arguments=@('/nologo','/target:winexe','/platform:x64','/optimize+','/utf8output',('/out:'+$output),('/win32manifest:'+(Join-Path $PSScriptRoot 'app.manifest')))
$icon=Join-Path (Split-Path $PSScriptRoot -Parent) 'assets/pet.ico'
if(Test-Path -LiteralPath $icon){$arguments+=('/win32icon:'+$icon)}
foreach($reference in $refs){$arguments+=('/reference:'+(Join-Path $framework $reference))}
$arguments+=@(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object{$_.FullName})
& $compiler $arguments
if($LASTEXITCODE -ne 0){throw 'Desktop pet compilation failed'}
Write-Output "Built: $output"

