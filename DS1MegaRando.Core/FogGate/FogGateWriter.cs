using System.Numerics;
using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.Graph;
using DS1MegaRando.Core.IO;
using DS1MegaRando.Data.Areas;
using SoulsFormats;

namespace DS1MegaRando.Core.FogGate;

/// <summary>
/// Writes fog gate randomization results to DS1R game files.
/// Ports the MSB and EMEVD writing logic from FogMod GameDataWriter.cs.
/// Requires FogMod's pre-compiled dist EMEVD files (in dist\DS1R\event\)
/// which contain events 5700 (area warp), 8900 (boss trigger), 8901, 8950.
/// </summary>
public class FogGateWriter
{
    private readonly string _distEventDir;

    public FogGateWriter(string distEventDir)
    {
        _distEventDir = distEventDir;
    }

    /// <summary>
    /// Returns the expected path to FogMod's dist event directory relative to the game dir.
    /// We ship FogMod alongside DSR_Mega so this path should be stable.
    /// </summary>
    public static string DefaultDistEventDir =>
        Path.Combine(
            AppContext.BaseDirectory,
            @"..\..\..\..\FogMod-master\dist\DS1R\event");

    private record WarpPoint(int ID, int Action, int Player, string Map);

    public void Write(
        string outDir,
        GameData gameData,
        FogGateResult fogResult,
        AnnotationData ann)
    {
        var graph = fogResult.ConnectedGraph;

        // Build lookup: internal area name → MSB map ID (e.g. "depths" → "m10_00_00_00")
        var areaToMapId = MapSpecs.All.ToDictionary(m => m.InternalName, m => m.MapId);

        // Track player spawn index per MSB area (c0000_0050, c0000_0051, ...)
        var playerCounters = MapSpecs.All.ToDictionary(m => m.InternalName, _ => 50);

        // Entity ID counter for new regions / players
        int mk = 5900100;

        // Warp events to add to common.emevd event 0
        var warpEvents = new List<List<object>>();
        int slot = 0;

        // ── Phase 0: Clean up any previous fog gate edits ──────────────────

        foreach (var (_, msb) in gameData.Maps)
        {
            msb.Regions.Regions.RemoveAll(r =>
                r.Name.StartsWith("Boss start for ", StringComparison.Ordinal) ||
                r.Name.StartsWith("FR: ", StringComparison.Ordinal) ||
                r.Name.StartsWith("BR: ", StringComparison.Ordinal) ||
                r.Name.StartsWith("Region for ", StringComparison.Ordinal));

            msb.Parts.Players.RemoveAll(p =>
                p.Name.StartsWith("c0000_", StringComparison.Ordinal) &&
                int.TryParse(p.Name.AsSpan(6), out int n) && n >= 50);
        }

        // ── Phase 1: Create trigger regions and player spawns per entrance side ──

        // (entranceFullName, sideArea) → WarpPoint
        var warpPoints = new Dictionary<(string, string), WarpPoint>();

        foreach (var entrance in ann.Entrances)
        {
            if (entrance.HasTag("unused") || entrance.HasTag("door")) continue;
            if (!areaToMapId.TryGetValue(entrance.Area, out string? entrMapId)) continue;
            if (!gameData.Maps.TryGetValue(entrMapId, out MSB1? entrMsb)) continue;

            // Fog gate object (the MSB object with the fog gate mesh)
            var fog = entrMsb.Parts.Objects.FirstOrDefault(o => o.Name == entrance.Name);
            if (fog == null) continue;

            for (int i = 0; i <= 1; i++)
            {
                bool front = i == 0;
                var side = front ? entrance.ASide : entrance.BSide;
                if (side == null) continue;

                // Destination map for player spawn: override or same as entrance area
                string destAreaName = side.DestinationMap ?? entrance.Area;
                if (!areaToMapId.TryGetValue(destAreaName, out string? destMapId))
                    destMapId = entrMapId;
                if (!gameData.Maps.TryGetValue(destMapId, out MSB1? destMsb))
                    destMsb = entrMsb;

                // ── Trigger region ───────────────────────────────────────────

                int actionId;
                if (side.ActionRegion != 0)
                {
                    actionId = side.ActionRegion;
                }
                else
                {
                    var region = new MSB1.Region();
                    region.EntityID = mk++;
                    actionId = region.EntityID;
                    region.Name = $"{(front ? "FR" : "BR")}: {entrance.Text ?? entrance.Name}";

                    // Rotation: front uses fog rotation, back uses opposite (180°)
                    float rotY = fog.Rotation.Y + (front ? 0 : 180);
                    if (rotY > 180) rotY -= 360;
                    region.Rotation = new Vector3(fog.Rotation.X, rotY, fog.Rotation.Z);

                    // Move 1m in region's facing direction, drop 1m vertically for fog gates
                    region.Position = MoveInDirection(
                        fog.Position,
                        region.Rotation,
                        1f,
                        yOffset: entrance.HasTag("world") ? 0 : -1f);

                    var box = new MSB.Shape.Box
                    {
                        Width  = side.CustomActionWidth != 0 ? side.CustomActionWidth : 1.5f,
                        Height = 3f,
                        Depth  = 1.5f,
                    };
                    region.Shape = box;

                    entrMsb.Regions.Regions.Insert(0, region);
                }

                // ── Player spawn (warp destination for inbound warps) ────────

                int warpId = mk++;

                Vector3 warpPos, warpRot;
                if (front)
                {
                    // Front side: player spawns 1m in front of fog gate, facing back (180°)
                    warpRot = new Vector3(0, Opposite(fog.Rotation.Y), 0);
                    warpPos = MoveInDirection(fog.Position, fog.Rotation, 1f,
                        yOffset: side.HasTag("higher") ? 1f : 0f);
                }
                else
                {
                    // Back side: player spawns 1m behind fog gate, facing forward
                    float oppY = Opposite(fog.Rotation.Y);
                    warpRot = new Vector3(0, fog.Rotation.Y, 0);
                    warpPos = MoveInDirection(fog.Position, new Vector3(0, oppY, 0), 1f,
                        yOffset: side.HasTag("higher") ? 1f : 0f);
                }

                warpPos.Y += entrance.AdjustHeight + side.AdjustHeight;

                var player = new MSB1.Part.Player();
                if (!playerCounters.TryGetValue(destAreaName, out int pIdx))
                    pIdx = 50;
                player.Name      = $"c0000_{pIdx:d4}";
                playerCounters[destAreaName] = pIdx + 1;
                player.ModelName = "c0000";
                player.EntityID  = warpId;
                player.Position  = warpPos;
                player.Rotation  = warpRot;
                player.Scale     = Vector3.One;
                destMsb.Parts.Players.Add(player);

                warpPoints[(entrance.FullName, side.Area)] =
                    new WarpPoint(entrance.ID, actionId, warpId, destAreaName);
            }
        }

        // ── Phase 2: Build warp EMEVD events from randomized graph ──────────
        // The dist EMEVD replaces vanilla area-transition scripts, so ALL fog gates
        // (fixed and randomized alike) must be re-registered via event 5700.

        var seen = new HashSet<string>();
        var vanillaSeen = new HashSet<string>();
        int traverseFlag = 6920; // counter for world-fog-gate pair flags

        foreach (var node in graph.Nodes.Values)
        {
            foreach (var exit in node.To)
            {
                if (exit.Link == null) continue;
                var entrance = exit.Link;
                if (exit.Name == null || entrance.Name == null) continue;

                // Avoid processing each pair twice
                string pairKey = string.CompareOrdinal(exit.Name, entrance.Name) <= 0
                    ? $"{exit.Name}|{entrance.Name}"
                    : $"{entrance.Name}|{exit.Name}";
                if (!seen.Add(pairKey)) continue;

                if (!graph.EntranceIds.TryGetValue(exit.Name, out var exitAnn)) continue;
                if (!graph.EntranceIds.TryGetValue(entrance.Name, out var entAnn)) continue;

                // ── Fixed (vanilla) fog gate ──────────────────────────────────
                // Re-register with event 5700 so the dist EMEVD can handle them.
                if (exit.IsFixed && exit.Name == entrance.Name)
                {
                    if (vanillaSeen.Add(exitAnn.FullName))
                    {
                        if (exitAnn.HasTag("pvp") || (!exitAnn.HasTag("world") && !exitAnn.HasTag("boss")))
                        {
                            // Mode 1: standard fog gate (uses vanilla entity IDs)
                            if (!areaToMapId.TryGetValue(exitAnn.Area, out string? fixedMapId)) continue;
                            (byte fa, byte fb) = ParseMapId(fixedMapId);
                            warpEvents.Add(new List<object>
                            {
                                slot++, (uint)5700,
                                (byte)1, fa, fb, (byte)0,
                                exitAnn.ID, exitAnn.ID + 1,
                                0, 0, 0, 0, 0,
                            });
                        }
                        // "world" fog gates (mode 3) need action regions that we don't build for fixed
                        // entrances — skip them; they'll still transition via the vanilla backing logic.
                    }
                    continue;
                }

                // ── Randomized fog gate ───────────────────────────────────────
                string? exitSideArea = exit.From;
                string? entSideArea  = entrance.To;
                if (exitSideArea == null || entSideArea == null) continue;

                if (!warpPoints.TryGetValue((exitAnn.FullName, exitSideArea), out var a)) continue;
                if (!warpPoints.TryGetValue((entAnn.FullName,  entSideArea),  out var b)) continue;

                // Exit side → destination at entrance side (forward warp)
                AddWarpEvent(warpEvents, ref slot, exitSideArea, b, a, exitAnn, entAnn, isFront: true, areaToMapId);
                // Entrance side → destination at exit side (return warp)
                AddWarpEvent(warpEvents, ref slot, entSideArea, a, b, entAnn, exitAnn, isFront: false, areaToMapId);
            }
        }

        // ── Phase 3: Write EMEVD files ──────────────────────────────────────

        WriteEmevdFiles(outDir, warpEvents, slot);
    }

