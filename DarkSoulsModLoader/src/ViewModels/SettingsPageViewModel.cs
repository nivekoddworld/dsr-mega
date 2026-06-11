using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DarkSoulsModLoader.Services;

namespace DarkSoulsModLoader.ViewModels;

public class SettingsPageViewModel : INotifyPropertyChanged
{
    private readonly ConfigService _configService;
    private readonly GameLaunchService _gameLaunchService;
    private string _gameDirectory = "";
    private string _backupStatus = "No backups yet";
    private bool _canBackup = false;
    private bool _canRestore = false;

    public string GameDirectory
    {
        get => _gameDirectory;
        set => SetProperty(ref _gameDirectory, value);
    }

    public string BackupStatus
    {
        get => _backupStatus;
        set => SetProperty(ref _backupStatus, value);
    }

    public bool CanBackup
    {
        get => _canBackup;
        set => SetProperty(ref _canBackup, value);
    }

    public bool CanRestore
    {
        get => _canRestore;
        set => SetProperty(ref _canRestore, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingsPageViewModel(ConfigService configService, GameLaunchService gameLaunchService)
    {
        _configService = configService;
        _gameLaunchService = gameLaunchService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        GameDirectory = await _configService.GetGameDirectoryAsync() ?? "Not set";
        RefreshBackupStatus();
    }

    public async Task SetGameDirectoryAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        await _configService.SetGameDirectoryAsync(path);
        GameDirectory = path;
        RefreshBackupStatus();
    }

    public async Task BackupGameFilesAsync()
    {
        if (string.IsNullOrEmpty(GameDirectory))
            return;

        try
        {
            BackupStatus = "Creating backup...";

            var backupDir = Path.Combine(GameDirectory, ".backup");
            Directory.CreateDirectory(backupDir);

            var filesToBackup = new[] { "DARKSOULS.exe", "dinput8.dll" };
            foreach (var file in filesToBackup)
            {
                var source = Path.Combine(GameDirectory, file);
                var dest = Path.Combine(backupDir, file);
                if (File.Exists(source))
                    File.Copy(source, dest, true);
            }

            BackupStatus = $"Backup created: {DateTime.Now:yyyy-MM-dd HH:mm}";
            CanRestore = true;
        }
        catch (Exception ex)
        {
            BackupStatus = $"Backup failed: {ex.Message}";
        }
    }

    public async Task RestoreFromBackupAsync()
    {
        if (string.IsNullOrEmpty(GameDirectory))
            return;

        try
        {
            BackupStatus = "Restoring from backup...";

            var backupDir = Path.Combine(GameDirectory, ".backup");
            if (!Directory.Exists(backupDir))
            {
                BackupStatus = "No backup found";
                return;
            }

            var filesToRestore = new[] { "DARKSOULS.exe", "dinput8.dll" };
            foreach (var file in filesToRestore)
            {
                var source = Path.Combine(backupDir, file);
                var dest = Path.Combine(GameDirectory, file);
                if (File.Exists(source))
                    File.Copy(source, dest, true);
            }

            BackupStatus = "Restored successfully";
        }
        catch (Exception ex)
        {
            BackupStatus = $"Restore failed: {ex.Message}";
        }
    }

    public async Task ClearCacheAsync()
    {
        try
        {
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DarkSoulsModLoader");

            if (Directory.Exists(cacheDir))
            {
                var settingsDir = Path.Combine(cacheDir, "settings");
                if (Directory.Exists(settingsDir))
                    Directory.Delete(settingsDir, true);
            }

            BackupStatus = "Cache cleared";
        }
        catch (Exception ex)
        {
            BackupStatus = $"Clear failed: {ex.Message}";
        }
    }

    private void RefreshBackupStatus()
    {
        if (string.IsNullOrEmpty(GameDirectory))
        {
            CanBackup = false;
            CanRestore = false;
            BackupStatus = "Set game directory first";
            return;
        }

        CanBackup = true;
        var backupDir = Path.Combine(GameDirectory, ".backup");
        CanRestore = Directory.Exists(backupDir) && File.Exists(Path.Combine(backupDir, "DARKSOULS.exe"));

        if (CanRestore)
            BackupStatus = "Backup available";
        else
            BackupStatus = "No backup found";
    }

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
