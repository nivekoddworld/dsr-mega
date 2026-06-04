@echo off
setlocal EnableDelayedExpansion

:: ============================================================
::  DS1 Mega Randomizer — full build script
::  Run from the repo root.  Requires:
::    - .NET 8 SDK
::    - Visual Studio 2022 with "Desktop development with C++"
:: ============================================================

set "REPO=%~dp0"
set "ERRORS=0"
set "INJECTOR_BUILT=0"

:: Output folders
set "PUB=%REPO%publish"
set "PUB_FRAMEWORK=%PUB%\framework"
set "PUB_BUNDLED=%PUB%\bundled-mods"
set "PUB_INJECTOR=%PUB%\injector"
set "UI_OUT=%REPO%DS1MegaRando.UI\bin\Release\net8.0-windows"

:: ── Locate MSBuild ────────────────────────────────────────────────────────
set "MSBUILD="
for %%P in (
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
) do (
    if exist "%%~P" ( set "MSBUILD=%%~P" & goto :found_msbuild )
)
where msbuild >nul 2>&1 && set "MSBUILD=msbuild"
:found_msbuild

echo.
echo ============================================================
echo  DS1 Mega Randomizer — Full Build
echo ============================================================
echo.

:: ── 1. Restore packages ───────────────────────────────────────────────────
echo [1/7] Restoring packages...
dotnet restore "%REPO%DS1MegaRando.slnx" --nologo
if errorlevel 1 ( echo [FAIL] dotnet restore & set /a ERRORS+=1 )

:: ── 2. Build C# solution ──────────────────────────────────────────────────
echo.
echo [2/7] Building C# solution (Release)...
dotnet build "%REPO%DS1MegaRando.slnx" -c Release --no-restore --nologo
if errorlevel 1 ( echo [FAIL] dotnet build & set /a ERRORS+=1 )

:: ── 3. Publish DS1Mod.Host → publish\framework\ ───────────────────────────
echo.
echo [3/7] Publishing DS1Mod.Host...
if exist "%PUB_FRAMEWORK%" rd /s /q "%PUB_FRAMEWORK%"
mkdir "%PUB_FRAMEWORK%"
dotnet publish "%REPO%DS1Mod\DS1Mod.Host\DS1Mod.Host.csproj" ^
    -c Release --no-build --nologo ^
    -o "%PUB_FRAMEWORK%"
if errorlevel 1 ( echo [FAIL] dotnet publish DS1Mod.Host & set /a ERRORS+=1 )

:: ── 4. Build C++ injector (dinput8.dll) ───────────────────────────────────
echo.
echo [4/7] Building C++ injector (dinput8.dll)...
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
    echo [SKIP] Visual Studio / MSBuild not found — dinput8.dll not built.
    echo        Install VS 2022 with "Desktop development with C++" workload.
)

:: ── 5. Assemble publish folders ───────────────────────────────────────────
echo.
echo [5/7] Assembling publish folders...

:: Add dinput8.dll to the framework folder
if !INJECTOR_BUILT! EQU 1 (
    if exist "%PUB_INJECTOR%\dinput8.dll" (
        copy /Y "%PUB_INJECTOR%\dinput8.dll" "%PUB_FRAMEWORK%\dinput8.dll" >nul
        echo        + dinput8.dll
    )
)

:: Collect bundled mods
if exist "%PUB_BUNDLED%" rd /s /q "%PUB_BUNDLED%"
mkdir "%PUB_BUNDLED%"

set "DEMO_DLL=%REPO%DS1Mod\DS1Mod.DemoMod\bin\Release\net8.0-windows\DS1Mod.DemoMod.dll"
if exist "!DEMO_DLL!" (
    copy /Y "!DEMO_DLL!" "%PUB_BUNDLED%\DS1Mod.DemoMod.dll" >nul
    echo        + DS1Mod.DemoMod.dll
) else (
    echo        [WARN] DS1Mod.DemoMod.dll not found at expected path — skipping.
)

:: ── 6. Run tests ──────────────────────────────────────────────────────────
echo.
echo [6/7] Running tests...
dotnet test "%REPO%DS1MegaRando.Test\DS1MegaRando.Test.csproj" ^
    -c Release --no-build --nologo --logger "console;verbosity=minimal"
if errorlevel 1 ( echo [FAIL] tests & set /a ERRORS+=1 )

:: ── 7. Deploy ─────────────────────────────────────────────────────────────
echo.
echo [7/7] Deploying...

:: Copy framework + bundled-mods next to the UI exe (so it can deploy them)
if exist "%UI_OUT%" (
    :: framework/
    if exist "%UI_OUT%\framework" rd /s /q "%UI_OUT%\framework"
    xcopy /E /I /Y /Q "%PUB_FRAMEWORK%" "%UI_OUT%\framework\" >nul
    echo        Copied framework\ to UI output

    :: bundled-mods/
    if exist "%UI_OUT%\bundled-mods" rd /s /q "%UI_OUT%\bundled-mods"
    xcopy /E /I /Y /Q "%PUB_BUNDLED%" "%UI_OUT%\bundled-mods\" >nul
    echo        Copied bundled-mods\ to UI output
) else (
    echo        [WARN] UI output folder not found — skipping copy to UI output.
    echo               Build the C# solution first.
)

:: Auto-deploy framework to DSR game folder
set "DSR_DIR="

for /f "tokens=2*" %%A in (
    'reg query "HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570940" /v InstallLocation 2^>nul'
) do set "DSR_DIR=%%B"

if not defined DSR_DIR (
    for /f "tokens=2*" %%A in (
        'reg query "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 570940" /v InstallLocation 2^>nul'
    ) do set "DSR_DIR=%%B"
)

if not defined DSR_DIR (
    set "SETTINGS=%APPDATA%\DS1MegaRando\settings.json"
    if exist "!SETTINGS!" (
        for /f "usebackq delims=" %%A in (
            `powershell -NoProfile -Command "try { (Get-Content '!SETTINGS!' | ConvertFrom-Json).Global.GameDirectory } catch {}"`
        ) do set "DSR_DIR=%%A"
    )
)

if defined DSR_DIR (
    if exist "!DSR_DIR!\DarkSoulsRemastered.exe" (
        :: Deploy full framework to game dir
        xcopy /E /I /Y /Q "%PUB_FRAMEWORK%" "!DSR_DIR!\" >nul
        echo        Deployed framework to !DSR_DIR!
    ) else (
        echo        [SKIP] DSR folder invalid ^(no DarkSoulsRemastered.exe^): !DSR_DIR!
    )
) else (
    echo        [SKIP] DSR game folder not found — deploy manually from publish\framework\
)

:: ── Summary ───────────────────────────────────────────────────────────────
echo.
echo ============================================================
if !ERRORS! EQU 0 (
    echo  BUILD SUCCEEDED
    echo.
    echo  Framework   : publish\framework\
    echo    dinput8.dll, DS1Mod.Host.dll, DS1Mod.Core.dll, DS1Mod.SDK.dll, ...
    echo.
    echo  Bundled mods: publish\bundled-mods\
    echo    DS1Mod.DemoMod.dll
    echo.
    echo  UI output   : DS1MegaRando.UI\bin\Release\net8.0-windows\
    echo    framework\ and bundled-mods\ copied alongside the UI exe
) else (
    echo  BUILD FAILED  ^(!ERRORS! step^(s^) failed^)
    exit /b 1
)
echo ============================================================
echo.
