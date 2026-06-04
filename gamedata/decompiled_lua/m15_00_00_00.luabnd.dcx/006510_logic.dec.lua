REGISTER_LOGIC_FUNC(LOGIC_ID_BlackIron_Knight6510, "BlackIron_Knight6510_Logic", "BlackIron_Knight6510_Interupt")

function BlackIron_Knight6510_Logic(ai)
    ai:AddTopGoal(GOAL_BlackIron_Knight6510_Battle, -1, 0, 0, 0, 0)
    
end

function BlackIron_Knight6510_Interupt(ai, goal)
    
end


