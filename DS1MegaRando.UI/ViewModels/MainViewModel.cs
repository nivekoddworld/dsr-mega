using System.ComponentModel;
using System.Runtime.CompilerServices;
using DS1MegaRando.Settings;

namespace DS1MegaRando.UI.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private MegaSettings _megaSettings = new();
    public MegaSettings MegaSettings
    {
        get => _megaSettings;
        set { _megaSettings = value; Notify(); }
    }

    // Flat property accessors for XAML binding convenience
    public GlobalSettings  Global  => _megaSettings.Global;
    public FogGateSettings FogGate => _megaSettings.FogGate;
    public ItemSettings    Items   => _megaSettings.Items;
    public EnemySettings   Enemies => _megaSettings.Enemies;

    public void ApplyFrom(MegaSettings loaded)
    {
        _megaSettings = loaded;
        Notify(nameof(MegaSettings));
        Notify(nameof(Global));
        Notify(nameof(FogGate));
        Notify(nameof(Items));
        Notify(nameof(Enemies));
    }

    public MegaSettings BuildMegaSettings() => _megaSettings.Clone();

    public void NotifySeedChanged() => Notify(nameof(Global));

    protected void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
