[CmdletBinding()]
param([switch]$NoZip)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$recipe = Get-Content -LiteralPath (Join-Path $root 'release-files.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($recipe.schema -ne 1 -or $recipe.version -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Invalid release recipe.'
}
$version = [string]$recipe.version
$declared = (Get-Content -LiteralPath (Join-Path $root 'source\Maintenance.cs') -Raw -Encoding UTF8) -match ('Version="' + [regex]::Escape($version) + '"')
if (-not $declared) { throw 'Release version and source version differ.' }

& (Join-Path $root 'source\build.ps1')
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }

$dist = Join-Path $root 'dist'
$stage = Join-Path $dist ('Kiana-Desktop-Pet-' + $version + '-Windows')
$distFull = [IO.Path]::GetFullPath($dist).TrimEnd('\')
$stageFull = [IO.Path]::GetFullPath($stage)
if ((Split-Path $stageFull -Parent) -ne $distFull) { throw 'Unexpected staging path.' }
New-Item -ItemType Directory -Path $dist -Force | Out-Null
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

$prefix = [IO.Path]::GetFullPath($root).TrimEnd('\') + '\'
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$entries = @()
foreach ($relative in $recipe.files) {
    $relative = [string]$relative
    if ([IO.Path]::IsPathRooted($relative) -or $relative -match '(^|[\\/])\.\.([\\/]|$)' -or -not $seen.Add($relative)) {
        throw "Invalid or duplicate release path: $relative"
    }
    $source = [IO.Path]::GetFullPath((Join-Path $root $relative))
    if (-not $source.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Missing release file: $relative"
    }
    $destination = Join-Path $stage $relative
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination
    $item = Get-Item -LiteralPath $destination
    $entries += [ordered]@{ path = $relative.Replace('\','/'); size = $item.Length; sha256 = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant() }
}

$manifest = [ordered]@{ schema = 1; product = 'Kiana Desktop Pet'; version = $version; platform = 'Windows 10/11 x64, .NET Framework 4.8'; files = $entries }
$json = $manifest | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText((Join-Path $stage 'package-manifest.json'), $json + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
& (Join-Path $stage 'install.ps1') -VerifyOnly

if (-not $NoZip) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = Join-Path $dist ('Kiana-Desktop-Pet-' + $version + '-Windows.zip')
    if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
    [IO.Compression.ZipFile]::CreateFromDirectory($stage, $archive, [IO.Compression.CompressionLevel]::Optimal, $false, [Text.Encoding]::UTF8)
    Write-Output "Release ZIP: $archive"
}
Write-Output "Install from: $stage"
