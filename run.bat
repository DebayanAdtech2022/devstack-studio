:; 2>/dev/null; if [ -n "$BASH_VERSION" ] || [ -n "$ZSH_VERSION" ] || [ -n "$SH_VERSION" ] || [ "$(uname -s 2>/dev/null)" = "Darwin" ] || [ "$(uname -s 2>/dev/null)" = "Linux" ]; then
# ==============================================================================
# [BASH / SH / MACOS / LINUX SECTION]
# ==============================================================================
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# If running on Windows under Git Bash / MSYS / Cygwin, invoke Windows launcher seamlessly
if [ "$(uname -o 2>/dev/null)" = "Msys" ] || [ "$(uname -o 2>/dev/null)" = "Cygwin" ] || [ -n "$WINDIR" ]; then
  if [ -f "$SCRIPT_DIR/scripts/install.bat" ]; then
    cmd.exe /c "$SCRIPT_DIR/scripts/install.bat" "$@"
    exit $?
  elif [ -f "$SCRIPT_DIR/install.bat" ]; then
    cmd.exe /c "$SCRIPT_DIR/install.bat" "$@"
    exit $?
  fi
fi

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
exit 0
fi
@echo off
rem ==============================================================================
rem [WINDOWS BATCH / CMD SECTION]
rem ==============================================================================
setlocal
cd /d "%~dp0"

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
exit /b %ERRORLEVEL%
