-- Asylum Demon "Slam Only" AI
-- =============================
-- Entity 223200 (model "MiniGreaterDemon") = the Asylum Demon, the first boss
-- of the Northern Undead Asylum. (Note: entity 223000, the Stray Demon, is a
-- different fight that reuses the same model.)
--
-- This replaces his decision logic so he only ever does his two slam attacks:
--   * 3007 - flying leap / body slam, used from range (he hops into the air
--            and crashes down on you).
--   * 3008 - point-blank "butt slam" / hip drop, used up close. Driven by
--            AttackTunableSpin so he rotates to slam toward you even if you
--            are behind/under him. (MeowScript's own tutorial labels 3008 the
--            "butt slam".)
--
-- Behaviour: if you are far he leaps to close the gap; once he lands and you
-- are within range he butt-slams. Roll away and he leaps again -> repeat.
--
-- REGISTER_GOAL must stay: it binds goal 223200 (GOAL_MiniGreaterDemon223200_Battle,
-- predefined in aiCommon/goal_list.lua) to the *Battle functions below. Without
-- it the engine never loads this AI. (When building via MeowScript instead, drop
-- these two lines and use the --@battle_goal directive, which injects them.)
--
-- Build: compiled to Lua 5.0 bytecode and repacked into m18_01_00_00.luabnd.dcx.
-- Tweak the 8.0 threshold to bias toward more leaps (raise) or butt slams (lower).

REGISTER_GOAL(GOAL_MiniGreaterDemon223200_Battle, "MiniGreaterDemon223200Battle")
REGISTER_GOAL_NO_UPDATE(GOAL_MiniGreaterDemon223200_Battle, 1)

function MiniGreaterDemon223200Battle_Activate(ai, goal)
    local targetDist = ai:GetDist(TARGET_ENE_0)
    if targetDist >= 8.0 then
        -- Far: leap into the air and body-slam down onto the target.
        goal:AddSubGoal(GOAL_COMMON_Attack, 10, 3007, TARGET_ENE_0, DIST_Middle, 0)
    else
        -- Close: spinning butt slam that tracks the target.
        goal:AddSubGoal(GOAL_COMMON_AttackTunableSpin, 10, 3008, TARGET_ENE_0, DIST_Middle, 0, -1)
    end
end

function MiniGreaterDemon223200Battle_Update(ai, goal)
    return GOAL_RESULT_Continue
end

function MiniGreaterDemon223200Battle_Terminate(ai, goal)
end

function MiniGreaterDemon223200Battle_Interupt(ai, goal)
    return false
end
