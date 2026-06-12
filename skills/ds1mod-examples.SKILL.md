---
name: ds1-example-mods
description: Comprehensive pattern compendium derived from every example mod in the repository. Use these as templates when building new mods.
---


# DS1Mod Example Mods — Patterns & Reference

Comprehensive pattern compendium derived from every example mod in the repository. Use these as templates when building new mods.

---

## 1. Mod Architecture Quick Reference

| Mod | Features | Interfaces | Framework Refs |
|-----|----------|------------|----------------|
| **FogLogger** | Runtime-only, fog gate hook, file logging | `ModBase` | SDK only |
| **DiscordRPC** | Runtime-only, all 4 hooks, Discord Rich Presence | `ModBase` | SDK only |
| **HpLogger** | Minimal stub (template for new mods) | `ModBase, IGamePatcher` | SDK only |
| **AsylumSlam** | AI swap (embedded pre-compiled Lua) | `ModBase, IGamePatcher` | SDK, Core |
| **DemoMod** | All hooks + reader/writer + ImGui overlay + vftable probe | `ModBase, IGamePatcher, IGuiMod` | SDK, Core |
| **ImGuiDemo** | ImGui overlay with stats panel + debug window | `ModBase, IGuiMod` | SDK, Core |
| **EsdTestMod** | Full ESD testing + bonfire items + EMEVD + MSB | `ModBase, IGamePatcher, IGuiMod` | SDK, Modding, SoulsFormats |
| **GoofyDemon** | AI swap + EMEVD + FMG + custom items + flag bridge | `ModBase, IGamePatcher` | SDK, Modding, SoulsFormats |
| **ItemsDemo** | Everything: items, EMEVD, AI, MSB, ImGui checklist | `ModBase, IGamePatcher, IGuiMod` | SDK, Core, Modding, SoulsFormats |
| **ChaosMod** | Complete game: items+AI+hooks+ImGui+score tracker | `ModBase, IGamePatcher, IGuiMod` | SDK, Core, Modding, SoulsFormats |
| **EnemyRandomizer** | Full enemy randomizer (MSB + EMEVD + VFX) | `ModBase, IGamePatcher` | SDK, Modding, SoulsFormats |
| **AutoEquip** | Inventory heap-signature scan, auto-equips pickups, ModConfig | `ModBase` | SDK |
| **ChrumburGoofyRings** | Custom rings, per-ring file architecture, EnemyDamaged hook, native HUD bar (rally mechanic) | `ModBase, IGamePatcher` | SDK, Core, Modding, SoulsFormats |

---

## 1b. Pattern: Collection Mod (One File Per Feature)

**Example: ChrumburGoofyRings** — when a mod ships many independent features (rings), give each its own file behind a small interface; the entrypoint only orchestrates.

```csharp
internal interface IGoofyRing
{
    string Name { get; }
    int AccessoryId { get; }                                   // set during Patch
    void InitializeConfig(ModConfig config);
    void Patch(IPatchContext ctx, GamePatch g, byte[] paramdefs);
    void OnLoad(IModContext ctx, ModConfig config);
    void OnUnload();
    void OnTick(bool equipped);                                // shared equip check fed in
}

// Entrypoint: _rings array + foreach in InitializeConfig/Patch/OnLoad/OnTick/OnUnload.
// Shared services (equip detection via inventory signature) live in the entrypoint
// and pass results down — rings never duplicate them. Adding a feature = 1 file + 1 line.
```

Full source: `DS1Mod/mods/DS1Mod.ChrumburGoofyRings/` (`Rings/HuntersRing.cs` also demonstrates `EnemyDamaged`, `PlayerBody.WriteHp`, `LagBar` HUD pinning, and a fast 8–50 ms worker thread — see `ds1mod-memory.SKILL.md`).

---

## 2. Pattern: Runtime-Only Mod (No File Changes)

**Example: FogLoggerMod** — listens to fog gate hook, writes to console and log file.

