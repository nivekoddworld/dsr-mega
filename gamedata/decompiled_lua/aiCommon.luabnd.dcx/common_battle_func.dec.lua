local DEF_MAX_ACT = 20

function Common_Clear_Param(ActXPer, funcActX, defFuncParamTbl)
    local i = 1
    for f1_local0 = 1, DEF_MAX_ACT, 1 do
        ActXPer[f1_local0] = 0
        funcActX[f1_local0] = nil
        defFuncParamTbl[f1_local0] = {}
    end
    

end

function Common_Battle_Activate(ai, goal, ActXPer, funcActX, atkAfterFunc, defFuncParamTbl)
    local funcActArr = {}
    local perActArr = {}
    local totalPerAct = 0

    local f2_local0 = {function ()
        return defAct01(ai, goal, defFuncParamTbl[1])
        
    end, function ()
        return defAct02(ai, goal, defFuncParamTbl[2])
        
    end, function ()
        return defAct03(ai, goal, defFuncParamTbl[3])
        
    end, function ()
        return defAct04(ai, goal, defFuncParamTbl[4])
        
    end, function ()
        return defAct05(ai, goal, defFuncParamTbl[5])
        
    end, function ()
        return defAct06(ai, goal, defFuncParamTbl[6])
        
    end, function ()
        return defAct07(ai, goal, defFuncParamTbl[7])
        
    end, function ()
        return defAct08(ai, goal, defFuncParamTbl[8])
        
    end, function ()
        return defAct09(ai, goal, defFuncParamTbl[9])
        
    end, function ()
        return defAct10(ai, goal, defFuncParamTbl[10])
        
    end, function ()
        return defAct11(ai, goal, defFuncParamTbl[11])
        
    end, function ()
        return defAct12(ai, goal, defFuncParamTbl[12])
        
    end, function ()
        return defAct12(ai, goal, defFuncParamTbl[13])
        
    end, function ()
        return defAct12(ai, goal, defFuncParamTbl[14])
        
    end, function ()
        return defAct12(ai, goal, defFuncParamTbl[15])
        
    end, function ()
        return defAct12(ai, goal, defFuncParamTbl[16])
        
    end, function ()
        return defAct12(ai, goal, defFuncParamTbl[17])
        
    end, function ()
        return defAct12(ai, goal, defFuncParamTbl[18])
        
    end, function ()
        return defAct12(ai, goal, defFuncParamTbl[19])
        
    end, function ()
        return defAct12(ai, goal, defFuncParamTbl[20])
        
    end}

    local i = 1
    for f2_local1 = 1, DEF_MAX_ACT, 1 do
        if funcActX[f2_local1] ~= nil then
            funcActArr[f2_local1] = funcActX[f2_local1]
        else
            funcActArr[f2_local1] = f2_local0[f2_local1]
        end
        perActArr[f2_local1] = ActXPer[f2_local1]
        totalPerAct = totalPerAct + perActArr[f2_local1]
    end
    local f2_local1 = nil
    if atkAfterFunc ~= nil then
        f2_local1 = atkAfterFunc
    else
        f2_local1 = function ()
            HumanCommon_ActAfter_AdjustSpace(ai, goal, atkAfterOddsTbl)
            
        end
    end
    local GetWellSpace_Odds = 0
    local doAdmirer = ai:GetExcelParam(AI_EXCEL_THINK_PARAM_TYPE__thinkAttr_doAdmirer)
    local role = ai:GetTeamOrder(ORDER_TYPE_Role)
    local Odds_Guard = 0
    if doAdmirer == 1 and role == ROLE_TYPE_Torimaki then
        Torimaki_Act(ai, goal, Odds_Guard)
    elseif doAdmirer == 1 and role == ROLE_TYPE_Kankyaku then
        Kankyaku_Act(ai, goal, Odds_Guard)
    else
        local forceActIdx = ai:DbgGetForceActIdx()
        if 0 < forceActIdx and forceActIdx <= DEF_MAX_ACT then
            GetWellSpace_Odds = funcActArr[forceActIdx]()
            ai:DbgSetLastActIdx(forceActIdx)
        else
            local fateAct = ai:GetRandam_Int(1, totalPerAct)
            local totalPer = 0
            local i = 1
            for f2_local4 = 1, DEF_MAX_ACT, 1 do
                totalPer = totalPer + perActArr[f2_local4]
                if fateAct <= totalPer then
                    GetWellSpace_Odds = funcActArr[f2_local4]()
                    ai:DbgSetLastActIdx(f2_local4)
                    f2_local4 = DEF_MAX_ACT
                end
            end
        end
    end
    local fateGWS = ai:GetRandam_Int(1, 100)
    if fateGWS <= GetWellSpace_Odds then
        f2_local1()
    end
    

