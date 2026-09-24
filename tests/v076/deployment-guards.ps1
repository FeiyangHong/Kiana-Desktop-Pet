[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$test=Join-Path ([IO.Path]::GetTempPath()) ('Kiana-Deploy-Tests-'+[guid]::NewGuid().ToString('N'))
$work=Join-Path $test 'repo with spaces'
$state=Join-Path $test 'local state'
New-Item -ItemType Directory -Path (Join-Path $work 'scripts'),$state -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'scripts/Repository-Deploy.ps1') -Destination (Join-Path $work 'scripts')
@'
[IO.File]::WriteAllText((Join-Path $PSScriptRoot '.local/build-reached'),'yes')
throw 'Expected fixture compilation failure'
'@ | Set-Content -LiteralPath (Join-Path $work 'build-release.ps1') -Encoding utf8
'.local/' | Set-Content -LiteralPath (Join-Path $work '.gitignore')
'initial' | Set-Content -LiteralPath (Join-Path $work 'sample.txt')
function Invoke-TestGit([string]$folder,[string[]]$arguments){& git -C $folder @arguments | Out-Null;if($LASTEXITCODE -ne 0){throw ('Fixture Git failed: '+($arguments -join ' '))}}
Invoke-TestGit $work @('init','-b','main')
Invoke-TestGit $work @('add','.')
Invoke-TestGit $work @('-c','user.name=Test','-c','user.email=test@example.invalid','commit','-m','fixture')
$remote=Join-Path $test 'origin.git'
& git init --bare $remote | Out-Null
Invoke-TestGit $work @('remote','add','origin',$remote)
Invoke-TestGit $work @('push','-u','origin','main')
$checks=[Collections.Generic.List[string]]::new()
function Check([bool]$pass,[string]$message){if(-not $pass){throw $message};$checks.Add($message)}
function Run([string]$mode,[string]$root=$state){
 $log=Join-Path $test ('result-'+[guid]::NewGuid().ToString('N')+'.log')
 & (Get-Process -Id $PID).Path -NoLogo -NoProfile -File (Join-Path $work 'scripts/Repository-Deploy.ps1') -Mode $mode -InstallRoot $root -NoZip *> $log
 return @{Code=$LASTEXITCODE;Text=(Get-Content -LiteralPath $log -Raw)}
}
$marker=Join-Path $work '.local/build-reached'
Add-Content -LiteralPath (Join-Path $work 'sample.txt') 'local edit'
$r=Run 'update'
Check ($r.Code -ne 0 -and -not(Test-Path -LiteralPath $marker)) 'Dirty update stops before build'
Check (-not(Test-Path -LiteralPath (Join-Path $state 'exit.request'))) 'Dirty update does not stop the pet'
$r=Run 'build'
Check ($r.Code -ne 0 -and (Test-Path -LiteralPath $marker)) 'Build mode accepts local edits, compiler failure is surfaced'
Check (-not(Test-Path -LiteralPath (Join-Path $state 'exit.request'))) 'Compiler failure does not stop the pet'
Remove-Item -LiteralPath $marker
$r=Run 'build' $work
Check ($r.Code -ne 0 -and -not(Test-Path -LiteralPath $marker)) 'Source directory cannot be an installation target'
$lock=[IO.File]::Open((Join-Path $work '.local/deploy.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try{$r=Run 'build';Check ($r.Code -ne 0 -and -not(Test-Path -LiteralPath $marker)) 'Concurrent deployment is rejected'}finally{$lock.Dispose()}
# A disposable bare repository reproduces two-machine divergence without any network.
Invoke-TestGit $work @('add','sample.txt')
Invoke-TestGit $work @('-c','user.name=Test','-c','user.email=test@example.invalid','commit','-m','local change')
$other=Join-Path $test 'other'
& git clone --branch main $remote $other | Out-Null
'other change' | Set-Content -LiteralPath (Join-Path $other 'sample.txt')
Invoke-TestGit $other @('add','sample.txt')
Invoke-TestGit $other @('-c','user.name=Test','-c','user.email=test@example.invalid','commit','-m','other machine')
Invoke-TestGit $other @('push')
$before=(& git -C $work rev-parse HEAD)
$r=Run 'update'
Check ($r.Code -ne 0 -and -not(Test-Path -LiteralPath $marker)) 'Divergence stops before build'
Check ($before -eq (& git -C $work rev-parse HEAD)) 'Divergence preserves local commit'
Check (-not(Test-Path -LiteralPath (Join-Path $state 'exit.request'))) 'Divergence does not stop the pet'
$checks | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $test 'checks.json') -Encoding utf8
Write-Output ('PASS '+$checks.Count+' deployment guards; reports: '+$test)
