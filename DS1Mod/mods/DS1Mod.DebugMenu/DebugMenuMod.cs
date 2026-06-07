using System.Runtime.InteropServices;
using DS1Mod.Core;
using DS1Mod.Core.ImGui;
using DS1Mod.Core.Memory;
using DS1Mod.SDK;

namespace DS1Mod.DebugMenu;

/// <summary>
/// Full in-game ImGui debug menu. Press F3 to toggle.
///
/// Feature parity with horkrux/DSR-ImGuiDebugMenu plus the full
/// JKAnderson/DSR-Gadget feature set (cheats, stats, graphics, event flags).
/// Uses direct in-process memory access — no ReadProcessMemory overhead.
///
/// Address sources:
///   horkrux/DSR-ImGuiDebugMenu  — static ChrDbg flag RVAs
///   JKAnderson/DSR-Gadget       — pointer chain patterns &amp; offsets
/// </summary>
public sealed class DebugMenuMod : ModBase, IGuiMod
{
    public override string Name    => "Debug Menu";
    public override string Version => "2.0.0";
    public override string Author  => "DS1MegaRando";

    // ── UI state ──────────────────────────────────────────────────────────────
    private bool _visible    = false;
    private bool _f3WasDown  = false;

    // Position store/restore
    private float _storedX, _storedY, _storedZ, _storedAngle;
    private bool  _posStored = false;

    // Animation speed toggle
    private bool  _speedEnabled = false;
    private float _animSpeedVal = 1.0f;

    // Filter
    private bool  _filterEnabled;
    private float _filterBrR = 1f, _filterBrG = 1f, _filterBrB = 1f;
    private float _filterCtR = 1f, _filterCtG = 1f, _filterCtB = 1f;
    private float _filterSat = 1f, _filterHue  = 0f;

    // Misc — event flag editor
    private int _eventFlagId  = 0;
    private bool _eventFlagVal;

    // Items tab state
    private int  _itemCatIdx    = 0;
    private int  _itemSelIdx    = 0;
    private int  _itemInfusionIdx = 0;
    private int  _itemUpgrade   = 0;
    private int  _itemQuantity  = 1;
    private readonly byte[] _itemSearchBuf = new byte[64];
    private string _itemFilter  = "";

    // ── Static ChrDbg flag addresses (RVA from module base) ──────────────────
    // From horkrux/DSR-ImGuiDebugMenu. These are static game globals — no
    // pointer dereference required. ChrDbgAOB in DSR-Gadget resolves here.
    private const long RVA_ChrDbg = 0x1D151C9;   // base of the ChrDbg byte array

    private const int CDBG_PlayerNoDead      = 0x0;
    private const int CDBG_PlayerExterminate = 0x1;
    private const int CDBG_AllNoStamina      = 0x2;
    private const int CDBG_AllNoMp           = 0x3;
    private const int CDBG_AllNoArrow        = 0x4;
    private const int CDBG_AllNoMagicQty     = 0x5;
    private const int CDBG_PlayerHide        = 0x6;
    private const int CDBG_PlayerSilence     = 0x7;
    private const int CDBG_AllNoDead         = 0x8;
    private const int CDBG_AllNoDamage       = 0x9;
    private const int CDBG_AllNoHit          = 0xA;
    private const int CDBG_AllNoAttack       = 0xB;
    private const int CDBG_AllNoMove         = 0xC;
    private const int CDBG_AllNoUpdateAI     = 0xD;
    private const int CDBG_PlayerReload      = 0x12;

    // ── WorldChrManDbg (pointer at static address, then field offsets) ───────
    private const long RVA_WorldChrManDbgPtr = 0x1D151F8;
    private const int  WCMD_AllDrawHit       = 0x39;
    private const int  WCMD_NewKnockBackMode = 0xDC;

    // ── GameMan (pointer at static address, then field offsets) ─────────────
    private const long RVA_GameManPtr        = 0x1D10E18;
    private const int  GM_HitDispMapModel    = 0xD31;
    private const int  GM_HitDispLoHit       = 0xD32;
    private const int  GM_HitDispHiHit       = 0xD33;
    private const int  GM_DisableAllAreaEne  = 0xD3F;
    private const int  GM_DisableAllAreaEvt  = 0xD40;
    private const int  GM_DisableAllAreaMap  = 0xD41;
    private const int  GM_DisableAllAreaObj  = 0xD42;
    private const int  GM_EnableAllAreaObj   = 0xD43;
    private const int  GM_DisableAllAreaHiHit= 0xD45;
    private const int  GM_EnableAllAreaLoHit = 0xD46;
    private const int  GM_DisableAllAreaSfx  = 0xD47;
    private const int  GM_DisableAllAreaSnd  = 0xD48;
    private const int  GM_IsSaveMode         = 0xB70;
    private const int  GM_IsOnlineMode       = 0xB7D;
    private const int  GM_IsNoInput          = 0xB97;

    // ── AOB patterns (from JKAnderson/DSR-Gadget DSROffsets.cs) ─────────────
    // All `mov reg,[rip+disp32]` patterns: disp at +3, instr len 7 → rip-based
    private const string AOB_WorldChrBase =
        "48 8B 05 ? ? ? ? 48 8B 48 68 48 85 C9 0F 84 ? ? ? ? 48 39 5E 10 0F 84 ? ? ? ? 48";
    private const string AOB_ChrClassBase =
        "48 8B 05 ? ? ? ? 48 85 C0 ? ? F3 0F 58 80 AC 00 00 00";
    // `cmp byte[rip+disp32], 0` pattern: disp at +2, instr len 7
    private const string AOB_GroupMask =
        "80 3D ? ? ? ? 00 BE 00 00 00 80";
    // GraphicsData chain: *(*(target) + 0x738) = GraphicsData struct
    private const string AOB_GraphicsData =
        "48 8B 05 ? ? ? ? 48 8B 48 08 48 8B 01 48 8B 40 58";
    // ChrClassWarp: *(target) = ChrClassWarp struct
    private const string AOB_ChrClassWarp =
        "48 8B 05 ? ? ? ? 66 0F 7F 80 ? ? ? ? 0F 28 02 66 0F 7F 80 ? ? ? ? C6 80";

