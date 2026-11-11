@echo off
setlocal
cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0launcher.ps1" %*
if %ERRORLEVEL% NEQ 0 (
  pause
)