end

function defAct01(ai, goal, paramTbl)
    local f24_local0 = {1.5, 0, 3000, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f24_local0 = paramTbl
    end
    local AppDist = f24_local0[1]
    local DashDist = f24_local0[1] + 2
    local Odds_Guard = f24_local0[2]
    local AttID = f24_local0[3]
    local AttDist = f24_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f24_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct02(ai, goal, paramTbl)
    local f25_local0 = {1.5, 0, 10, 40, nil, nil, nil, nil}
    if paramTbl[1] ~= nil then
        f25_local0 = paramTbl
    end
    return HumanCommon_Approach_and_ComboAtk(ai, goal, f25_local0)
    
end

function defAct03(ai, goal, paramTbl)
    local f26_local0 = {1.5, 0, 3005, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f26_local0 = paramTbl
    end
    local AppDist = f26_local0[1]
    local DashDist = f26_local0[1] + 2
    local Odds_Guard = f26_local0[2]
    local AttID = f26_local0[3]
    local AttDist = f26_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f26_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct04(ai, goal, paramTbl)
    local f27_local0 = {5, 0, 3007, DIST_Middle, 3005, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f27_local0 = paramTbl
    end
    local AppDist = f27_local0[1]
    local DashDist = f27_local0[1] + 2
    local Odds_Guard = f27_local0[2]
    local GBAID = f27_local0[3]
    local GBADist = f27_local0[4]
    local AttID = f27_local0[5]
    local AttDist = f27_local0[6]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f27_local0[7], 100)
    Approach_and_GuardBreak_Act(ai, goal, AppDist, DashDist, Odds_Guard, GBAID, GBADist, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct05(ai, goal, paramTbl)
    local f28_local0 = {4, 6, 0, 3008, DIST_None, nil}
    if paramTbl[1] ~= nil then
        f28_local0 = paramTbl
    end
    return HumanCommon_KeepDist_and_ThrowSomething(ai, goal, f28_local0)
    
end

function defAct06(ai, goal, paramTbl)
    local f29_local0 = {3000, DIST_Far, nil}
    if paramTbl[1] ~= nil then
        f29_local0 = paramTbl
    end
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f29_local0[3], 0)
    goal:AddSubGoal(GOAL_COMMON_Attack, 10, f29_local0[1], TARGET_ENE_0, f29_local0[2], 0)
    return GetWellSpace_Odds
    
end

function defAct07(ai, goal, paramTbl)
    local f30_local0 = {1.5, 0, 3001, DIST_Middle}
    if paramTbl[1] ~= nil then
        f30_local0 = paramTbl
    end
    local AppDist = f30_local0[1]
    local DashDist = f30_local0[1] + 2
    local Odds_Guard = f30_local0[2]
    local AttID = f30_local0[3]
    local AttDist = f30_local0[4]
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return 100
    
end

function defAct08(ai, goal, paramTbl)
    local f31_local0 = {nil}
    if paramTbl[1] ~= nil then
        f31_local0 = paramTbl
    end
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f31_local0[1], 0)
    Watching_Parry_Chance_Act(ai, goal)
    return GetWellSpace_Odds
    
end

function defAct09(ai, goal, paramTbl)
    local f32_local0 = {1.5, 0, 10, 40, nil, nil, nil, nil}
    if paramTbl[1] ~= nil then
        f32_local0 = paramTbl
    end
    return HumanCommon_Approach_and_ComboAtk(ai, goal, f32_local0)
    
end

function defAct10(ai, goal, paramTbl)
    local f33_local0 = {3000, 3001, 2, 4, 0}
    if paramTbl[1] ~= nil then
        f33_local0 = paramTbl
    end
    return HumanCommon_Shooting_Act(ai, goal, Tbl)
    
end

function defAct11(ai, goal, paramTbl)
    local f34_local0 = {3002, 3003, 2, 4, 0}
    if paramTbl[1] ~= nil then
        f34_local0 = paramTbl
    end
    return HumanCommon_Shooting_Act(ai, goal, Tbl)
    
end

function defAct12(ai, goal, paramTbl)
    local f35_local0 = {1.5, 0, 3001, DIST_Middle}
    if paramTbl[1] ~= nil then
        f35_local0 = paramTbl
    end
    local AppDist = f35_local0[1]
    local DashDist = f35_local0[1] + 2
    local Odds_Guard = f35_local0[2]
    local AttID = f35_local0[3]
    local AttDist = f35_local0[4]
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return 100
    
end

function defAct13(ai, goal, paramTbl)
    local f36_local0 = {1.5, 0, 3000, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f36_local0 = paramTbl
    end
    local AppDist = f36_local0[1]
    local DashDist = f36_local0[1] + 2
    local Odds_Guard = f36_local0[2]
    local AttID = f36_local0[3]
    local AttDist = f36_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f36_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct14(ai, goal, paramTbl)
    local f37_local0 = {1.5, 0, 3000, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f37_local0 = paramTbl
    end
    local AppDist = f37_local0[1]
    local DashDist = f37_local0[1] + 2
    local Odds_Guard = f37_local0[2]
    local AttID = f37_local0[3]
    local AttDist = f37_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f37_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct15(ai, goal, paramTbl)
    local f38_local0 = {1.5, 0, 3000, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f38_local0 = paramTbl
    end
    local AppDist = f38_local0[1]
    local DashDist = f38_local0[1] + 2
    local Odds_Guard = f38_local0[2]
    local AttID = f38_local0[3]
    local AttDist = f38_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f38_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct16(ai, goal, paramTbl)
    local f39_local0 = {1.5, 0, 3000, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f39_local0 = paramTbl
    end
    local AppDist = f39_local0[1]
    local DashDist = f39_local0[1] + 2
    local Odds_Guard = f39_local0[2]
    local AttID = f39_local0[3]
    local AttDist = f39_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f39_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct17(ai, goal, paramTbl)
    local f40_local0 = {1.5, 0, 3000, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f40_local0 = paramTbl
    end
    local AppDist = f40_local0[1]
    local DashDist = f40_local0[1] + 2
    local Odds_Guard = f40_local0[2]
    local AttID = f40_local0[3]
    local AttDist = f40_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f40_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct18(ai, goal, paramTbl)
    local f41_local0 = {1.5, 0, 3000, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f41_local0 = paramTbl
    end
    local AppDist = f41_local0[1]
    local DashDist = f41_local0[1] + 2
    local Odds_Guard = f41_local0[2]
    local AttID = f41_local0[3]
    local AttDist = f41_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f41_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct19(ai, goal, paramTbl)
    local f42_local0 = {1.5, 0, 3000, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f42_local0 = paramTbl
    end
    local AppDist = f42_local0[1]
    local DashDist = f42_local0[1] + 2
    local Odds_Guard = f42_local0[2]
    local AttID = f42_local0[3]
    local AttDist = f42_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f42_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function defAct20(ai, goal, paramTbl)
    local f43_local0 = {1.5, 0, 3000, DIST_Middle, nil}
    if paramTbl[1] ~= nil then
        f43_local0 = paramTbl
    end
    local AppDist = f43_local0[1]
    local DashDist = f43_local0[1] + 2
    local Odds_Guard = f43_local0[2]
    local AttID = f43_local0[3]
    local AttDist = f43_local0[4]
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(f43_local0[5], 100)
    Approach_and_Attack_Act(ai, goal, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    return GetWellSpace_Odds
    
end

function HumanCommon_KeepDist_and_ThrowSomething(ai, goal, paramTbl)
    local LeaDist = paramTbl[1]
    local AppDist = paramTbl[2]
    local DashDist = paramTbl[2] + 2
    local Odds_Guard = paramTbl[3]
    local AttID = paramTbl[4]
    local AttDist = paramTbl[5]
    KeepDist_and_Attack_Act(ai, goal, LeaDist, AppDist, DashDist, Odds_Guard, AttID, AttDist)
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(paramTbl[6], 0)
    return GetWellSpace_Odds
    
end

function HumanCommon_ActAfter_AdjustSpace(ai, goal, paramTbl)
    local Odds_Guard = paramTbl[1]
    local Odds_NoAct = paramTbl[2]
    local Odds_BackAndSide = paramTbl[3]
    local Odds_Back = paramTbl[4]
    local Odds_BitWait = paramTbl[5]
    local Odds_Backstep = paramTbl[6]
    GetWellSpace_Act(ai, goal, Odds_Guard, Odds_NoAct, Odds_BackAndSide, Odds_Back, Odds_BitWait, Odds_Backstep)
    
end

function HumanCommon_ActAfter_AdjustSpace_IncludeSidestep(ai, goal, paramTbl)
    local Odds_Guard = paramTbl[1]
    local Odds_NoAct = paramTbl[2]
    local Odds_BackAndSide = paramTbl[3]
    local Odds_Back = paramTbl[4]
    local Odds_BitWait = paramTbl[5]
    local Odds_Backstep = paramTbl[6]
    local Odds_Sidestep = paramTbl[7]
    GetWellSpace_Act_IncludeSidestep(ai, goal, Odds_Guard, Odds_NoAct, Odds_BackAndSide, Odds_Back, Odds_BitWait, Odds_Backstep, Odds_Sidestep)
    
end

function HumanCommon_Approach_and_ComboAtk(ai, goal, paramTbl)
    local AppDist = paramTbl[1]
    local DashDist = paramTbl[1] + 2
    local Odds_Guard = paramTbl[2]
    Approach_Act(ai, goal, AppDist, DashDist, Odds_Guard)
    local ID_Att01 = GET_PARAM_IF_NIL_DEF(paramTbl[5], 3000)
    local ID_Att02 = GET_PARAM_IF_NIL_DEF(paramTbl[6], 3001)
    local ID_Att03 = GET_PARAM_IF_NIL_DEF(paramTbl[7], 3002)
    local fate = ai:GetRandam_Int(1, 100)
    if fate <= paramTbl[3] then
        goal:AddSubGoal(GOAL_COMMON_Attack, 10, ID_Att01, TARGET_ENE_0, DIST_Middle, 0)
    elseif fate <= paramTbl[3] + paramTbl[4] then
        goal:AddSubGoal(GOAL_COMMON_ComboAttack, 10, ID_Att01, TARGET_ENE_0, DIST_Middle, 0)
        goal:AddSubGoal(GOAL_COMMON_ComboFinal, 10, ID_Att02, TARGET_ENE_0, DIST_Middle, 0)
    else
        goal:AddSubGoal(GOAL_COMMON_ComboAttack, 10, ID_Att01, TARGET_ENE_0, DIST_Middle, 0)
        goal:AddSubGoal(GOAL_COMMON_ComboRepeat, 10, ID_Att02, TARGET_ENE_0, DIST_Middle, 0)
        goal:AddSubGoal(GOAL_COMMON_ComboFinal, 10, ID_Att03, TARGET_ENE_0, DIST_Middle, 0)
    end
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(paramTbl[8], 100)
    return GetWellSpace_Odds
    
end

function HumanCommon_Watching_Parry_Chance_Act(ai, goal, paramTbl)
    Watching_Parry_Chance_Act(ai, goal)
    local GetWellSpace_Odds = GET_PARAM_IF_NIL_DEF(paramTbl[1], 100)
    return GetWellSpace_Odds
    
end

function HumanCommon_Shooting_Act(ai, goal, paramTbl)
    local FirstAttID = paramTbl[1]
    local ComAttID = paramTbl[2]
    local ShootNum = ai:GetRandam_Int(paramTbl[3], paramTbl[4])
    local AfterActID = paramTbl[5]
    Shoot_Act(ai, goal, FirstAttID, ComAttID, ShootNum)
    if AfterActID == 0 then
    elseif AfterActID == 1 then
        goal:AddSubGoal(GOAL_COMMON_LeaveTarget, 2, TARGET_ENE_0, 20, TARGET_ENE_0, true, -1)
    elseif AfterActID == 2 then
        goal:AddSubGoal(GOAL_COMMON_LeaveTarget, 5, TARGET_ENE_0, 20, TARGET_SELF, false, -1)
    else
        ai:PrintText("??logical error, get the manager!?? ")
    end
    return 0
    
end

function GET_PARAM_IF_NIL_DEF(param, defaultParam)
    if param ~= nil then
        return param
    end
    return defaultParam
    
end

function REGIST_FUNC(ai, goal, funcName, paramTbl)
    return function ()
        return funcName(ai, goal, paramTbl)
        
    end

    
end


