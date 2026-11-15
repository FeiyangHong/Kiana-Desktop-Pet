# One-shot outfit request regression

Build with `.\source\build.ps1`, then run `.\tests\v092\Run-Tests.ps1` in PowerShell 7 on Windows x64 with .NET Framework 4.8.

The WPF suite stages copied assets and independent state under `.local/tests`. It verifies button availability, single-request coalescing, cancellation, untouched countdown while queuing, independent use with automatic modes disabled or today's hold active, protection during settings/tasks/focus/chorus/interaction, and the separate animation-boundary requirement. Successful changes retain the music timeline and polling intervals. Subsequent manual changes cancel stale requests. Missing assets roll back and consume the one-shot request, which is never persisted for replay.

The fixture never starts or controls GPT or NetEase. Its missing-asset test temporarily renames a copied atlas and restores it in `finally`. The original randomized planner, persistence and animation boundary coverage remains in `tests/v091`.
