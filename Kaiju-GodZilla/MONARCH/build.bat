@echo off
:: ============================================================
::  MONARCH Intelligence Report System - Build Script
::
::  NOTE: build.ps1 is the canonical build tool. Use it.
::  This batch file is a thin wrapper that launches build.ps1
::  from environments where PowerShell is not the default shell.
::
::  Source  : src\monarch.py
::  Output  : %USERPROFILE%\Documents\NinjaTrader 8\MONARCH\MONARCH.exe
:: ============================================================

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
