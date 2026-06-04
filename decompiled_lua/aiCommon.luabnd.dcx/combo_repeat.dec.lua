REGISTER_GOAL(GOAL_COMMON_ComboRepeat, "ComboRepeat")
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_ComboRepeat, 0, "EzStateID", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_ComboRepeat, 1, "?U?????", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_ComboRepeat, 2, "????????", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_ComboRepeat, 3, "??U???p?x", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_ComboRepeat, 4, "???U???p?x", 0)
REGISTER_GOAL_NO_UPDATE(GOAL_COMMON_ComboRepeat, 1)
ENABLE_COMBO_ATK_CANCEL(GOAL_COMMON_ComboRepeat)

function ComboRepeat_Activate(ai, goal)
    local life = goal:GetLife()
    local behaviorNo = goal:GetParam(0)
    local attackTarget = goal:GetParam(1)
    local distType = goal:GetParam(2)
    local successAngle = 90
    local turnTime = 0
    local turnFaceAngle = 90
    local isComboAttack = true
    local isMoveCancel = true
    local isGaurdBreakAtk = false
    local isTurn = false
    local angleUpAtt = goal:GetParam(3)
    local angleDownAtt = goal:GetParam(4)
    goal:AddSubGoal(GOAL_COMMON_CommonAttack, life, behaviorNo, attackTarget, distType, successAngle, turnTime, turnFaceAngle, isComboAttack, isMoveCancel, isGaurdBreakAtk, isTurn, angleUpAtt, angleDownAtt)
    
end

function ComboRepeat_Update(ai, goal)
    return GOAL_RESULT_Continue
    
end

function ComboRepeat_Terminate(ai, goal)
    
end

REGISTER_GOAL_NO_INTERUPT(GOAL_COMMON_ComboRepeat, true)

function ComboRepeat_Interupt(ai, goal)
    return false
    
end


