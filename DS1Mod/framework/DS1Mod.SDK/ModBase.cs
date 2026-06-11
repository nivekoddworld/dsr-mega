using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DS1Mod.Core;

namespace DS1Mod.SDK;

/// <summary>
/// Base class for DS1 mods. Inherit this instead of implementing
/// <see cref="IGameMod"/> directly — all lifecycle methods are virtual
/// no-ops so you only override what you need.
///
/// To patch game files before any map loads, also implement
/// <see cref="IGamePatcher"/> and override <see cref="Patch"/>:
///
///   public class MyMod : ModBase, IGamePatcher {
///       public override string Name    => "My Mod";
///       public override string Version => "1.0.0";
///       public override string Author  => "YourName";
///
///       public void Patch(IPatchContext ctx) {
///           // modify files in ctx.GameDir here
///       }
///
///       public override void OnLoad(IModContext ctx) {
///           ctx.Hooks.BossKilled += kill => Console.WriteLine($"Killed {kill.BossName}!");
///       }
///   }
/// </summary>
public abstract class ModBase : IGameMod
{
    /// <summary>
    /// The mod's configuration. Access after calling LoadConfigAsync.
    /// </summary>
    protected ModConfig? Config { get; set; }

    public abstract string Name    { get; }
    public abstract string Version { get; }
    public abstract string Author  { get; }

    public virtual void OnLoad  (IModContext ctx) { }
    public virtual void OnUnload()                { }
    public virtual void OnTick  ()                { }

    /// <summary>
    /// Called by the mod loader to initialize the mod's configuration schema.
    /// Override this to set up default config values and schema.
    /// </summary>
    public virtual void InitializeConfig(ModConfig config) { }

    /// <summary>
    /// Load the mod's configuration from the config file.
    /// Call this from OnLoad or during initialization.
    /// </summary>
    protected async Task LoadConfigAsync(string gameDir)
    {
        var configDir = Path.Combine(gameDir, "mods", "config");
        var configPath = Path.Combine(configDir, $"{Name}.json");

        Directory.CreateDirectory(configDir);

        Config = await ModConfig.LoadAsync(Name, configPath);
        InitializeConfig(Config);
        await Config.SaveAsync();
    }

    /// <summary>
    /// Save the mod's configuration to the config file.
    /// Call this after making changes to Config.
    /// </summary>
    protected async Task SaveConfigAsync()
    {
        if (Config != null)
            await Config.SaveAsync();
    }
}
