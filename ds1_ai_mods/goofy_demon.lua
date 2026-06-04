-- GOOFY DEMON  (Asylum Demon, entity 223200 / "MiniGreaterDemon")
-- ================================================================
-- This demon has given up on being a boss. Every action it re-rolls its
-- "mood" and mostly just goofs off. Now with 10 moods.
--
-- MOOD SIGNALLING: the enemy AI cannot draw player text directly, so each
-- cycle we broadcast the current mood through event flags. We clear the whole
-- mood-flag block, then set exactly one, so any watcher (the C# mod's console
-- readout, or an EMEVD event) can tell which mood is active.
--
-- Flags 11817000..11817009 are m18_01 (Undead Asylum) local event flags in a
-- high, vanilla-unused range. This range matters: it must be decodable by the
-- mod framework's event-flag reader (group 1, area 181), which the old Kiln
-- range (15105610) was NOT — that's why the readout showed nothing.
--
-- All sub-goals use call signatures lifted verbatim from the vanilla
-- Asylum/Stray Demon AI, so every animation exists on this model.

REGISTER_GOAL(GOAL_MiniGreaterDemon223200_Battle, "MiniGreaterDemon223200Battle")
REGISTER_GOAL_NO_UPDATE(GOAL_MiniGreaterDemon223200_Battle, 1)

-- Broadcast mood index n (0-based) over the mood-flag block: clear all ten,
-- then set exactly one. Unrolled (no loop) so it round-trips cleanly.
function GoofyDemon_SetMood(ai, n)
    ai:SetEventFlag(11817000, false)
    ai:SetEventFlag(11817001, false)
    ai:SetEventFlag(11817002, false)
    ai:SetEventFlag(11817003, false)
    ai:SetEventFlag(11817004, false)
    ai:SetEventFlag(11817005, false)
    ai:SetEventFlag(11817006, false)
    ai:SetEventFlag(11817007, false)
    ai:SetEventFlag(11817008, false)
    ai:SetEventFlag(11817009, false)
    ai:SetEventFlag(11817000 + n, true)
end

