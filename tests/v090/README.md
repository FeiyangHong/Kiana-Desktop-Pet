# Companion workflows regression

Run in PowerShell 7 on Windows x64 with .NET Framework 4.8:

```powershell
.\source\build.ps1
.\tests\v090\Run-Tests.ps1
```

The suite stages an isolated runtime under `.local/tests` and uses synthetic music state. It never launches, restarts or controls GPT or NetEase Cloud Music.

Coverage includes actual-versus-planned music actions, missing and stale progress, manual chorus precedence, scene debounce and restoration, persistent preferences during temporary overlays, manual pause persistence, selective shared imports and concurrent publications, focus pause/resume/checkpoints, external task deduplication and event ordering (including equal-millisecond terminal events), bounded output actions, original performance policies, battery overrides, settings search, theme changes and small-window screenshots.

PowerShell helper tests run a tiny fixture executable with exit codes 0 and 7. They check start/terminal notifications, Unicode and spaced paths, trailing separators, unchanged output and preserved exit codes. No real training, export or download command is executed.

Reports and WPF screenshots stay in the isolated state directory printed by the runner. Cross-device cloud-drive transport, physical battery life and native lock/suspend delivery still require the corresponding hardware/environment; timer transition logic is tested with controlled signals.
