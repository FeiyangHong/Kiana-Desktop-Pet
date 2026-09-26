param([string[]]$Names=@('reliability-tests','reliability-ui-tests','notification-center-tests','settings-sidebar-tests','music-reaction-tests'))
$ErrorActionPreference='Stop'
$bundle=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$testRoot=Join-Path ([IO.Path]::GetTempPath()) ('KianaPet-Tests-'+[guid]::NewGuid().ToString('N'))
$framework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$classes=@{'reliability-tests'='ReliabilityTests';'reliability-ui-tests'='ReliabilityUiTests';'notification-center-tests'='NotificationCenterTests';'settings-sidebar-tests'='SettingsSidebarTests';'music-reaction-tests'='MusicReactionTests'}
foreach($name in $Names){
 if(-not $classes.ContainsKey($name)){throw ('Unknown suite: '+$name)}
 $runtime=Join-Path $testRoot $name
 New-Item -ItemType Directory -Path $runtime -Force | Out-Null
 Copy-Item -LiteralPath (Join-Path $bundle 'KianaDesktopPet.exe') -Destination $runtime
 if($true){Copy-Item -LiteralPath (Join-Path $bundle 'assets') -Destination $runtime -Recurse}
 $executable=Join-Path $runtime ($classes[$name]+'.exe')
 $arguments=@('/nologo','/target:exe','/platform:x64','/utf8output',('/out:'+$executable),('/reference:'+(Join-Path $runtime 'KianaDesktopPet.exe')),('/win32manifest:'+(Join-Path $bundle 'source/app.manifest')))
 foreach($reference in @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Net.Http.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Xaml.dll','WPF/WindowsBase.dll','WPF/PresentationCore.dll','WPF/PresentationFramework.dll')){$arguments+=('/reference:'+(Join-Path $framework $reference))}
 $arguments+=(Join-Path $PSScriptRoot ($name+'.cs'))
 & (Join-Path $framework 'csc.exe') $arguments
 if($LASTEXITCODE -ne 0){throw ('Compilation failed: '+$name)}
 $state=Join-Path $runtime 'state';New-Item -ItemType Directory -Path $state -Force | Out-Null
 $process=Start-Process -FilePath $executable -ArgumentList ('"'+$state+'"') -WindowStyle Hidden -PassThru
 if(-not $process.WaitForExit(40000)){throw ('Timed out: '+$name+'; test folder: '+$testRoot)}
 if($process.ExitCode -ne 0){throw ('Failed: '+$name+'; reports: '+$state)}
 Write-Output ('PASS '+$name)
}
Write-Output ('Isolated reports: '+$testRoot)
