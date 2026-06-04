REGISTER_GOAL(GOAL_COMMON_NonBattleAct, "NonBattleAct")
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_NonBattleAct, 0, "?G????????ûÐ???ym?z", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_NonBattleAct, 1, "?G??????I???H", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_NonBattleAct, 2, "????H", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_NonBattleAct, 3, "????yTYPE?z", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_NonBattleAct, 4, "????????????ym?z", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_NonBattleAct, 5, "??@???S?[??", 0)
REGISTER_GOAL_UPDATE_TIME(GOAL_COMMON_NonBattleAct, 0.1, 0.2)

function NonBattleAct_Activate(ai, goal)
    local movePointNo = ai:GetMovePointNumber()
    if movePointNo >= 0 then
        local isWalk = goal:GetParam(2)
        if isWalk == 0 then
            isWalk = true
        else
            isWalk = false
        end
        goal:AddSubGoal(GOAL_COMMON_MoveToSomewhere, goal:GetLife(), POINT_MOVE_POINT, AI_DIR_TYPE_CENTER, 0, TARGET_SELF, isWalk)
    else
        local pointDist = ai:GetMovePointEffectRange()
        local turn_tgt = goal:GetParam(3)
        local arrive_dist = goal:GetParam(4)
        if arrive_dist == 0 then
            arrive_dist = 5
        end
        if arrive_dist < pointDist then
            local isEnableForceBattleGoal = goal:GetParam(6)
            goal:AddSubGoal(GOAL_COMMON_BackToHome, goal:GetLife(), 0, isEnableForceBattleGoal, 0, 0)
        elseif goal:IsExistParam(5) then
            local wait_goalID = goal:GetParam(5)
            if wait_goalID > 0 then
                goal:AddSubGoal(wait_goalID, goal:GetLife())
            else
                goal:AddSubGoal(GOAL_COMMON_Stay, goal:GetLife(), 0, turn_tgt)
            end
        else
            goal:AddSubGoal(GOAL_COMMON_Stay, goal:GetLife(), 0, turn_tgt)
        end
    end
    
end

function NonBattleAct_Update(ai, goal)
    return GOAL_RESULT_Continue
    
end

function NonBattleAct_Terminate(ai, goal)
    
end

function NonBattleAct_Interupt(ai, goal)
    local isDamaged = false
    isDamaged = isDamaged or ai:IsInterupt(INTERUPT_Damaged_Stranger) or ai:IsInterupt(INTERUPT_Damaged)
    if isDamaged then
        goal:SetTimer(1, 99999)
        return true
    end
    return false
    
end


