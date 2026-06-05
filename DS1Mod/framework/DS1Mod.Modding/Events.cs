using SoulsFormats;
using ArgType = SoulsFormats.EMEVD.Instruction.ArgType;

namespace DS1Mod.Modding;

// ── Enums ─────────────────────────────────────────────────────────────────────

public enum FlagState    : byte  { Off = 0, On = 1 }
public enum LifeState    : byte  { Dead = 0, Alive = 1 }
public enum EnabledState : byte  { Disabled = 0, Enabled = 1 }

public enum ComparisonType    : byte  { Equal=0, NotEqual=1, Greater=2, Less=3, GreaterOrEqual=4, LessOrEqual=5 }
public enum TargetFlagType    : byte  { EventFlag=0 }
public enum LogicType         : byte  { AllON=0, AllOFF=1, AnyON=2, AnyOFF=3 }
public enum InsideOutside     : byte  { Outside=0, Inside=1 }
public enum ItemType          : byte  { Weapon=0, Armor=1, Ring=2, Goods=3 }
public enum TeamType          : byte  { Default=255, None=0, Human=1, WhitePhantom=2, BlackPhantom=3, Hollow=4, Enemy=6, StrongEnemy=7, Ally=8, HostileAlly=9, FriendlyEnemy=12 }
public enum CharacterSpecialEffect : byte { None=0, Immortal=1 }
public enum NPCPartType       : byte  { Body=0, Arm=1, Leg=2, Head=3, Tail=4 }
public enum AIStateType       : byte  { Roaming=0, Combat=1, Idle=2 }
public enum MultiplayerState  : sbyte { Offline=-1, Online=0 }
public enum ActionButtonState : byte  { Hide=0, Show=1 }
public enum FogGateState      : byte  { None=0, Normal=1 }
public enum HealthBarType     : byte  { None=0, BossBar=1 }
public enum TendencyType      : byte  { World=0, PlayerBlack=1, PlayerWhite=2 }
public enum SoundType         : byte  { SE=0, BGM=1 }
public enum NavimeshType      : uint  { Solid=1, Exit=2, Obstacle=4, Wall=8, LandingPoint=64 }
public enum WarpType          : byte  { Object=0, Area=1, Character=2 }

// ── Instr factory ─────────────────────────────────────────────────────────────

/// <summary>
/// Factory + matchers for EMEVD instructions, so you write
/// <c>Instr.DisplayMessage(123)</c> instead of remembering bank/id/arg-widths.
/// Arg widths must match the EMEDF: use the exact boxed types below.
/// </summary>
public static class Instr
{
    /// <summary>Any instruction by bank/id with explicit args (widths inferred from the boxed types).</summary>
    public static EMEVD.Instruction Raw(int bank, int id, params object[] args) => new(bank, id, args.ToList());

    // ── control flow ──────────────────────────────────────────────────────────

    /// <summary>2000:0 — start an event from the constructor.</summary>
    public static EMEVD.Instruction InitializeEvent(long eventId, int slot = 0) =>
        new(2000, 0, new List<object> { slot, (uint)eventId, (uint)0 });

    /// <summary>2000:2 — initialize a common event with args.</summary>
    public static EMEVD.Instruction InitializeCommonEvent(int slot, uint commonEventId, uint arg) =>
        new(2000, 2, new List<object> { slot, commonEventId, arg });

    /// <summary>1000:4 — unconditional end (EndType 0) or restart (EndType 1).</summary>
    public static EMEVD.Instruction EndUnconditionally(byte endType = 0) =>
        new(1000, 4, new List<object> { endType });

    // ── wait instructions (bank 2002) ─────────────────────────────────────────

    /// <summary>2002:6 — wait a fixed number of seconds.</summary>
    public static EMEVD.Instruction WaitSeconds(float seconds) =>
        new(2002, 6, new List<object> { seconds });

    /// <summary>2002:7 — wait a fixed number of frames.</summary>
    public static EMEVD.Instruction WaitFrames(short frames) =>
        new(2002, 7, new List<object> { frames });

    /// <summary>2002:8 — wait a random duration in seconds.</summary>
    public static EMEVD.Instruction WaitRandomSeconds(float min, float max) =>
        new(2002, 8, new List<object> { min, max });

    /// <summary>2002:9 — wait a random number of frames.</summary>
    public static EMEVD.Instruction WaitRandomFrames(short min, short max) =>
        new(2002, 9, new List<object> { min, max });

    // ── conditions ────────────────────────────────────────────────────────────

    /// <summary>0:0 — check a sub-condition group into a result group (AND/OR composition).</summary>
    public static EMEVD.Instruction IfConditionGroup(sbyte resultGroup, byte desired, sbyte targetGroup) =>
        new(0, 0, new List<object> { resultGroup, desired, targetGroup });

    /// <summary>0:1 — IF parameter comparison (short lhs vs short rhs).</summary>
    public static EMEVD.Instruction IfParamComparison(int condGroup, ComparisonType compType, short lhs, short rhs) =>
        new(0, 1, new List<object> { condGroup, (int)compType, lhs, rhs });

    /// <summary>1:0 — IF elapsed seconds.</summary>
    public static EMEVD.Instruction IfElapsedSeconds(int condGroup, sbyte seconds) =>
        new(1, 0, new List<object> { condGroup, seconds });

    /// <summary>1:1 — IF elapsed frames.</summary>
    public static EMEVD.Instruction IfElapsedFrames(int condGroup, short frames) =>
        new(1, 1, new List<object> { condGroup, frames });

    /// <summary>1:2 — IF random elapsed seconds.</summary>
    public static EMEVD.Instruction IfRandomElapsedSeconds(int condGroup, sbyte min, sbyte max) =>
        new(1, 2, new List<object> { condGroup, min, max });

    /// <summary>1:3 — IF random elapsed frames.</summary>
    public static EMEVD.Instruction IfRandomElapsedFrames(int condGroup, short min, short max) =>
        new(1, 3, new List<object> { condGroup, min, max });

    /// <summary>3:0 — block until an event flag reaches <paramref name="state"/>.</summary>
    public static EMEVD.Instruction IfEventFlag(FlagState state, int flagId, sbyte condGroup = 0) =>
        new(3, 0, new List<object> { condGroup, (byte)state, (byte)0, flagId });

    /// <summary>3:1 — IF batch event flags match a logic state.</summary>
    public static EMEVD.Instruction IfBatchEventFlags(int condGroup, LogicType logicState, int startFlag, int endFlag) =>
        new(3, 1, new List<object> { condGroup, (byte)logicState, (byte)0, startFlag, endFlag });

    /// <summary>3:2 — block until entity is inside (desired=1) or outside (desired=0) an area.</summary>
    public static EMEVD.Instruction IfInsideArea(int entityId, int areaEntityId, byte desired = 1, sbyte condGroup = 0) =>
        new(3, 2, new List<object> { condGroup, desired, entityId, areaEntityId });

    /// <summary>3:3 — IF entity A is within dist of entity B.</summary>
    public static EMEVD.Instruction IfEntityInRadius(int condGroup, byte desired, int entityA, int entityB, sbyte dist) =>
        new(3, 3, new List<object> { condGroup, desired, (short)entityA, (short)entityB, dist });

    /// <summary>3:4 — block until player has (desired=1) or lacks (desired=0) an item.</summary>
    public static EMEVD.Instruction IfPlayerHasItem(int itemType, int itemId, byte desired = 1, sbyte condGroup = 0) =>
        new(3, 4, new List<object> { condGroup, itemType, itemId, desired });

