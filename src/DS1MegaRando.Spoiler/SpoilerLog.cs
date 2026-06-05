using DS1MegaRando.Enemies;
using DS1MegaRando.FogGate;
using DS1MegaRando.Items;
using DS1MegaRando.Settings;

namespace DS1MegaRando.Spoiler;

public class SpoilerLog
{
    public string Header       { get; set; } = "";
    public string FogSection   { get; set; } = "";
    public string ItemSection  { get; set; } = "";
    public string EnemySection { get; set; } = "";
    public string FullText     { get; set; } = "";

    public static SpoilerLog Build(
        MegaSettings settings,
        FogGateResult? fogResult,
        ItemResult? itemResult,
        EnemyResult? enemyResult)
    {
        var log = new SpoilerLog();

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("═══════════════════════════════════════════════════════");
        sb.AppendLine("           DS1 MEGA RANDOMIZER — SPOILER LOG          ");
        sb.AppendLine("═══════════════════════════════════════════════════════");
        sb.AppendLine($"Seed:    {settings.Global.Seed:X8}");
        sb.AppendLine($"Date:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Version: 1.0.0");
        sb.AppendLine($"Settings Hash: {settings.ToHashString()}");
        sb.AppendLine();

        log.Header = sb.ToString();
        sb.Clear();

        if (fogResult != null && settings.FogGate.SpoilerLogFogConnections)
        {
            sb.AppendLine("───────────────────────────────────────────────────────");
            sb.AppendLine("  FOG GATE CONNECTIONS");
            sb.AppendLine("───────────────────────────────────────────────────────");
            foreach (var conn in fogResult.Connections.OrderBy(c => c.FromArea))
                sb.AppendLine($"  {conn.FromArea,-35} → {conn.ToArea}");
            sb.AppendLine();
            log.FogSection = sb.ToString();
            sb.Clear();
        }

        if (itemResult != null && settings.Items.SpoilerLogItemPlacements)
        {
            sb.AppendLine("───────────────────────────────────────────────────────");
            sb.AppendLine("  KEY ITEM PLACEMENTS");
            sb.AppendLine("───────────────────────────────────────────────────────");
            foreach (var (name, area, loc) in itemResult.KeyItemPlacements.OrderBy(x => x.Item1))
                sb.AppendLine($"  {name,-35} : {area} ({loc})");
            sb.AppendLine();

            log.ItemSection = sb.ToString();
            sb.Clear();
        }

        if (enemyResult != null && settings.Enemies.SpoilerLogEnemyPlacements)
        {
            sb.AppendLine("───────────────────────────────────────────────────────");
            sb.AppendLine("  ENEMY REPLACEMENTS");
            sb.AppendLine("───────────────────────────────────────────────────────");
            foreach (var (oldModel, newModel, area) in enemyResult.SpoilerLog.OrderBy(x => x.Item3))
                sb.AppendLine($"  {area,-20} {oldModel,-10} → {newModel}");
            sb.AppendLine();
            log.EnemySection = sb.ToString();
        }

        log.FullText = log.Header + log.FogSection + log.ItemSection + log.EnemySection;
        return log;
    }
}
