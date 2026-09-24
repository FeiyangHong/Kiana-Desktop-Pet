@echo off
setlocal
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Repository-Bootstrap.ps1" -Mode update -KeepOpen
exit /b %ERRORLEVEL%