    /// <summary>3:5 — IF character has/hasn't a tag.</summary>
    public static EMEVD.Instruction IfCharacterHasTag(int condGroup, int entityId, int tagId, byte desired) =>
        new(3, 5, new List<object> { condGroup, entityId, tagId, desired });

    /// <summary>3:6 — IF a map is loaded.</summary>
    public static EMEVD.Instruction IfMapLoaded(int condGroup, byte areaId, byte blockId, byte desired) =>
        new(3, 6, new List<object> { condGroup, areaId, blockId, desired });

    /// <summary>3:7 — IF distance between two entities meets a comparison.</summary>
    public static EMEVD.Instruction IfEntityDistanceComparison(int condGroup, int entityA, int entityB, ComparisonType compType, float dist) =>
        new(3, 7, new List<object> { condGroup, entityA, entityB, (byte)compType, dist });

    /// <summary>3:8 — IF a random percent chance triggers.</summary>
    public static EMEVD.Instruction IfRandomPercent(int condGroup, float percentChance) =>
        new(3, 8, new List<object> { condGroup, percentChance });

    /// <summary>4:0 — block until a character is dead or alive.</summary>
    public static EMEVD.Instruction IfCharacterDeadAlive(LifeState state, int entityId, sbyte condGroup = 0) =>
        new(4, 0, new List<object> { condGroup, entityId, (byte)state });

    /// <summary>4:1 — IF character is a given type (byte charType).</summary>
    public static EMEVD.Instruction IfCharacterType(int condGroup, int entityId, byte charType) =>
        new(4, 1, new List<object> { condGroup, entityId, charType });

    /// <summary>4:2 — block until entity HP ratio meets a comparison.</summary>
    public static EMEVD.Instruction IfHpRatio(int entityId, sbyte compType, float ratio, sbyte condGroup = 0) =>
        new(4, 2, new List<object> { condGroup, entityId, compType, ratio });

    /// <summary>4:3 — IF character has a status effect.</summary>
    public static EMEVD.Instruction IfCharacterStatusEffect(int condGroup, int entityId, int spEffectId) =>
        new(4, 3, new List<object> { condGroup, entityId, spEffectId });

    /// <summary>4:4 — IF character part HP ratio meets a comparison.</summary>
    public static EMEVD.Instruction IfCharacterPartHpRatio(int condGroup, int entityId, byte partId, ComparisonType compType, float ratio) =>
        new(4, 4, new List<object> { condGroup, entityId, partId, (byte)compType, ratio });

    /// <summary>4:5 — block until a character has/lacks a SpEffect.</summary>
    public static EMEVD.Instruction IfCharacterHasSpEffect(int entityId, int spEffectId, bool shouldHave = true, sbyte condGroup = 0) =>
        new(4, 5, new List<object> { condGroup, entityId, spEffectId, (byte)(shouldHave ? 1 : 0) });

    /// <summary>4:6 — IF character is targeting a specific entity.</summary>
    public static EMEVD.Instruction IfCharacterTargetEntity(int condGroup, int entityId, int targetId, byte desired) =>
        new(4, 6, new List<object> { condGroup, entityId, targetId, desired });

    /// <summary>4:7 — IF character has (or lacks) a talisman.</summary>
    public static EMEVD.Instruction IfCharacterHasTalisman(int condGroup, int entityId, int talismanId, byte desired) =>
        new(4, 7, new List<object> { condGroup, entityId, talismanId, desired });

    /// <summary>4:8 — IF character HP value meets a comparison.</summary>
    public static EMEVD.Instruction IfCharacterHpValue(int condGroup, int entityId, ComparisonType compType, int hp) =>
        new(4, 8, new List<object> { condGroup, entityId, (byte)compType, hp });

    /// <summary>4:9 — IF character alive flag matches desired.</summary>
    public static EMEVD.Instruction IfCharacterAliveFlag(int condGroup, int entityId, byte desired) =>
        new(4, 9, new List<object> { condGroup, entityId, desired });

    /// <summary>5:0 — IF object is destroyed or intact.</summary>
    public static EMEVD.Instruction IfObjectDestroyed(int condGroup, int entityId, byte desired) =>
        new(5, 0, new List<object> { condGroup, entityId, desired });

    /// <summary>11:0 — IF map tendency meets a comparison.</summary>
    public static EMEVD.Instruction IfMapTendency(int condGroup, byte areaId, byte blockId, ComparisonType compType, short tendency) =>
        new(11, 0, new List<object> { condGroup, areaId, blockId, (byte)compType, tendency });

    /// <summary>11:2 — IF new game (NG+) cycle meets a comparison.</summary>
    public static EMEVD.Instruction IfNewGameComparison(int condGroup, ComparisonType compType, byte cycle) =>
        new(11, 2, new List<object> { condGroup, (byte)compType, cycle });

    /// <summary>11:3 — IF multiplayer state matches.</summary>
    public static EMEVD.Instruction IfMultiplayerState(int condGroup, byte stateId, byte desired) =>
        new(11, 3, new List<object> { condGroup, stateId, desired });

    /// <summary>11:4 — IF covenant state matches.</summary>
    public static EMEVD.Instruction IfCovenantState(int condGroup, byte covenantId, byte desired) =>
        new(11, 4, new List<object> { condGroup, covenantId, desired });

    // ── event flags ───────────────────────────────────────────────────────────

    /// <summary>2003:2 — set an event flag on/off.</summary>
    public static EMEVD.Instruction SetEventFlag(int flagId, FlagState state) =>
        new(2003, 2, new List<object> { flagId, (byte)state });

    /// <summary>2003:3 — store event flag to a slot.</summary>
    public static EMEVD.Instruction StoreEventFlag(int flagId, byte slot) =>
        new(2003, 3, new List<object> { flagId, slot });

    /// <summary>2003:8 — set event flag by slot.</summary>
    public static EMEVD.Instruction SetEventFlagBySlot(byte slot, byte state) =>
        new(2003, 8, new List<object> { slot, state });

    /// <summary>2003:19 — set a range of event flags.</summary>
    public static EMEVD.Instruction SetEventFlagRange(byte flagType, int startFlag, int endFlag, byte state) =>
        new(2003, 19, new List<object> { flagType, startFlag, endFlag, state });

    /// <summary>2003:30 — set an event flag group state.</summary>
    public static EMEVD.Instruction SetEventFlagGroup(int groupId, byte state) =>
        new(2003, 30, new List<object> { groupId, state });

    // ── items ─────────────────────────────────────────────────────────────────

    /// <summary>2003:4 — give the player an item lot.</summary>
    public static EMEVD.Instruction AwardItemLot(int itemLotId) =>
        new(2003, 4, new List<object> { itemLotId });

    /// <summary>2003:37 — award item lot by entity id.</summary>
    public static EMEVD.Instruction AwardItemLotById(int entityId, int itemLotId) =>
        new(2003, 37, new List<object> { entityId, itemLotId });

    // ── display ───────────────────────────────────────────────────────────────

    /// <summary>2007:0 — display a subtitle message.</summary>
    public static EMEVD.Instruction DisplaySubtitle(int messageId, byte pad = 0) =>
        new(2007, 0, new List<object> { messageId, pad });

    /// <summary>2007:1 — enable or disable a message.</summary>
    public static EMEVD.Instruction SetMessageEnabled(int messageId, byte state) =>
        new(2007, 1, new List<object> { messageId, state });