function MiniGreaterDemon223200Battle_Activate(ai, goal)
    local mood = ai:GetRandam_Int(1, 100)

    if mood <= 18 then
        -- 0: THE SHIMMY — side-step back and forth like it's at a wedding.
        GoofyDemon_SetMood(ai, 0)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 2.0, TARGET_ENE_0, ai:GetRandam_Int(0, 1), ai:GetRandam_Int(20, 40), true, true, -1)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 2.0, TARGET_ENE_0, ai:GetRandam_Int(0, 1), ai:GetRandam_Int(20, 40), true, true, -1)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 2.0, TARGET_ENE_0, ai:GetRandam_Int(0, 1), ai:GetRandam_Int(20, 40), true, true, -1)

    elseif mood <= 34 then
        -- 1: THE BREAKDANCE — spin-step in place, repeatedly, for no reason.
        GoofyDemon_SetMood(ai, 1)
        goal:AddSubGoal(GOAL_COMMON_SpinStep, 1, 701, TARGET_ENE_0, -1, AI_DIR_TYPE_B, 6)
        goal:AddSubGoal(GOAL_COMMON_SpinStep, 1, 701, TARGET_ENE_0, -1, AI_DIR_TYPE_B, 6)
        goal:AddSubGoal(GOAL_COMMON_SpinStep, 1, 701, TARGET_ENE_0, -1, AI_DIR_TYPE_B, 6)

    elseif mood <= 48 then
        -- 2: THE COWARD — sprint to the far wall in abject terror.
        GoofyDemon_SetMood(ai, 2)
        goal:AddSubGoal(GOAL_COMMON_LeaveTarget, 1, TARGET_ENE_0, 100, TARGET_SELF, false, -1)

    elseif mood <= 60 then
        -- 3: EXISTENTIAL CRISIS — stand still, turn slowly, stand still again.
        GoofyDemon_SetMood(ai, 3)
        goal:AddSubGoal(GOAL_COMMON_Wait, ai:GetRandam_Float(1.5, 3.5), TARGET_ENE_0, 0, 0, 0)
        goal:AddSubGoal(GOAL_COMMON_Turn, 1, TARGET_ENE_0, 0, 0, 0)
        goal:AddSubGoal(GOAL_COMMON_Wait, ai:GetRandam_Float(1.0, 2.0), TARGET_ENE_0, 0, 0, 0)

    elseif mood <= 66 then
        -- 4: SURPRISE! — a sudden flying body slam, then immediately flee.
        GoofyDemon_SetMood(ai, 4)
        goal:AddSubGoal(GOAL_COMMON_Attack, 10, 3007, TARGET_ENE_0, DIST_Middle, 0)
        goal:AddSubGoal(GOAL_COMMON_LeaveTarget, 1, TARGET_ENE_0, 100, TARGET_SELF, false, -1)

    elseif mood <= 72 then
        -- 5: FINE. FIGHT. — the rare butt slam where it acts like a boss.
        GoofyDemon_SetMood(ai, 5)
        goal:AddSubGoal(GOAL_COMMON_AttackTunableSpin, 10, 3008, TARGET_ENE_0, DIST_Middle, 0, -1)

    elseif mood <= 82 then
        -- 6: THE ZOOMIES — frantic shimmy/spin combo, like a cat at 3am.
        GoofyDemon_SetMood(ai, 6)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 0.6, TARGET_ENE_0, 0, ai:GetRandam_Int(30, 45), true, true, -1)
        goal:AddSubGoal(GOAL_COMMON_SpinStep, 0.5, 701, TARGET_ENE_0, -1, AI_DIR_TYPE_B, 6)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 0.6, TARGET_ENE_0, 1, ai:GetRandam_Int(30, 45), true, true, -1)
        goal:AddSubGoal(GOAL_COMMON_SpinStep, 0.5, 701, TARGET_ENE_0, -1, AI_DIR_TYPE_B, 6)

    elseif mood <= 90 then
        -- 7: HOKEY POKEY — step in, step back, step in, shake it all about.
        GoofyDemon_SetMood(ai, 7)
        goal:AddSubGoal(GOAL_COMMON_ApproachTarget, 3, TARGET_ENE_0, 1, TARGET_SELF, false, -1)
        goal:AddSubGoal(GOAL_COMMON_LeaveTarget, 1, TARGET_ENE_0, 8, TARGET_SELF, false, -1)
        goal:AddSubGoal(GOAL_COMMON_ApproachTarget, 3, TARGET_ENE_0, 1, TARGET_SELF, false, -1)
        goal:AddSubGoal(GOAL_COMMON_Turn, 1, TARGET_ENE_0, 0, 0, 0)

    elseif mood <= 95 then
        -- 8: STAGE FRIGHT — freezes (forgot its lines), tiny embarrassed turn.
        GoofyDemon_SetMood(ai, 8)
        goal:AddSubGoal(GOAL_COMMON_Wait, ai:GetRandam_Float(4.0, 7.0), TARGET_ENE_0, 0, 0, 0)
        goal:AddSubGoal(GOAL_COMMON_Turn, 1, TARGET_ENE_0, 0, 0, 0)

    else
        -- 9: VICTORY LAP — celebrates prematurely, running a circle around you.
        GoofyDemon_SetMood(ai, 9)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 1.5, TARGET_ENE_0, 0, ai:GetRandam_Int(35, 50), true, true, -1)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 1.5, TARGET_ENE_0, 0, ai:GetRandam_Int(35, 50), true, true, -1)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 1.5, TARGET_ENE_0, 0, ai:GetRandam_Int(35, 50), true, true, -1)
    end
end

function MiniGreaterDemon223200Battle_Update(ai, goal)
    return GOAL_RESULT_Continue
end

function MiniGreaterDemon223200Battle_Terminate(ai, goal)
end

function MiniGreaterDemon223200Battle_Interupt(ai, goal)
    -- Unbothered. Moisturized. In its lane. Keeps dancing even when hit.
    return false
end