    private static void AddWarpEvent(
        List<List<object>> events,
        ref int slot,
        string triggerArea,
        WarpPoint dest,
        WarpPoint trigger,
        AnnotationEntrance triggerAnn,
        AnnotationEntrance destAnn,
        bool isFront,
        Dictionary<string, string> areaToMapId)
    {
        // Find destination map ID from dest.Map (area name)
        if (!areaToMapId.TryGetValue(dest.Map, out string? destMapId))
        {
            // Try parent area (e.g. "burg_upper" → look at entrance area)
            // Fall back: dest.Map itself
            destMapId = dest.Map;
        }

        (byte destArea, byte destBlock) = ParseMapId(destMapId);

        // Side flags
        var triggerSide = isFront ? triggerAnn.ASide : triggerAnn.BSide;
        var destSide    = isFront ? destAnn.BSide    : destAnn.ASide;
        triggerSide ??= triggerAnn.ASide ?? triggerAnn.BSide!;
        destSide    ??= destAnn.ASide    ?? destAnn.BSide!;

        events.Add(new List<object>
        {
            slot++,
            (uint)5700,
            (byte)0,        // mode: regular fog gate warp
            destArea,
            destBlock,
            (byte)0,        // hasCutscene
            trigger.ID,     // entrance ID (used to check lordvessel flag etc)
            dest.Player,    // player spawn entity in destination map
            triggerSide?.TrapFlag ?? 0,
            trigger.Action, // action region entity that triggers this warp
            triggerSide?.Flag ?? 0,
            destSide?.EntryFlag ?? 0,
            destSide?.BeforeWarpFlag ?? 0,
        });
    }

