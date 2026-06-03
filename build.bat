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
echo [1/4] Restoring packages...
dotnet restore "%REPO%DS1MegaRando.slnx"
if errorlevel 1 ( echo [FAIL] dotnet restore & set /a ERRORS+=1 )

:: ── 2. Build C# solution (randomizer + DS1Mod.Core) ─────────────────────
echo.
echo [2/4] Building C# solution (Release)...
dotnet build "%REPO%DS1MegaRando.slnx" -c Release --no-restore
if errorlevel 1 ( echo [FAIL] dotnet build & set /a ERRORS+=1 )

:: ── 3. Build C++ injector (dinput8.dll) ──────────────────────────────────
echo.
echo [3/4] Building C++ injector (dinput8.dll)...
if defined MSBUILD (
    "%MSBUILD%" "%REPO%DS1Mod\DS1Mod.Injector\DS1Mod.Injector.vcxproj" ^
        /p:Configuration=Release /p:Platform=x64 ^
        /p:SolutionDir="%REPO%" ^
        /v:minimal /nologo
    if errorlevel 1 ( echo [FAIL] MSBuild injector & set /a ERRORS+=1 )
) else (
    echo [SKIP] Visual Studio / MSBuild not found — skipping dinput8.dll build.
    echo        Install VS 2022 with "Desktop development with C++" workload.
)

:: ── 4. Run tests ─────────────────────────────────────────────────────────
echo.
echo [4/4] Running tests...
dotnet test "%REPO%DS1MegaRando.Test\DS1MegaRando.Test.csproj" ^
    -c Release --no-build --nologo --logger "console;verbosity=minimal"
if errorlevel 1 ( echo [FAIL] tests & set /a ERRORS+=1 )

:: ── Summary ──────────────────────────────────────────────────────────────
echo.
echo ============================================================
if !ERRORS! EQU 0 (
    echo  BUILD SUCCEEDED
    echo.
    echo  Randomizer : DS1MegaRando.UI\bin\Release\net8.0-windows\
    echo  Injector   : publish\injector\dinput8.dll
    echo               ^(copy to DarkSoulsRemastered.exe folder^)
) else (
    echo  BUILD FAILED  ^(!ERRORS! step^(s^) failed^)
    exit /b 1
)
echo ============================================================
echo.
