$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$sandbox = Join-Path $repo ('.local/tests/skin-size-' + [Guid]::NewGuid().ToString('N'))
$state = Join-Path $sandbox 'state'
New-Item -ItemType Directory -Path $state -Force | Out-Null
foreach ($name in @('assets','KianaDesktopPet.exe','KianaDesktopPet.exe.config')) {
    Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $sandbox -Recurse
}
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$runner = Join-Path $sandbox 'SkinSizeTests.exe'
$arguments = @('/nologo','/target:exe','/platform:x64','/utf8output',('/out:'+$runner),('/reference:'+(Join-Path $sandbox 'KianaDesktopPet.exe')),('/win32manifest:'+(Join-Path $repo 'source/app.manifest')))
foreach ($library in @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Xaml.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll')) {
    $arguments += '/reference:' + (Join-Path $framework $library)
}
$arguments += Join-Path $PSScriptRoot 'skin-size-tests.cs'
& (Join-Path $framework 'csc.exe') $arguments
if ($LASTEXITCODE -ne 0) { throw 'Skin size tests failed to compile. Build the current app first.' }
$process = Start-Process -FilePath $runner -ArgumentList ('"'+$state+'"') -WindowStyle Hidden -PassThru
if (!$process.WaitForExit(45000)) { throw "Skin size tests timed out. See $state" }
$result = Get-Content -LiteralPath (Join-Path $state 'result.txt') -Raw -Encoding UTF8
if ($process.ExitCode -ne 0) { throw $result }
Write-Output $result
