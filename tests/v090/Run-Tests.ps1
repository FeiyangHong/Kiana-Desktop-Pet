param()
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runtime=Join-Path $repo ('.local/tests/v090-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runtime -Force | Out-Null
foreach($file in @('KianaDesktopPet.exe','KianaDesktopPet.exe.config','assets')){Copy-Item -LiteralPath (Join-Path $repo $file) -Destination $runtime -Recurse}
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$exe=Join-Path $runtime 'CompanionTests.exe'
$arguments=@('/nologo','/target:exe','/platform:x64','/utf8output',('/out:'+$exe),('/reference:'+(Join-Path $runtime 'KianaDesktopPet.exe')),('/win32manifest:'+(Join-Path $repo 'source/app.manifest')))
foreach($ref in @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Net.Http.dll','System.Xaml.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll')){$arguments+=('/reference:'+(Join-Path $framework $ref))}
$arguments+=(Join-Path $PSScriptRoot 'companion-tests.cs')
& (Join-Path $framework 'csc.exe') $arguments
if($LASTEXITCODE -ne 0){throw 'Companion tests failed to compile'}
$state=Join-Path $runtime 'state'
$process=Start-Process -FilePath $exe -ArgumentList ('"'+$state+'"') -WindowStyle Hidden -PassThru
if(-not $process.WaitForExit(45000)){throw "Companion tests timed out: $runtime"}
if($process.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $state 'error.json');throw "Companion tests failed: $runtime"}
$result=Get-Content -LiteralPath (Join-Path $state 'result.json') -Raw | ConvertFrom-Json
Write-Output ("PASS companion-tests: "+$result.checks+" checks")
$senderState=Join-Path $runtime 'sender fixture'
$senderApp=Join-Path $senderState 'app'
New-Item -ItemType Directory -Path $senderApp -Force | Out-Null
foreach($file in @('KianaDesktopPet.exe','KianaDesktopPet.exe.config')){Copy-Item -LiteralPath (Join-Path $runtime $file) -Destination $senderApp}
& (Join-Path $repo 'tools/Send-TaskNotice.ps1') -StateRoot $senderState -Source '训练 测试' -TaskId 'quoted-path' -Title '训练与导出 测试完成' -Status succeeded -OpenPath ($senderState+'\')
$event=Get-ChildItem -LiteralPath (Join-Path $senderState 'task-inbox') -Filter '*.json' | Select-Object -First 1
$data=Get-Content -LiteralPath $event.FullName -Raw | ConvertFrom-Json
if($data.Title -ne '训练与导出 测试完成' -or $data.OpenPath -ne ($senderState+'\')){throw 'Sender Unicode/space/trailing-separator roundtrip failed'}
$fixtureSource=Join-Path $runtime 'exit-fixture.cs'
Set-Content -LiteralPath $fixtureSource -Value 'class ExitFixture {static int Main(string[] args){System.Console.WriteLine("fixture output");return int.Parse(args[0]);}}' -Encoding utf8
$fixture=Join-Path $runtime 'exit-fixture.exe'
& (Join-Path $framework 'csc.exe') /nologo /target:exe ('/out:'+$fixture) $fixtureSource
if($LASTEXITCODE -ne 0){throw 'Exit fixture compilation failed'}
foreach($code in @(0,7)){
 & (Get-Command pwsh).Source -NoProfile -File (Join-Path $repo 'tools/Invoke-TaskWithNotice.ps1') -StateRoot $senderState -Source 'Fixture' -TaskId ('exit-'+$code) -Title 'Task fixture' -FilePath $fixture -ArgumentList $code
 if($LASTEXITCODE -ne $code){throw 'Task wrapper did not preserve the command exit code'}
 $events=@(Get-ChildItem -LiteralPath (Join-Path $senderState 'task-inbox') -Filter '*.json' | ForEach-Object {Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json} | Where-Object {$_.TaskId -eq ('exit-'+$code)})
 $terminal=if($code -eq 0){'succeeded'}else{'failed'}
 if($events.Count -ne 2 -or 'running' -notin $events.Status -or $terminal -notin $events.Status){throw 'Task wrapper status events missing'}
}
Write-Output 'PASS task helpers: Unicode paths, running/success/failure events and original exit codes'
Write-Output "Reports: $state"
