# Tutorial: Advanced ESD Tutorial - Implementing a Full Warp System

Source: https://soulsmodding.wikidot.com/tutorial:advanced-esd-tutorial

## Tools
- soulstruct / esdtool / zeditor
- DS Map Studio
- DarkScript3

## Introduction
The Warp menu is hardcoded in Dark Souls 1. To implement one, we must be creative:
- In Talk ESD: build the text menu and set flags
- In EMEVD: create events to wait for those flags

## Key Concept
**Bonfires are just invisible players you talk to.** Their State 4 holds the text menu. When a player selects an option, a flag is set. EMEVD events then listen for that flag and execute behavior (warp, give item, etc.).

## ESD Structure Pattern

### State 4 (Main Menu)
```python
def enter(self):
    ShowShopMessage(0, 0, 0)
    SetFlagState(flag=10000601, state=1)  # Mark region as visited
    AddTalkListData(menu_index=9, menu_text=15000005, required_flag=-1)
```

### Menu State (e.g., State 54 - Warp Menu)
```python
class State_54(State):
    """ 54: custom warp menu """
 
    def previous_states(self):
        return [State_59]
 
    def enter(self):
        ShowShopMessage(0, 0, 0)
        AddTalkListData(menu_index=1, menu_text=10000600, required_flag=10000601)
        AddTalkListData(menu_index=2, menu_text=10000700, required_flag=10000701)
 
    def test(self):
        if CompareBonfireState(0) == 1 or IsPlayerDead() == 1:
            return State_6
        if GetTalkListEntryResult() == 1:
            return State_61  # Go to firelink bonfire handler
        if GetTalkListEntryResult() == 2:
            return State_71  # Go to depths bonfire handler
```

### Action State (e.g., State 61 - Warp to Firelink)
```python
class State_61(State):
    """ 61: firelink shrine bonfire """
 
    def previous_states(self):
        return [State_60]
 
    def enter(self):
        ForceEndTalk(unk1=0)
        ClearTalkProgressData()
        CloseShopMessage()
        EndBonfireKindleAnimLoop()
        ClearTalkDisabledState()
        SetFlagState(flag=10000610, state=1)  # Flag for EMEVD to listen to
 
    def test(self):
        return State_1
```

**Critical**: When you select an option, set a unique flag that EMEVD will listen for.

## EMEVD Integration

### InitializeEvent (Register Warp Points)
```python
InitializeEvent(0, 500, 10000610, 10, 2, 1020980, 1022960)
```
- `500` = Event ID
- `10000610` = Flag that ESD sets when option is selected
- `10` = Map main number
- `2` = Map part number  
- `1020980` = Player object ID (the warp destination)
- `1022960` = Region point for respawn

### Event Handler (Listen and Execute)
```python
$Event(500, Default, function(X0_4, X4_4, X8_4, X12_4, X16_4) {
    SetEventFlag(X0_4, OFF);           // Clear flag initially
    WaitFor(EventFlag(X0_4));          // Wait for ESD to set it
    ForceAnimationPlayback(10000, 7697, false, false, false);
    WaitFixedTimeSeconds(1.6);
    PlaySE(10000, SoundType.sSFX, 777777774);
    WaitFixedTimeSeconds(0.6);
    SpawnOneshotSFX(TargetEntityType.Character, 10000, 245, 20147);
    WarpPlayer(X4_4, X8_4, X12_4);      // Warp to destination
    SetPlayerRespawnPoint(X16_4);       // Set respawn point
    RestartEvent();                     // Loop for next time
});
```

## Flow Summary
1. Player interacts with bonfire → ESD State 4
2. Player selects menu option → ESD transitions to action state
3. Action state sets a flag (e.g., `10000610`)
4. EMEVD event listening on that flag detects it
5. EMEVD executes behavior (warp, animation, item spawn, etc.)
6. Event clears flag and restarts loop

## For Item Giving
Same pattern, but instead of `WarpPlayer()`, use item-giving commands in EMEVD.
