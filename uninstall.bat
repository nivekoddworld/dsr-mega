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

:: List of files to remove
set "FILES_TO_REMOVE="
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! dinput8.dll"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.Host.dll"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.Host.pdb"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.Rendering.dll"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.Rendering.pdb"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.SDK.dll"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.SDK.pdb"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.Core.dll"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.Core.pdb"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.Host.deps.json"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! DS1Mod.Host.runtimeconfig.json"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! dsmod.log"
set "FILES_TO_REMOVE=!FILES_TO_REMOVE! imgui.ini"

:: Check if any framework files exist
set "FOUND_ANY=0"
for %%F in (!FILES_TO_REMOVE!) do (
    if exist "!DSR_DIR!\%%F" (
        set "FOUND_ANY=1"
    )
)

if exist "!DSR_DIR!\tools\luac50.exe" (
    set "FOUND_ANY=1"
)

if !FOUND_ANY! EQU 0 (
    echo Mod Framework is not installed in:
    echo   !DSR_DIR!
    echo Nothing to remove.
    goto :done
)

echo Found Mod Framework files in:
echo   !DSR_DIR!
echo.
echo Files to remove:
for %%F in (!FILES_TO_REMOVE!) do (
    if exist "!DSR_DIR!\%%F" echo   - %%F
)
if exist "!DSR_DIR!\tools\luac50.exe" (
    echo   - tools\ (folder)
)
echo.
echo Note: The mods\ folder will be preserved.
echo.
set /p "CONFIRM=Remove all Mod Framework files? [Y/N]: "
if /i not "!CONFIRM!"=="Y" (
    echo Aborted.
    exit /b 0
)

set "ERRORS=0"

:: Remove individual files
for %%F in (!FILES_TO_REMOVE!) do (
    if exist "!DSR_DIR!\%%F" (
        del "!DSR_DIR!\%%F" >nul 2>&1
        if errorlevel 1 (
            echo [FAIL] Could not delete %%F
            set /a ERRORS+=1
        ) else (
            echo [OK] Removed %%F
        )
    )
)

:: Remove tools folder and all contents
if exist "!DSR_DIR!\tools" (
    rd /s /q "!DSR_DIR!\tools" >nul 2>&1
    if errorlevel 1 (
        echo [FAIL] Could not remove tools\ folder
        set /a ERRORS+=1
    ) else (
        echo [OK] Removed tools\ folder
    )
)

if !ERRORS! GTR 0 (
    echo.
    echo [FAIL] Some files could not be deleted.
    echo        Try running as Administrator.
    exit /b 1
)

:done
echo.
echo ============================================================
echo  Done.
echo ============================================================
echo.
