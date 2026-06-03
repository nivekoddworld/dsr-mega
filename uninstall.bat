@echo off
setlocal EnableDelayedExpansion

:: ============================================================
::  DS1 Mega Randomizer — uninstall Mod Framework
::  Removes dinput8.dll from the DSR game folder.
:: ============================================================

echo.
echo ============================================================
echo  DS1 Mega Randomizer — Uninstall Mod Framework
echo ============================================================
echo.

:: ── Find DSR game folder ──────────────────────────────────────────────────
set "DSR_DIR="

:: Try Steam registry (64-bit view)
for /f "tokens=2*" %%A in (
    'reg query "HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570940" /v InstallLocation 2^>nul'
) do set "DSR_DIR=%%B"

:: Try Steam registry (32-bit view as fallback)
if not defined DSR_DIR (
    for /f "tokens=2*" %%A in (
        'reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570940" /v InstallLocation 2^>nul'
    ) do set "DSR_DIR=%%B"
)

:: Fallback: parse settings.json via PowerShell
if not defined DSR_DIR (
    set "SETTINGS=%APPDATA%\DS1MegaRando\settings.json"
    if exist "!SETTINGS!" (
        for /f "usebackq delims=" %%A in (
            `powershell -NoProfile -Command "try { (Get-Content '!SETTINGS!' | ConvertFrom-Json).Global.GameDirectory } catch {}"`
        ) do set "DSR_DIR=%%A"
    )
)

:: ── Validate and remove ───────────────────────────────────────────────────
if not defined DSR_DIR (
    echo Could not auto-detect the DSR game folder.
    echo.
    set /p "DSR_DIR=Enter the path to the DSR game folder: "
)

if not defined DSR_DIR (
    echo Aborted.
    exit /b 1
)

set "TARGET=!DSR_DIR!\dinput8.dll"

if not exist "!TARGET!" (
    echo Mod Framework is not installed in:
    echo   !DSR_DIR!
    echo Nothing to remove.
    goto :done
)

echo Found: !TARGET!
echo.
set /p "CONFIRM=Remove dinput8.dll from the DSR folder? [Y/N]: "
if /i not "!CONFIRM!"=="Y" (
    echo Aborted.
    exit /b 0
)

del "!TARGET!"
if errorlevel 1 (
    echo [FAIL] Could not delete !TARGET!
    echo        Try running as Administrator.
    exit /b 1
) else (
    echo [OK] Removed !TARGET!
)

:done
echo.
echo ============================================================
echo  Done.
echo ============================================================
echo.
