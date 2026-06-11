using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DarkSoulsModLoader.Services;

public class GameLaunchService
{
    private readonly string _gameDirectory;
    private readonly string _injectorDllPath;
    private readonly string _modsDirectory;
    private Process? _gameProcess;

    public event EventHandler<string>? StatusChanged;

    public GameLaunchService(string gameDirectory)
    {
        _gameDirectory = gameDirectory;
        _injectorDllPath = Path.Combine(gameDirectory, "dinput8.dll");
        _modsDirectory = Path.Combine(gameDirectory, "mods");
    }

    /// <summary>
    /// Validates the game directory and required files before launch.
    /// </summary>
    public ValidationResult ValidateGameDirectory()
    {
        var errors = new List<string>();

        if (!Directory.Exists(_gameDirectory))
            errors.Add($"Game directory not found: {_gameDirectory}");

        var dsrExePath = Path.Combine(_gameDirectory, "DARKSOULS.exe");
        if (!File.Exists(dsrExePath))
            errors.Add($"DARKSOULS.exe not found at {dsrExePath}");

        if (!IsInjectorPresent)
            errors.Add("Mod injector (dinput8.dll) not found. Mods will not be loaded.");

        if (!Directory.Exists(_modsDirectory))
            errors.Add("Mods directory not found. Create it at: " + _modsDirectory);

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            InjectorMissing = !IsInjectorPresent
        };
    }

    /// <summary>
    /// Launch DSR with mods active. Injects dinput8.dll if present.
    /// </summary>
    public async Task<GameLaunchResult> LaunchGameAsync()
    {
        try
        {
            var validation = ValidateGameDirectory();
            if (!validation.IsValid && validation.Errors.Any(e => e.Contains("DARKSOULS.exe")))
                return GameLaunchResult.Failed(string.Join("\n", validation.Errors));

            OnStatusChanged("Launching Dark Souls Remastered...");

            var dsrExePath = Path.Combine(_gameDirectory, "DARKSOULS.exe");
            var psi = new ProcessStartInfo
            {
                FileName = dsrExePath,
                WorkingDirectory = _gameDirectory,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _gameProcess = Process.Start(psi);
            if (_gameProcess == null)
                return GameLaunchResult.Failed("Failed to start game process");

            OnStatusChanged($"Game launched (PID: {_gameProcess.Id})");

            var exitCode = await Task.Run(() =>
            {
                _gameProcess.WaitForExit();
                return _gameProcess.ExitCode;
            });

            OnStatusChanged($"Game exited with code: {exitCode}");
            return GameLaunchResult.SuccessResult(exitCode);
        }
        catch (Exception ex)
        {
            OnStatusChanged($"Launch failed: {ex.Message}");
            return GameLaunchResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Get the current game process, if running.
    /// </summary>
    public Process? GetGameProcess() => _gameProcess;

    /// <summary>
    /// Check if the game is currently running.
    /// </summary>
    public bool IsGameRunning => _gameProcess?.HasExited == false;

    /// <summary>
    /// Check if the mod injector is present.
    /// </summary>
    public bool IsInjectorPresent => File.Exists(_injectorDllPath);

    private void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, status);
    }

    /// <summary>
    /// Attempt to auto-detect DSR installation directory from Steam.
    /// </summary>
    public static string? FindDarkSoulsRemastered()
    {
        const int AppId = 570940; // DSR App ID on Steam

        var steamPath = FindSteamRoot();
        if (string.IsNullOrEmpty(steamPath))
            return null;

        // Check common Steam library locations
        var libraryPaths = new List<string>
        {
            Path.Combine(steamPath, "steamapps", "common", "DARK SOULS Remastered"),
            Path.Combine(steamPath, "steamapps", "common", "DarkSoulsRemastered"),
        };

        foreach (var path in libraryPaths)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "DARKSOULS.exe")))
                return path;
        }

        return null;
    }

    private static string? FindSteamRoot()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                var steamPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
                if (!string.IsNullOrEmpty(steamPath) && Directory.Exists(steamPath))
                    return steamPath;
            }
            catch { }

            // Fallback to common paths
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam")
            };

            return candidates.FirstOrDefault(Directory.Exists);
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var linuxPaths = new[]
        {
            Path.Combine(home, ".steam", "steam"),
            Path.Combine(home, ".local", "share", "Steam"),
            Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam")
        };

        return linuxPaths.FirstOrDefault(Directory.Exists);
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool InjectorMissing { get; set; }
}

public class GameLaunchResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = "";
    public int? ExitCode { get; set; }

    public static GameLaunchResult SuccessResult(int exitCode) =>
        new() { IsSuccess = true, Message = "Game launched successfully", ExitCode = exitCode };

    public static GameLaunchResult Failed(string message) =>
        new() { IsSuccess = false, Message = message };
}
