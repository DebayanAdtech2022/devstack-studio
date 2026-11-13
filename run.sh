#!/usr/bin/env bash
set -e

# ==============================================================================
# DevStack Studio - Universal Runnable Root Entry Point (Cross-Platform)
# ==============================================================================
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# If running on Windows under bash/Git Bash/MSYS:
if [ "$(uname -o 2>/dev/null)" = "Msys" ] || [ -n "$WINDIR" ]; then
    if [ -f "$SCRIPT_DIR/scripts/install.bat" ]; then
        cmd.exe /c "$SCRIPT_DIR/scripts/install.bat" "$@"
        exit $?
    elif [ -f "$SCRIPT_DIR/install.bat" ]; then
        cmd.exe /c "$SCRIPT_DIR/install.bat" "$@"
        exit $?
    fi
fi

# If on macOS or Linux:
if [ -f "$SCRIPT_DIR/scripts/install.command" ]; then
    chmod +x "$SCRIPT_DIR/scripts/install.command" 2>/dev/null || true
    exec "$SCRIPT_DIR/scripts/install.command" "$@"
elif [ -f "$SCRIPT_DIR/install.command" ]; then
    chmod +x "$SCRIPT_DIR/install.command" 2>/dev/null || true
    exec "$SCRIPT_DIR/install.command" "$@"
else
    echo "Error: DevStack Studio launcher script (install.command) not found in $SCRIPT_DIR"
    exit 1
fi