    /// <summary>2007:2 — the big banner.</summary>
    public static EMEVD.Instruction DisplayBanner(byte bannerType) =>
        new(2007, 2, new List<object> { bannerType });

    /// <summary>2007:3 — status/explanation text box.</summary>
    public static EMEVD.Instruction DisplayStatusMessage(int messageId, bool pad = false) =>
        new(2007, 3, new List<object> { messageId, (byte)(pad ? 1 : 0) });

    /// <summary>2007:4 — pop a centered on-screen message.</summary>
    public static EMEVD.Instruction DisplayMessage(int messageId, byte screenLocation = 0) =>
        new(2007, 4, new List<object> { messageId, screenLocation });

    /// <summary>2007:5 — set runtime message text.</summary>
    public static EMEVD.Instruction SetRuntimeMessageText(int messageId, int runtimeTextId) =>
        new(2007, 5, new List<object> { messageId, runtimeTextId });

    // ── cutscene / warp ───────────────────────────────────────────────────────

    /// <summary>2003:1 — play a cutscene.</summary>
    public static EMEVD.Instruction PlayCutscene(uint cutsceneId, int skipEventFlagId, short cameraAnimId, int cameraEntranceId) =>
        new(2003, 1, new List<object> { cutsceneId, skipEventFlagId, cameraAnimId, cameraEntranceId });

    /// <summary>2003:6 — warp player to an entity.</summary>
    public static EMEVD.Instruction Warp(int destEntityId, WarpType warpType = WarpType.Object) =>
        new(2003, 6, new List<object> { destEntityId, (byte)warpType });

    /// <summary>2003:16 — warp player to a respawn point.</summary>
    public static EMEVD.Instruction WarpPlayerToRespawn(int respawnPointId) =>
        new(2003, 16, new List<object> { respawnPointId });

    /// <summary>2003:31 — set the respawn point.</summary>
    public static EMEVD.Instruction SetRespawnPoint(int respawnPointId) =>
        new(2003, 31, new List<object> { respawnPointId });

    // ── character actions (2003 bank) ─────────────────────────────────────────

    /// <summary>2003:0 — handle miniboss defeat.</summary>
    public static EMEVD.Instruction HandleMinibossDefeat(int entityId) =>
        new(2003, 0, new List<object> { entityId });

    /// <summary>2003:5 — shoot a bullet from an entity.</summary>
    public static EMEVD.Instruction ShootBullet(int teamEntityId, int producerEntityId, int dummypolyId, int behaviorId, int angleX, int angleY, int angleZ) =>
        new(2003, 5, new List<object> { teamEntityId, producerEntityId, dummypolyId, behaviorId, angleX, angleY, angleZ });

    /// <summary>2003:7 — shake camera at an entity.</summary>
    public static EMEVD.Instruction ShakeCameraAtEntity(int entityId, int vibrationId, float radius, float fadeout) =>
        new(2003, 7, new List<object> { entityId, vibrationId, radius, fadeout });

    /// <summary>2003:9 — destroy an object.</summary>
    public static EMEVD.Instruction DestroyObject(int entityId, byte slot = 0) =>
        new(2003, 9, new List<object> { entityId, slot });

    /// <summary>2003:10 — set fog gate state.</summary>
    public static EMEVD.Instruction SetFogGateState(int fogGateEntityId, FogGateState state) =>
        new(2003, 10, new List<object> { fogGateEntityId, (byte)state });

    /// <summary>2003:11 — show/hide the boss HP bar.</summary>
    public static EMEVD.Instruction DisplayBossHealthBar(int entityId, EnabledState state,
        int slot = 0, short nameId = 0) =>
        new(2003, 11, new List<object> { (sbyte)state, entityId, (short)slot, nameId });

    /// <summary>2003:12 — trigger the boss-death sequence.</summary>
    public static EMEVD.Instruction HandleBossDefeat(int entityId) =>
        new(2003, 12, new List<object> { entityId });

    /// <summary>2003:14 — play a sound effect at an entity.</summary>
    public static EMEVD.Instruction PlaySoundEffect(int entityId, int soundId, byte soundType = 0) =>
        new(2003, 14, new List<object> { entityId, soundId, soundType });

    /// <summary>2003:15 — set navimesh type.</summary>
    public static EMEVD.Instruction SetNavimeshType(int navimeshEntityId, uint navimeshType, byte operation) =>
        new(2003, 15, new List<object> { navimeshEntityId, navimeshType, operation });

    /// <summary>2003:17 — kill an enemy.</summary>
    public static EMEVD.Instruction KillEnemy(int entityId, byte immediate = 0) =>
        new(2003, 17, new List<object> { entityId, immediate });

    /// <summary>2003:18 — force-play an animation on a character.</summary>
    public static EMEVD.Instruction ForceAnimation(int entityId, int animationId,
        bool loop = false, bool waitForCompletion = false, bool ignoreTransition = false) =>
        new(2003, 18, new List<object> { entityId, animationId,
            (byte)(loop ? 1 : 0), (byte)(waitForCompletion ? 1 : 0), (byte)(ignoreTransition ? 1 : 0) });

    /// <summary>2003:20 — enable an object.</summary>
    public static EMEVD.Instruction EnableObject(int entityId) =>
        new(2003, 20, new List<object> { entityId });

    /// <summary>2003:21 — disable an object.</summary>
    public static EMEVD.Instruction DisableObject(int entityId) =>
        new(2003, 21, new List<object> { entityId });

    /// <summary>2003:22 — create a multipart NPC.</summary>
    public static EMEVD.Instruction CreateMultipartNpc(int npcEntityId, short modelId, int npcParamId, int npcThinkParamId, byte teamId, int partId) =>
        new(2003, 22, new List<object> { npcEntityId, modelId, npcParamId, npcThinkParamId, teamId, partId });

    /// <summary>2003:23 — set NPC part HP.</summary>
    public static EMEVD.Instruction SetNpcPartHp(int entityId, byte partId, ushort hp) =>
        new(2003, 23, new List<object> { entityId, partId, hp });

    /// <summary>2003:24 — set NPC part SFX.</summary>
    public static EMEVD.Instruction SetNpcPartSfx(int entityId, byte partId, int sfxId) =>
        new(2003, 24, new List<object> { entityId, partId, sfxId });

    /// <summary>2003:25 — set NPC part bullet damage multiplier.</summary>
    public static EMEVD.Instruction SetNpcPartBulletMult(int entityId, byte partId, float mult) =>
        new(2003, 25, new List<object> { entityId, partId, mult });

    /// <summary>2003:26 — enable navimesh.</summary>
    public static EMEVD.Instruction SetNavimeshEnabled(int entityId, byte state) =>
        new(2003, 26, new List<object> { entityId, state });

    /// <summary>2003:27 — disable navimesh.</summary>
    public static EMEVD.Instruction SetNavimeshDisabled(int entityId, byte state) =>
        new(2003, 27, new List<object> { entityId, state });

    /// <summary>2003:28 — set bullet hitbox state.</summary>
    public static EMEVD.Instruction SetBulletHitbox(int entityId, byte state) =>
        new(2003, 28, new List<object> { entityId, state });

    /// <summary>2003:29 — force animation (DSR 4-arg variant).</summary>
    public static EMEVD.Instruction ForceAnimDsr1(int entityId, int animId, byte loop, byte wait) =>
        new(2003, 29, new List<object> { entityId, animId, loop, wait });

    /// <summary>2003:32 — set mortal state.</summary>
    public static EMEVD.Instruction SetMortalState(int entityId, byte state) =>
        new(2003, 32, new List<object> { entityId, state });