```csharp
public sealed class FogLoggerMod : ModBase
{
    private IModContext? _ctx;
    private StreamWriter? _log;
    private int _count;

    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;
        _log = new StreamWriter(Path.Combine(ctx.ModsDir, "FogLogger.log"), append: true)
            { AutoFlush = true };

        ctx.Hooks.FogGateEntered += OnFogGateEntered;
    }

    private void OnFogGateEntered(FogGate gate)
    {
        _count++;
        int soulLevel = _ctx?.Reader.GetSoulLevel() ?? 0;
        var pos = _ctx?.Reader.GetPlayerState();
        string where = pos is null ? "unknown position"
            : $"({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})";
        Log($"#{_count}  fog gate at {where}  (SL {soulLevel})");
    }

    public override void OnUnload()
    {
        Log($"=== {_count} fog gates this session ===");
        _log?.Dispose();
    }

    private void Log(string msg) { /* write to _log + Console */ }
}
```

**Key patterns:**
- No `IGamePatcher`, no `Patch()` — mod is purely runtime
- Only needs `DS1Mod.SDK` reference
- `AutoFlush = true` for real-time log writes
- Dispose `StreamWriter` in `OnUnload()`

---

## 3. Pattern: AI Swap (Pre-Compiled Lua)

**Example: AsylumSlamMod** — replaces the Asylum Demon's AI with a slam-only version.

```csharp
public sealed class AsylumSlamMod : ModBase, IGamePatcher
{
    private const string LuaBnd      = "m18_01_00_00.luabnd.dcx";
    private const string EntryLeaf   = "223200_battle.lua";
    private const string ResourceLua = "223200_battle.luac";

    public void Patch(IPatchContext ctx)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        string path = Path.Combine(ctx.GameDir, "script", LuaBnd);
        byte[] replacement = ReadEmbedded(ResourceLua);
        if (replacement.Length == 0) return;

        ctx.BackupFile(path);
        byte[] dec = DCX.Decompress(path, out DCX.Type dcxType);
        BND3 bnd = BND3.Read(dec);

        foreach (BinderFile f in bnd.Files)
        {
            string leaf = Path.GetFileName(f.Name.Replace('\\', '/'));
            if (string.Equals(leaf, EntryLeaf, StringComparison.OrdinalIgnoreCase))
                f.Bytes = replacement;
        }

        File.WriteAllBytes(path, DCX.Compress(bnd.Write(), dcxType));
        ctx.Log($"Patched {EntryLeaf} ({replacement.Length} bytes)");
    }
}
```