    private void WriteEmevdFiles(string outDir, List<List<object>> warpEvents, int slotCount)
    {
        if (!Directory.Exists(_distEventDir))
        {
            // Cannot write fog gate EMEVD — log a warning but don't crash
            System.Diagnostics.Debug.WriteLine(
                $"FogGateWriter: dist event dir not found: {_distEventDir}");
            return;
        }

        string outEventDir = Path.Combine(outDir, "event");
        Directory.CreateDirectory(outEventDir);

        foreach (string distPath in Directory.EnumerateFiles(_distEventDir, "*.emevd*"))
        {
            string fileName = Path.GetFileName(distPath);
            string outPath  = Path.Combine(outEventDir, fileName);
            string baseName = fileName.Replace(".emevd.dcx", "").Replace(".emevd", "");

            EMEVD evd;
            try { evd = EMEVD.Read(distPath); }
            catch { continue; }

            if (baseName == "common" && warpEvents.Count > 0)
            {
                // Inject warp event initialization calls into event 0 (constructor)
                var evt0 = evd.Events.FirstOrDefault(e => e.ID == 0);
                if (evt0 != null)
                {
                    foreach (var args in warpEvents)
                        evt0.Instructions.Add(new EMEVD.Instruction(2000, 0, args));
                }
            }

            try { evd.Write(outPath); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FogGateWriter: failed to write {outPath}: {ex.Message}");
            }
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static Vector3 MoveInDirection(Vector3 pos, Vector3 rot, float dist, float yOffset = 0f)
    {
        float angle = rot.Y * MathF.PI / 180f;
        return new Vector3(
            pos.X + MathF.Sin(angle) * dist,
            pos.Y + yOffset,
            pos.Z + MathF.Cos(angle) * dist);
    }

    private static float Opposite(float rotY)
    {
        float opp = rotY + 180f;
        return opp > 180f ? opp - 360f : opp;
    }

    private static (byte area, byte block) ParseMapId(string mapId)
    {
        // "m10_02_00_00" → area=10, block=2
        ReadOnlySpan<char> s = mapId.AsSpan();
        if (s.StartsWith("m")) s = s.Slice(1);
        int sep = s.IndexOf('_');
        if (sep < 0) return (0, 0);
        byte area  = byte.TryParse(s.Slice(0, sep), out byte a) ? a : (byte)0;
        s = s.Slice(sep + 1);
        sep = s.IndexOf('_');
        byte block = byte.TryParse(sep >= 0 ? s.Slice(0, sep) : s, out byte b) ? b : (byte)0;
        return (area, block);
    }
}
