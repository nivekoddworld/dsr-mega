using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DarkSoulsModLoader.Services;

namespace DarkSoulsModLoader.ViewModels;

public class DebugPageViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;
    public ObservableCollection<string> LogLines { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public DebugPageViewModel(LogService logService)
    {
        _logService = logService;
        _logService.LogAdded += (s, entry) =>
        {
            LogLines.Add(entry.ToString());
        };

        // Populate existing logs
        foreach (var log in _logService.Logs)
        {
            LogLines.Add(log.ToString());
        }
    }

    public void ClearLogs()
    {
        LogLines.Clear();
        _logService.Clear();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
