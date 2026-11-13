#!/bin/sh
: << 'EXTRACT_BATCH'
@echo off
setlocal
cd /d "%~dp0"

REM ==============================================================================
REM DevStack Studio - Universal Smart Launcher (Windows Environment)
REM ==============================================================================
if exist "%~dp0scripts\launcher.ps1" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\launcher.ps1" %*
) else if exist "%~dp0launcher.ps1" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0launcher.ps1" %*
) else (
  echo Error: Could not locate launcher.ps1
  pause
)

if %ERRORLEVEL% NEQ 0 (
  pause
)
goto :END_BATCH
EXTRACT_BATCH

# ==============================================================================
# DevStack Studio - Universal Smart Launcher (macOS & Linux Environment)
# ==============================================================================
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

if [ -f "$SCRIPT_DIR/scripts/install.command" ]; then
  chmod +x "$SCRIPT_DIR/scripts/install.command" 2>/dev/null || true
  exec "$SCRIPT_DIR/scripts/install.command" "$@"
elif [ -f "$SCRIPT_DIR/install.command" ]; then
  chmod +x "$SCRIPT_DIR/install.command" 2>/dev/null || true
  exec "$SCRIPT_DIR/install.command" "$@"
else
  echo "Error: Could not locate install.command in $SCRIPT_DIR"
  exit 1
fi

:END_BATCH
