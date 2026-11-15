# Settings-open and completed-task outfit handoffs

Run `.\source\build.ps1` followed by `.\tests\v093\Run-Tests.ps1` in PowerShell 7 on Windows x64 with .NET Framework 4.8.

The isolated WPF suite keeps the settings window open, queues the actual button, observes real animation phases and completes the request at a safe idle/review wrap. It checks that scheduled rotation still respects settings, review notifications remain unread and intact, the original animation timestamp and provider polling remain unchanged, and genuinely active or uncertain tasks still block even when the aggregate state is review.

Coverage also includes precise wait reasons, review speeds from 0.1x to 4x, a delayed tick that misses the first frame, quiet-mode sampling aliasing and rendering-budget restoration, and reduced-motion static review. Reports include a screenshot and named checks under `.local/tests`. Only synthetic task state and copied assets are used; no GPT/NetEase process, playback or native notification is manipulated.

The older `v091`/`v092` expectations are updated for this intentional behavior change: review is no longer an active-task blocker, and explicit requests can run with settings open while retaining the animation-boundary requirement.
