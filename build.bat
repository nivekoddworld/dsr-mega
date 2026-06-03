@echo off
setlocal EnableDelayedExpansion

:: ============================================================
::  DS1 Mega Randomizer — full build script
::  Run from the repo root.  Requires:
::    - .NET 8 SDK
::    - Visual Studio 2022 (for the C++ injector)
:: ============================================================

set "REPO=%~dp0"
set "ERRORS=0"
set "INJECTOR_BUILT=0"

:: ── Locate MSBuild (needed for the C++ injector) ─────────────────────────
set "MSBUILD="
for %%P in (
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
) do (
    if exist "%%~P" ( set "MSBUILD=%%~P" & goto :found_msbuild )
)
:: Fallback: try PATH
where msbuild >nul 2>&1 && set "MSBUILD=msbuild"

:found_msbuild

echo.
echo ============================================================
echo  DS1 Mega Randomizer Build
echo ============================================================
echo.

:: ── 1. Restore NuGet packages ────────────────────────────────────────────
echo [1/5] Restoring packages...
dotnet restore "%REPO%DS1MegaRando.slnx"
if errorlevel 1 ( echo [FAIL] dotnet restore & set /a ERRORS+=1 )

:: ── 2. Build C# solution (randomizer + DS1Mod.Core) ─────────────────────
echo.
echo [2/5] Building C# solution (Release)...
dotnet build "%REPO%DS1MegaRando.slnx" -c Release --no-restore
if errorlevel 1 ( echo [FAIL] dotnet build & set /a ERRORS+=1 )

:: ── 3. Build C++ injector (dinput8.dll) ──────────────────────────────────
echo.
echo [3/5] Building C++ injector (dinput8.dll)...
if defined MSBUILD (
    "%MSBUILD%" "%REPO%DS1Mod\DS1Mod.Injector\DS1Mod.Injector.vcxproj" ^
        /p:Configuration=Release /p:Platform=x64 ^
        /p:SolutionDir="%REPO%" ^
        /v:minimal /nologo
    if errorlevel 1 (
        echo [FAIL] MSBuild injector
        set /a ERRORS+=1
    ) else (
        set "INJECTOR_BUILT=1"
    )
) else (
    echo [SKIP] Visual Studio / MSBuild not found — skipping dinput8.dll build.
    echo        Install VS 2022 with "Desktop development with C++" workload.
)

:: ── 4. Run tests ─────────────────────────────────────────────────────────
echo.
echo [4/5] Running tests...
dotnet test "%REPO%DS1MegaRando.Test\DS1MegaRando.Test.csproj" ^
    -c Release --no-build --nologo --logger "console;verbosity=minimal"
if errorlevel 1 ( echo [FAIL] tests & set /a ERRORS+=1 )

:: ── 5. Deploy dinput8.dll ─────────────────────────────────────────────────
echo.
echo [5/5] Deploying dinput8.dll...
set "DLL_SRC=%REPO%publish\injector\dinput8.dll"

if !INJECTOR_BUILT! EQU 0 (
    echo [SKIP] Injector not built — skipping deploy.
    goto :summary
)

if not exist "!DLL_SRC!" (
    echo [SKIP] !DLL_SRC! not found — skipping deploy.
    goto :summary
)

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

:: ── Copy DLL ──────────────────────────────────────────────────────────────
if defined DSR_DIR (
    if exist "!DSR_DIR!\DarkSoulsRemastered.exe" (
        copy /Y "!DLL_SRC!" "!DSR_DIR!\dinput8.dll" >nul
        if errorlevel 1 (
            echo [FAIL] Could not copy to !DSR_DIR!
            set /a ERRORS+=1
        ) else (
            echo [OK] Deployed to !DSR_DIR!
        )

        :: Also copy alongside the UI exe so "Launch with Mod Framework" can find it
        set "UI_OUT=%REPO%DS1MegaRando.UI\bin\Release\net8.0-windows"
        if exist "!UI_OUT!" (
            copy /Y "!DLL_SRC!" "!UI_OUT!\dinput8.dll" >nul 2>&1
        )
    ) else (
        echo [SKIP] DSR folder not valid ^(DarkSoulsRemastered.exe not found^): !DSR_DIR!
        echo        Copy manually: !DLL_SRC!
    )
) else (
    echo [SKIP] DSR game folder not found.
    echo        Copy manually: publish\injector\dinput8.dll ^> DarkSoulsRemastered.exe folder
)

:summary
:: ── Summary ──────────────────────────────────────────────────────────────
echo.
echo ============================================================
if !ERRORS! EQU 0 (
    echo  BUILD SUCCEEDED
    echo.
    echo  Randomizer : DS1MegaRando.UI\bin\Release\net8.0-windows\
    echo  Injector   : publish\injector\dinput8.dll
) else (
    echo  BUILD FAILED  ^(!ERRORS! step^(s^) failed^)
    exit /b 1
)
echo ============================================================
echo.