    /// <summary>2003:33 — unknown 2003:33.</summary>
    public static EMEVD.Instruction Unknown2003_33(int entityId, int unk) =>
        new(2003, 33, new List<object> { entityId, unk });

    /// <summary>2003:34 — unknown 2003:34.</summary>
    public static EMEVD.Instruction Unknown2003_34(byte area, byte block, byte unk) =>
        new(2003, 34, new List<object> { area, block, unk });

    /// <summary>2003:35 — place a bonfire warp point.</summary>
    public static EMEVD.Instruction PlaceBonfireWarpPoint(int bonfireEntityId, int animId) =>
        new(2003, 35, new List<object> { bonfireEntityId, animId });

    /// <summary>2003:36 — handle summon sign.</summary>
    public static EMEVD.Instruction HandleSummonSign(int signEntityId, int summonedEntityId) =>
        new(2003, 36, new List<object> { signEntityId, summonedEntityId });

    /// <summary>2003:40 — unknown 2003:40.</summary>
    public static EMEVD.Instruction Unknown2003_40(int unk) =>
        new(2003, 40, new List<object> { unk });

    /// <summary>2003:43 — set character talk range.</summary>
    public static EMEVD.Instruction SetCharacterTalkRange(int entityId, int rangeId) =>
        new(2003, 43, new List<object> { entityId, rangeId });

    /// <summary>2003:44 — force animation DSR variant A (with unk byte).</summary>
    public static EMEVD.Instruction ForceAnimDs1rA(int entityId, int animId, byte loop, byte wait, byte unk) =>
        new(2003, 44, new List<object> { entityId, animId, loop, wait, unk });

    /// <summary>2003:46 — force animation DSR variant B (with unk byte).</summary>
    public static EMEVD.Instruction ForceAnimDs1rB(int entityId, int animId, byte loop, byte wait, byte unk) =>
        new(2003, 46, new List<object> { entityId, animId, loop, wait, unk });

    // ── character actions (2004 bank) ─────────────────────────────────────────

    /// <summary>2004:0 — set a character's team type.</summary>
    public static EMEVD.Instruction SetTeamType(int entityId, TeamType teamType) =>
        new(2004, 0, new List<object> { entityId, (byte)teamType });

    /// <summary>2004:1 — enable or disable a character's AI.</summary>
    public static EMEVD.Instruction SetCharacterAI(int entityId, EnabledState state) =>
        new(2004, 1, new List<object> { entityId, (byte)state });

    /// <summary>2004:2 — set character look.</summary>
    public static EMEVD.Instruction SetCharacterLook(int entityId, int lookId) =>
        new(2004, 2, new List<object> { entityId, lookId });

    /// <summary>2004:3 — set character face param.</summary>
    public static EMEVD.Instruction SetCharacterFaceParam(int entityId, int faceParamId) =>
        new(2004, 3, new List<object> { entityId, faceParamId });

    /// <summary>2004:4 — force character death.</summary>
    public static EMEVD.Instruction KillCharacter(int entityId, bool awardSouls = false) =>
        new(2004, 4, new List<object> { entityId, (byte)(awardSouls ? 1 : 0) });

    /// <summary>2004:5 — enable or disable a character.</summary>
    public static EMEVD.Instruction SetCharacterEnabled(int entityId, EnabledState state) =>
        new(2004, 5, new List<object> { entityId, (byte)state });

    /// <summary>2004:6 — set NPC standby animation id.</summary>
    public static EMEVD.Instruction SetNpcStandbyAnimId(int entityId, int animId, byte slot = 0) =>
        new(2004, 6, new List<object> { entityId, animId, slot });

    /// <summary>2004:7 — set player respawn point.</summary>
    public static EMEVD.Instruction SetPlayerRespawnPoint(int respawnId) =>
        new(2004, 7, new List<object> { respawnId });

    /// <summary>2004:8 — apply a SpEffect to a character.</summary>
    public static EMEVD.Instruction SetSpEffect(int entityId, int spEffectId) =>
        new(2004, 8, new List<object> { entityId, spEffectId });

    /// <summary>2004:9 — set animation id on a character.</summary>
    public static EMEVD.Instruction SetAnimationId(int entityId, int animId) =>
        new(2004, 9, new List<object> { entityId, animId });

    /// <summary>2004:10 — set pathfinding state.</summary>
    public static EMEVD.Instruction SetPathfinding(int entityId, byte state) =>
        new(2004, 10, new List<object> { entityId, state });

    /// <summary>2004:11 — reset pathfinding for a character.</summary>
    public static EMEVD.Instruction ResetPathfinding(int entityId) =>
        new(2004, 11, new List<object> { entityId });

    /// <summary>2004:12 — make a character immortal (unkillable) or mortal.</summary>
    public static EMEVD.Instruction SetCharacterImmortal(int entityId, EnabledState state) =>
        new(2004, 12, new List<object> { entityId, (byte)state });

    /// <summary>2004:13 — set a character's home point to a region entity.</summary>
    public static EMEVD.Instruction SetCharacterHome(int entityId, int regionEntityId) =>
        new(2004, 13, new List<object> { entityId, regionEntityId });

    /// <summary>2004:15 — make a character invincible or vincible.</summary>
    public static EMEVD.Instruction SetCharacterInvincible(int entityId, EnabledState state) =>
        new(2004, 15, new List<object> { entityId, (byte)state });

    /// <summary>2004:16 — set character model.</summary>
    public static EMEVD.Instruction SetCharacterModel(int entityId, short modelId) =>
        new(2004, 16, new List<object> { entityId, modelId });

    /// <summary>2004:17 — set character NPC param.</summary>
    public static EMEVD.Instruction SetCharacterNpcParam(int entityId, int npcParamId) =>
        new(2004, 17, new List<object> { entityId, npcParamId });

    /// <summary>2004:18 — set character think param.</summary>
    public static EMEVD.Instruction SetCharacterThinkParam(int entityId, int thinkParamId) =>
        new(2004, 18, new List<object> { entityId, thinkParamId });

    /// <summary>2004:19 — set character event range.</summary>
    public static EMEVD.Instruction SetCharacterEventRange(int entityId, int rangeId) =>
        new(2004, 19, new List<object> { entityId, rangeId });

    /// <summary>2004:20 — set character talk range id.</summary>
    public static EMEVD.Instruction SetCharacterTalkRangeId(int entityId, int talkId) =>
        new(2004, 20, new List<object> { entityId, talkId });

    /// <summary>2004:21 — set follow entity.</summary>
    public static EMEVD.Instruction SetFollowEntity(int entityId, int targetId) =>
        new(2004, 21, new List<object> { entityId, targetId });

    /// <summary>2004:22 — create multipart NPC (legacy variant).</summary>
    public static EMEVD.Instruction CreateMultipartNpcLegacy(int npcEntityId, short modelId, int npcParamId, int npcThinkParamId, byte teamType, int npcPartId) =>
        new(2004, 22, new List<object> { npcEntityId, modelId, npcParamId, npcThinkParamId, teamType, npcPartId });

    /// <summary>2004:23 — set NPC part HP (variant 2).</summary>
    public static EMEVD.Instruction SetNpcPartHp2(int entityId, byte npcPartId, ushort hp) =>
        new(2004, 23, new List<object> { entityId, npcPartId, hp });