**Key patterns:**
- AI bytecode is pre-compiled Lua 5.0, embedded as `EmbeddedResource` in `.csproj`
- Uses `DCX.Decompress/Compress` for BND3 archives
- Matches entry by leaf filename (handles `\` vs `/` separators)
- Calls `ctx.BackupFile()` before modifying
- `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` for shift-jis filenames

---

## 4. Pattern: Hook All Events + ImGui Overlay

**Example: DemoMod** — subscribes to all hooks, demonstrates reader/writer round-trip, has an ImGui overlay.

```csharp
public sealed class DemoMod : ModBase, IGamePatcher, IGuiMod
{
    private IModContext? _ctx;
    private StreamWriter? _log;
    private int _deaths, _bossKills, _fogGates;
    private bool _showProbe = true;

    // ── Patch (set up environment, back up files) ──────────────────────────────
    public void Patch(IPatchContext ctx) { /* back up common.emevd.dcx, inspect dirs */ }

    // ── OnLoad (subscribe to hooks) ────────────────────────────────────────────
    public override void OnLoad(IModContext ctx)
    {
        _ctx = ctx;
        _log = new StreamWriter(Path.Combine(ctx.ModsDir, "DemoMod.log"), append: false)
            { AutoFlush = true };

        ctx.Hooks.BossKilled      += kill => { _bossKills++; Log($"[BOSS] {kill.BossName}"); };
        ctx.Hooks.FogGateEntered  += gate => { _fogGates++; Log($"[FOG] {gate.Name}"); };
        ctx.Hooks.PlayerDied      += () => { _deaths++; Log($"[DIED] #{_deaths}"); };
        ctx.Hooks.PlayerLeveledUp += sl => Log($"[LEVEL] SL → {sl}");

        // Writer round-trip demo
        bool before = ctx.Reader.GetEventFlag(16);
        ctx.Writer.SetEventFlag(16, before);
    }

    // ── OnTick (periodic state snapshot) ───────────────────────────────────────
    public override void OnTick()
    {
        if (_ctx is null || _tickCount++ % 10 != 0) return;
        var stats  = _ctx.Reader.GetPlayerStats();
        var state  = _ctx.Reader.GetPlayerState();
        int souls  = _ctx.Reader.GetSouls();
        int level  = _ctx.Reader.GetSoulLevel();
        bool flag = _ctx.Reader.GetEventFlag(16);
        Log($"[Tick] HP {stats?.CurrentHp}/{stats?.MaxHp} SL {level} souls {souls}");
    }

    // ── OnUnload (session summary) ────────────────────────────────────────────
    public override void OnUnload()
    {
        Log($"=== Summary: {_deaths}D {_bossKills}B {_fogGates}F ===");
        _log?.Dispose();
    }

    // ── OnGui (ImGui overlay) ──────────────────────────────────────────────────
    public void OnGui()
    {
        if (!_showProbe) return;
        DS1ImGui.SetNextWindowPos(320, 10, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowSize(560, 360, ImGuiCond.FirstUseEver);
        if (DS1ImGui.Begin("Demo Window", ref _showProbe))
        {
            DS1ImGui.Text("Devices and Desires");
            if (DS1ImGui.Button("Click Me"))
                Log("Button clicked!");
        }
        DS1ImGui.End();
    }
}
```

**Key patterns:**
- All four hooks demonstrated: `BossKilled`, `FogGateEntered`, `PlayerDied`, `PlayerLeveledUp`
- Reader/Writer round-trip on load
- `OnTick` throttled with `_tickCount % 10` (every 5 seconds)
- Logs to both console and file
- Session summary in `OnUnload()`

---

## 5. Pattern: Custom Bonfire Items + EMEVD + MSB

**Example: EsdTestMod** — adds a "Spawn Rat" bonfire menu item, places a rat via MSB, wires EMEVD.

```csharp
public void Patch(IPatchContext ctx)
{
    var g = new GamePatch(ctx);

    // Allocate IDs
    int testFlag     = ctx.AllocateId("EventFlags_m18_00_00_00");
    int ratEntityId  = ctx.AllocateId("MsbEntities_m18_00_00_00");
    int evBase       = ctx.AllocateIds("EmevdEvents_m18_00_00_00", 2);
    int ratInit      = evBase;
    int ratSpawn     = evBase + 1;

    // FMG text for bonfire menu
    g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
        Texts.Set(bnd, Texts.EventText, 15001200, "Spawn Rat"));

    // Add bonfire menu item → sets testFlag when selected
    g.AddBonfireMenuItem(talkId: 15001200, gateFlag: -1, flagId: 90);

    // Unlock Level Up via shared bonfire ESD
    g.EditBonfireEsd(esd =>
        esd.SetTalkListGateFlag(1, 4, 15000100, -1));

    // Place rat NPC in Firelink
    g.EditMsb("m18_00_00_00", msb => msb.PlaceEnemy(
        entityId: ratEntityId, modelName: "c1201",
        npcParamId: 120100, thinkParamId: 120100,
        position: new Vector3(48f, -62.5f, 103f)));

    // EMEVD: disable rat at load, then enable on flag
    g.EditEmevd("m18_00_00_00", emevd =>
    {
        emevd.DefineEvent(ratInit, EMEVD.Event.RestBehaviorType.Default, ev => ev
            .SetCharacterEnabled(ratEntityId, EnabledState.Disabled));

        emevd.DefineEvent(ratSpawn, EMEVD.Event.RestBehaviorType.Restart, ev => ev
            .WhenFlag(testFlag, FlagState.On)
            .SetCharacterEnabled(ratEntityId, EnabledState.Enabled)
            .SetFlag(testFlag, FlagState.Off));
    });
}
```

**Key patterns:**
- `AddBonfireMenuItem` creates a menu item that sets a flag when selected
- `EditBonfireEsd` unlocks existing items via gate flags
- `PlaceEnemy` adds an MSB enemy part
- Two EMEVD events: one-shot init (disable rat) + restart loop (watch flag → enable)
- `RestBehaviorType.Default` = runs once, `Restart` = loops

---

## 6. Pattern: Full Item Pipeline (SpEffect → Goods → Lot → EMEVD → C#)

**Example: ItemsDemoMod** (excerpts showing the complete pipeline):

```csharp
public void Patch(IPatchContext ctx)
{
    var g = new GamePatch(ctx);
    byte[] paramdefs = GetEmbeddedResource("paramdef.paramdefbnd.dcx");

    // ── 1. Allocate IDs ─────────────────────────────────────────────────────
    int spEffect  = ctx.AllocateId(IdSpaces.SpEffectParam);
    int goodsA    = ctx.AllocateIds(IdSpaces.EquipParamGoods, 2);
    int goodsB    = goodsA + 1;
    int lotA      = ctx.AllocateIds(IdSpaces.ItemLotParam, 2);
    int lotB      = lotA + 1;
    int flagBase  = ctx.AllocateIds(IdSpaces.EventFlags(Map), 7);
    int evtBase   = ctx.AllocateIds(IdSpaces.EmevdEvents(Map), 7);
    int entityId  = ctx.AllocateId(IdSpaces.MsbEntities(Map));
    int msgBase   = ctx.AllocateIds(IdSpaces.EventText, 3);

    // ── 2. Create SpEffect (healing burst) ──────────────────────────────────
    g.DefineSpEffect(paramdefs, new SpEffectDef
    {
        Id             = spEffect,
        Duration       = 5f,
        HpRecoverPoint = 400,
        Configure      = row => row["motionInterval"].Value = 0f,
    });

    // ── 3. Create Goods (consumable using the SpEffect) ──────────────────────
    g.DefineGoods(paramdefs, new ItemDef
    {
        Id           = goodsA,
        DonorId      = 384,           // Estus Flask base
        SpEffectId   = spEffect,
        Name         = "Goofy Draught",
        Description  = "Restores 400 HP.",
        LongDesc     = "Tastes of regret.",
        MaxCount     = 5,
        AllowQuickUse = true,
    });

    // Key item (no effect):
    g.DefineGoods(paramdefs, new ItemDef
    {
        Id          = goodsB,
        DonorId     = 384,
        Name        = "Stone Trinket",
        Description = "A small stone.",
        MaxCount    = 1,
        GoodsType   = 1,  // key item
    });

    // ── 4. Create Lots (infinite + once-only) ───────────────────────────────
    g.DefineLot(paramdefs, new LotDef
    {
        LotId = lotA, ItemId = goodsA, Category = LotCategory.Goods,
        Rarity = 3, Count = 3, OnceOnlyFlag = -1,
    });
    g.DefineLot(paramdefs, new LotDef
    {
        LotId = lotB, ItemId = goodsB, Category = LotCategory.Goods,
        Rarity = 3, Count = 1, OnceOnlyFlag = getFlag,
    });

    // ── 5. Place ground pickup in MSB ──────────────────────────────────────
    g.EditMsb(Map, msb => msb.PlaceTreasure(
        lotId: lotB, position: new Vector3(-13f, 190f, 11f), entityId: entityId));

    // ── 6. EMEVD events ─────────────────────────────────────────────────────
    g.DefineItemTrigger(Map, spEffectId: spEffect, triggerFlagId: useFlag);

    g.EditEmevd(Map, emevd =>
    {
        // Coin used → show message
        emevd.DefineEvent(evtBase, EMEVD.Event.RestBehaviorType.Restart, ev => ev
            .WhenFlag(useFlag, FlagState.On)
            .DisplayMessage(msgBase)
            .WhenFlag(useFlag, FlagState.Off)
            .Restart());

        // Boss HP < 50% → award coins once
        emevd.DefineEvent(evtBase + 1, EMEVD.Event.RestBehaviorType.Default, ev => ev
            .WhenHpBelow(demonEntity, 0.5f)
            .AwardItemLot(lotA)
            .DisplayStatusMessage(msgBase + 1)
            .End());

        // Hide trinket pickup once collected
        emevd.DefineEvent(evtBase + 2, EMEVD.Event.RestBehaviorType.Default, ev => ev
            .WhenFlag(getFlag, FlagState.On)
            .SetObjectEnabled(entityId, EnabledState.Disabled)
            .End());
    });

    // ── 7. FMG text ─────────────────────────────────────────────────────────
    g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
    {
        Texts.Set(bnd, Texts.EventText, msgBase,     "The draught takes hold.");
        Texts.Set(bnd, Texts.EventText, msgBase + 1, "A reward for your tenacity.");
    });
}
```

**OnLoad: Register C# item callback:**
```csharp
public override void OnLoad(IModContext ctx)
{
    ctx.Hooks.RegisterItemUsed(goodsA, useFlag);
    ctx.Hooks.ItemUsed += goodsId =>
    {
        if (goodsId == goodsA)
            Console.WriteLine("Player used Goofy Draught!");
    };
}
```

**Key patterns:**
- Complete pipeline: SpEffect → Goods → Lot → MSB → EMEVD → C# hook
- `DefineItemTrigger` bridges consumable use to C# via flag pulses
- `PlaceTreasure` places ground pickup, `entityId` lets EMEVD hide it on collect
- Once-only lot paired with `SetObjectEnabled(Disabled)` event

---

## 7. Pattern: AI Editing via AiBuilder (C# DSL)

**Example: ChaosMod** (excerpt — 6-act weighted battle AI):

```csharp
g.EditAi(Map, "223200", ai => ai
    .Goal("Battle", goal => goal

        // Act 1 (30%): Berserker Rush
        .Act(30, q => q
            .ApproachTarget(Target.Enemy0, distMeters: 6f, cancelTime: 8)
            .Attack(animId: 3007, cancelTime: 6)
            .Wait(1))

        // Act 2 (22%): Double Slam
        .Act(22, q => q
            .ApproachTarget(Target.Enemy0, distMeters: 9f, cancelTime: 8)
            .Attack(animId: 3007, cancelTime: 6)
            .ApproachTarget(Target.Enemy0, distMeters: 4f, cancelTime: 5)
            .Attack(animId: 3007, cancelTime: 6))

        // Act 3 (18%): Spin Fury
        .Act(18, q => q
            .SpinStep(cancelTime: 4)
            .SpinStep(cancelTime: 4)
            .ApproachTarget(Target.Enemy0, distMeters: 5f, cancelTime: 6)
            .Attack(animId: 3007, cancelTime: 6))

        // Act 4 (15%): Hit and Run
        .Act(15, q => q
            .ApproachTarget(Target.Enemy0, distMeters: 5f, cancelTime: 7)
            .Attack(animId: 3007, cancelTime: 5)
            .LeaveTarget(cancelTime: 5)
            .WaitRandom(1, 2))

        // Act 5 (10%): Side Strafe
        .Act(10, q => q
            .SidewayMove(cancelTime: 3)
            .SidewayMove(cancelTime: 3)
            .ApproachTarget(Target.Enemy0, distMeters: 5f, cancelTime: 6)
            .Attack(animId: 3007, cancelTime: 6))

        // Act 6 (5%): Slow Terror
        .Act(5, q => q
            .WaitRandom(2, 4)
            .SpinStep(cancelTime: 3)
            .ApproachTarget(Target.Enemy0, distMeters: 4f, cancelTime: 5)
            .Attack(animId: 3007, cancelTime: 5)
            .Attack(animId: 3007, cancelTime: 5))

        .OnInterrupt(_ => true)),  // Always interruptible

    luaId: "MiniGreaterDemon223200");  // Must match vanilla goal name prefix
