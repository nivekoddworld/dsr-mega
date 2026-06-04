REGISTER_GOAL(GOAL_COMMON_Wait, "Wait")
REGISTER_GOAL(GOAL_COMMON_Stay, "Stay")
REGISTER_GOAL(GOAL_COMMON_MoveToSomewhere, "MoveToSomewhere")
REGISTER_GOAL(GOAL_COMMON_Guard, "Guard")
REGISTER_GOAL(GOAL_COMMON_SidewayMove, "SidewayMove")
REGISTER_GOAL(GOAL_COMMON_KeepDist, "KeepDist")
REGISTER_GOAL(GOAL_COMMON_BackToHome, "BackToHome")
REGISTER_GOAL(GOAL_COMMON_SpinStep, "SpinStep")
REGISTER_GOAL(GOAL_COMMON_LeaveTarget, "LeaveTarget")
REGISTER_GOAL(GOAL_COMMON_ApproachStep, "ApproachStep")
REGISTER_GOAL(GOAL_COMMON_Parry, "Parry")

function _COMMON_GetEzStateAnimId(ai, animTblParamId)
    ret = {}
    local i = 1
    for f1_local0 = 1, 30, 1 do
        ret[f1_local0] = ai:GetEzStateAnimId(animTblParamId, f1_local0 - 1)
    end
    return ret
    

end

function _COMMON_GetMinDist(ai, animTblParamId)
    ret = {}
    local i = 1
    for f2_local0 = 1, 30, 1 do
        ret[f2_local0] = ai:GetMinDist(animTblParamId, f2_local0 - 1)
    end
    return ret
    

end

function _COMMON_GetMaxDist(ai, animTblParamId)
    ret = {}
    local i = 0
    for f3_local0 = 0, 29, 1 do
        ret[f3_local0] = ai:GetMaxDist(animTblParamId, f3_local0 - 1)
    end
    return ret
    

end

function _COMMON_GetAtkDistType(ai, animTblParamId)
    ret = {}
    local i = 1
    for f4_local0 = 1, 30, 1 do
        ret[f4_local0] = ai:GetAtkDistType(animTblParamId, f4_local0 - 1)
        if ret[f4_local0] == 0 then
            ret[f4_local0] = DIST_Near
        elseif ret[f4_local0] == 1 then
            ret[f4_local0] = DIST_Middle
        elseif ret[f4_local0] == 2 then
            ret[f4_local0] = DIST_Far
        elseif ret[f4_local0] == 3 then
            ret[f4_local0] = DIST_Out
        elseif ret[f4_local0] == 4 then
            ret[f4_local0] = DIST_None
        end
    end
    return ret
    

end

function _COMMON_GetOddsParam(ai, oddsParamId)
    ret = {}
    local oddsParamOffset = ai:GetOddsParamIdOffset(100)
    local i = 0
    for f5_local0 = 0, 99, 1 do
        ret[f5_local0] = ai:GetOddsParam(oddsParamOffset + oddsParamId, f5_local0)
    end
    return ret
    

end

function _COMMON_MulOddsXWeight(inoutAct, inFinal)
    local i = 0
    local bInitFinal = true
    if table.getn(inFinal) == 0 then
        bInitFinal = false
    end
    local totalActVal = 0
    local maxActVal = 0
    for f6_local0 = 0, 99, 1 do
        if bInitFinal == false then
            inFinal[f6_local0] = 1
        end
        inoutAct[f6_local0] = inoutAct[f6_local0] * inFinal[f6_local0]
        if inoutAct[f6_local0] < 0 then
            inoutAct[f6_local0] = 0
        end
        inoutAct[f6_local0] = inoutAct[f6_local0] + totalActVal
        totalActVal = inoutAct[f6_local0]
        if maxActVal < inoutAct[f6_local0] then
            maxActVal = inoutAct[f6_local0]
        end
    end
    return maxActVal
    

end

function _COMMON_MulWeightParam(ai, final, oddsParamId)
    local zeroTbl = false
    if table.getn(final) == 0 then
        zeroTbl = true
    end
    local oddsParamOffset = ai:GetOddsParamIdOffset(100)
    local i = 0
    for f7_local0 = 0, 99, 1 do
        if zeroTbl then
            final[f7_local0] = 1
        end
        final[f7_local0] = final[f7_local0] * ai:GetOddsParam(oddsParamOffset + oddsParamId, f7_local0)
    end
    

end


