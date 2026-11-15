# Skin shuffle regression

Run `.\source\build.ps1` and `.\tests\v091\Run-Tests.ps1` in PowerShell 7 on Windows x64 with .NET Framework 4.8.

Tests use a seeded planner and an isolated preview runtime under `.local/tests`. Coverage includes visible/awake elapsed time, hidden and suspension gaps, jitter bounds, candidate normalization, shuffled rounds, manual resets, independent startup selection, today holds across restart and midnight, persisted state, empty/single pools, safe animation boundaries and protected task/interaction states.

Actual WPF tests save custom intervals and candidate selections, switch between real Q-version assets while preserving the music session, pose timestamp, native window bounds and polling intervals, and verify outfit-derived toolbar colors. A missing-asset fixture renames only a copied atlas in the isolated runtime and restores it in `finally` to check rollback. No original assets, running music, GPT or NetEase processes are modified.

The runner prints its report directory with JSON results and a screenshot. Real scheduling over multiple hours and physical suspend/resume should still be checked during normal use; timing and resume transitions are exercised with deterministic inputs in the suite.
