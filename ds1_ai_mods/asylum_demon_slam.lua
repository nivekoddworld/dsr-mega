--@package: m18_01_00_00.luabnd, 223200_battle.lua
--@battle_goal: 223200, MiniGreaterDemon223200Battle

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
-- Build it: drag this file onto MeowScript_Build.exe (with DarkSoulsDataPath
-- pointing at your UXM-extracted DSR install). Then trigger any loading screen
-- in-game to hot-reload the AI and walk into the Asylum Demon fight.
--
-- Tweak: change the 8.0 threshold below to bias toward more leaps (raise it)
-- or more butt slams (lower it). To make him do ONLY one move, delete the
-- branch you don't want and unconditionally AddSubGoal the other.

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
