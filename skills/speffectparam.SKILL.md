---
name: ds1-speffectparam
description: Complete reference for SpEffectParam — Dark Souls Remastered's param table for status effects (buffs, debuffs, passive effects, weapon buffs, ring effects, etc.)
---

# SpEffectParam — Status Effect Parameter Reference

Complete reference for Dark Souls Remastered's `SpEffectParam` table. SpEffects are the game's universal effect system — used for rings, weapon buffs, temporary buffs/debuffs, passive boss traits, item effects, and custom mod effects.

**Source**: [soulsmodding.com — SpEffectParam](https://soulsmodding.com/doku.php?id=ds1-refmat:param:speffectparam)

---

## 1. Overview

SpEffectParam defines every status effect in the game. Each row is a single effect identified by an integer ID. Effects can:

- Modify stats (HP, stamina, defense, attack power)
- Apply damage/healing over time (via `motionInterval` + `changeHpRate`)
- Add behavior/attack data (via `behaviorId` → BehaviorParam)
- Chain to other effects on expiry/cycle/on-hit
- Trigger hardcoded game logic (via `stateInfo`)
- Show/hide the effect icon in the HUD

**In the DS1Mod framework**, SpEffects are created via `DefineSpEffect()`:

```csharp
g.DefineSpEffect(paramdefBnd, new SpEffectDef
{
    Id       = mySpEffectId,
    DonorId  = 110,             // Clone from a benign vanilla effect
    Duration = 8f,
    MaxHpRate = 1.5f,          // +50% max HP
    Configure = row => row["motionInterval"].Value = 0f,
});
```

---

## 2. Field Reference

### 2a. Core Effect Parameters

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x00` | `iconId` | `s32` | HUD icon displayed while effect is active. -1 = no icon. |
| `0x04` | `conditionHp` | `f32` | HP% threshold for effect activation. -1 = always, 40 = active below 40% HP, 80 = active below 80% HP. |
| `0x08` | `effectEndurance` | `f32` | Duration in seconds. 0 = instant/one-shot, >0 = lingering effect. |
| `0x0c` | `motionInterval` | `f32` | Re-application interval in seconds. 0 = every frame. Used for periodic effects (poison, Replenishment). |

### 2b. Max Stat Multipliers

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x10` | `maxHpRate` | `f32` | Max HP multiplier. 1.0 = default, 0.5 = -50%, 2.0 = +100%. See `bCurrHPIndependeMaxHP` for current HP behavior. |
| `0x14` | `maxMpRate` | `f32` | Max MP multiplier. |
| `0x18` | `maxStaminaRate` | `f32` | Max Stamina multiplier. |

### 2c. Damage Cut Rates (Incoming Damage Multipliers)

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x1c` | `slashDamageCutRate` | `f32` | Slash physical damage taken. <1 = reduced, >1 = increased. |
| `0x20` | `blowDamageCutRate` | `f32` | Strike physical damage taken. |
| `0x24` | `thrustDamageCutRate` | `f32` | Thrust physical damage taken. |
| `0x28` | `neutralDamageCutRate` | `f32` | Neutral physical damage taken. |
| `0x2c` | `magicDamageCutRate` | `f32` | Magic damage taken. |
| `0x30` | `fireDamageCutRate` | `f32` | Fire damage taken. |
| `0x34` | `thunderDamageCutRate` | `f32` | Lightning damage taken. |

### 2d. Attack Rates (Outgoing Damage Multipliers — Post-Defense)

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x38` | `physicsAttackRate` | `f32` | Physical damage dealt (post-defense, bypasses AR). |
| `0x3c` | `magicAttackRate` | `f32` | Magic damage dealt (post-defense). |
| `0x40` | `fireAttackRate` | `f32` | Fire damage dealt (post-defense). |
| `0x44` | `thunderAttackRate` | `f32` | Lightning damage dealt (post-defense). |

### 2e. Attack Power Rates (Outgoing Damage Multipliers — Pre-Defense)

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x48` | `physicsAttackPowerRate` | `f32` | Physical attack rating multiplier. |
| `0x4c` | `magicAttackPowerRate` | `f32` | Magic attack rating multiplier. |
| `0x50` | `fireAttackPowerRate` | `f32` | Fire attack rating multiplier. |
| `0x54` | `thunderAttackPowerRate` | `f32` | Lightning attack rating multiplier. |

### 2f. Attack Power Add (Flat AR Addition)

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x58` | `physicsAttackPower` | `s32` | Flat physical AR addition. |
| `0x5c` | `magicAttackPower` | `s32` | Flat magic AR addition. |
| `0x60` | `fireAttackPower` | `s32` | Flat fire AR addition. |
| `0x64` | `thunderAttackPower` | `s32` | Flat lightning AR addition. |

### 2g. Defense Rates (Multipliers)

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x68` | `physicsDiffenceRate` | `f32` | Physical defense multiplier. |
| `0x6c` | `magicDiffenceRate` | `f32` | Magic defense multiplier. |
| `0x70` | `fireDiffenceRate` | `f32` | Fire defense multiplier. |
| `0x74` | `thunderDiffenceRate` | `f32` | Lightning defense multiplier. |

### 2h. Defense Add (Flat)

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x78` | `physicsDiffence` | `s32` | Flat physical defense addition. |
| `0x7c` | `magicDiffence` | `s32` | Flat magic defense addition. |
| `0x80` | `fireDiffence` | `s32` | Flat fire defense addition. |
| `0x84` | `thunderDiffence` | `s32` | Flat lightning defense addition. |

### 2i. Spot Damage Modifiers

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x88` | `NoGuardDamageRate` | `f32` | Damage multiplier when target is NOT guarding. |
| `0x8c` | `vitalSpotChangeRate` | `f32` | Damage multiplier when hitting weak points. |
| `0x90` | `normalSpotChangeRate` | `f32` | Damage multiplier for normal hits. |
| `0x94` | `maxHpChangeRate` | `f32` | Multiplier applied to effect owner's `maxHpRate` values. |

### 2j. Behavior & Periodic Effects

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x98` | `behaviorId` | `s32` | BehaviorParam/BehaviorParam_PC ID to apply. Requires `stateInfo 142` (NPC) or `stateInfo 171` (player). |
| `0x9c` | `changeHpRate` | `f32` | HP change per tick (% of max HP). Negative = heal. |
| `0xa0` | `changeHpPoint` | `s32` | HP change per tick (flat value). Negative = heal. |
| `0xa4` | `changeMpRate` | `f32` | MP change per tick (% of max MP). Negative = restore. |
| `0xa8` | `changeMpPoint` | `s32` | MP change per tick (flat value). Negative = restore. |
| `0xac` | `mpRecoverChangeSpeed` | `s32` | MP recovered per second. |
| `0xb0` | `changeStaminaRate` | `f32` | Stamina change per tick (% of max Stamina). |
| `0xb4` | `changeStaminaPoint` | `s32` | Stamina change per tick (flat value). |
| `0xb8` | `staminaRecoverChangeSpeed` | `s32` | Stamina recovered per second. |

### 2k. Magic Extension & Durability

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0xbc` | `magicEffectTimeChange` | `f32` | Add/subtract time from spell durations (only spells with duration >= 0.1s). Lingering Dragoncrest uses `stateInfo 193`. |
| `0xc0` | `insideDurability` | `s32` | Durability change per tick (equipment). Negative = repair. |
| `0xc4` | `maxDurability` | `s32` | Hits before durability consumed. 5 = +5 hits, -5 = -5 hits. |

### 2l. Stamina Damage & Status Effects

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0xc8` | `staminaAttackRate` | `f32` | Stamina damage dealt multiplier. |
| `0xcc` | `poizonAttackPower` | `s32` | Poison status inflicted per hit. |
| `0xd0` | `registIllness` | `s32` | Toxic status inflicted per hit. |
| `0xd4` | `registBlood` | `s32` | Bleed status inflicted per hit. |
| `0xd8` | `registCurse` | `s32` | Curse status inflicted per hit. |

### 2m. Movement & Physics

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0xdc` | `fallDamageRate` | `f32` | **Does nothing.** Fall control uses `stateInfo 47` instead. |
| `0xe0` | `soulRate` | `f32` | Souls received from enemies multiplier. |
| `0xe4` | `equipWeightChangeRate` | `f32` | Max equip load multiplier. |
| `0xe8` | `allItemWeightChangeRate` | `f32` | **Does nothing.** Probable DeS remnant. |
| `0xec` | `soul` | `s32` | Flat souls add/subtract. |

### 2n. Animation & Detection

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0xf0` | `animIdOffset` | `s32` | Offsets animation IDs. Used to swap enemy movesets. |
| `0xf4` | `haveSoulRate` | `f32` | Souls granted on death multiplier (requires base value in NpcParam). |
| `0xf8` | `targetPriority` | `f32` | Aggro target switch chance. +1 = 100% to target owner, -1 = 100% to target someone else. |
| `0xfc` | `sightSearchEnemyCut` | `s32` | Reduces enemy sight distance. 0 = default, 50 = -50%, 100 = -100%. |
| `0x100` | `hearingSearchEnemyCut` | `s32` | Reduces enemy hearing distance. 0 = default, 50 = -50%, 100 = -100%. |
| `0x104` | `grabityRate` | `f32` | Animation speed multiplier. 1.0 = default, 0.5 = half speed, 2.0 = double speed. |

### 2o. Status Resistance Multipliers

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x108` | `registPoizonChangeRate` | `f32` | Poison resistance multiplier. |
| `0x10c` | `registIllnessChangeRate` | `f32` | Toxic resistance multiplier. |
| `0x110` | `registBloodChangeRate` | `f32` | Bleed resistance multiplier. |
| `0x114` | `registCurseChangeRate` | `f32` | Curse resistance multiplier. |

### 2p. Soul Steal, Duration Reduction, Healing Multiplier

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x118` | `soulStealRate` | `f32` | Defense against HP loss from soul steal moves. |
| `0x11c` | `lifeReductionRate` | `f32` | Duration multiplier for status effects specified by `lifeReductionType`. |
| `0x120` | `hpRecoverRate` | `f32` | Healing received multiplier. |

### 2q. Effect Chaining

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x124` | `replaceSpEffectId` | `s32` | SpEffect ID to add when this effect ends. |
| `0x128` | `cycleOccurrenceSpEffectId` | `s32` | SpEffect ID to add every `motionInterval`. |
| `0x12c` | `atkOccurrenceSpEffectId` | `s32` | SpEffect ID to apply to victim when owner hits an enemy. Requires `stateInfo 152` or `153` for weapon buff VFX. |

### 2r. Guard & Poise

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x130` | `guardDefFlickPowerRate` | `f32` | Guard deflection multiplier. |
| `0x134` | `guardStaminaCutRate` | `f32` | Shield stability multiplier. Requires `stateInfo 158` (Magic Shield) or `stateInfo 204` (Great Magic Shield) for VFX. Stability > 100 = zero stamina damage. |
| `0x138` | `rayCastPassedTime` | `s16` | Evil Eye line-of-sight activation time (ms). |
| `0x13a` | `changeSuperArmorPoint` | `s16` | Flat poise add/subtract. Requires `stateInfo 155` to work. |
| `0x13c` | `bowDistRate` | `s16` | Addition to weapon `bowDistRate` for the effect owner. |

### 2s. Stacking & Categorization

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x13e` | `spCategory` | `u16` | Stacking behavior category. Determines how effects react when another effect in the same category is added. See enum `SP_EFFECT_SPCATEGORY`. |
| `0x140` | `categoryPriority` | `u8` | Used with specific `spCategory` values to determine stacking priority. Higher = lower priority. |
| `0x141` | `saveCategory` | `s8` | Save persistence slot. Effects persist across save/load if they share a `saveCategory`. Only one effect per category saved. |
| `0x142` | `changeMagicSlot` | `u8` | Attunement slot increase (sorcery/pyromancy). |
| `0x143` | `changeMiracleSlot` | `u8` | Attunement slot increase (miracles). |
| `0x144` | `heroPointDamage` | `s8` | Humanity change. -1 = +1 humanity, +1 = -1 humanity. |

### 2t. Deflection & Repulsion

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x145` | `defFlickPower` | `u8` | Repulsion defense modifier for owner's weapon. |
| `0x146` | `flickDamageCutRate` | `u8` | Absorption rate for repulsion damage calculation. |
| `0x147` | `bloodDamageRate` | `u8` | Bleed damage multiplier when bleed is triggered on the owner. |

### 2u. Damage Level Animations

These fields override the damage reaction animation played when the owner is hit at each damage level.

| Offset | Field | Type | Enum |
|--------|-------|------|------|
| `0x148` | `dmgLv_None` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x149` | `dmgLv_S` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x14a` | `dmgLv_M` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x14b` | `dmgLv_L` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x14c` | `dmgLv_BlowM` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x14d` | `dmgLv_Push` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x14e` | `dmgLv_Strike` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x14f` | `dmgLv_BlowS` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x150` | `dmgLv_Min` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x151` | `dmgLv_Uppercut` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x152` | `dmgLv_BlowLL` | `s8` | `ATKPARAM_REP_DMG TYPE` |
| `0x153` | `dmgLv_Breath` | `s8` | `ATKPARAM_REP_DMG TYPE` |

### 2v. Attack & Special Attributes

| Offset | Field | Type | Enum | Description |
|--------|-------|------|------|-------------|
| `0x154` | `atkAttribute` | `u8` | `ATKPARAM_ATKATTR_TYPE` | Attack attribute for hits from this effect. |
| `0x155` | `spAttribute` | `u8` | `ATKPARAM_SPATTR_TYPE` | Special attributes for hits from this effect. |

### 2w. Hardcoded Behaviors

| Offset | Field | Type | Enum | Description |
|--------|-------|------|------|-------------|
| `0x156` | `stateInfo` | `u8` | `SP_EFFECT_TYPE` | **Most important field.** Selects hardcoded game logic system. Also determines SpEffectVfxParam ID for visual effects when `useSpEffectEffect` is true. |
| `0x157` | `wepParamChange` | `u8` | `SP_EFE_WEP_CHANGE_PARAM` | Determines how damage modifiers behave with attack categories. |
| `0x158` | `moveType` | `u8` | `SP_EFFECT_MOVE_TYPE` | Offsets character movement animations. |
| `0x159` | `lifeReductionType` | `u8` | `SP_EFFECT_TYPE` | Status effect type whose duration is modified by `lifeReductionRate`. |
| `0x15a` | `throwCondition` | `u8` | `SP_EFFECT_THROW_CONDITION_TYPE` | Affects throwing masks. |
| `0x15b` | `addBehaviorJudgeId_condition` | `s8` | — | Condition value for BehaviorJudgeId. |
| `0x15c` | `addBehaviorJudgeId_add` | `u8` | — | Value for BehaviorJudgeId. |

### 2x. Effect Target Flags (Byte at `0x15d`)

Controls WHICH types of characters receive this effect. Bitfield packed as individual bytes.

| Bit | Field | Description |
|-----|-------|-------------|
| `0x15d [0]` | `effectTargetSelf` | Affects the effect owner. |
| `0x15d [1]` | `effectTargetFriend` | Affects owner's allies. |
| `0x15d [2]` | `effectTargetEnemy` | Affects owner's enemies. |
| `0x15d [3]` | `effectTargetPlayer` | Affects the player. |
| `0x15d [4]` | `effectTargetAI` | Affects NPCs. |
| `0x15d [5]` | `effectTargetLive` | Affects host player in multiplayer. |
| `0x15d [6]` | `effectTargetGhost` | Affects all client players in multiplayer. |
| `0x15d [7]` | `effectTargetWhiteGhost` | Affects friendly client players. |

| Bit | Field | Description |
|-----|-------|-------------|
| `0x15e [0]` | `effectTargetBlackGhost` | Affects hostile client players. |
| `0x15e [1]` | `effectTargetAttacker` | If ON and applied via `equipParamWeapon.spEffectBehavior`, applies to victim instead of owner. |

### 2y. Display & Scaling Booleans (Byte at `0x15e`)

| Bit | Field | Description |
|-----|-------|-------------|
| `0x15e [2]` | `dispIconNonactive` | HUD icon still appears even when effect is conditionally inactive. |
| `0x15e [3]` | `useSpEffectEffect` | Enables SpEffectVfxParam visuals. `stateInfo` determines which VfxParam entry to use. |
| `0x15e [4]` | `bAdjustMagicAblity` | INT stat corrects damage/defense fields. |
| `0x15e [5]` | `bAdjustFaithAblity` | FTH stat corrects damage/defense fields. |
| `0x15e [6]` | `bGameClearBonus` | Effect only active in NG+ or higher. |
| `0x15e [7]` | `magParamChange` | Damage/scaling fields apply to sorcery and pyromancies. |

### 2z. Misc Booleans (Byte at `0x15f`)

| Bit | Field | Description |
|-----|-------|-------------|
| `0x15f [0]` | `miracleParamChange` | Damage/scaling fields apply to miracles. |
| `0x15f [1]` | `clearSoul` | Sets soul counter to 0. |
| `0x15f [2]` | `requestSOS` | White Sign Soapstone network matchmaking. |
| `0x15f [3]` | `requestBlackSOS` | Red Sign Soapstone network matchmaking. |
| `0x15f [4]` | `requestForceJoinBlackSOS` | Red Eye Orb network matchmaking. |
| `0x15f [5]` | `requestKickSession` | Kicks clients out of your world. |
| `0x15f [6]` | `requestLeaveSession` | Black Separation Crystal — leave network session. |
| `0x15f [7]` | `requestNpcInveda` | Black Eye Orb (Lautrec quest / cut Shiva quest). |

### 2aa. Immunity & Behavior Booleans (Byte at `0x160`)

| Bit | Field | Description |
|-----|-------|-------------|
| `0x160 [0]` | `noDead` | Character cannot become a corpse (cannot die). |
| `0x160 [1]` | `bCurrHPIndependeMaxHP` | `true` = current HP unchanged when max HP is modified. `false` = current HP scales with max HP. |
| `0x160 [2]` | `corrosionIgnore` | Disables durability damage to weapons/armor. |
| `0x160 [3]` | `sightSearchCutIgnore` | Ignores `sightSearchEnemyCut` adjustments. |
| `0x160 [4]` | `hearingSearchCutIgnore` | Ignores `hearingSearchEnemyCut` adjustments. |
| `0x160 [5]` | `antiMagicIgnore` | Ignores magic blocking effects (Vow of Silence). |
| `0x160 [6]` | `fakeTargetIgnore` | Ignores fake targets (Aural Decoy). |
| `0x160 [7]` | `fakeTargetIgnoreUndead` | Ignores fake undead targets (Alluring Skull). |

### 2ab. More Immunity Booleans (Byte at `0x161`)

| Bit | Field | Description |
|-----|-------|-------------|
| `0x161 [0]` | `fakeTargetIgnoreAnimal` | Ignores fake animal targets. |
| `0x161 [1]` | `grabityIgnore` | Ignores `grabityRate` changes. |
| `0x161 [2]` | `disablePoison` | Immune to poison. |
| `0x161 [3]` | `disableDisease` | Immune to toxic. |
| `0x161 [4]` | `disableBlood` | Immune to bleed. |
| `0x161 [5]` | `disableCurse` | Immune to curse. |
| `0x161 [6]` | `enableCharm` | Vulnerable to charm (Undead Rapport). |
| `0x161 [7]` | `enableLifeTime` | Duration can be extended by TimeAct event. |

### 2ac. More Misc Booleans (Byte at `0x162`)

| Bit | Field | Description |
|-----|-------|-------------|
| `0x162 [0]` | `hasTarget` | Enables Evil Eye activation conditions. |
| `0x162 [1]` | `isFireDamageCancel` | Effect cancelled early when fire damage is taken. |
| `0x162 [2]` | `isExtendSpEffectLife` | Duration affected by `stateInfo 193` (Lingering Dragoncrest Ring). |
| `0x162 [3]` | `requestLeaveColiseumSession` | Purple Coward's Crystal. |
| `0x162 [4-7]` | `pad_2` | Padding. |

### 2ad. Covenant Restriction Flags (Bytes at `0x163`–`0x164`)

Effect only works while in a specific covenant.

| Bit | Field | Covenant |
|-----|-------|----------|
| `0x163 [0]` | `vowType0` | No covenant |
| `0x163 [1]` | `vowType1` | Way of White |
| `0x163 [2]` | `vowType2` | Princess Guard |
| `0x163 [3]` | `vowType3` | Warrior of Sunlight |
| `0x163 [4]` | `vowType4` | Darkwraith |
| `0x163 [5]` | `vowType5` | Path of the Dragon |
| `0x163 [6]` | `vowType6` | Gravelord Servant |
| `0x163 [7]` | `vowType7` | Forest Hunter |
| `0x164 [0]` | `vowType8` | Blade of the Dark Moon |
| `0x164 [1]` | `vowType9` | Chaos Servant |
| `0x164 [2]` | `vowType10` | ? |
| `0x164 [3]` | `vowType11` | ? |
| `0x164 [4]` | `vowType12` | ? |
| `0x164 [5]` | `vowType13` | ? |
| `0x164 [6]` | `vowType14` | ? |
| `0x164 [7]` | `vowType15` | ? |

### 2ae. Padding

| Offset | Field | Type | Description |
|--------|-------|------|-------------|
| `0x165` | `pad1` | `dummy8` | Padding. |

---

## 3. Key `stateInfo` Values

The `stateInfo` field (offset `0x156`) selects hardcoded game logic. This is the most powerful field in the param — it determines the effect's BEHAVIOR, not just its stat modifications.

| Value | Effect | Notes |
|-------|--------|-------|
| `0` | None / passive stat modifier | Simple stat tweaks, no special logic |
| `47` | Fall control | Overrides `fallDamageRate` |
| `142` | Add behavior to NPC | Requires `behaviorId` → BehaviorParam |
| `152` | Weapon buff (hit VFX on victim) | Used with `atkOccurrenceSpEffectId` |
| `153` | Weapon buff (hit VFX on victim) | Same as 152, alternative VFX path |
| `155` | Poise modifier | Required for `changeSuperArmorPoint` to work |
| `158` | Magic Shield VFX | Required for `guardStaminaCutRate` (Magic Shield) |
| `171` | Add behavior to player | Requires `behaviorId` → BehaviorParam_PC |
| `193` | Extend spell duration | Lingering Dragoncrest Ring — checks `isExtendSpEffectLife` |
| `204` | Great Magic Shield VFX | Required for `guardStaminaCutRate` (Great Magic Shield) |

> **Important**: When `useSpEffectEffect` is ON, `stateInfo` doubles as the ID into `SpEffectVfxParam` for visual effects. For example, a weapon buff effect typically uses `stateInfo` 152 or 153 both for the hit logic AND to look up the correct VFX.

---

## 4. `spCategory` Stacking Behavior

The `spCategory` field controls how effects interact when multiple effects in the same category are active.

| Category | Behavior |
|----------|----------|
| `0` | No category — no stacking restrictions |
| `1` | **Single only** — new effect replaces old one |
| `2` | **Priority-based** — higher `categoryPriority` loses |
| `3` | **Stack additive** — effects accumulate |
| `4` | **Single with save** — replaces old and persists |
| `5+` | Game-specific behaviors |

The `saveCategory` field (offset `0x141`) works alongside `spCategory` — only one effect per `saveCategory` value is persisted across save/load cycles. This is how the game remembers which weapon buff you had active.

---

## 5. Common Patterns

### 5a. Weapon Buff (e.g., Crystal Magic Weapon)

```csharp
g.DefineSpEffect(paramdefBnd, new SpEffectDef
{
    Id                = myBuffId,
    DonorId           = 120,  // Crystal Magic Weapon as base
    Duration          = 60f,
    MagicAttackRate   = 1.4f,  // +40% magic damage
    Configure = row =>
    {
        row["stateInfo"].Value = (byte)153;          // Weapon buff VFX
        row["atkOccurrenceSpEffectId"].Value = myHitEffectId;  // Hit VFX on enemy
        row["useSpEffectEffect"].Value = (byte)1;     // Enable VFX
        row["spCategory"].Value = (ushort)1;          // Replace other weapon buffs
        row["saveCategory"].Value = (sbyte)5;         // Persist on save/load
    },
});
```

### 5b. Healing Over Time (Replenishment-style)

```csharp
g.DefineSpEffect(paramdefBnd, new SpEffectDef
{
    Id          = myRegenId,
    DonorId     = 110,        // Benign effect
    Duration    = 300f,       // 5 minutes
    MotionInterval = 2f,      // Tick every 2 seconds
    HpRecoverPoint = -30,     // -30 HP per tick → heals 30 HP
});
```

### 5c. Temporary Stat Boost (Grass-style)

```csharp
g.DefineSpEffect(paramdefBnd, new SpEffectDef
{
    Id            = myGrassId,
    DonorId       = 110,
    Duration      = 60f,
    MaxStaminaRate = 1.5f,   // +50% stamina
    StaminaRecoverChangeSpeed = 20,  // Faster stamina recovery
    Configure = row =>
    {
        row["staminaRecoverChangeSpeed"].Value = 15;
    },
});
```

### 5d. Permanent Passive Ring Effect

```csharp
g.DefineSpEffect(paramdefBnd, new SpEffectDef
{
    Id              = myRingEffectId,
    DonorId         = 110,
    Duration        = -1f,          // -1 = permanent (infinite)
    PhysicsAttackRate = 1.1f,       // +10% physical damage
    Configure = row =>
    {
        row["effectEndurance"].Value = -1f;
        row["conditionHp"].Value = -1f;
    },
});
```

### 5e. Evil Eye-style (HP Recovery on Kill)

```csharp
g.DefineSpEffect(paramdefBnd, new SpEffectDef
{
    Id          = myEvilEyeId,
    DonorId     = 110,
    Duration    = -1f,
    MotionInterval = 0.1f,
    Configure = row =>
    {
        row["rayCastPassedTime"].Value = (short)500;  // 500ms LOS check
        row["hasTarget"].Value = (byte)1;              // Enable evil eye detection
        row["changeHpPoint"].Value = 30;               // 30 HP per kill
    },
});
```

---

## 6. Cross-References

| Param Table | Relation | Field |
|-------------|----------|-------|
| **BehaviorParam / BehaviorParam_PC** | Attack behavior data applied by the effect. | `behaviorId` |
| **SpEffectVfxParam** | Visual effects when `useSpEffectEffect = true`. | `stateInfo` → VfxParam ID |
| **EquipParamWeapon** | Weapon-level SpEffect application via `spEffectBehavior` fields. | References `spEffectId` |
| **EquipParamGoods** | Items apply SpEffects via `refId` field. | References `spEffectId` |
| **NpcParam** | Enemy base stats. `haveSoulRate` needs base soul value here. | `haveSoulRate` |
| **SpEffectParam (self)** | Effect chaining: cycle, on-expiry, on-hit. | `replaceSpEffectId`, `cycleOccurrenceSpEffectId`, `atkOccurrenceSpEffectId` |

---

## 7. SpEffectDef (DS1Mod Framework)

The `SpEffectDef` class in `DS1Mod.Modding.SpEffect` provides a high-level C# wrapper for creating SpEffectParam rows:

| Property | Type | Maps To |
|----------|------|---------|
| `Id` | `int` | Row ID |
| `DonorId` | `int` | Vanilla effect to clone from |
| `Duration` | `float` | `effectEndurance` |
| `MotionInterval` | `float` | `motionInterval` |
| `MaxHpRate` | `float` | `maxHpRate` |
| `MaxMpRate` | `float` | `maxMpRate` |
| `MaxStaminaRate` | `float` | `maxStaminaRate` |
| `PhysAtkPowerRate` | `float` | `physicsAttackPowerRate` |
| `MagicAtkPowerRate` | `float` | `magicAttackPowerRate` |
| `FireAtkPowerRate` | `float` | `fireAttackPowerRate` |
| `ThunderAtkPowerRate` | `float` | `thunderAttackPowerRate` |
| `HpRecoverPoint` | `int` | `changeHpPoint` (negated — positive = heal) |
| `SoulRate` | `float` | `soulRate` |
| `EquipWeightRate` | `float` | `equipWeightChangeRate` |
| `StaminaRecoverSpeed` | `int` | `staminaRecoverChangeSpeed` |
| `Configure` | `Action<Param.Row>` | Escape hatch for raw field overrides |

For advanced effects, use the `Configure` delegate to set `stateInfo`, `spCategory`, `saveCategory`, `useSpEffectEffect`, or any other field not covered by the typed properties:

```csharp
g.DefineSpEffect(paramdefBnd, new SpEffectDef
{
    Id       = myCustomId,
    DonorId  = 110,
    Duration = 30f,
    Configure = row =>
    {
        // State info (hardcoded logic)
        row["stateInfo"].Value = (byte)152;

        // Boolean flags (bitfield bytes)
        row["useSpEffectEffect"].Value = (byte)1;
        row["spCategory"].Value = (ushort)1;
        row["saveCategory"].Value = (sbyte)5;

        // Raw field override
        row["guardStaminaCutRate"].Value = 0.5f;
    },
});
```

---

## 8. Vanilla SpEffect ID Reference

Useful vanilla SpEffect IDs to use as `DonorId` values when cloning:

| ID | Effect | Use As Base For |
|----|--------|-----------------|
| `110` | Empty slot (0s, no icon) | Clean passive stat mods |
| `120` | Crystal Magic Weapon | Weapon buffs |
| `121` | Great Magic Weapon | Weapon buffs |
| `122` | Magic Weapon | Weapon buffs |
| `140` | Power Within | Duration-limited self-buffs |
| `150` | Iron Flesh | Defense/poise buffs |
| `200` | Homeward | Effect-on-use templates |
| `400` | Poison | Status effect DoT templates |
| `401` | Toxic | Status effect DoT templates |
| `402` | Bleed | Status effect templates |
| `410` | Replenishment | Heal-over-time templates |
| `500` | Green Blossom | Stamina regen templates |
| `4000` | Soul Arrow | Spell effect templates |
| `4100` | Great Heavy Soul Arrow | Spell effect templates |
| `5000` | Ring of Favor | Passive stat ring effect templates |
| `5001` | Havel's Ring | Equip load ring templates |
| `5020` | Bellowing Dragoncrest | Magic damage ring templates |
| `5040` | Ring of Steel Protection | Defense ring templates |
| `5050` | Hawk Ring | Bow range ring templates |
| `5070` | Cloranthy Ring | Stamina regen ring templates |
| `5080` | Ring of Fog | Visibility ring templates |
| `5110` | Covetous Gold Serpent Ring | Soul/item discovery ring templates |
| `5130` | Sun Princess Ring | HP regen ring templates |

---

## 9. Best Practices

1. **Always clone from a close match** — Choose a `DonorId` that already has the right `stateInfo`, `spCategory`, and boolean flags. This saves you from setting a dozen fields manually.

2. **Duration semantics** — `0` = instant (applied and removed in one frame). `-1` = permanent (infinite). Any positive value = duration in seconds.

3. **Healing convention** — `changeHpPoint` with a **negative** value = healing. `HpRecoverPoint` in `SpEffectDef` automatically negates the value (positive = heal).

4. **`stateInfo` is king** — Many fields ONLY work when paired with the correct `stateInfo`. E.g., `changeSuperArmorPoint` is ignored without `stateInfo 155`.

5. **Use `useSpEffectEffect` for VFX** — Set this to 1 and ensure `stateInfo` matches a valid SpEffectVfxParam ID to get visual effects.

6. **Test immunity flags carefully** — `noDead` (bit 0x160[0]) prevents death entirely, not just the death animation. Bosses with `noDead` can never be killed.