    // ── AOB-scanned targets (resolved once at load) ──────────────────────────
    private static nint _worldChrTarget;
    private static nint _chrClassTarget;
    private static nint _groupMaskBase;    // direct flat-byte-array address
    private static nint _graphicsTarget;   // holds ptr → chain: *(*(tgt)+0x738)
    private static nint _chrClassWarpTarget;

    // ── Version boosts (set from module size, from DSR-Gadget logic) ─────────
    private static int _boost1;      // ChrData1Boost1: applied to ChrMapData, ChrFlags1
    private static int _boost2;      // ChrData1Boost2: applied to HP, Stamina, ChrFlags2
    private static int _warpBoost;   // ChrClassWarpBoost: applied to LastBonfire, StableXYZ

    private static bool _initialized;

    // ── Per-character ChrData1 offsets (from DSROffsets) ────────────────────
    private const int CHR_FLAGS1_OFF   = 0x284;  // + boost1 for v1.03
    private const int CHR_FLAGS2_OFF   = 0x514;  // + boost2 for v1.03
    private const int CHR_HP_OFF       = 0x3D8;  // + boost2
    private const int CHR_MAXHP_OFF    = 0x3DC;  // + boost2
    private const int CHR_STAMINA_OFF  = 0x3E8;  // + boost2
    private const int CHR_MAXST_OFF    = 0x3EC;  // + boost2
    private const int CHR_MAPDATA_OFF  = 0x48;   // + boost1 → ptr to ChrMapData

    // ChrFlags1 bitmask values
    private const uint F1_NoGravity      = 0x00004000;
    private const uint F1_SuperArmor     = 0x00010000;
    private const uint F1_DeadMode       = 0x02000000; // no die, but killboxes work
    private const uint F1_DisableDamage  = 0x04000000; // no damage, no healing
    private const uint F1_Invincible     = 0x08000000; // super armor + disable damage

    // ChrFlags2 bitmask values
    private const uint F2_DrawHit        = 0x00000004;
    private const uint F2_NoDead         = 0x00000020; // invincible vs killboxes too
    private const uint F2_NoDamage       = 0x00000040; // + prevents healing
    private const uint F2_NoHit          = 0x00000080;
    private const uint F2_NoAttack       = 0x00000100;
    private const uint F2_NoMove         = 0x00000200;
    private const uint F2_NoStamina      = 0x00000400;
    private const uint F2_NoGoods        = 0x01000000;

    // WorldChrMan offsets
    private const int WCM_PLAYERCHR_OFF  = 0x68;  // → ChrData1
    private const int WCM_DEATHCAM_OFF   = 0x70;  // bool

    // ChrMapData offsets (from ChrMapData struct base, not via pointer)
    private const int MAP_ANIMDATA_OFF   = 0x18;  // → ChrAnimData ptr
    private const int MAP_POSDATA_OFF    = 0x28;  // → ChrPosData ptr
    private const int MAP_WARP_OFF       = 0x108; // bool — trigger warp
    private const int MAP_WARPX_OFF      = 0x110;
    private const int MAP_WARPY_OFF      = 0x114;
    private const int MAP_WARPZ_OFF      = 0x118;
    private const int MAP_WARPANGLE_OFF  = 0x124;

    // ChrAnimData
    private const int ANIM_SPEED_OFF     = 0xA8;

    // ChrPosData
    private const int POS_ANGLE_OFF      = 0x04;
    private const int POS_X_OFF          = 0x10;
    private const int POS_Y_OFF          = 0x14;
    private const int POS_Z_OFF          = 0x18;

    // ChrClassWarp offsets (+ _warpBoost for v1.03)
    private const int WARP_LASTBONFIRE   = 0xB24;
    private const int WARP_STABLEX       = 0xB90;
    private const int WARP_STABLEY       = 0xB94;
    private const int WARP_STABLEZ       = 0xB98;
    private const int WARP_STABLEANGLE  = 0xBA4;

    // PlayerGameData offsets (from DSR-Gadget ChrData2 enum)
    private const int PGD_VITALITY       = 0x40;
    private const int PGD_ATTUNEMENT     = 0x48;
    private const int PGD_ENDURANCE      = 0x50;
    private const int PGD_STRENGTH       = 0x58;
    private const int PGD_DEXTERITY      = 0x60;
    private const int PGD_INTELLIGENCE   = 0x68;
    private const int PGD_FAITH          = 0x70;
    private const int PGD_HUMANITY       = 0x84;
    private const int PGD_RESISTANCE     = 0x88;
    private const int PGD_SOULLEVEL      = 0x90;
    private const int PGD_SOULS          = 0x94;

    // GraphicsData offsets (from DSROffsets.GraphicsData enum)
    private const int GFX_FILTER_OVERRIDE  = 0x34D;
    private const int GFX_BRIGHTNESS_R     = 0x350;
    private const int GFX_BRIGHTNESS_G     = 0x354;
    private const int GFX_BRIGHTNESS_B     = 0x358;
    private const int GFX_SATURATION       = 0x35C;
    private const int GFX_CONTRAST_R       = 0x360;
    private const int GFX_CONTRAST_G       = 0x364;
    private const int GFX_CONTRAST_B       = 0x368;
    private const int GFX_HUE              = 0x36C;

    // GroupMask indices (from DSROffsets.GroupMask enum)
    private const int GM_MAP           = 0;
    private const int GM_OBJECTS       = 1;
    private const int GM_CHARACTERS    = 2;
    private const int GM_SFX           = 3;
    private const int GM_CUTSCENES     = 4;

    // ── IGameMod ─────────────────────────────────────────────────────────────

