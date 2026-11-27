$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runtime = Join-Path $repo ('.local/tests/haibara-' + [Guid]::NewGuid().ToString('N'))
$state = Join-Path $runtime 'state'
New-Item -ItemType Directory -Path $state -Force | Out-Null
foreach ($file in @('KianaDesktopPet.exe','KianaDesktopPet.exe.config','assets')) {
    Copy-Item -LiteralPath (Join-Path $repo $file) -Destination $runtime -Recurse
}
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$runner = Join-Path $runtime 'CharacterAnimationTests.exe'
$arguments = @('/nologo','/target:exe','/platform:x64','/utf8output',('/out:' + $runner),('/reference:' + (Join-Path $runtime 'KianaDesktopPet.exe')))
foreach ($library in @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Xaml.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll')) {
    $arguments += '/reference:' + (Join-Path $framework $library)
}
$arguments += Join-Path $PSScriptRoot 'character-animation-tests.cs'
& (Join-Path $framework 'csc.exe') $arguments
if ($LASTEXITCODE -ne 0) { throw 'Character animation tests failed to compile. Build the current application first.' }
& $runner $state
if ($LASTEXITCODE -ne 0) { throw "Character animation integration failed. Isolated data: $state" }
