REGISTER_GOAL(GOAL_COMMON_Turn, "Turn")
REGISTER_GOAL_UPDATE_TIME(GOAL_COMMON_Turn, 0.2, 0.5)
REGISTER_GOAL_NO_INTERUPT(GOAL_COMMON_Turn, true)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_Turn, 0, "????^?[?Q?b?g", 0)
REGISTER_DBG_GOAL_PARAM(GOAL_COMMON_Turn, 1, "???????p?x", 0)

function Turn_Update(ai, goal)
    local turnTarget = goal:GetParam(0)
    local angle_range = goal:GetParam(1)
    if angle_range <= 0 then
        angle_range = 10
    end
    ai:TurnTo(turnTarget)
    if ai:IsLookToTarget(angle_range) == true then
        return GOAL_RESULT_Success
    end
    return GOAL_RESULT_Continue
    
end

function Turn_Activate(ai, goal)
    
end

function Turn_Terminate(ai, goal)
    
end

function Turn_Interupt(ai, goal)
    return false
    
end