    public override void OnLoad(IModContext ctx)
    {
        EnsureInitialized();
        Console.WriteLine("[DebugMenu] Loaded — press F3 to toggle.");
    }

    // ── IGuiMod ──────────────────────────────────────────────────────────────

    public void OnGui()
    {
        HandleToggleKey();
        if (!_visible) return;
        EnsureInitialized();
        DrawMenu();
    }

    // ── Initialization (AOB scans + version detection) ────────────────────────

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        int ms = GameMemory.ModuleSize;
        if (ms == 0) return; // module not ready yet

        _initialized = true;

        // Version boosts (from JKAnderson/DSR-Gadget DSROffsets.GetOffsets)
        int version = ms switch
        {
            0x4869400 => 0, // 1.01
            0x496BE00 => 1, // 1.01.1
            0x37CB400 => 2, // 1.01.2
            0x3817800 => 3, // 1.03
            _ => 100        // newer Steam build: apply latest boosts
        };
        if (version > 1) _warpBoost = 0x10;
        if (version > 2) { _boost1 = 0x20; _boost2 = 0x10; }

        // AOB scans
        _worldChrTarget     = ScanRip(AOB_WorldChrBase);
        _chrClassTarget     = ScanRip(AOB_ChrClassBase);
        _groupMaskBase      = ScanRipCmp(AOB_GroupMask);
        _graphicsTarget     = ScanRip(AOB_GraphicsData);
        _chrClassWarpTarget = ScanRip(AOB_ChrClassWarp);