    /// <summary>2004:24 — set NPC part SFX (variant 2).</summary>
    public static EMEVD.Instruction SetNpcPartSfx2(int entityId, byte npcPartId, int sfxId) =>
        new(2004, 24, new List<object> { entityId, npcPartId, sfxId });

    /// <summary>2004:25 — set NPC part bullet damage multiplier (variant 2).</summary>
    public static EMEVD.Instruction SetNpcPartBulletMult2(int entityId, byte npcPartId, float mult) =>
        new(2004, 25, new List<object> { entityId, npcPartId, mult });

    /// <summary>2004:26 — set character AI id.</summary>
    public static EMEVD.Instruction SetCharacterAiId(int entityId, int aiId) =>
        new(2004, 26, new List<object> { entityId, aiId });

    /// <summary>2004:27 — set character event target.</summary>
    public static EMEVD.Instruction SetCharacterEventTarget(int entityId, int targetId) =>
        new(2004, 27, new List<object> { entityId, targetId });

    /// <summary>2004:28 — ride one character on another.</summary>
    public static EMEVD.Instruction RideCharacter(int riderEntityId, int mountEntityId) =>
        new(2004, 28, new List<object> { riderEntityId, mountEntityId });

    /// <summary>2004:29 — set animation time.</summary>
    public static EMEVD.Instruction SetAnimationTime(int entityId, int animId, float time) =>
        new(2004, 29, new List<object> { entityId, animId, time });

    /// <summary>2004:30 — activate character multi arm.</summary>
    public static EMEVD.Instruction ActivateCharacterMultiArm(int entityId) =>
        new(2004, 30, new List<object> { entityId });

    /// <summary>2004:31 — set NPC part solidity.</summary>
    public static EMEVD.Instruction SetNpcPartSolidity(int entityId, byte partId, byte solid) =>
        new(2004, 31, new List<object> { entityId, partId, solid });

    /// <summary>2004:32 — set character event table.</summary>
    public static EMEVD.Instruction SetCharacterEventTable(int entityId, int tableId) =>
        new(2004, 32, new List<object> { entityId, tableId });

    /// <summary>2004:33 — create referred damage pair.</summary>
    public static EMEVD.Instruction CreateReferredDamagePair(int sourceEntityId, int targetEntityId) =>
        new(2004, 33, new List<object> { sourceEntityId, targetEntityId });

    /// <summary>2004:34 — set character gravity state.</summary>
    public static EMEVD.Instruction SetCharacterGravity(int entityId, byte state) =>
        new(2004, 34, new List<object> { entityId, state });

    /// <summary>2004:35 — set AI param id.</summary>
    public static EMEVD.Instruction SetAiParamId(int entityId, int aiParamId) =>
        new(2004, 35, new List<object> { entityId, aiParamId });

    /// <summary>2004:36 — unknown 2004:36.</summary>
    public static EMEVD.Instruction Unknown2004_36(int entityId, byte unk) =>
        new(2004, 36, new List<object> { entityId, unk });

    /// <summary>2004:37 — set character fog.</summary>
    public static EMEVD.Instruction SetCharacterFog(int entityId, int fogId) =>
        new(2004, 37, new List<object> { entityId, fogId });

    /// <summary>2004:38 — unknown 2004:38.</summary>
    public static EMEVD.Instruction Unknown2004_38(int entityId, int unk) =>
        new(2004, 38, new List<object> { entityId, unk });

    /// <summary>2004:39 — unknown 2004:39.</summary>
    public static EMEVD.Instruction Unknown2004_39(int entityId, int unk) =>
        new(2004, 39, new List<object> { entityId, unk });

    /// <summary>2004:40 — warp character and set floor.</summary>
    public static EMEVD.Instruction WarpAndSetFloor(int entityId, WarpType warpType, int destEntityId, int setFloor) =>
        new(2004, 40, new List<object> { entityId, (byte)warpType, destEntityId, setFloor });

    /// <summary>2004:41 — short warp: teleport character to a destination entity.</summary>
    public static EMEVD.Instruction WarpCharacter(int entityId, int destEntityId, byte warpType = 0) =>
        new(2004, 41, new List<object> { entityId, warpType, destEntityId, (int)-1 });

    /// <summary>2004:42 — warp character and copy floor.</summary>
    public static EMEVD.Instruction WarpAndCopyFloor(int entityId, WarpType warpType, int destEntityId, int copyFrom) =>
        new(2004, 42, new List<object> { entityId, (byte)warpType, destEntityId, copyFrom });

    /// <summary>2004:43 — unknown 2004:43.</summary>
    public static EMEVD.Instruction Unknown2004_43(int entityId, int unk1, int unk2) =>
        new(2004, 43, new List<object> { entityId, unk1, unk2 });

    /// <summary>2004:44 — unknown 2004:44.</summary>
    public static EMEVD.Instruction Unknown2004_44(int entityId, int unk) =>
        new(2004, 44, new List<object> { entityId, unk });

    /// <summary>2004:45 — set character NPC type.</summary>
    public static EMEVD.Instruction SetCharacterNpcType(int entityId, byte npcType) =>
        new(2004, 45, new List<object> { entityId, npcType });

    /// <summary>2004:46 — set special effect on character.</summary>
    public static EMEVD.Instruction SetSpecialEffect(int entityId, int spEffectId) =>
        new(2004, 46, new List<object> { entityId, spEffectId });

    /// <summary>2004:47 — reset special effect on character.</summary>
    public static EMEVD.Instruction ResetSpecialEffect(int entityId, int spEffectId) =>
        new(2004, 47, new List<object> { entityId, spEffectId });

    /// <summary>2004:48 — fade out a character over time.</summary>
    public static EMEVD.Instruction CharacterFadeOut(int entityId, float time) =>
        new(2004, 48, new List<object> { entityId, time });

    /// <summary>2004:49 — unknown 2004:49.</summary>
    public static EMEVD.Instruction Unknown2004_49(int entityId, int unk) =>
        new(2004, 49, new List<object> { entityId, unk });

    // ── object (2005 bank) ────────────────────────────────────────────────────

    /// <summary>2005:0 — use (activate) an object.</summary>
    public static EMEVD.Instruction UseObject(int entityId) =>
        new(2005, 0, new List<object> { entityId });

    /// <summary>2005:1 — set object treasure flag.</summary>
    public static EMEVD.Instruction SetObjectTreasureFlag(int entityId, int flagId) =>
        new(2005, 1, new List<object> { entityId, flagId });

    /// <summary>2005:2 — set object activation state.</summary>
    public static EMEVD.Instruction SetObjectActivation(int entityId, byte state) =>
        new(2005, 2, new List<object> { entityId, state });

    /// <summary>2005:3 — enable or disable a map object.</summary>
    public static EMEVD.Instruction SetObjectEnabled(int entityId, EnabledState state) =>
        new(2005, 3, new List<object> { entityId, (byte)state });

    /// <summary>2005:4 — set object state.</summary>
    public static EMEVD.Instruction SetObjectState(int entityId, byte state) =>
        new(2005, 4, new List<object> { entityId, state });

    /// <summary>2005:5 — move object to destination entity.</summary>
    public static EMEVD.Instruction MoveObject(int entityId, int destEntityId) =>
        new(2005, 5, new List<object> { entityId, destEntityId });

    /// <summary>2005:6 — unknown 2005:6.</summary>
    public static EMEVD.Instruction Unknown2005_6(int entityId, byte unk) =>
        new(2005, 6, new List<object> { entityId, unk });

    // ── sfx (2006 bank) ───────────────────────────────────────────────────────

