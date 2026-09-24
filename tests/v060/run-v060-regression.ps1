param([string]$Name,[string]$Class,[string]$Report,[switch]$Ui)
& (Join-Path $PSScriptRoot 'Run-Tests.ps1') -Names $Name