        Console.WriteLine($"[DebugMenu] version={version} boost1=0x{_boost1:X} boost2=0x{_boost2:X}");
        Console.WriteLine($"[DebugMenu] worldChr=0x{(ulong)_worldChrTarget:X} chrClass=0x{(ulong)_chrClassTarget:X}");
        Console.WriteLine($"[DebugMenu] groupMask=0x{(ulong)_groupMaskBase:X} graphics=0x{(ulong)_graphicsTarget:X}");
    }

    // ── AOB scan helpers ──────────────────────────────────────────────────────

    /// <summary>Resolve a `mov reg,[rip+disp32]` at disp=+3, instrLen=7.</summary>
    private static nint ScanRip(string pattern)
    {
        nint hit = GameMemory.Scan(pattern);
        if (hit == 0) return 0;
        int disp = GameMemory.Read<int>(hit + 3);
        return hit + 7 + disp;
    }

    /// <summary>Resolve a `cmp byte[rip+disp32], imm8` at disp=+2, instrLen=7.</summary>
    private static nint ScanRipCmp(string pattern)
    {
        nint hit = GameMemory.Scan(pattern);
        if (hit == 0) return 0;
        int disp = GameMemory.Read<int>(hit + 2);
        return hit + 7 + disp;
    }

    // ── Pointer chain helpers ─────────────────────────────────────────────────

    private static nint WorldChrMan
        => _worldChrTarget == 0 ? 0 : GameMemory.Read<nint>(_worldChrTarget);

    private static nint PlayerChr
    { get { nint w = WorldChrMan; return w == 0 ? 0 : GameMemory.Read<nint>(w + WCM_PLAYERCHR_OFF); } }

    private static nint PlayerGameData
    {
        get
        {
            if (_chrClassTarget == 0) return 0;
            nint gdm = GameMemory.Read<nint>(_chrClassTarget);
            return gdm == 0 ? 0 : GameMemory.Read<nint>(gdm + 0x10);
        }
    }

    private static nint ChrMapData
    { get { nint chr = PlayerChr; return chr == 0 ? 0 : GameMemory.Read<nint>(chr + CHR_MAPDATA_OFF + _boost1); } }

    private static nint ChrPosData
    { get { nint m = ChrMapData; return m == 0 ? 0 : GameMemory.Read<nint>(m + MAP_POSDATA_OFF); } }

    private static nint ChrAnimData
    { get { nint m = ChrMapData; return m == 0 ? 0 : GameMemory.Read<nint>(m + MAP_ANIMDATA_OFF); } }

    private static nint ChrClassWarp
        => _chrClassWarpTarget == 0 ? 0 : GameMemory.Read<nint>(_chrClassWarpTarget);

    private static nint GraphicsData
    {
        get
        {
            if (_graphicsTarget == 0) return 0;
            nint p = GameMemory.Read<nint>(_graphicsTarget);
            return p == 0 ? 0 : GameMemory.Read<nint>(p + 0x738);
        }
    }

    // ── Toggle key ────────────────────────────────────────────────────────────

    private void HandleToggleKey()
    {
        bool f3Down = (GetAsyncKeyState(0x72) & 0x8000) != 0; // VK_F3
        if (f3Down && !_f3WasDown)
            _visible = !_visible;
        _f3WasDown = f3Down;
    }

    // ── Main window ──────────────────────────────────────────────────────────

    private void DrawMenu()
    {
        DS1ImGui.SetNextWindowPos(20, 20, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowSize(420, 600, ImGuiCond.FirstUseEver);
        DS1ImGui.SetNextWindowBgAlpha(0.88f);

        if (!DS1ImGui.Begin("DS1 Debug Menu  [F3]", ref _visible))
        {
            DS1ImGui.End();
            return;
        }

        DS1ImGui.TextDisabled("F3 to close  |  in-process direct writes");
        DS1ImGui.Separator();

        if (DS1ImGui.BeginTabBar("##tabs"))
        {
            if (DS1ImGui.BeginTabItem("Player"))   { DrawPlayerTab();   DS1ImGui.EndTabItem(); }
            if (DS1ImGui.BeginTabItem("Stats"))    { DrawStatsTab();    DS1ImGui.EndTabItem(); }
            if (DS1ImGui.BeginTabItem("Items"))    { DrawItemsTab();    DS1ImGui.EndTabItem(); }
            if (DS1ImGui.BeginTabItem("World/AI")) { DrawWorldTab();    DS1ImGui.EndTabItem(); }
            if (DS1ImGui.BeginTabItem("Graphics")) { DrawGraphicsTab(); DS1ImGui.EndTabItem(); }
            if (DS1ImGui.BeginTabItem("Game"))     { DrawGameTab();     DS1ImGui.EndTabItem(); }
            if (DS1ImGui.BeginTabItem("Misc"))     { DrawMiscTab();     DS1ImGui.EndTabItem(); }
            if (DS1ImGui.BeginTabItem("Info"))     { DrawInfoTab();     DS1ImGui.EndTabItem(); }
            DS1ImGui.EndTabBar();
        }

        DS1ImGui.End();
    }

    // ── Player tab ───────────────────────────────────────────────────────────

    private void DrawPlayerTab()
    {
        nint chr = PlayerChr;
        bool loaded = chr != 0;

        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("Invincibility", ImGuiTreeNodeFlags.DefaultOpen))
        {
            // ChrDbg static flags (always accessible)
            ChrDbgCheckbox("God Mode (No Dead)",  CDBG_PlayerNoDead);
            ChrDbgCheckbox("All No Dead",         CDBG_AllNoDead);
            ChrDbgCheckbox("All No Damage",       CDBG_AllNoDamage);

            if (!loaded) DS1ImGui.TextDisabled("  (load a save for per-player flags)");
            else
            {
                ChrFlag1Checkbox("Dead Mode (ChrFlags1)",    chr, F1_DeadMode);
                ChrFlag1Checkbox("Disable Damage (ChrF1)",   chr, F1_DisableDamage);
                ChrFlag1Checkbox("Invincible (ChrF1)",        chr, F1_Invincible);
                ChrFlag2Checkbox("NoDead (ChrFlags2 full)",  chr, F2_NoDead);
                ChrFlag2Checkbox("NoDamage (ChrFlags2)",     chr, F2_NoDamage);
            }
        }

        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("Combat", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ChrDbgCheckbox("One-Hit Kill (Exterminate)", CDBG_PlayerExterminate);
            ChrDbgCheckbox("Hide from Enemies",          CDBG_PlayerHide);
            ChrDbgCheckbox("No Sound Emitted",           CDBG_PlayerSilence);

            if (loaded)
            {
                ChrFlag1Checkbox("Super Armor",  chr, F1_SuperArmor);
                ChrFlag2Checkbox("No Hit",       chr, F2_NoHit);
                ChrFlag2Checkbox("No Attack",    chr, F2_NoAttack);
                ChrFlag2Checkbox("No Move",      chr, F2_NoMove);
                ChrFlag2Checkbox("No Goods Use", chr, F2_NoGoods);
            }
        }

        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("Resources", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ChrDbgCheckbox("No Stamina Consume",  CDBG_AllNoStamina);
            ChrDbgCheckbox("No MP Consume",       CDBG_AllNoMp);
            ChrDbgCheckbox("No Arrow Consume",    CDBG_AllNoArrow);
            ChrDbgCheckbox("No Spell Uses",       CDBG_AllNoMagicQty);
            if (loaded)
                ChrFlag2Checkbox("No Stamina (ChrF2)", chr, F2_NoStamina);
        }

        DS1ImGui.Spacing();
        if (!loaded)
        {
            if (DS1ImGui.CollapsingHeader("HP / Stamina"))
                DS1ImGui.TextDisabled("Load a save file first.");
            if (DS1ImGui.CollapsingHeader("Position"))
                DS1ImGui.TextDisabled("Load a save file first.");
        }
        else
        {
            if (DS1ImGui.CollapsingHeader("HP / Stamina", ImGuiTreeNodeFlags.DefaultOpen))
            {
                int hp    = GameMemory.Read<int>(chr + CHR_HP_OFF    + _boost2);
                int maxHp = GameMemory.Read<int>(chr + CHR_MAXHP_OFF + _boost2);
                int st    = GameMemory.Read<int>(chr + CHR_STAMINA_OFF + _boost2);
                int maxSt = GameMemory.Read<int>(chr + CHR_MAXST_OFF   + _boost2);

                DS1ImGui.Text($"HP:      {hp} / {maxHp}");
                if (maxHp > 0)
                {
                    int hpEdit = hp;
                    DS1ImGui.SetNextItemWidth(200f);
                    if (DS1ImGui.SliderInt("##hp", ref hpEdit, 0, maxHp))
                        GameMemory.Write(chr + CHR_HP_OFF + _boost2, hpEdit);
                    DS1ImGui.SameLine();
                    if (DS1ImGui.Button("Fill HP"))
                        GameMemory.Write(chr + CHR_HP_OFF + _boost2, maxHp);
                }

                DS1ImGui.Text($"Stamina: {st} / {maxSt}");
                if (maxSt > 0)
                {
                    int stEdit = st;
                    DS1ImGui.SetNextItemWidth(200f);
                    if (DS1ImGui.SliderInt("##st", ref stEdit, 0, maxSt))
                        GameMemory.Write(chr + CHR_STAMINA_OFF + _boost2, stEdit);
                    DS1ImGui.SameLine();
                    if (DS1ImGui.Button("Fill Stamina"))
                        GameMemory.Write(chr + CHR_STAMINA_OFF + _boost2, maxSt);
                }
            }

            if (DS1ImGui.CollapsingHeader("Position"))
            {
                nint pos = ChrPosData;
                if (pos == 0) { DS1ImGui.TextDisabled("ChrPosData null"); }
                else
                {
                    float x = GameMemory.Read<float>(pos + POS_X_OFF);
                    float y = GameMemory.Read<float>(pos + POS_Y_OFF);
                    float z = GameMemory.Read<float>(pos + POS_Z_OFF);
                    float angle = GameMemory.Read<float>(pos + POS_ANGLE_OFF);
                    DS1ImGui.Text($"X:{x,9:F3}  Y:{y,9:F3}  Z:{z,9:F3}  A:{angle,6:F3}");

                    if (DS1ImGui.Button("Store Position"))
                    {
                        _storedX = x; _storedY = y; _storedZ = z; _storedAngle = angle;
                        _posStored = true;
                    }
                    DS1ImGui.SameLine();

                    nint mapData = ChrMapData;
                    bool canWarp = _posStored && mapData != 0;
                    if (!canWarp) DS1ImGui.TextDisabled("(store first)");
                    else if (DS1ImGui.Button("Teleport to Stored"))
                    {
                        GameMemory.Write(mapData + MAP_WARPX_OFF,     _storedX);
                        GameMemory.Write(mapData + MAP_WARPY_OFF,     _storedY);
                        GameMemory.Write(mapData + MAP_WARPZ_OFF,     _storedZ);
                        GameMemory.Write(mapData + MAP_WARPANGLE_OFF, _storedAngle);
                        GameMemory.Write<byte>(mapData + MAP_WARP_OFF, 1);
                    }

                    if (_posStored)
                        DS1ImGui.Text($"Stored: ({_storedX:F2}, {_storedY:F2}, {_storedZ:F2})");
                }
            }

            if (DS1ImGui.CollapsingHeader("Movement / Camera"))
            {
                nint w = WorldChrMan;
                if (w != 0)
                {
                    bool deathCam = GameMemory.Read<byte>(w + WCM_DEATHCAM_OFF) != 0;
                    if (DS1ImGui.Checkbox("Death Cam", ref deathCam))
                        GameMemory.Write<byte>(w + WCM_DEATHCAM_OFF, deathCam ? (byte)1 : (byte)0);
                }

                ChrFlag1Checkbox("No Gravity", chr, F1_NoGravity);

                DS1ImGui.Checkbox("Override Anim Speed", ref _speedEnabled);
                if (_speedEnabled)
                {
                    nint anim = ChrAnimData;
                    if (anim != 0)
                    {
                        DS1ImGui.SetNextItemWidth(200f);
                        DS1ImGui.SliderFloat("##speed", ref _animSpeedVal, 0f, 5f, "%.2f×");
                        GameMemory.Write(anim + ANIM_SPEED_OFF, _animSpeedVal);
                    }
                    else DS1ImGui.TextDisabled("ChrAnimData null");
                }
                else
                {
                    // Reset to 1 when unchecked
                    nint anim = ChrAnimData;
                    if (anim != 0) GameMemory.Write(anim + ANIM_SPEED_OFF, 1.0f);
                }
            }
        }
    }

    // ── Stats tab ────────────────────────────────────────────────────────────

    private void DrawStatsTab()
    {
        nint pgd = PlayerGameData;
        DS1ImGui.Spacing();

        if (pgd == 0)
        {
            DS1ImGui.TextDisabled("Player game data not loaded.");
            return;
        }

        int sl   = GameMemory.Read<int>(pgd + PGD_SOULLEVEL);
        int souls = GameMemory.Read<int>(pgd + PGD_SOULS);
        int hum  = GameMemory.Read<int>(pgd + PGD_HUMANITY);

        DS1ImGui.Text($"Soul Level:  {sl}");
        DS1ImGui.Spacing();

        if (DS1ImGui.CollapsingHeader("Souls / Humanity", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DS1ImGui.SetNextItemWidth(160f);
            if (DS1ImGui.InputInt("Souls", ref souls))
                GameMemory.Write(pgd + PGD_SOULS, souls);

            DS1ImGui.SetNextItemWidth(160f);
            if (DS1ImGui.InputInt("Humanity", ref hum))
                GameMemory.Write(pgd + PGD_HUMANITY, hum);
        }

        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("Attributes (read-only)", ImGuiTreeNodeFlags.DefaultOpen))
        {
            int vit = GameMemory.Read<int>(pgd + PGD_VITALITY);
            int att = GameMemory.Read<int>(pgd + PGD_ATTUNEMENT);
            int end = GameMemory.Read<int>(pgd + PGD_ENDURANCE);
            int str = GameMemory.Read<int>(pgd + PGD_STRENGTH);
            int dex = GameMemory.Read<int>(pgd + PGD_DEXTERITY);
            int res = GameMemory.Read<int>(pgd + PGD_RESISTANCE);
            int intel = GameMemory.Read<int>(pgd + PGD_INTELLIGENCE);
            int fth = GameMemory.Read<int>(pgd + PGD_FAITH);

            DS1ImGui.Text($"Vit {vit,3}  Att {att,3}  End {end,3}  Str {str,3}");
            DS1ImGui.Text($"Dex {dex,3}  Res {res,3}  Int {intel,3}  Fth {fth,3}");
        }
    }

    // ── Items tab ────────────────────────────────────────────────────────────

    private void DrawItemsTab()
    {
        DS1ImGui.Spacing();

        bool playerLoaded = PlayerChr != 0;
        if (!playerLoaded)
        {
            DS1ImGui.TextDisabled("Load a save file to give items.");
            DS1ImGui.Spacing();
        }

        var cats = ItemDatabase.Categories;

        // ── Category selector ─────────────────────────────────────────────
        DS1ImGui.SetNextItemWidth(200f);
        string catName = cats[_itemCatIdx].Name;
        if (DS1ImGui.BeginCombo("Category", catName))
        {
            for (int i = 0; i < cats.Length; i++)
            {
                bool sel = (i == _itemCatIdx);
                if (DS1ImGui.Selectable(cats[i].Name, sel))
                {
                    _itemCatIdx    = i;
                    _itemSelIdx    = 0;
                    _itemUpgrade   = 0;
                    _itemInfusionIdx = 0;
                }
                if (sel) DS1ImGui.SetItemDefaultFocus();
            }
            DS1ImGui.EndCombo();
        }

        // ── Search filter ─────────────────────────────────────────────────
        DS1ImGui.SetNextItemWidth(200f);
        if (DS1ImGui.InputText("Search", _itemSearchBuf))
            _itemFilter = System.Text.Encoding.UTF8.GetString(_itemSearchBuf).TrimEnd('\0');

        // ── Build filtered list ───────────────────────────────────────────
        var allItems = cats[_itemCatIdx].Items;
        var filtered = string.IsNullOrEmpty(_itemFilter)
            ? allItems
            : System.Array.FindAll(allItems,
                i => i.Name.Contains(_itemFilter, StringComparison.OrdinalIgnoreCase));

        if (_itemSelIdx >= filtered.Length) _itemSelIdx = 0;

        // ── Scrollable item list ──────────────────────────────────────────
        DS1ImGui.BeginChild("##itemlist", 0f, 160f, true);
        for (int i = 0; i < filtered.Length; i++)
        {
            bool sel = (i == _itemSelIdx);
            if (DS1ImGui.Selectable(filtered[i].Name, sel))
            {
                _itemSelIdx    = i;
                _itemUpgrade   = 0;
                _itemInfusionIdx = 0;
            }
            if (sel) DS1ImGui.SetItemDefaultFocus();
        }
        DS1ImGui.EndChild();

        if (filtered.Length == 0)
        {
            DS1ImGui.TextDisabled("No items match.");
            return;
        }

        var item = filtered[_itemSelIdx];

        // ── Upgrade selector ──────────────────────────────────────────────
        bool hasPyro = item.UpgradeType == 5 || item.UpgradeType == 6;
        bool hasInfusion = item.UpgradeType == 3 || item.UpgradeType == 4;
        bool hasUpgrade  = item.UpgradeType != 0;

        int maxUpgrade = item.UpgradeType switch
        {
            1 => 5,
            2 => 10,
            5 => 15,
            6 => 5,
            3 or 4 => ItemDatabase.Infusions[_itemInfusionIdx].MaxUpgrade,
            _ => 0
        };

        if (hasUpgrade)
        {
            DS1ImGui.SetNextItemWidth(200f);
            DS1ImGui.SliderInt("Upgrade", ref _itemUpgrade, 0, maxUpgrade);
        }

        // ── Infusion selector ─────────────────────────────────────────────
        if (hasInfusion)
        {
            var infusions = ItemDatabase.Infusions;
            // InfusableRestricted (4) = only non-restricted infusions
            var avail = item.UpgradeType == 4
                ? System.Array.FindAll(infusions, inf => !inf.Restricted)
                : infusions;

            if (_itemInfusionIdx >= avail.Length) _itemInfusionIdx = 0;

            DS1ImGui.SetNextItemWidth(120f);
            if (DS1ImGui.BeginCombo("Infusion", avail[_itemInfusionIdx].Name))
            {
                for (int i = 0; i < avail.Length; i++)
                {
                    bool s = (i == _itemInfusionIdx);
                    if (DS1ImGui.Selectable(avail[i].Name, s))
                    {
                        _itemInfusionIdx = i;
                        _itemUpgrade = Math.Min(_itemUpgrade, avail[i].MaxUpgrade);
                    }
                    if (s) DS1ImGui.SetItemDefaultFocus();
                }
                DS1ImGui.EndCombo();
            }
        }

        // ── Quantity ──────────────────────────────────────────────────────
        int maxQty = item.StackLimit > 1 ? item.StackLimit : 1;
        if (_itemQuantity < 1) _itemQuantity = 1;
        if (_itemQuantity > maxQty) _itemQuantity = maxQty;
        DS1ImGui.SetNextItemWidth(120f);
        DS1ImGui.InputInt("Quantity", ref _itemQuantity);
        _itemQuantity = Math.Clamp(_itemQuantity, 1, maxQty);

        // ── Compute final item ID ─────────────────────────────────────────
        int finalId = item.Id;
        if (hasPyro)
            finalId += _itemUpgrade * 100;
        else
            finalId += _itemUpgrade;

        if (hasInfusion)
        {
            var avail = item.UpgradeType == 4
                ? System.Array.FindAll(ItemDatabase.Infusions, inf => !inf.Restricted)
                : ItemDatabase.Infusions;
            if (_itemInfusionIdx < avail.Length)
                finalId += avail[_itemInfusionIdx].Value;
        }

        DS1ImGui.TextDisabled($"ID: 0x{finalId:X}  Category: 0x{cats[_itemCatIdx].CategoryId:X8}");

        // ── Give button ───────────────────────────────────────────────────
        DS1ImGui.Spacing();
        if (!playerLoaded)
        {
            DS1ImGui.TextDisabled("[Give Item]  (load a save first)");
        }
        else if (DS1ImGui.Button("Give Item", 120f, 0f))
        {
            bool ok = ItemGiver.GiveItem(
                _chrClassTarget,
                cats[_itemCatIdx].CategoryId,
                finalId,
                _itemQuantity);
            if (!ok)
                Console.WriteLine("[DebugMenu/Items] GiveItem failed — item-get function not found or player not loaded.");
        }
    }

    // ── World / AI tab ───────────────────────────────────────────────────────

    private void DrawWorldTab()
    {
        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("All Characters", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ChrDbgCheckbox("All No Dead",      CDBG_AllNoDead);
            ChrDbgCheckbox("All No Damage",    CDBG_AllNoDamage);
            ChrDbgCheckbox("All No Hit",       CDBG_AllNoHit);
            ChrDbgCheckbox("All No Attack",    CDBG_AllNoAttack);
            ChrDbgCheckbox("All No Move",      CDBG_AllNoMove);
            ChrDbgCheckbox("All No Update AI", CDBG_AllNoUpdateAI);
        }

        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("WorldChrManDbg"))
        {
            nint dbg = GameMemory.Read<nint>(GameMemory.ModuleBase + (nint)RVA_WorldChrManDbgPtr);
            if (dbg == 0) DS1ImGui.TextDisabled("Not loaded");
            else
            {
                PtrFlagCheckbox("All Draw Hitboxes",    dbg, WCMD_AllDrawHit);
                PtrFlagCheckbox("New Knockback Mode",   dbg, WCMD_NewKnockBackMode);
            }
        }
    }

    // ── Graphics tab ─────────────────────────────────────────────────────────

    private void DrawGraphicsTab()
    {
        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("Draw Groups", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (_groupMaskBase == 0)
            {
                DS1ImGui.TextDisabled("GroupMask not found (AOB scan failed)");
            }
            else
            {
                GroupMaskCheckbox("Draw Map",        GM_MAP);
                GroupMaskCheckbox("Draw Objects",    GM_OBJECTS);
                GroupMaskCheckbox("Draw Characters", GM_CHARACTERS);
                GroupMaskCheckbox("Draw SFX",        GM_SFX);
                GroupMaskCheckbox("Draw Cutscenes",  GM_CUTSCENES);
            }
        }

        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("Post-Process Filter"))
        {
            nint gfx = GraphicsData;
            if (gfx == 0)
            {
                DS1ImGui.TextDisabled("GraphicsData not loaded");
            }
            else
            {
                if (DS1ImGui.Checkbox("Filter Override", ref _filterEnabled))
                    GameMemory.Write<byte>(gfx + GFX_FILTER_OVERRIDE, _filterEnabled ? (byte)1 : (byte)0);

                if (_filterEnabled)
                {
                    DS1ImGui.SetNextItemWidth(240f);
                    if (DS1ImGui.SliderFloat("Brightness R", ref _filterBrR, 0f, 2f, "%.2f"))
                        GameMemory.Write(gfx + GFX_BRIGHTNESS_R, _filterBrR);
                    DS1ImGui.SetNextItemWidth(240f);
                    if (DS1ImGui.SliderFloat("Brightness G", ref _filterBrG, 0f, 2f, "%.2f"))
                        GameMemory.Write(gfx + GFX_BRIGHTNESS_G, _filterBrG);
                    DS1ImGui.SetNextItemWidth(240f);
                    if (DS1ImGui.SliderFloat("Brightness B", ref _filterBrB, 0f, 2f, "%.2f"))
                        GameMemory.Write(gfx + GFX_BRIGHTNESS_B, _filterBrB);

                    DS1ImGui.Separator();
                    DS1ImGui.SetNextItemWidth(240f);
                    if (DS1ImGui.SliderFloat("Contrast R",   ref _filterCtR, 0f, 2f, "%.2f"))
                        GameMemory.Write(gfx + GFX_CONTRAST_R, _filterCtR);
                    DS1ImGui.SetNextItemWidth(240f);
                    if (DS1ImGui.SliderFloat("Contrast G",   ref _filterCtG, 0f, 2f, "%.2f"))
                        GameMemory.Write(gfx + GFX_CONTRAST_G, _filterCtG);
                    DS1ImGui.SetNextItemWidth(240f);
                    if (DS1ImGui.SliderFloat("Contrast B",   ref _filterCtB, 0f, 2f, "%.2f"))
                        GameMemory.Write(gfx + GFX_CONTRAST_B, _filterCtB);

                    DS1ImGui.Separator();
                    DS1ImGui.SetNextItemWidth(240f);
                    if (DS1ImGui.SliderFloat("Saturation",   ref _filterSat,  0f, 2f, "%.2f"))
                        GameMemory.Write(gfx + GFX_SATURATION, _filterSat);
                    DS1ImGui.SetNextItemWidth(240f);
                    if (DS1ImGui.SliderFloat("Hue",          ref _filterHue, -1f, 1f, "%.2f"))
                        GameMemory.Write(gfx + GFX_HUE, _filterHue);

                    DS1ImGui.Spacing();
                    if (DS1ImGui.Button("Reset Filter"))
                    {
                        _filterBrR = _filterBrG = _filterBrB = 1f;
                        _filterCtR = _filterCtG = _filterCtB = 1f;
                        _filterSat = 1f; _filterHue = 0f;
                        GameMemory.Write(gfx + GFX_BRIGHTNESS_R, 1f);
                        GameMemory.Write(gfx + GFX_BRIGHTNESS_G, 1f);
                        GameMemory.Write(gfx + GFX_BRIGHTNESS_B, 1f);
                        GameMemory.Write(gfx + GFX_CONTRAST_R,   1f);
                        GameMemory.Write(gfx + GFX_CONTRAST_G,   1f);
                        GameMemory.Write(gfx + GFX_CONTRAST_B,   1f);
                        GameMemory.Write(gfx + GFX_SATURATION,   1f);
                        GameMemory.Write(gfx + GFX_HUE,          0f);
                    }
                }
            }
        }
    }

    // ── Game tab ─────────────────────────────────────────────────────────────

    private void DrawGameTab()
    {
        nint gm = GameMemory.Read<nint>(GameMemory.ModuleBase + (nint)RVA_GameManPtr);

        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("Hitbox Visualisation"))
        {
            if (gm == 0) DS1ImGui.TextDisabled("GameMan not loaded");
            else
            {
                PtrFlagCheckbox("Map Model Hitboxes",  gm, GM_HitDispMapModel);
                PtrFlagCheckbox("Lo-Hit Hitboxes",     gm, GM_HitDispLoHit);
                PtrFlagCheckbox("Hi-Hit Hitboxes",     gm, GM_HitDispHiHit);
                PtrFlagCheckbox("Disable Hi-Hit Draw", gm, GM_DisableAllAreaHiHit);
                PtrFlagCheckbox("Enable Lo-Hit Draw",  gm, GM_EnableAllAreaLoHit);
            }
        }

        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("Area Toggles"))
        {
            if (gm == 0) DS1ImGui.TextDisabled("GameMan not loaded");
            else
            {
                PtrFlagCheckbox("Disable All Enemies", gm, GM_DisableAllAreaEne);
                PtrFlagCheckbox("Disable All Events",  gm, GM_DisableAllAreaEvt);
                PtrFlagCheckbox("Disable All Map",     gm, GM_DisableAllAreaMap);
                PtrFlagCheckbox("Disable All Objects", gm, GM_DisableAllAreaObj);
                PtrFlagCheckbox("Enable All Objects",  gm, GM_EnableAllAreaObj);
                PtrFlagCheckbox("Disable All SFX",     gm, GM_DisableAllAreaSfx);
                PtrFlagCheckbox("Disable All Sound",   gm, GM_DisableAllAreaSnd);
            }
        }

        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("System"))
        {
            if (gm == 0) DS1ImGui.TextDisabled("GameMan not loaded");
            else
            {
                PtrFlagCheckbox("Save Mode",   gm, GM_IsSaveMode);
                PtrFlagCheckbox("Online Mode", gm, GM_IsOnlineMode);
                PtrFlagCheckbox("No Input",    gm, GM_IsNoInput);
            }
        }
    }

    // ── Misc tab ─────────────────────────────────────────────────────────────

    private void DrawMiscTab()
    {
        DS1ImGui.Spacing();
        if (DS1ImGui.CollapsingHeader("Event Flag Editor", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DS1ImGui.SetNextItemWidth(160f);
            DS1ImGui.InputInt("Flag ID", ref _eventFlagId, 1, 1000);

            if (DS1ImGui.Button("Read Flag"))
                _eventFlagVal = EventFlags.Get(_eventFlagId);
            DS1ImGui.SameLine();
            DS1ImGui.Checkbox("##efval", ref _eventFlagVal);
            DS1ImGui.SameLine();
            if (DS1ImGui.Button("Write Flag"))
                EventFlags.Set(_eventFlagId, _eventFlagVal);

            DS1ImGui.TextDisabled("Format: 8 digits, e.g. 11510000");
        }
    }

    // ── Info tab ─────────────────────────────────────────────────────────────

    private void DrawInfoTab()
    {
        DS1ImGui.Spacing();
        DS1ImGui.Text($"ImGui  {DS1ImGui.GetVersion()}");
        DS1ImGui.Text($"FPS    {DS1ImGui.GetFramerate():F1}");
        DS1ImGui.Spacing();
        DS1ImGui.Text($"Module 0x{(ulong)GameMemory.ModuleBase:X}");
        DS1ImGui.Text($"Size   0x{GameMemory.ModuleSize:X}");
        DS1ImGui.Text($"Boost1 0x{_boost1:X}  Boost2 0x{_boost2:X}  WarpBoost 0x{_warpBoost:X}");
        DS1ImGui.Spacing();
        DS1ImGui.Text($"WorldChrMan   0x{(ulong)WorldChrMan:X}");
        DS1ImGui.Text($"PlayerChr     0x{(ulong)PlayerChr:X}");
        DS1ImGui.Text($"PlayerGD      0x{(ulong)PlayerGameData:X}");
        DS1ImGui.Text($"ChrClassWarp  0x{(ulong)ChrClassWarp:X}");
        DS1ImGui.Text($"GraphicsData  0x{(ulong)GraphicsData:X}");
        DS1ImGui.Spacing();
        DS1ImGui.TextDisabled("horkrux/DSR-ImGuiDebugMenu  +  JKAnderson/DSR-Gadget");
        DS1ImGui.TextDisabled("Direct in-process reads/writes — no RPM overhead.");
    }

    // ── Widget helpers ────────────────────────────────────────────────────────

    /// <summary>Checkbox for a ChrDbg static flag (module-base + RVA + index).</summary>
    private static void ChrDbgCheckbox(string label, int index)
    {
        nint addr = GameMemory.ModuleBase + (nint)RVA_ChrDbg + index;
        bool val  = GameMemory.Read<byte>(addr) != 0;
        if (DS1ImGui.Checkbox(label, ref val))
            GameMemory.Write<byte>(addr, val ? (byte)1 : (byte)0);
    }

    /// <summary>Checkbox for a byte flag at (basePtr + offset).</summary>
    private static void PtrFlagCheckbox(string label, nint basePtr, int offset)
    {
        nint addr = basePtr + offset;
        bool val  = GameMemory.Read<byte>(addr) != 0;
        if (DS1ImGui.Checkbox(label, ref val))
            GameMemory.Write<byte>(addr, val ? (byte)1 : (byte)0);
    }

    /// <summary>Checkbox for a bit in ChrData1.ChrFlags1 (uint32, with boost1).</summary>
    private static void ChrFlag1Checkbox(string label, nint chr, uint flag)
    {
        nint addr = chr + CHR_FLAGS1_OFF + _boost1;
        uint val  = GameMemory.Read<uint>(addr);
        bool set  = (val & flag) != 0;
        if (DS1ImGui.Checkbox(label, ref set))
            GameMemory.Write(addr, set ? val | flag : val & ~flag);
    }

    /// <summary>Checkbox for a bit in ChrData1.ChrFlags2 (uint32, with boost2).</summary>
    private static void ChrFlag2Checkbox(string label, nint chr, uint flag)
    {
        nint addr = chr + CHR_FLAGS2_OFF + _boost2;
        uint val  = GameMemory.Read<uint>(addr);
        bool set  = (val & flag) != 0;
        if (DS1ImGui.Checkbox(label, ref set))
            GameMemory.Write(addr, set ? val | flag : val & ~flag);
    }

    /// <summary>Checkbox for a GroupMask draw flag (flat byte array indexed by GM_*).</summary>
    private static void GroupMaskCheckbox(string label, int index)
    {
        nint addr = _groupMaskBase + index;
        bool val  = GameMemory.Read<byte>(addr) != 0;
        if (DS1ImGui.Checkbox(label, ref val))
            GameMemory.Write<byte>(addr, val ? (byte)1 : (byte)0);
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
