using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Input.Platform;
using DarkSoulsModLoader.Services;

namespace DarkSoulsModLoader.ViewModels;

public class DebugPageViewModel : INotifyPropertyChanged
{
    private readonly LogService _logService;
    private double _fontSize = 12;

    public ObservableCollection<string> LogLines { get; } = new();

    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

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

    public async Task CopySelected(System.Collections.IList? selectedItems, IClipboard? clipboard)
    {
        if (selectedItems == null || selectedItems.Count == 0 || clipboard == null)
            return;

        var text = string.Join("\n", selectedItems.Cast<string>());
        await clipboard.SetTextAsync(text);
    }

    public async Task CopyAll(IClipboard? clipboard)
    {
        if (LogLines.Count == 0 || clipboard == null)
            return;

        var text = string.Join("\n", LogLines);
        await clipboard.SetTextAsync(text);
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
        return false;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