```

**Key patterns:**
- Weights are relative (don't need to sum to 100); AiBuilder normalizes to percentages
- `luaId` parameter ensures valid Lua identifiers (prepends "Npc" if ID starts with digit)
- `OnInterrupt(_ => true)` allows the AI to be interrupted (e.g., by taking damage)
- `distMeters` parameter uses literal world-unit distances, NOT sentinel values
- Each act emits a separate `_Act01`, `_Act02`, etc. function

---

## 8. Pattern: AI Mood Broadcasting (AI → C# via Flags)

**Example: GoofyDemonMod** — the AI sets event flags to communicate its current "mood" to C# code.

```csharp
// In Patch(): allocate mood flags
int moodFlagBase = ctx.AllocateIds(IdSpaces.EventFlags(Map), 10);

// In EditAi(): AI broadcasts mood via flags
g.EditAi(Map, npcFileId, ai => ai
    .Goal("Battle", goal => goal
        .Act(70, q => q
            .SetActiveFlagInRange(moodFlagBase, 10, 0)  // Mood 0
            .ApproachTarget()
            .Attack(3007))
        .Act(30, q => q
            .SetActiveFlagInRange(moodFlagBase, 10, 1)  // Mood 1
            .SpinStep()
            .LeaveTarget())
        .OnInterrupt(_ => true)));

