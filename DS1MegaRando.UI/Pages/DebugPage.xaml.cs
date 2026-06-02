using System.Text;
using System.Windows;
using System.Windows.Controls;
using DS1MegaRando.Core;
using DS1MegaRando.Enemies;
using DS1MegaRando.FogGate;
using DS1MegaRando.Items;
using DS1MegaRando.Settings;

namespace DS1MegaRando.UI.Pages;

public partial class DebugPage : UserControl
{
    private MegaResult? _result;
    private MegaSettings? _settings;
    private string _activeTab = "Overview";

    public DebugPage() => InitializeComponent();

    /// <summary>Called by MainWindow after each successful randomization.</summary>
    public void SetResult(MegaResult result, MegaSettings settings)
    {
        _result   = result;
        _settings = settings;
        Refresh();
    }

    // ── Tab switching ──────────────────────────────────────────────────────

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag)
        {
            _activeTab = tag;
            Refresh();
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(Content.Text))
            Clipboard.SetText(Content.Text);
    }

    // ── Content builders ───────────────────────────────────────────────────

    private void Refresh()
    {
        if (Content == null) return;

        if (_result == null)
        {
            Content.Text = "No randomization has been run yet.\n\nRun the randomizer to populate this page.";
            return;
        }

        Content.Text = _activeTab switch
        {
            "Overview" => BuildOverview(),
            "Fog"      => BuildFog(),
            "Items"    => BuildItems(),
            "Enemies"  => BuildEnemies(),
            "Raw"      => BuildRaw(),
            _          => ""
        };
    }

    // ── Overview ───────────────────────────────────────────────────────────

    private string BuildOverview()
    {
        var sb = new StringBuilder();
        var s  = _settings!;

        sb.AppendLine("══════════════════════════════════════════════════════");
        sb.AppendLine("  OVERVIEW");
        sb.AppendLine("══════════════════════════════════════════════════════");
        sb.AppendLine();

        // Seed
        sb.AppendLine($"  Global Seed   : {s.Global.Seed:X8}");
        if (s.Global.UseSeparateSeedsPerModule)
        {
            sb.AppendLine($"  Fog Seed      : {s.Global.FogSeed:X8}");
            sb.AppendLine($"  Item Seed     : {s.Global.ItemSeed:X8}");
            sb.AppendLine($"  Enemy Seed    : {s.Global.EnemySeed:X8}");
        }
        sb.AppendLine($"  Generated     : {DateTime.Now:yyyy-MM-dd  HH:mm:ss}");
        sb.AppendLine($"  Dry Run       : {s.Global.DryRun}");
        sb.AppendLine();

        // Modules
        sb.AppendLine("  ── Modules enabled ──────────────────────────────");
        sb.AppendLine($"  Fog Gates     : {(s.FogGate.EnableFogGateRandomizer ? "YES" : "no")}");
        sb.AppendLine($"  Items         : {(s.Items.EnableItemRandomizer       ? "YES" : "no")}");
        sb.AppendLine($"  Enemies       : {(s.Enemies.EnableEnemyRandomizer    ? "YES" : "no")}");
        sb.AppendLine();

        // Starting area
        var fog = _result!.FogGate;
        if (fog != null)
        {
            string startArea = fog.StartArea;
            string customStartName = fog.ConnectedGraph?.CustomStart?.Name ?? "(none)";
            string customStartPos  = fog.ConnectedGraph?.CustomStart?.Pos  ?? "";
            string customStartMap  = fog.ConnectedGraph?.CustomStart?.MapId ?? "";
            sb.AppendLine("  ── Starting location ────────────────────────────");
            sb.AppendLine($"  Graph area    : {startArea}");
            sb.AppendLine($"  Custom start  : {customStartName}");
            if (!string.IsNullOrEmpty(customStartMap))
                sb.AppendLine($"  Map           : {customStartMap}");
            if (!string.IsNullOrEmpty(customStartPos))
                sb.AppendLine($"  Position      : {customStartPos}");
            sb.AppendLine();
        }

        // Quick stats
        sb.AppendLine("  ── Quick stats ──────────────────────────────────");
        var items = _result!.Items;
        if (items != null)
        {
            sb.AppendLine($"  Lot assignments   : {items.LotAssignments.Count}");
            sb.AppendLine($"  Shop assignments  : {items.ShopAssignments.Count}");
            sb.AppendLine($"  Key items placed  : {items.KeyItemPlacements.Count}");
            sb.AppendLine($"  Starting loadouts : {items.StartingLoadouts.Count} classes");
        }
        var enemies = _result!.Enemies;
        if (enemies != null)
        {
            int total = enemies.Placements.Values.Sum(l => l.Count);
            int changed = enemies.Placements.Values.SelectMany(l => l)
                .Count(p => p.OldModelId != p.NewModelId);
            sb.AppendLine($"  Enemy slots total : {total}");
            sb.AppendLine($"  Models changed    : {changed}");
            sb.AppendLine($"  Stat mods         : {enemies.StatModifications.Count}");
        }
        if (fog != null)
            sb.AppendLine($"  Fog connections   : {fog.Connections.Count}");

        sb.AppendLine();

        // Settings snapshot
        sb.AppendLine("  ── Settings snapshot ────────────────────────────");
        if (s.FogGate.EnableFogGateRandomizer)
        {
            sb.AppendLine($"  FOG: Boss={s.FogGate.RandomizeBossEntrances}  World={s.FogGate.RandomizeWorldEntrances}  Major={s.FogGate.RandomizeMajorEntrances}  Minor={s.FogGate.RandomizeMinorEntrances}");
            sb.AppendLine($"       Warps={s.FogGate.RandomizeWarps}  DLC={s.FogGate.IncludeDLC}  LordvesselDoors={s.FogGate.LordvesselDoorsRandomized}");
            sb.AppendLine($"       RandomStartArea={s.FogGate.RandomizeStartingArea}  Logic={s.FogGate.LogicMode}");
        }
        if (s.Items.EnableItemRandomizer)
        {
            sb.AppendLine($"  ITEMS: KeyMode={s.Items.KeyItemMode}  Diff={s.Items.Difficulty}  Shops={s.Items.RandomizeShops}");
            sb.AppendLine($"         StartLoadout={s.Items.RandomizeStartingLoadout}  LoadoutMode={s.Items.StartingLoadoutMode}");
            sb.AppendLine($"         BossSouls={s.Items.BossSoulHandling}  DLCLocs={s.Items.IncludeDLCItemLocations}");
        }
        if (s.Enemies.EnableEnemyRandomizer)
        {
            sb.AppendLine($"  ENEMIES: Mode={s.Enemies.EnemyPlacementMode}  Bosses={s.Enemies.RandomizeBosses}  BossMode={s.Enemies.BossRandomizationMode}");
            sb.AppendLine($"           Scale={s.Enemies.ScaleEnemyStats}  DupBosses={s.Enemies.AllowDuplicateBosses}  DLC={s.Enemies.IncludeDLCEnemies}");
        }

        return sb.ToString();
    }

    // ── Fog gates ──────────────────────────────────────────────────────────

    private string BuildFog()
    {
        var fog = _result!.FogGate;
        var sb  = new StringBuilder();

        sb.AppendLine("══════════════════════════════════════════════════════");
        sb.AppendLine("  FOG GATE CONNECTIONS");
        sb.AppendLine("══════════════════════════════════════════════════════");

        if (fog == null || fog.Connections.Count == 0)
        {
            sb.AppendLine("\n  (Fog gate randomizer was not active)");
            return sb.ToString();
        }

        sb.AppendLine($"\n  {fog.Connections.Count} connections  |  Start area: {fog.StartArea}");
        sb.AppendLine();

        // Group by FromArea first letter / area prefix for readability
        var grouped = fog.Connections
            .OrderBy(c => c.FromArea)
            .GroupBy(c => c.FromArea);

        foreach (var grp in grouped)
        {
            foreach (var conn in grp)
            {
                string label = conn.Text != null ? $"  ({conn.Text})" : "";
                sb.AppendLine($"  {conn.FromArea,-32} ──►  {conn.ToArea}{label}");
            }
        }

        // Area ratios (enemy scaling depth)
        if (fog.AreaRatios.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("  ── Area depth ratios (health × / damage ×) ─────");
            foreach (var (area, (hp, dmg)) in fog.AreaRatios.OrderBy(kv => kv.Value.Health))
                sb.AppendLine($"  {area,-32}  HP {hp:F2}×   DMG {dmg:F2}×");
        }

        return sb.ToString();
    }

    // ── Items ──────────────────────────────────────────────────────────────

    private string BuildItems()
    {
        var items = _result!.Items;
        var sb    = new StringBuilder();

        sb.AppendLine("══════════════════════════════════════════════════════");
        sb.AppendLine("  ITEM PLACEMENTS");
        sb.AppendLine("══════════════════════════════════════════════════════");

        if (items == null)
        {
            sb.AppendLine("\n  (Item randomizer was not active)");
            return sb.ToString();
        }

        // Starting gear
        sb.AppendLine("\n  ── Starting gear ────────────────────────────────");
        if (items.StartingLoadouts.Count > 0)
            for (int i = 0; i < items.StartingLoadouts.Count; i++)
            {
                var lo = items.StartingLoadouts[i];
                sb.AppendLine($"  Class {3000 + i}: right={lo.RightHand}  left={lo.LeftHand}"
                    + (lo.SubRightHand != StartingLoadout.Keep ? $"  rightOffhand={lo.SubRightHand}" : "")
                    + $"  armor=[{lo.Helm}, {lo.Chest}, {lo.Gauntlets}, {lo.Legs}]");
            }
        else
            sb.AppendLine("  (none / starting loadout not randomized)");

        // Key items
        sb.AppendLine();
        sb.AppendLine("  ── Key item placements ──────────────────────────");
        if (items.KeyItemPlacements.Count > 0)
            foreach (var (name, area, loc) in items.KeyItemPlacements.OrderBy(x => x.Item2))
                sb.AppendLine($"  {name,-36} {area,-22} {loc}");
        else
            sb.AppendLine("  (none)");

        // Lot assignments — show first 60 and summary
        sb.AppendLine();
        sb.AppendLine($"  ── Lot assignments  ({items.LotAssignments.Count} total) ─────────────");
        int shown = 0;
        foreach (var (lotId, (itemId, cat)) in items.LotAssignments.OrderBy(kv => kv.Key))
        {
            string catStr = cat switch
            {
                0          => "Weapon",
                0x10000000 => "Armor",
                0x20000000 => "Ring",
                0x40000000 => "Item",
                _          => $"0x{cat:X}",
            };
            sb.AppendLine($"  Lot {lotId,10}  →  ID {itemId,8}  [{catStr}]");
            if (++shown >= 60) { sb.AppendLine($"  ... and {items.LotAssignments.Count - shown} more"); break; }
        }

        // Shop assignments
        if (items.ShopAssignments.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"  ── Shop assignments  ({items.ShopAssignments.Count} total) ─────────────");
            int sShown = 0;
            foreach (var (rowId, itemId) in items.ShopAssignments.OrderBy(kv => kv.Key))
            {
                int price = items.ShopPrices.TryGetValue(rowId, out var p) ? p : -1;
                sb.AppendLine($"  Shop row {rowId,8}  →  ID {itemId,8}  {(price >= 0 ? $"({price} souls)" : "")}");
                if (++sShown >= 40) { sb.AppendLine($"  ... and {items.ShopAssignments.Count - sShown} more"); break; }
            }
        }

        return sb.ToString();
    }

    // ── Enemies ────────────────────────────────────────────────────────────

    private string BuildEnemies()
    {
        var enemies = _result!.Enemies;
        var sb      = new StringBuilder();

        sb.AppendLine("══════════════════════════════════════════════════════");
        sb.AppendLine("  ENEMY PLACEMENTS");
        sb.AppendLine("══════════════════════════════════════════════════════");

        if (enemies == null)
        {
            sb.AppendLine("\n  (Enemy randomizer was not active)");
            return sb.ToString();
        }

        // Boss / notable swaps from spoiler log
        if (enemies.SpoilerLog.Count > 0)
        {
            sb.AppendLine("\n  ── Model replacements ───────────────────────────");
            foreach (var (oldModel, newModel, area) in enemies.SpoilerLog.OrderBy(x => x.Item3))
                sb.AppendLine($"  {area,-22}  {oldModel,-10}  →  {newModel}");
        }

        // All placements grouped by map
        sb.AppendLine();
        sb.AppendLine("  ── All placements by map ────────────────────────");
        foreach (var (mapId, placements) in enemies.Placements.OrderBy(kv => kv.Key))
        {
            var changed = placements.Where(p => p.OldModelId != p.NewModelId).ToList();
            sb.AppendLine($"\n  {mapId}  ({placements.Count} slots, {changed.Count} changed)");
            foreach (var p in changed)
            {
                string eid = p.EntityId > 0 ? $"@{p.EntityId}" : $"[{p.PartIndex}]";
                sb.AppendLine($"    {eid,-12}  {p.OldModelId,-10}  →  {p.NewModelId}   npc {p.OldNpcParam}→{p.NewNpcParam}");
            }
        }

        // Stat modifications
        if (enemies.StatModifications.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"  ── NPC stat mods  ({enemies.StatModifications.Count}) ─────────────────────");
            int shown = 0;
            foreach (var (npcId, (hp, dmg)) in enemies.StatModifications.OrderBy(kv => kv.Key))
            {
                sb.AppendLine($"  NPC {npcId,8}   HP ×{hp:F2}   DMG ×{dmg:F2}");
                if (++shown >= 50) { sb.AppendLine($"  ... and {enemies.StatModifications.Count - shown} more"); break; }
            }
        }

        return sb.ToString();
    }

    // ── Raw dump ───────────────────────────────────────────────────────────

    private string BuildRaw()
    {
        var sb  = new StringBuilder();
        var s   = _settings!;
        var fog = _result!.FogGate;
        var itm = _result!.Items;
        var enm = _result!.Enemies;

        sb.AppendLine("══════════════════════════════════════════════════════");
        sb.AppendLine("  RAW DATA DUMP  (for copy-paste debugging)");
        sb.AppendLine("══════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine($"SEED: {s.Global.Seed:X8}");
        sb.AppendLine($"FOG_SEED: {s.Global.FogSeed:X8}");
        sb.AppendLine($"ITEM_SEED: {s.Global.ItemSeed:X8}");
        sb.AppendLine($"ENEMY_SEED: {s.Global.EnemySeed:X8}");
        sb.AppendLine();

        if (fog != null)
        {
            sb.AppendLine($"START_AREA: {fog.StartArea}");
            if (fog.ConnectedGraph?.CustomStart is { } cs)
            {
                sb.AppendLine($"CUSTOM_START_NAME: {cs.Name}");
                sb.AppendLine($"CUSTOM_START_MAP: {cs.MapId}");
                sb.AppendLine($"CUSTOM_START_POS: {cs.Pos}");
            }
            sb.AppendLine();
            sb.AppendLine("FOG_CONNECTIONS:");
            foreach (var c in fog.Connections.OrderBy(c => c.EntranceName))
                sb.AppendLine($"  {c.EntranceName}  {c.FromArea} <-> {c.ToArea}");
            sb.AppendLine();
        }

        if (itm != null)
        {
            sb.AppendLine($"STARTING_LOADOUTS: {string.Join("; ", itm.StartingLoadouts.Select(l => $"{l.RightHand}/{l.LeftHand}/{l.SubRightHand}/{l.Helm}/{l.Chest}/{l.Gauntlets}/{l.Legs}"))}");
            sb.AppendLine("KEY_ITEMS:");
            foreach (var (name, area, loc) in itm.KeyItemPlacements)
                sb.AppendLine($"  {name} = {area} ({loc})");
            sb.AppendLine("LOT_ASSIGNMENTS:");
            foreach (var (lotId, (itemId, cat)) in itm.LotAssignments.OrderBy(kv => kv.Key))
                sb.AppendLine($"  {lotId} {itemId} {cat}");
            sb.AppendLine();
        }

        if (enm != null)
        {
            sb.AppendLine("ENEMY_PLACEMENTS:");
            foreach (var (map, placements) in enm.Placements.OrderBy(kv => kv.Key))
                foreach (var p in placements.Where(p => p.OldModelId != p.NewModelId))
                    sb.AppendLine($"  {map} {p.EntityId} {p.OldModelId} {p.NewModelId} {p.NewNpcParam}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
