REGISTER_LOGIC_FUNC(LOGIC_ID_PondAunt6530, "PondAunt6530_Logic", "PondAunt6530_Interupt")

function PondAunt6530_Logic(ai)
    ai:AddTopGoal(GOAL_PondAunt6530_Battle, -1, 0, 0, 0, 0)
    
end

function PondAunt6530_Interupt(ai, goal)
    
end