// In EMEVD(): display mood as on-screen text
g.EditEmevd(Map, emevd =>
{
    for (int i = 0; i < 10; i++)
        emevd.DefineEvent(moodEventBase + i, EMEVD.Event.RestBehaviorType.Restart,
            Instr.IfEventFlag(FlagState.On,  moodFlagBase + i),
            Instr.DisplayMessage(moodMsgBase + i),
            Instr.IfEventFlag(FlagState.Off, moodFlagBase + i));
});

// In OnLoad(): read mood flags from C#
public override void OnTick()
{
    for (int i = 0; i < 10; i++)
        if (_ctx.Reader.GetEventFlag(moodFlagBase + i))
            Console.WriteLine($"Mood = {MoodNames[i]}");
}
```

**Key patterns:**
- `SetActiveFlagInRange(base, count, active)` clears all flags in range then sets one
- EMEVD watches each flag and shows corresponding FMG text
- C# reads the same flags via `GetEventFlag` for console/debug logging
- Tri-directional communication: AI → flag → (EMEVD + C#)

---

## 9. Pattern: Boss Difficulty Enhancement (SpEffect Aura)

**Example: ChaosMod** — applies a permanent SpEffect to the boss that multiplies stats:

```csharp
// In Patch():

// Define Evil Aura SpEffect
g.DefineSpEffect(paramdefs, new SpEffectDef
{
    Id       = evilAuraSpEffect,
    Duration = 9999f,  // Permanent
    Configure = row =>
    {
        row["maxHpRate"].Value              = 2.5f;   // 2.5× HP
        row["physicsAttackPowerRate"].Value = 1.8f;   // 1.8× phys damage
        row["magicAttackPowerRate"].Value   = 1.8f;   // 1.8× magic damage
        row["motionInterval"].Value         = 0f;
    },
});

