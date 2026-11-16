@echo off
setlocal
cd /d "%~dp0"

echo ========================================
echo DevStack Studio - Windows Launcher
echo ========================================

rem Check if DevStackStudio.App.exe exists right beside this script
set "EXE=%~dp0DevStackStudio.App.exe"

rem Universal package layout check
if not exist "%EXE%" (
    rem Detect architecture
    if /i "%PROCESSOR_ARCHITECTURE%"=="ARM64" (
        set "RID=win-arm64"
    ) else (
        set "RID=win-x64"
    )
    if exist "%~dp0platforms\%RID%\DevStackStudio.App.exe" (
        set "EXE=%~dp0platforms\%RID%\DevStackStudio.App.exe"
    ) else if exist "%~dp0..\artifacts\publish\%RID%\DevStackStudio.App.exe" (
        set "EXE=%~dp0..\artifacts\publish\%RID%\DevStackStudio.App.exe"
    )
)

if not exist "%EXE%" (
    echo Error: Could not locate DevStackStudio.App.exe
    pause
    exit /b 1
)

rem Prerequisite check: Docker
where docker >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [Notice] Docker is not detected on your PATH.
    echo Docker Desktop can be installed from: https://docs.docker.com/desktop/setup/install/windows-install/
    echo Proceeding to launch DevStack Studio Control Plane...
    echo.
)

echo Launching DevStack Studio Control Plane...
start "" "%EXE%" %*
exit /b 0


echo Launching DevStack Studio Control Plane...
start "" "%EXE%" %*
exit /b 0