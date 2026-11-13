#!/usr/bin/env bash
set -e

# DevStack Studio - Standalone Universal Launcher for macOS and Linux
# Double-clickable on macOS Finder (.command extension) and runnable in Linux/macOS terminal.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ==============================================================================
# Automatic self-elevation: clear macOS Gatekeeper quarantine & ensure executable bits
# ==============================================================================
if [ "$(uname -s)" = "Darwin" ]; then
    # Recursively strip quarantine attribute from bundle directory silently
    if command -v xattr >/dev/null 2>&1; then
        xattr -dr com.apple.quarantine "$SCRIPT_DIR" 2>/dev/null || true
    fi
    # Ensure all binaries and helper scripts in this bundle have execute permissions
    find "$SCRIPT_DIR" -type f \( -name "DevStackStudio.App" -o -name "*.command" -o -name "*.sh" \) -exec chmod +x {} + 2>/dev/null || true
fi

echo "========================================"
echo "DevStack Studio - Universal Launcher"
echo "========================================"

OS="$(uname -s)"
ARCH="$(uname -m)"
RID=""

if [ "$OS" = "Linux" ]; then
    if [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then
        RID="linux-arm64"
    else
        RID="linux-x64"
    fi
elif [ "$OS" = "Darwin" ]; then
    if [ "$ARCH" = "arm64" ]; then
        RID="osx-arm64"
    else
        RID="osx-x64"
    fi
else
    echo "Unsupported operating system: $OS"
    exit 1
fi

echo "Detected Host Platform: $RID ($ARCH)"

# ==============================================================================
# Linux: Self-healing dependency check, group permissions, and service health
# ==============================================================================
if [ "$OS" = "Linux" ]; then
    MISSING_PKGS=()

    # Check for basic X11 and font libraries required by Avalonia UI / .NET
    if command -v ldconfig >/dev/null 2>&1; then
        ldconfig -p 2>/dev/null | grep -q "libX11.so.6" || MISSING_PKGS+=("libx11-6")
        ldconfig -p 2>/dev/null | grep -q "libX11-xcb.so.1" || MISSING_PKGS+=("libx11-xcb1")
        ldconfig -p 2>/dev/null | grep -q "libXrandr.so.2" || MISSING_PKGS+=("libxrandr2")
        ldconfig -p 2>/dev/null | grep -q "libXi.so.6" || MISSING_PKGS+=("libxi6")
        ldconfig -p 2>/dev/null | grep -q "libfontconfig.so.1" || MISSING_PKGS+=("libfontconfig1")
        ldconfig -p 2>/dev/null | grep -q "libICE.so.6" || MISSING_PKGS+=("libice6")
        ldconfig -p 2>/dev/null | grep -q "libSM.so.6" || MISSING_PKGS+=("libsm6")
    fi

    # Check for Docker and Docker Compose
    if ! command -v docker >/dev/null 2>&1; then
        MISSING_PKGS+=("docker.io")
    fi
    if ! docker compose version >/dev/null 2>&1 && ! command -v docker-compose >/dev/null 2>&1; then
        MISSING_PKGS+=("docker-compose-v2")
    fi

    SUDO=""
    if [ "$(id -u)" -ne 0 ]; then
        if command -v sudo >/dev/null 2>&1; then
            SUDO="sudo"
        fi
    fi

    # Auto-install any missing packages
    if [ ${#MISSING_PKGS[@]} -gt 0 ]; then
        echo ""
        echo "--> Missing system prerequisites detected: ${MISSING_PKGS[*]}"
        if command -v apt-get >/dev/null 2>&1; then
            echo "--> Attempting automatic installation via apt-get..."
            if [ -n "$SUDO" ] || [ "$(id -u)" -eq 0 ]; then
                $SUDO apt-get update -qq || true
                $SUDO apt-get install -y "${MISSING_PKGS[@]}" || {
                    echo "Warning: Automated package installation encountered an issue."
                }
            fi
        elif command -v dnf >/dev/null 2>&1; then
            echo "--> Attempting automatic installation via dnf (Fedora/RHEL)..."
            $SUDO dnf install -y libX11 libXrandr libXi fontconfig libICE libSM docker docker-compose-plugin 2>/dev/null || true
        elif command -v pacman >/dev/null 2>&1; then
            echo "--> Attempting automatic installation via pacman (Arch)..."
            $SUDO pacman -Sy --noconfirm libx11 libxrandr libxi fontconfig libice libsm docker docker-compose 2>/dev/null || true
        fi
    fi

    # Auto-fix Docker daemon status if not running
    if command -v systemctl >/dev/null 2>&1 && command -v docker >/dev/null 2>&1; then
        if ! systemctl is-active --quiet docker 2>/dev/null; then
            echo "--> Starting Docker service..."
            $SUDO systemctl enable --now docker 2>/dev/null || true
        fi
    fi

    # Auto-fix Docker permission / group membership for current user
    if command -v docker >/dev/null 2>&1; then
        # Ensure 'docker' group exists
        if ! getent group docker >/dev/null 2>&1; then
            $SUDO groupadd docker 2>/dev/null || true
        fi

        # Check if user can run docker without permission denied
        if ! docker info >/dev/null 2>&1; then
            # If user is not yet in docker group, add them
            if ! id -nG "$USER" 2>/dev/null | grep -qw "docker"; then
                echo "--> Adding user '$USER' to 'docker' group..."
                $SUDO usermod -aG docker "$USER" 2>/dev/null || true
            fi

            # Check if docker socket exists and fix ACL/permissions if needed
            if [ -e "/var/run/docker.sock" ]; then
                $SUDO chgrp docker /var/run/docker.sock 2>/dev/null || true
                $SUDO chmod 660 /var/run/docker.sock 2>/dev/null || true
            fi
        fi
    fi
fi

# ==============================================================================
# macOS: Self-healing Docker check & automated zero-click/unattended installation
# ==============================================================================
if [ "$OS" = "Darwin" ]; then
    # 1. Expand PATH with standard macOS Docker binary locations
    export PATH="/usr/local/bin:/opt/homebrew/bin:/usr/bin:/bin:/usr/sbin:/sbin:$HOME/.docker/bin:/Applications/Docker.app/Contents/Resources/bin:$PATH"

    # 2. Check if Docker Desktop app bundle is installed but symlinks/PATH are missing
    if ! command -v docker >/dev/null 2>&1; then
        if [ -x "/Applications/Docker.app/Contents/Resources/bin/docker" ]; then
            echo "--> Found Docker.app in /Applications. Linking CLI to /usr/local/bin..."
            if [ -w "/usr/local/bin" ]; then
                ln -sf /Applications/Docker.app/Contents/Resources/bin/docker /usr/local/bin/docker 2>/dev/null || true
                ln -sf /Applications/Docker.app/Contents/Resources/bin/docker-compose /usr/local/bin/docker-compose 2>/dev/null || true
            else
                sudo ln -sf /Applications/Docker.app/Contents/Resources/bin/docker /usr/local/bin/docker 2>/dev/null || true
                sudo ln -sf /Applications/Docker.app/Contents/Resources/bin/docker-compose /usr/local/bin/docker-compose 2>/dev/null || true
            fi
        fi
    fi

    # 3. If Docker is still missing, perform automated self-healing installation
    if ! command -v docker >/dev/null 2>&1; then
        echo ""
        echo "=================================================================="
        echo "--> [Prerequisite Check] Docker is not installed on this Mac."
        echo "--> Initiating automated zero-friction self-healing installation..."
        echo "=================================================================="

        INSTALLED=false

        # Strategy A: Use Homebrew if available
        if command -v brew >/dev/null 2>&1; then
            echo "--> Attempting automated installation via Homebrew cask..."
            if brew install --cask docker; then
                INSTALLED=true
            fi
        fi

        # Strategy B: Direct official Apple Silicon / Intel unattended installer
        if [ "$INSTALLED" = false ]; then
            DMG_URL=""
            if [ "$ARCH" = "arm64" ]; then
                echo "--> Downloading Docker Desktop for Apple Silicon (ARM64)..."
                DMG_URL="https://desktop.docker.com/mac/main/arm64/Docker.dmg"
            else
                echo "--> Downloading Docker Desktop for Intel Mac (x86_64)..."
                DMG_URL="https://desktop.docker.com/mac/main/amd64/Docker.dmg"
            fi

            TMP_DMG="/tmp/DockerDesktopInstaller.dmg"
            rm -f "$TMP_DMG"

            if curl -fSL --progress-bar -o "$TMP_DMG" "$DMG_URL"; then
                echo "--> Mounting Docker installer disk image..."
                hdiutil attach "$TMP_DMG" -nobrowse -quiet -mountpoint /Volumes/DockerInstaller || true

                if [ -d "/Volumes/DockerInstaller/Docker.app" ]; then
                    echo "--> Installing Docker Desktop to /Applications (may request Mac administrator password)..."
                    sudo cp -R /Volumes/DockerInstaller/Docker.app /Applications/
                    
                    if [ -x "/Applications/Docker.app/Contents/MacOS/install" ]; then
                        sudo /Applications/Docker.app/Contents/MacOS/install --accept-license --user="$USER" || true
                    fi

                    sudo ln -sf /Applications/Docker.app/Contents/Resources/bin/docker /usr/local/bin/docker 2>/dev/null || true
                    sudo ln -sf /Applications/Docker.app/Contents/Resources/bin/docker-compose /usr/local/bin/docker-compose 2>/dev/null || true
                    INSTALLED=true
                fi

                hdiutil detach /Volumes/DockerInstaller -quiet -force 2>/dev/null || true
                rm -f "$TMP_DMG"
            fi
        fi

        # Verify installation outcome
        if command -v docker >/dev/null 2>&1 || [ -x "/usr/local/bin/docker" ]; then
            echo "--> Docker CLI successfully installed!"
            export PATH="/usr/local/bin:$PATH"
        else
            echo "Warning: Automated Docker installation could not complete automatically."
            echo "Notice: You can install Docker Desktop for Mac directly from: https://docs.docker.com/desktop/setup/install/mac-install/"
        fi
    fi

    # 4. If Docker daemon / Docker.app is not actively running, launch it
    if ! docker info >/dev/null 2>&1; then
        if [ -d "/Applications/Docker.app" ]; then
            echo "--> Starting Docker Desktop background engine..."
            open -a /Applications/Docker.app --hide 2>/dev/null || true
            echo "--> Waiting for Docker daemon to become responsive..."
            for i in {1..30}; do
                if docker info >/dev/null 2>&1; then
                    echo "--> Docker engine is online and ready!"
                    break
                fi
                sleep 2
            done
        fi
    fi
fi

# ==============================================================================
# Locate and launch executable (with instant docker group elevation if needed)
# ==============================================================================
TARGET_DIR="$SCRIPT_DIR/platforms/$RID"
EXE="$TARGET_DIR/DevStackStudio.App"

if [ ! -f "$EXE" ]; then
    ALT_EXE="$SCRIPT_DIR/../artifacts/publish/$RID/DevStackStudio.App"
    if [ -f "$ALT_EXE" ]; then
        EXE="$ALT_EXE"
    fi
fi

if [ ! -f "$EXE" ]; then
    echo "Error: Executable not found at $EXE"
    exit 1
fi

chmod +x "$EXE"
echo "Launching DevStack Studio Control Plane..."

# If on Linux and the current shell process does not yet have the active 'docker' group,
# launch the application seamlessly inside 'sg docker' so it has immediate Docker socket permissions
if [ "$OS" = "Linux" ] && command -v sg >/dev/null 2>&1 && getent group docker >/dev/null 2>&1 && ! id -G | grep -qw "$(getent group docker | cut -d: -f3)"; then
    exec sg docker -c "\"$EXE\" \"$@\""
fi

exec "$EXE" "$@"