// EMEVD: apply when demon becomes alive
g.EditEmevd(Map, emevd =>
{
    emevd.DefineEvent(EvtApplyEvilAura, EMEVD.Event.RestBehaviorType.Default, ev => ev
        .WhenDead(DemonEntity)          // Fires when entity is detected as alive
        .SetSpEffect(DemonEntity, evilAuraSpEffect)
        .SetFlag(FlagEvilAuraApplied, FlagState.On)
        .End());
});
```

**Key patterns:**
- `SetSpEffect` on a boss entity from EMEVD
- `WhenDead` is used as "entity is alive" check (fires when entity transitions from non-existent to alive)
- Duration 9999 = permanent
- Story: player gets a +300 HP Chaos Coin to counter the buffed boss

---

## 10. Pattern: Chaos Score + Live ImGui Dashboard

**Example: ChaosMod** — computes a running score from all hooks and renders a live dashboard:

```csharp
// Score formula
private int ChaosScore => _deaths * 15 + _bossKills * 100 + _fogGates * 5 + _levelUps * 3;

// Rank thresholds
private static readonly (int threshold, string rank)[] Ranks =
{
    (0,   "Perfectly Sane"),
    (15,  "Slightly Unhinged"),
    (30,  "Certified Gremlin"),
    (60,  "Chaos Apprentice"),
    (120, "Chaos Adept"),
    (250, "Lord of Absolute Chaos"),
};

