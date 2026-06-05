REGISTER_GOAL(GOAL_HusiMino_Kon253000_Battle, "HusiMino_Kon253000Battle")
local Att3000_Dist_min = 0
local Att3000_Dist_max = 1.5
local Att3003_Dist_min = 2.5
local Att3003_Dist_max = 3.5
local Att3004_Dist_min = 3.5
local Att3004_Dist_max = 6
local Att3005_Dist_min = 0
local Att3005_Dist_max = 1.2
local Att3006_Dist_min = 0
local Att3006_Dist_max = 1
local Att3007_Dist_min = 4
local Att3007_Dist_max = 6
REGISTER_GOAL_NO_UPDATE(GOAL_HusiMino_Kon253000_Battle, 1)

function HusiMino_Kon253000Battle_Activate(ai, goal)
    local actPerArr = {}
    local actFuncArr = {}
    local defFuncParamTbl = {}
    Common_Clear_Param(actPerArr, actFuncArr, defFuncParamTbl)
    local targetDist = ai:GetDist(TARGET_ENE_0)
    if targetDist >= 8 then
        actPerArr[7] = 60
        actPerArr[2] = 10
        actPerArr[3] = 0
        actPerArr[4] = 30
    elseif targetDist >= 5 then
        if ai:IsTargetGuard(TARGET_ENE_0) == true then
            actPerArr[7] = 30
            actPerArr[2] = 10
            actPerArr[3] = 10
            actPerArr[4] = 50
        else
            actPerArr[7] = 50
            actPerArr[2] = 20
            actPerArr[3] = 20
            actPerArr[4] = 10
        end
    elseif targetDist >= 2.5 then
        if ai:IsTargetGuard(TARGET_ENE_0) == true then
            actPerArr[7] = 10
            actPerArr[2] = 20
            actPerArr[3] = 20
            actPerArr[4] = 50
        else
            actPerArr[7] = 30
            actPerArr[2] = 40
            actPerArr[3] = 20
            actPerArr[4] = 10
        end
    elseif ai:IsTargetGuard(TARGET_ENE_0) == true then
        actPerArr[7] = 0
        actPerArr[2] = 25
        actPerArr[3] = 25
        actPerArr[4] = 50
    else
        actPerArr[7] = 0
        actPerArr[2] = 60
        actPerArr[3] = 30
        actPerArr[4] = 10
    end
    defFuncParamTbl[2] = {Att3000_Dist_max, 0, 10, 40, nil, nil, nil, nil}
    defFuncParamTbl[3] = {Att3005_Dist_max, 0, 3005, DIST_Middle, nil}
    actFuncArr[4] = REGIST_FUNC(ai, goal, HusiMino_Kon253000_Act04)
    actFuncArr[7] = REGIST_FUNC(ai, goal, HusiMino_Kon253000_Act07)
    local f1_local0 = {0, 50, 0, 10, 10, 15, 15}
    local atkAfterFunc = REGIST_FUNC(ai, goal, HumanCommon_ActAfter_AdjustSpace_IncludeSidestep, f1_local0)
    Common_Battle_Activate(ai, goal, actPerArr, actFuncArr, atkAfterFunc, defFuncParamTbl)
    
end

function HusiMino_Kon253000_Act04(ai, goal, paramTbl)
    local targetDist = ai:GetDist(TARGET_ENE_0)
    local fate = ai:GetRandam_Int(1, 100)
    local fate2 = ai:GetRandam_Int(1, 100)
    if Att3007_Dist_min <= targetDist then
        local AppDist = Att3007_Dist_max
        local DashDist = Att3007_Dist_max + 2
        local Odds_Guard = 0
        local AttID = 3007
        local AttDist = DIST_Middle
        Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    else
        local AppDist = Att3006_Dist_max
        local DashDist = Att3006_Dist_max + 2
        local Odds_Guard = 0
        local AttID = 3006
        local AttDist = DIST_Middle
        Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    end
    GetWellSpace_Odds = 100
    return GetWellSpace_Odds
    
end

function HusiMino_Kon253000_Act07(ai, goal, paramTbl)
    local targetDist = ai:GetDist(TARGET_ENE_0)
    local fate = ai:GetRandam_Int(1, 100)
    local fate2 = ai:GetRandam_Int(1, 100)
    if Att3004_Dist_min <= targetDist and fate <= 40 then
        local AppDist = Att3004_Dist_max
        local DashDist = Att3004_Dist_max + 2
        local Odds_Guard = 0
        local AttID = 3004
        local AttDist = DIST_Middle
        Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    else
        local AppDist = Att3003_Dist_max
        local DashDist = Att3003_Dist_max + 2
        local Odds_Guard = 0
        local AttID = 3003
        local AttDist = DIST_Middle
        Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    end
    GetWellSpace_Odds = 100
    return GetWellSpace_Odds
    
end

function HusiMino_Kon253000Battle_Update(ai, goal)
    return GOAL_RESULT_Continue
    
end

function HusiMino_Kon253000Battle_Terminate(ai, goal)
    
end

function HusiMino_Kon253000Battle_Interupt(ai, goal)
    local fate = ai:GetRandam_Int(1, 100)
    local fate2 = ai:GetRandam_Int(1, 100)
    local fate3 = ai:GetRandam_Int(1, 100)
    local targetDist = ai:GetDist(TARGET_ENE_0)
    local distDamagedStep = 3
    local oddsDamagedStep = 15
    local bkStepPer = 30
    local leftStepPer = 35
    local rightStepPer = 35
    local safetyDist = 4
    if Damaged_Step(ai, goal, distDamagedStep, oddsDamagedStep, bkStepPer, leftStepPer, rightStepPer, safetyDist) then
        return true
    end
    return false
    
end


