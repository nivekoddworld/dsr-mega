using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.Enemies;
using DS1MegaRando.Core.FogGate;
using DS1MegaRando.Core.IO;
using DS1MegaRando.Core.Items;
using DS1MegaRando.Core.Settings;
using DS1MegaRando.Core.Spoiler;
using DS1MegaRando.Core.Verification;

namespace DS1MegaRando.Core;

public class RandomizationFailedException : Exception
{
    public IReadOnlyList<string> Issues { get; }
    public RandomizationFailedException(IEnumerable<string> issues)
        : base("Randomization failed softlock check: " + string.Join("; ", issues))
    {
        Issues = issues.ToList();
    }
}

public class MegaResult
{
    public FogGateResult? FogGate  { get; set; }
    public ItemResult?    Items    { get; set; }
    public EnemyResult?   Enemies  { get; set; }
    public SpoilerLog?    SpoilerLog { get; set; }
}

public class MegaRandomizer
{
    public event EventHandler<string>? LogMessage;
    public event EventHandler<int>?    ProgressChanged;

    private const int MaxRetries = 10;

    public async Task<MegaResult> RandomizeAsync(MegaSettings settings) =>
        await Task.Run(() => Randomize(settings));

    /// <summary>
    /// Randomize using pre-loaded game data and annotations (useful for batch testing).
    /// GameData is used read-only when DryRun = true.
    /// </summary>
    public MegaResult Randomize(MegaSettings settings, GameData gameData, AnnotationData ann)
    {
        return RunCore(settings, gameData, ann);
    }

    public MegaResult Randomize(MegaSettings settings)
    {
        Emit("Loading game files...");
        Progress(5);

        var reader   = new GameFileReader(settings.Global);
        var gameData = reader.ReadAll();

        Emit("Loading world annotations...");
        var ann = AnnotationLoader.LoadFog();

        Progress(10);
        return RunCore(settings, gameData, ann);
    }

    private MegaResult RunCore(MegaSettings settings, GameData gameData, AnnotationData ann)
    {
        MegaResult? result = null;
        uint baseSeed = settings.Global.Seed;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            if (attempt > 0)
            {
                Emit($"Softlock detected — retry {attempt}/{MaxRetries} with seed {baseSeed + (uint)attempt:X8}...");
                settings = settings.Clone();
                settings.Global.Seed = baseSeed + (uint)attempt;
            }

            try
            {
                result = RandomizeAttempt(settings, ann, gameData);
                break;
            }
            catch (RandomizationFailedException ex)
            {
                if (attempt == MaxRetries - 1)
                    throw new RandomizationFailedException(
                        new[] { $"All {MaxRetries} attempts failed. Last issues: " + string.Join("; ", ex.Issues) });
            }
        }

        if (result == null)
            throw new RandomizationFailedException(new[] { "All attempts exhausted." });

        if (!settings.Global.DryRun)
        {
            Emit("Backing up game files...");
            Progress(80);
            new BackupManager().BackupAll(settings.Global);

            Emit("Writing randomized files...");
            Progress(85);
            new GameFileWriter(settings.Global).WriteAll(gameData, result.FogGate, result.Items, result.Enemies, ann, settings.FogGate);
        }

        if (settings.Global.CreateSpoilerLog)
        {
            Emit("Writing spoiler log...");
            Progress(95);
            string logPath = string.IsNullOrEmpty(settings.Global.SpoilerLogPath)
                ? SpoilerSerializer.DefaultTextPath(
                    string.IsNullOrEmpty(settings.Global.OutputDirectory)
                        ? settings.Global.GameDirectory
                        : settings.Global.OutputDirectory,
                    settings.Global.Seed)
                : settings.Global.SpoilerLogPath;
            SpoilerSerializer.WriteText(result.SpoilerLog!, logPath);
        }

        Progress(100);
        Emit("Randomization complete!");
        return result;
    }

    private MegaResult RandomizeAttempt(MegaSettings settings, AnnotationData ann, GameData gameData)
    {
        FogGateResult? fogResult  = null;
        ItemResult?    itemResult = null;
        EnemyResult?   enemyResult = null;

        if (settings.FogGate.EnableFogGateRandomizer)
        {
            Emit("Randomizing fog gates...");
            Progress(15);
            var rng = new Random((int)settings.GetFogSeed());
            var fg  = new FogGateRandomizer();
            fg.Log += (_, m) => Emit(m);
            fogResult = fg.Randomize(settings.FogGate, ann, rng);
        }

        if (settings.Items.EnableItemRandomizer)
        {
            Emit("Randomizing items...");
            Progress(40);
            var rng = new Random((int)settings.GetItemSeed());
            var ir  = new ItemRandomizer();
            ir.Log += (_, m) => Emit(m);
            itemResult = ir.Randomize(settings.Items, ann, gameData,
                fogResult, fogResult?.ConnectedGraph, rng);
        }

        if (settings.Enemies.EnableEnemyRandomizer)
        {
            Emit("Randomizing enemies...");
            Progress(60);
            var rng = new Random((int)settings.GetEnemySeed());
            var er  = new EnemyRandomizer();
            er.Log += (_, m) => Emit(m);
            enemyResult = er.Randomize(settings.Enemies, ann, gameData, fogResult, rng);
        }

        Emit("Verifying — checking for softlocks...");
        Progress(75);
        var verification = new SoftlockChecker().Verify(fogResult, itemResult, ann, settings);
        if (!verification.Passed && !settings.Global.DryRun)
            throw new RandomizationFailedException(verification.Issues);

        var spoilerLog = SpoilerLog.Build(settings, fogResult, itemResult, enemyResult);

        return new MegaResult
        {
            FogGate    = fogResult,
            Items      = itemResult,
            Enemies    = enemyResult,
            SpoilerLog = spoilerLog,
        };
    }

    private void Emit(string message) => LogMessage?.Invoke(this, message);
    private void Progress(int pct)    => ProgressChanged?.Invoke(this, pct);
}