private string ChaosRank
{
    get { foreach (var (t, r) in Ranks) if (ChaosScore >= t) return r; return Ranks[0].rank; }
}

// In OnGui():
DS1ImGui.PushStyleColor(ImGuiCol.PlotHistogram, 0.85f, 0.2f, 0.05f, 1f);
DS1ImGui.ProgressBar(
    Math.Min(1f, (float)ChaosScore / 250), -1, 0, $"Chaos: {ChaosScore}");
DS1ImGui.PopStyleColor();
DS1ImGui.Text($"Rank: {ChaosRank}");
```

**Key patterns:**
- All hooks feed into a single score
- Progress bar shows progress toward next rank
- `PushStyleColor`/`PopStyleColor` for visual styling
- Volatile fields for thread-safe ImGui reads

---

## 11. Patterns: EMEVD Conditions & Actions Catalog

All from ItemsDemoMod + ChaosMod:

| Pattern | Example |
|---------|---------|
| Simple flag wait | `.WhenFlag(flagId, FlagState.On)` |
| Boss HP milestone | `.WhenHpBelow(entity, 0.5f)` |
| Area entry/exit | `.WhenInsideArea(player, areaId)` → `.WhenOutsideArea(player, areaId)` |
| Compound AND | `.WhenAllOf(main, and => and.Dead(entity).Flag(flag, On))` |
| Compound OR | `.WhenAnyOf(or => or.Flag(a, On).Flag(b, On))` |
| Item award | `.AwardItemLot(lotId)` |
| Object visibility | `.SetObjectEnabled(entityId, EnabledState.Disabled)` |
| Character control | `.SetCharacterEnabled()`, `.SetCharacterAI()`, `.SetCharacterHome()` |
| Raw instruction | `.Raw(2006, 3, 1, entity, dummyPoly, sfxId)` |
| Insert after match | `emevd.InsertAfter(eventId, Instr.IsForceAnimation(e, a), newInstr, alreadyPresent)` |

---

## 12. Discord Rich Presence Mod

**Example: DiscordRpcMod** — all 4 hooks + Discord presence:

```csharp
public override void OnLoad(IModContext ctx)
{
    _ctx = ctx;
    _client = new DiscordRpcClient(AppId, logger: new NullLogger());
    _client.Initialize();

    ctx.Hooks.BossKilled      += k => { _lastBoss = k.BossName; _fightingBoss = false; };
    ctx.Hooks.FogGateEntered  += f => { _fightingBoss = true; _currentActivity = $"Fighting {f.Name}"; };
    ctx.Hooks.PlayerDied      += () => { _deaths++; };
    ctx.Hooks.PlayerLeveledUp += l => { };  // unused but subscribed
}

public override void OnTick()
{
    _client?.Invoke();
    PushPresence();
}

