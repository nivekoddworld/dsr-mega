-- GOOFY DEMON  (Asylum Demon, entity 223200 / "MiniGreaterDemon")
-- ================================================================
-- This demon has completely given up on being a boss. Every time it picks an
-- action it re-rolls its "mood", and 80% of the time that mood is: dance,
-- spin, sprint away in terror, or stand around having an existential crisis.
-- Only occasionally does it remember it is a 20-foot hell-beast and panic-slam.
--
-- All sub-goals below use call signatures lifted verbatim from the vanilla
-- Asylum/Stray Demon AI, so the animations exist on this model:
--   SidewayMove / SpinStep(701) / Wait / Turn / LeaveTarget / Attack(3007/3008)
--
-- REGISTER_GOAL binds goal 223200 to the *Battle functions; without it the
-- engine never loads this AI.

REGISTER_GOAL(GOAL_MiniGreaterDemon223200_Battle, "MiniGreaterDemon223200Battle")
REGISTER_GOAL_NO_UPDATE(GOAL_MiniGreaterDemon223200_Battle, 1)

function MiniGreaterDemon223200Battle_Activate(ai, goal)
    local mood = ai:GetRandam_Int(1, 100)

    if mood <= 30 then
        -- ~~ THE SHIMMY ~~  side-step back and forth like it's at a wedding.
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 2.0, TARGET_ENE_0, ai:GetRandam_Int(0, 1), ai:GetRandam_Int(20, 40), true, true, -1)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 2.0, TARGET_ENE_0, ai:GetRandam_Int(0, 1), ai:GetRandam_Int(20, 40), true, true, -1)
        goal:AddSubGoal(GOAL_COMMON_SidewayMove, 2.0, TARGET_ENE_0, ai:GetRandam_Int(0, 1), ai:GetRandam_Int(20, 40), true, true, -1)

    elseif mood <= 50 then
        -- ~~ THE BREAKDANCE ~~  spin-step in place repeatedly. No reason.
        goal:AddSubGoal(GOAL_COMMON_SpinStep, 1, 701, TARGET_ENE_0, -1, AI_DIR_TYPE_B, 6)
        goal:AddSubGoal(GOAL_COMMON_SpinStep, 1, 701, TARGET_ENE_0, -1, AI_DIR_TYPE_B, 6)
        goal:AddSubGoal(GOAL_COMMON_SpinStep, 1, 701, TARGET_ENE_0, -1, AI_DIR_TYPE_B, 6)

    elseif mood <= 68 then
        -- ~~ THE COWARD ~~  sprint to the far wall in abject terror.
        goal:AddSubGoal(GOAL_COMMON_LeaveTarget, 1, TARGET_ENE_0, 100, TARGET_SELF, false, -1)

    elseif mood <= 84 then
        -- ~~ EXISTENTIAL CRISIS ~~  stand still, then slowly turn, questioning
        -- every decision that led to this moment.
        goal:AddSubGoal(GOAL_COMMON_Wait, ai:GetRandam_Float(1.5, 3.5), TARGET_ENE_0, 0, 0, 0)
        goal:AddSubGoal(GOAL_COMMON_Turn, 1, TARGET_ENE_0, 0, 0, 0)
        goal:AddSubGoal(GOAL_COMMON_Wait, ai:GetRandam_Float(1.0, 2.0), TARGET_ENE_0, 0, 0, 0)

    elseif mood <= 93 then
        -- ~~ SURPRISE! ~~  a sudden, fully-committed flying body slam out of
        -- absolutely nowhere, then immediately panic and flee.
        goal:AddSubGoal(GOAL_COMMON_Attack, 10, 3007, TARGET_ENE_0, DIST_Middle, 0)
        goal:AddSubGoal(GOAL_COMMON_LeaveTarget, 1, TARGET_ENE_0, 100, TARGET_SELF, false, -1)

    else
        -- ~~ FINE. FIGHT. ~~  the rare moment it acts like a boss: butt slam.
        goal:AddSubGoal(GOAL_COMMON_AttackTunableSpin, 10, 3008, TARGET_ENE_0, DIST_Middle, 0, -1)
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
