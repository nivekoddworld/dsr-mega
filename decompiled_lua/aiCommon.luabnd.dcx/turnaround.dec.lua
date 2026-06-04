REGISTER_GOAL(GOAL_COMMON_TurnAround, "TurnAround")
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_TurnAround, 0, "??????yType?z", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_TurnAround, 1, "????????yType?z", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_TurnAround, 2, "?p?x?y?x?z", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_TurnAround, 3, "?????", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_TurnAround, 4, "??????????H", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_TurnAround, 5, "?K?[?hEzState???", 0)
REGISTER_GOAL_UPDATE_TIME(GOAL_COMMON_TurnAround, 0.1, 0.2)

function TurnAround_Activate(ai, goal)
    local targetType = goal:GetParam(0)
    local targetDir = goal:GetParam(1)
    local bWalk = goal:GetParam(3)
    local guardActionId = goal:GetParam(5)
    if guardActionId > 0 then
        goal:AddSubGoal(GOAL_COMMON_Guard, goal:GetLife(), guardActionId, targetType, 0, 0)
    end
    ai:TurnTo(targetType)
    local move_dir = ai:GetTurnAroundOptimizedDirection(targetType, targetDir)
    ai:SetMoveLROnly(true)
    ai:MoveTo(TARGET_SELF, move_dir, 10, bWalk, 0)
    if move_dir == AI_DIR_TYPE_L then
        goal:AddGoalScopedTeamRecord(COORDINATE_TYPE_SideWalk_L, targetType, 0)
    elseif move_dir == AI_DIR_TYPE_R then
        goal:AddGoalScopedTeamRecord(COORDINATE_TYPE_SideWalk_R, targetType, 0)
    end
    
end

function TurnAround_Update(ai, goal, dT)
    local dirType = goal:GetParam(1)
    local turnAng = goal:GetParam(2)
    local bLifeSucces = goal:GetParam(4)
    local distRange = 999
    if ai:IsInsideTargetEx(targetType, TARGET_SELF, dirType, turnAng, distRange) then
        return GOAL_RESULT_Success
    end
    if bLifeSucces ~= 0 and goal:GetLife() <= 0 then
        return GOAL_RESULT_Success
    end
    if ai:CannotMove() then
        return GOAL_RESULT_Success
    end
    return GOAL_RESULT_Continue
    
end

function TurnAround_Terminate(ai, goal)
    
end

REGISTER_GOAL_NO_INTERUPT(GOAL_COMMON_TurnAround, true)

function TurnAround_Interupt(ai, goal)
    return false
    
end