private void PushPresence()
{
    var stats = _ctx?.Reader.GetPlayerStats();
    int level = _ctx?.Reader.GetSoulLevel() ?? 0;
    int souls = _ctx?.Reader.GetSouls() ?? 0;

    _client.SetPresence(new RichPresence
    {
        Details = _currentActivity,
        State   = $"SL {level}  |  {souls:N0} souls  |  ☠ {_deaths}",
        Timestamps = new Timestamps(_sessionStart),
        Assets = new Assets
        {
            LargeImageKey = "ds1_bonfire",
            SmallImageKey = "ds1_skull",
        },
    });
}
```

**Key patterns:**
- Uses the `DiscordRPC` NuGet package (not part of DS1Mod)
- `_client.Invoke()` called every tick to process Discord callbacks
- Presence pushed every tick (could be throttled)

---

## 13. ImGui Checklist Overlay Pattern

**Example: ItemsDemoMod** — runtime checklist with status display:

```csharp
private void OnGui()
{
    if (!_windowOpen) return;
    DS1ImGui.SetNextWindowPos(10, 10, ImGuiCond.FirstUseEver);
    DS1ImGui.SetNextWindowBgAlpha(0.82f);

    if (DS1ImGui.Begin("Checklist", ref _windowOpen))
    {
        DS1ImGui.Text($"Progress: {ticked}/{total}");
        DS1ImGui.Separator();

        DS1ImGui.Text("Patch-time");
        DrawCheck("DefineSpEffect", _patchSpEffect);
        DrawCheck("DefineGoods",    _patchGoods);
        DrawCheck("DefineLot",      _patchLots);
        DrawCheck("EditMsb",        _patchMsb);

        DS1ImGui.Spacing();
        DS1ImGui.Text("Runtime");
        DrawCheck("Use the item",    _rtItemUsed);
        DrawCheck("Kill the boss",   _rtBossDead);
    }
    DS1ImGui.End();
}

private static void DrawCheck(string label, bool done)
{
    if (done)
    {
        DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.2f, 0.9f, 0.2f, 1f);  // Green
        DS1ImGui.Text($"  [x] {label}");
        DS1ImGui.PopStyleColor();
    }
    else
    {
        DS1ImGui.PushStyleColor(ImGuiCol.Text, 0.55f, 0.55f, 0.55f, 1f); // Grey
        DS1ImGui.Text($"  [ ] {label}");
        DS1ImGui.PopStyleColor();
    }
}
```

---

## 14. Common Gotchas & Rules

1. **Always `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)`** when touching BND entries (shift-jis filenames).

2. **Lua AI `luaId` parameter**: Must be a valid Lua identifier. If your NPC file ID starts with a digit (e.g., "223200"), pass a `luaId` like `"Npc223200"` or the emitted Lua will be invalid.

3. **`distMeters` for AI approach**: Pass a positive float (e.g. `8.0`), NOT a sentinel constant. Negative distances cause the AI to never reach its target.

4. **Bonfire menu is fixed layout**: You cannot add entirely new UI elements — only add entries to the existing menu list. Slot indices 0-4 are vanilla; custom slots start at 13+ (via `AllocateBonfireSlot()`).

5. **File edits are idempotent**: `GamePatch` backs up on first access. `PlaceTreasure` removes existing treasure for the same lot before re-adding. EMEVD `InsertAfter` has `alreadyPresent` guard.

6. **You need `paramdef.paramdefbnd.dcx`** for any PARAM editing. The game doesn't ship it; extract from DSR paramdefbnd or use the version bundled with example mods.

7. **Cross-mod conflicts are detected**: `GamePatch(ctx)` constructor wires conflict recording. Two mods patching the same file → logged at host startup.

8. **`OnGui()` on render thread**: Use `volatile` fields to share state from `OnTick()` (game thread). Never call `DS1ImGui.*` from hooks or `OnTick()`.

9. **`DefineItemTrigger` writes to a specific map's EMEVD**: The bridge event must run on the same map where the player uses the item. For global detection, write it to every map's EMEVD (or use common.emevd.dcx).

10. **EventPump polls every 500ms**: Hook handlers have up to 500ms latency. `OnTick()` fires at the same rate. For real-time detection (e.g., animation state), you need native hooks.
