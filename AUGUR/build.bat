@echo off
REM AUGUR Trade Intelligence - Build Wrapper
REM Launches build.ps1 via PowerShell for cmd.exe environments.
powershell.exe -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