    /// <summary>2006:0 — enable or disable an SFX entity.</summary>
    public static EMEVD.Instruction SetSfxEnabled(int sfxEntityId, byte state) =>
        new(2006, 0, new List<object> { sfxEntityId, state });

    /// <summary>2006:1 — create a temporary (one-shot) SFX.</summary>
    public static EMEVD.Instruction CreateTemporarySfx(int entityType, int entityId, int dummypolyId, int sfxId) =>
        new(2006, 1, new List<object> { entityType, entityId, dummypolyId, sfxId });

    /// <summary>2006:2 — create a permanent SFX.</summary>
    public static EMEVD.Instruction CreatePermanentSfx(int entityType, int entityId, int dummypolyId, int sfxId) =>
        new(2006, 2, new List<object> { entityType, entityId, dummypolyId, sfxId });

    /// <summary>2006:3 — spawn a one-shot SFX at an entity's dummypoly.</summary>
    public static EMEVD.Instruction SpawnOneshotSfx(int entityType, int entityId, int dummypolyId, int sfxId) =>
        new(2006, 3, new List<object> { entityType, entityId, dummypolyId, sfxId });

    /// <summary>2006:4 — delete an SFX.</summary>
    public static EMEVD.Instruction DeleteSfx(int sfxEntityId, byte immediate = 0) =>
        new(2006, 4, new List<object> { sfxEntityId, immediate });

    /// <summary>2006:5 — create an object SFX.</summary>
    public static EMEVD.Instruction CreateObjectSfx(int entityId, int dummypolyId, int sfxId) =>
        new(2006, 5, new List<object> { entityId, dummypolyId, sfxId });

    /// <summary>2006:6 — delete an object SFX.</summary>
    public static EMEVD.Instruction DeleteObjectSfx(int entityId, byte immediate = 0) =>
        new(2006, 6, new List<object> { entityId, immediate });

    // ── camera (2008 bank) ────────────────────────────────────────────────────

    /// <summary>2008:0 — set perspective camera state.</summary>
    public static EMEVD.Instruction SetPerspectiveCamera(byte state) =>
        new(2008, 0, new List<object> { state });

    /// <summary>2008:1 — camera follows entity for a duration.</summary>
    public static EMEVD.Instruction CameraFollowEntity(int entityId, float duration) =>
        new(2008, 1, new List<object> { entityId, duration });

    /// <summary>2008:2 — camera shake at an entity.</summary>
    public static EMEVD.Instruction CameraVibration(int vibrationId, int entityType, int entityId,
        int dummypolyId, float decayStart, float decayEnd) =>
        new(2008, 2, new List<object> { vibrationId, entityType, entityId, dummypolyId, decayStart, decayEnd });

    /// <summary>2008:3 — request a specific camera.</summary>
    public static EMEVD.Instruction RequestCamera(int cameraId) =>
        new(2008, 3, new List<object> { cameraId });

    // ── script activation (2009 bank) ─────────────────────────────────────────

    /// <summary>2009:0 — script activate object.</summary>
    public static EMEVD.Instruction ScriptActivateObject(int entityId, int scriptId, byte state) =>
        new(2009, 0, new List<object> { entityId, scriptId, state });

    /// <summary>2009:1 — script activate character.</summary>
    public static EMEVD.Instruction ScriptActivateCharacter(int entityId, int scriptId, byte state) =>
        new(2009, 1, new List<object> { entityId, scriptId, state });

    // ── sound (2010 bank) ─────────────────────────────────────────────────────

    /// <summary>2010:0 — enable or disable a sound entity.</summary>
    public static EMEVD.Instruction SetSoundEnabled(int soundEntityId, byte state) =>
        new(2010, 0, new List<object> { soundEntityId, state });

    /// <summary>2010:1 — enable or disable a music entity.</summary>
    public static EMEVD.Instruction SetMusicEnabled(int musicEntityId, byte state) =>
        new(2010, 1, new List<object> { musicEntityId, state });

    /// <summary>2010:2 — play a sound effect on an entity.</summary>
    public static EMEVD.Instruction PlaySound(int entityId, int soundType, int soundId) =>
        new(2010, 2, new List<object> { entityId, soundType, soundId });

    /// <summary>2010:3 — enable or disable BGM globally.</summary>
    public static EMEVD.Instruction SetBGMEnabled(byte state) =>
        new(2010, 3, new List<object> { state });

    /// <summary>2010:4 — play combat sound on an entity.</summary>
    public static EMEVD.Instruction PlayCombatSound(int entityId, byte soundType, int soundId) =>
        new(2010, 4, new List<object> { entityId, soundType, soundId });

    // ── collision (2011 bank) ─────────────────────────────────────────────────

    /// <summary>2011:0 — enable or disable a collidable entity.</summary>
    public static EMEVD.Instruction SetCollidableEnabled(int entityId, byte state) =>
        new(2011, 0, new List<object> { entityId, state });

    // ── map display (2012 bank) ───────────────────────────────────────────────

    /// <summary>2012:0 — enable or disable map display globally.</summary>
    public static EMEVD.Instruction SetMapDisplayEnabled(byte state) =>
        new(2012, 0, new List<object> { state });

    /// <summary>2012:1 — enable or disable a map area.</summary>
    public static EMEVD.Instruction SetMapAreaEnabled(byte areaId, byte blockId, byte state) =>
        new(2012, 1, new List<object> { areaId, blockId, state });

    /// <summary>2012:2 — enable or disable a map object.</summary>
    public static EMEVD.Instruction SetMapObjectEnabled(int entityId, byte state) =>
        new(2012, 2, new List<object> { entityId, state });

    /// <summary>2012:3 — request map area display.</summary>
    public static EMEVD.Instruction RequestMapAreaDisplay(byte areaId, byte blockId) =>
        new(2012, 3, new List<object> { areaId, blockId });

    // ── matchers (for InsertAfter / idempotency) ──────────────────────────────

    public static Func<EMEVD.Instruction, bool> IsForceAnimation(int entityId, int animationId) => i =>
        i.Bank == 2003 && i.ID == 18 && ArgAt(i, 0, ArgType.Int32, ArgType.Int32) == entityId
                                     && ArgAt(i, 1, ArgType.Int32, ArgType.Int32) == animationId;

    public static Func<EMEVD.Instruction, bool> IsDisplayMessage(int messageId) => i =>
        i.Bank == 2007 && i.ID == 4 && ArgAt(i, 0, ArgType.Int32, ArgType.Byte) == messageId;

    internal static long InitTargetId(EMEVD.Instruction i) =>
        ArgAtL(i, 1, ArgType.Int32, ArgType.UInt32, ArgType.UInt32);

    // ── arg decode helpers ────────────────────────────────────────────────────

    private static int ArgAt(EMEVD.Instruction i, int idx, params ArgType[] layout)
    {
        try { return Convert.ToInt32(i.UnpackArgs(layout)[idx]); } catch { return int.MinValue; }
    }
    private static long ArgAtL(EMEVD.Instruction i, int idx, params ArgType[] layout)
    {
        try { return Convert.ToInt64(i.UnpackArgs(layout)[idx]); } catch { return long.MinValue; }
    }
}

// ── SubConditionBuilder ───────────────────────────────────────────────────────

/// <summary>
/// Builder for one AND or OR condition group, used inside
/// <see cref="EventBuilder.WhenAllOf"/> / <see cref="EventBuilder.WhenAnyOf"/>.
/// </summary>
public sealed class SubConditionBuilder
{
    private readonly List<EMEVD.Instruction> _parent;
    private readonly sbyte _group;

