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

:: Output folders
set "PUB=%REPO%publish"
set "PUB_FRAMEWORK=%PUB%\framework"
set "PUB_BUNDLED=%PUB%\bundled-mods"
set "PUB_INJECTOR=%PUB%\injector"
set "PUB_APP=%PUB%\app"
set "UI_OUT=%REPO%DS1MegaRando.UI\bin\Release\net8.0-windows"

:: ── Locate MSBuild (vswhere → PATH fallback) ──────────────────────────────
set "MSBUILD="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
    for /f "usebackq delims=" %%i in (
        `"%VSWHERE%" -latest -property installationPath`
    ) do (
        if exist "%%i\MSBuild\Current\Bin\MSBuild.exe" (
            set "MSBUILD=%%i\MSBuild\Current\Bin\MSBuild.exe"
        )
    )
)
if not defined MSBUILD (
    where msbuild >nul 2>&1
    if not errorlevel 1 set "MSBUILD=msbuild"
)
if defined MSBUILD (
    echo [INFO] MSBuild: !MSBUILD!
) else (
    echo [INFO] MSBuild: not found
)

echo.
echo ============================================================
echo  DS1 Mega Randomizer — Full Build
echo ============================================================
echo.

:: ── 1. Restore packages ───────────────────────────────────────────────────
echo [1/8] Restoring packages...
dotnet restore "%REPO%DS1MegaRando.slnx" --nologo
if errorlevel 1 ( echo [FAIL] dotnet restore & set /a ERRORS+=1 )

:: ── 2. Build C# solution ──────────────────────────────────────────────────
echo.
echo [2/8] Building C# solution (Release)...
dotnet build "%REPO%DS1MegaRando.slnx" -c Release --no-restore --nologo
if errorlevel 1 ( echo [FAIL] dotnet build & set /a ERRORS+=1 )

:: ── 3. Publish DS1Mod.Host → publish\framework\ ───────────────────────────
echo.
echo [3/8] Publishing DS1Mod.Host...
if exist "%PUB_FRAMEWORK%" rd /s /q "%PUB_FRAMEWORK%"
mkdir "%PUB_FRAMEWORK%"
dotnet publish "%REPO%DS1Mod\DS1Mod.Host\DS1Mod.Host.csproj" ^
    -c Release --no-build --nologo ^
    -o "%PUB_FRAMEWORK%"
if errorlevel 1 ( echo [FAIL] dotnet publish DS1Mod.Host & set /a ERRORS+=1 )

:: ── 4. Build C++ injector (dinput8.dll) ───────────────────────────────────
echo.
echo [4/8] Building C++ injector (dinput8.dll)...
if not defined MSBUILD (
    echo [FAIL] MSBuild not found.
    echo        Install Visual Studio 2022 with the "Desktop development with C++" workload,
    echo        then re-run build.bat.
    set /a ERRORS+=1
    goto :summary
)
"%MSBUILD%" "%REPO%DS1Mod\DS1Mod.Injector\DS1Mod.Injector.vcxproj" ^
    /p:Configuration=Release /p:Platform=x64 ^
    /p:SolutionDir="%REPO%" ^
    /v:minimal /nologo
if errorlevel 1 (
    echo [FAIL] MSBuild injector
    set /a ERRORS+=1
    goto :summary
)

:: ── 5. Assemble publish folders ───────────────────────────────────────────
echo.
echo [5/8] Assembling publish folders...

:: Copy dinput8.dll into the framework folder
if exist "%PUB_INJECTOR%\dinput8.dll" (
    copy /Y "%PUB_INJECTOR%\dinput8.dll" "%PUB_FRAMEWORK%\dinput8.dll" >nul
    echo        + dinput8.dll
) else (
    echo [FAIL] dinput8.dll not found at %PUB_INJECTOR%\dinput8.dll
    set /a ERRORS+=1
    goto :summary
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
echo [6/8] Running tests...
dotnet test "%REPO%DS1MegaRando.Test\DS1MegaRando.Test.csproj" ^
    -c Release --no-build --nologo --logger "console;verbosity=minimal"
if errorlevel 1 ( echo [FAIL] tests & set /a ERRORS+=1 )

:: ── 7. Stage UI output (bin\ — for IDE runs / debugging) ──────────────────
echo.
echo [7/8] Staging framework + bundled-mods next to UI exe...

:: Copy framework + bundled-mods next to the UI exe.
:: The UI reads these folders and deploys them — nothing goes to the game dir
:: from here.  All game-dir deployment happens through the UI:
::   framework/     → "Launch with Mod Framework" button
::   bundled-mods/  → Mods tab Install button
if exist "%UI_OUT%" (
    if exist "%UI_OUT%\framework"     rd /s /q "%UI_OUT%\framework"
    if exist "%UI_OUT%\bundled-mods"  rd /s /q "%UI_OUT%\bundled-mods"

    xcopy /E /I /Y /Q "%PUB_FRAMEWORK%" "%UI_OUT%\framework\"    >nul
    xcopy /E /I /Y /Q "%PUB_BUNDLED%"   "%UI_OUT%\bundled-mods\" >nul

    echo        framework\    → %UI_OUT%\framework\
    echo        bundled-mods\ → %UI_OUT%\bundled-mods\
) else (
    echo        [WARN] UI output folder not found ^(build step 2 may have failed^).
)

:: ── 8. Publish complete app → publish\app\ ────────────────────────────────
echo.
echo [8/8] Publishing complete app to publish\app\...
if exist "%PUB_APP%" rd /s /q "%PUB_APP%"
mkdir "%PUB_APP%"
dotnet publish "%REPO%DS1MegaRando.UI\DS1MegaRando.UI.csproj" ^
    -c Release --nologo ^
    -o "%PUB_APP%"
if errorlevel 1 (
    echo [FAIL] dotnet publish UI
    set /a ERRORS+=1
) else (
    :: Drop framework\ and bundled-mods\ into the published app folder
    if exist "%PUB_APP%\framework"     rd /s /q "%PUB_APP%\framework"
    if exist "%PUB_APP%\bundled-mods"  rd /s /q "%PUB_APP%\bundled-mods"
    xcopy /E /I /Y /Q "%PUB_FRAMEWORK%" "%PUB_APP%\framework\"    >nul
    xcopy /E /I /Y /Q "%PUB_BUNDLED%"   "%PUB_APP%\bundled-mods\" >nul
    echo        → %PUB_APP%\
)

:: ── Summary ───────────────────────────────────────────────────────────────
:summary
echo.
echo ============================================================
if !ERRORS! EQU 0 (
    echo  BUILD SUCCEEDED
    echo.
    echo  publish\app\          — complete distributable ^(UI + framework + mods^)
    echo    framework\          — dinput8.dll + DS1Mod.Host/Core/SDK
    echo    bundled-mods\       — DS1Mod.DemoMod.dll
    echo    DS1MegaRando.UI.exe — launch this
    echo.
    echo  Use the UI to deploy to the game:
    echo    "Launch with Mod Framework" — copies framework\ to game dir
    echo    Mods tab Install button     — copies a mod to game dir\mods\
) else (
    echo  BUILD FAILED  ^(!ERRORS! step^(s^) failed^)
    exit /b 1
)
echo ============================================================
echo.
