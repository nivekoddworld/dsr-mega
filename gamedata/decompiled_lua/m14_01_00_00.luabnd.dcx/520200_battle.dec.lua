REGISTER_GOAL(GOAL_MukadeSippo520200_Battle, "mukadesippo520200Battle")
local Att3000_Dist_min = 0
local Att3000_Dist_max = 0.6
local Att3001_Dist_min = 0
local Att3001_Dist_max = 0.6
local Att3002_Dist_min = 4
local Att3002_Dist_max = 5
REGISTER_GOAL_NO_UPDATE(GOAL_MukadeSippo520200_Battle, 1)

function mukadesippo520200Battle_Activate(ai, goal)
    local actPerArr = {}
    local actFuncArr = {}
    local defFuncParamTbl = {}
    Common_Clear_Param(actPerArr, actFuncArr, defFuncParamTbl)
    local targetDist = ai:GetDist(TARGET_ENE_0)
    if targetDist >= 4 then
        actPerArr[1] = 0
        actPerArr[3] = 0
        actPerArr[4] = 100
    else
        actPerArr[1] = 50
        actPerArr[3] = 50
        actPerArr[4] = 0
    end
    defFuncParamTbl[1] = {Att3000_Dist_max, 0, 3000, DIST_Middle}
    defFuncParamTbl[3] = {Att3001_Dist_max, 0, 3001, DIST_Middle}
    actFuncArr[4] = REGIST_FUNC(ai, goal, MukadeSippo520200_Act04)
    local atkAfterFunc = REGIST_FUNC(ai, goal, MukadeSippo520200_ActAfter_AdjustSpace, atkAfterOddsTbl)
    Common_Battle_Activate(ai, goal, actPerArr, actFuncArr, atkAfterFunc, defFuncParamTbl)
    
end

function MukadeSippo520200_Act04(ai, goal, paramTbl)
    local targetDist = ai:GetDist(TARGET_ENE_0)
    local fate = ai:GetRandam_Int(1, 100)
    local AppDist = Att3002_Dist_max
    local DashDist = Att3002_Dist_max + 2
    local Odds_Guard = 0
    Approach_Act(ai, goal, AppDist, DashDist, Odds_Guard)
    goal:AddSubGoal(GOAL_COMMON_Attack, 10, 3002, TARGET_ENE_0, DIST_Middle, 0)
    GetWellSpace_Odds = 100
    return GetWellSpace_Odds
    
end

function MukadeSippo520200_ActAfter_AdjustSpace(ai, goal, paramTbl)
    local targetDist = ai:GetDist(TARGET_ENE_0)
    local fate = ai:GetRandam_Int(1, 100)
    if fate <= 60 then
    elseif fate <= 75 then
        goal:AddSubGoal(GOAL_COMMON_SpinStep, 5, 701, TARGET_ENE_0, -1, AI_DIR_TYPE_B, 4)
    else
        goal:AddSubGoal(GOAL_COMMON_Wait, 2, TARGET_NONE, 0, 0, 0)
        return true
    end
    return false
    
end

function mukadesippo520200Battle_Update(ai, goal)
    return GOAL_RESULT_Continue
    
end

function mukadesippo520200Battle_Terminate(ai, goal)
    
end

function mukadesippo520200Battle_Interupt(ai, goal)
    local fate = ai:GetRandam_Int(1, 100)
    local fate2 = ai:GetRandam_Int(1, 100)
    local fate3 = ai:GetRandam_Int(1, 100)
    local targetDist = ai:GetDist(TARGET_ENE_0)
    local oddsResponse = 25
    local bkStepPer = 100
    local leftStepPer = 0
    local rightStepPer = 0
    local safetyDist = 4
    if RebByOpGuard_Step(ai, goal, oddsResponse, bkStepPer, leftStepPer, rightStepPer, safetyDist) then
        return true
    end
    return false
    
end