    internal SubConditionBuilder(List<EMEVD.Instruction> parent, sbyte group)
    {
        _parent = parent;
        _group  = group;
    }

    public SubConditionBuilder Flag(int flagId, FlagState state)
    { _parent.Add(Instr.IfEventFlag(state, flagId, _group)); return this; }

    public SubConditionBuilder Dead(int entityId)
    { _parent.Add(Instr.IfCharacterDeadAlive(LifeState.Dead, entityId, _group)); return this; }

    public SubConditionBuilder Alive(int entityId)
    { _parent.Add(Instr.IfCharacterDeadAlive(LifeState.Alive, entityId, _group)); return this; }

    public SubConditionBuilder HpBelow(int entityId, float ratio)
    { _parent.Add(Instr.IfHpRatio(entityId, compType: 3, ratio, _group)); return this; }

    public SubConditionBuilder HpAbove(int entityId, float ratio)
    { _parent.Add(Instr.IfHpRatio(entityId, compType: 2, ratio, _group)); return this; }

    public SubConditionBuilder HpEqual(int entityId, float ratio)
    { _parent.Add(Instr.IfHpRatio(entityId, compType: 0, ratio, _group)); return this; }

    public SubConditionBuilder InsideArea(int entityId, int areaEntityId)
    { _parent.Add(Instr.IfInsideArea(entityId, areaEntityId, desired: 1, _group)); return this; }

    public SubConditionBuilder OutsideArea(int entityId, int areaEntityId)
    { _parent.Add(Instr.IfInsideArea(entityId, areaEntityId, desired: 0, _group)); return this; }

    public SubConditionBuilder InsideRadius(int entityA, int entityB, sbyte dist)
    { _parent.Add(Instr.IfEntityInRadius((int)_group, desired: 1, entityA, entityB, dist)); return this; }

    public SubConditionBuilder OutsideRadius(int entityA, int entityB, sbyte dist)
    { _parent.Add(Instr.IfEntityInRadius((int)_group, desired: 0, entityA, entityB, dist)); return this; }

    public SubConditionBuilder HasItem(int itemType, int itemId)
    { _parent.Add(Instr.IfPlayerHasItem(itemType, itemId, desired: 1, _group)); return this; }

    public SubConditionBuilder CharacterType(int entityId, byte charType)
    { _parent.Add(Instr.IfCharacterType((int)_group, entityId, charType)); return this; }

    public SubConditionBuilder BatchFlags(LogicType logicState, int startFlag, int endFlag)
    { _parent.Add(Instr.IfBatchEventFlags((int)_group, logicState, startFlag, endFlag)); return this; }

    public SubConditionBuilder MapLoaded(byte areaId, byte blockId)
    { _parent.Add(Instr.IfMapLoaded((int)_group, areaId, blockId, desired: 1)); return this; }

    public SubConditionBuilder NewGamePlus(ComparisonType compType, byte cycle)
    { _parent.Add(Instr.IfNewGameComparison((int)_group, compType, cycle)); return this; }

    /// <summary>Append any sub-condition by bank/id/args directly.</summary>
    public SubConditionBuilder Raw(int bank, int id, params object[] args)
    { _parent.Add(Instr.Raw(bank, id, args)); return this; }
}

// ── EventBuilder ──────────────────────────────────────────────────────────────

/// <summary>
/// Fluent builder for a single EMEVD event body. Use via
/// <see cref="EmevdEditor.DefineEvent(long, EMEVD.Event.RestBehaviorType, Action{EventBuilder}, bool, int)"/>.
/// </summary>
public sealed class EventBuilder
{
    private readonly List<EMEVD.Instruction> _instrs = new();
    private int _nextAndGroup = 1;
    private int _nextOrGroup  = -1;

    internal IEnumerable<EMEVD.Instruction> Build() => _instrs;

    public EventBuilder Raw(int bank, int id, params object[] args)
    { _instrs.Add(Instr.Raw(bank, id, args)); return this; }

    public EventBuilder End()
    { _instrs.Add(Instr.EndUnconditionally(endType: 0)); return this; }

    public EventBuilder Restart()
    { _instrs.Add(Instr.EndUnconditionally(endType: 1)); return this; }

    public EventBuilder WaitSeconds(float seconds)
    { _instrs.Add(Instr.WaitSeconds(seconds)); return this; }

    public EventBuilder WaitFrames(short frames)
    { _instrs.Add(Instr.WaitFrames(frames)); return this; }

    public EventBuilder WhenFlag(int flagId, FlagState state)
    { _instrs.Add(Instr.IfEventFlag(state, flagId)); return this; }

    public EventBuilder WhenDead(int entityId)
    { _instrs.Add(Instr.IfCharacterDeadAlive(LifeState.Dead, entityId)); return this; }

    public EventBuilder WhenAlive(int entityId)
    { _instrs.Add(Instr.IfCharacterDeadAlive(LifeState.Alive, entityId)); return this; }

    public EventBuilder WhenHpBelow(int entityId, float ratio)
    { _instrs.Add(Instr.IfHpRatio(entityId, compType: 3, ratio)); return this; }

    public EventBuilder WhenInsideArea(int entityId, int areaEntityId)
    { _instrs.Add(Instr.IfInsideArea(entityId, areaEntityId, desired: 1)); return this; }

    public EventBuilder WhenOutsideArea(int entityId, int areaEntityId)
    { _instrs.Add(Instr.IfInsideArea(entityId, areaEntityId, desired: 0)); return this; }

    public EventBuilder WhenCharacterHasSpEffect(int entityId, int spEffectId)
    { _instrs.Add(Instr.IfCharacterHasSpEffect(entityId, spEffectId, shouldHave: true)); return this; }

    public EventBuilder WhenCharacterLosesSpEffect(int entityId, int spEffectId)
    { _instrs.Add(Instr.IfCharacterHasSpEffect(entityId, spEffectId, shouldHave: false)); return this; }

    public EventBuilder WhenAllOf(Action<SubConditionBuilder> conds)
    {
        sbyte group = (sbyte)_nextAndGroup++;
        conds(new SubConditionBuilder(_instrs, group));
        _instrs.Add(Instr.IfConditionGroup(resultGroup: 0, desired: 1, targetGroup: group));
        return this;
    }

    public EventBuilder WhenAnyOf(Action<SubConditionBuilder> conds)
    {
        sbyte group = (sbyte)_nextOrGroup--;
        conds(new SubConditionBuilder(_instrs, group));
        _instrs.Add(Instr.IfConditionGroup(resultGroup: 0, desired: 1, targetGroup: group));
        return this;
    }

    public EventBuilder SetFlag(int flagId, FlagState state)
    { _instrs.Add(Instr.SetEventFlag(flagId, state)); return this; }

    public EventBuilder AwardItemLot(int itemLotId)
    { _instrs.Add(Instr.AwardItemLot(itemLotId)); return this; }

    public EventBuilder DisplayMessage(int messageId, byte screenLocation = 0)
    { _instrs.Add(Instr.DisplayMessage(messageId, screenLocation)); return this; }

    public EventBuilder DisplayStatusMessage(int messageId)
    { _instrs.Add(Instr.DisplayStatusMessage(messageId)); return this; }

    public EventBuilder DisplayBanner(byte bannerType)
    { _instrs.Add(Instr.DisplayBanner(bannerType)); return this; }

    public EventBuilder DisplayBossHealthBar(int entityId, EnabledState state, int slot = 0, short nameId = 0)
    { _instrs.Add(Instr.DisplayBossHealthBar(entityId, state, slot, nameId)); return this; }

    public EventBuilder ForceAnimation(int entityId, int animId,
        bool loop = false, bool wait = false, bool ignoreTransition = false)
    { _instrs.Add(Instr.ForceAnimation(entityId, animId, loop, wait, ignoreTransition)); return this; }

    public EventBuilder ForceAnimDs1rA(int entityId, int animId, byte loop = 0, byte wait = 0, byte unk = 0)
    { _instrs.Add(Instr.ForceAnimDs1rA(entityId, animId, loop, wait, unk)); return this; }

    public EventBuilder ForceAnimDs1rB(int entityId, int animId, byte loop = 0, byte wait = 0, byte unk = 0)
    { _instrs.Add(Instr.ForceAnimDs1rB(entityId, animId, loop, wait, unk)); return this; }

    public EventBuilder SetCharacterEnabled(int entityId, EnabledState state)
    { _instrs.Add(Instr.SetCharacterEnabled(entityId, state)); return this; }

    public EventBuilder SetObjectEnabled(int entityId, EnabledState state)
    { _instrs.Add(Instr.SetObjectEnabled(entityId, state)); return this; }

    public EventBuilder KillCharacter(int entityId, bool awardSouls = false)
    { _instrs.Add(Instr.KillCharacter(entityId, awardSouls)); return this; }

    public EventBuilder SetCharacterAI(int entityId, EnabledState state)
    { _instrs.Add(Instr.SetCharacterAI(entityId, state)); return this; }

    public EventBuilder SetCharacterHome(int entityId, int regionEntityId)
    { _instrs.Add(Instr.SetCharacterHome(entityId, regionEntityId)); return this; }

    public EventBuilder SetCharacterImmortal(int entityId, EnabledState state)
    { _instrs.Add(Instr.SetCharacterImmortal(entityId, state)); return this; }

    public EventBuilder SetCharacterInvincible(int entityId, EnabledState state)
    { _instrs.Add(Instr.SetCharacterInvincible(entityId, state)); return this; }

    public EventBuilder WarpCharacter(int entityId, int destEntityId)
    { _instrs.Add(Instr.WarpCharacter(entityId, destEntityId)); return this; }

    public EventBuilder WarpAndSetFloor(int entityId, WarpType warpType, int destEntityId, int setFloor)
    { _instrs.Add(Instr.WarpAndSetFloor(entityId, warpType, destEntityId, setFloor)); return this; }

    public EventBuilder WarpAndCopyFloor(int entityId, WarpType warpType, int destEntityId, int copyFrom)
    { _instrs.Add(Instr.WarpAndCopyFloor(entityId, warpType, destEntityId, copyFrom)); return this; }

    public EventBuilder SetTeamType(int entityId, TeamType teamType)
    { _instrs.Add(Instr.SetTeamType(entityId, teamType)); return this; }

    public EventBuilder SetSpEffect(int entityId, int spEffectId)
    { _instrs.Add(Instr.SetSpEffect(entityId, spEffectId)); return this; }

    public EventBuilder CreateMultipartNpc(int npcEntityId, short modelId, int npcParamId, int npcThinkParamId, byte teamId, int partId)
    { _instrs.Add(Instr.CreateMultipartNpc(npcEntityId, modelId, npcParamId, npcThinkParamId, teamId, partId)); return this; }

    public EventBuilder SetNpcPartHp(int entityId, byte partId, ushort hp)
    { _instrs.Add(Instr.SetNpcPartHp(entityId, partId, hp)); return this; }

    public EventBuilder SetNpcPartSfx(int entityId, byte partId, int sfxId)
    { _instrs.Add(Instr.SetNpcPartSfx(entityId, partId, sfxId)); return this; }

    public EventBuilder HandleBossDefeat(int entityId)
    { _instrs.Add(Instr.HandleBossDefeat(entityId)); return this; }
}

// ── EmevdEditor ───────────────────────────────────────────────────────────────

/// <summary>
/// High-level edits over an EMEVD, with the gotchas baked in. All operations are
/// idempotent so a patcher can run every launch.
/// </summary>
public sealed class EmevdEditor
{
    public EMEVD Evd { get; }
    private readonly Action<string>? _record;

    public EmevdEditor(EMEVD evd, Action<string>? record = null)
    {
        Evd = evd;
        _record = record;
    }

    public EMEVD.Event Constructor => Evd.Events.First(e => e.ID == 0);
    public EMEVD.Event? Event(long id) => Evd.Events.FirstOrDefault(e => e.ID == id);

    /// <summary>
    /// Define (or replace) an event using a fluent <see cref="EventBuilder"/> and,
    /// by default, register it at the top of the constructor.
    /// </summary>
    public EMEVD.Event DefineEvent(long id, EMEVD.Event.RestBehaviorType rest,
        Action<EventBuilder> build, bool register = true, int slot = 0)
    {
        var builder = new EventBuilder();
        build(builder);
        return DefineEvent(id, rest, builder.Build(), register, slot);
    }

    /// <summary>
    /// Define (or replace) an event and, by default, register it at the top of the constructor.
    /// </summary>
    public EMEVD.Event DefineEvent(long id, EMEVD.Event.RestBehaviorType rest,
        IEnumerable<EMEVD.Instruction> body, bool register = true, int slot = 0)
    {
        _record?.Invoke($"EMEVD:Event:{id}");
        Evd.Events.RemoveAll(e => e.ID == id);
        var ev = new EMEVD.Event(id, rest);
        ev.Instructions.AddRange(body);
        Evd.Events.Add(ev);
        if (register) RegisterAtConstructorTop(id, slot);
        return ev;
    }

    /// <summary>Convenience overload: <c>DefineEvent(id, rest, instr, instr, …)</c>.</summary>
    public EMEVD.Event DefineEvent(long id, EMEVD.Event.RestBehaviorType rest, params EMEVD.Instruction[] body) =>
        DefineEvent(id, rest, body, register: true);

    /// <summary>Register an event at the top of the constructor (idempotent).</summary>
    public void RegisterAtConstructorTop(long eventId, int slot = 0)
    {
        EMEVD.Event c = Constructor;
        c.Instructions.RemoveAll(x => x.Bank == 2000 && x.ID == 0 && Instr.InitTargetId(x) == eventId);
        c.Instructions.Insert(0, Instr.InitializeEvent(eventId, slot));
    }

    /// <summary>
    /// Insert <paramref name="toInsert"/> immediately after the first instruction
    /// in event <paramref name="eventId"/> matching <paramref name="match"/>.
    /// Skips if <paramref name="alreadyPresent"/> already matches (idempotent).
    /// Fixes up event-parameter indices. Returns true if inserted.
    /// </summary>
    public bool InsertAfter(long eventId, Func<EMEVD.Instruction, bool> match, EMEVD.Instruction toInsert,
        Func<EMEVD.Instruction, bool>? alreadyPresent = null)
    {
        EMEVD.Event? ev = Event(eventId);
        if (ev == null) return false;
        if (alreadyPresent != null && ev.Instructions.Any(alreadyPresent)) return false;
        for (int i = 0; i < ev.Instructions.Count; i++)
        {
            if (!match(ev.Instructions[i])) continue;
            ev.Instructions.Insert(i + 1, toInsert);
            foreach (EMEVD.Parameter p in ev.Parameters)
                if (p.InstructionIndex >= i + 1) p.InstructionIndex++;
            return true;
        }
        return false;
    }
}
