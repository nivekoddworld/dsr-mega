ClearBossId = -1
ClearBoss = false
isKill_host = false
deathState = 0
isDeathPenaltySkip = 0
DeadtextEffectId = TEXT_TYPE_Dead

function g_Initialize(proxy)
    print("g_Initialize global_event begin")
    proxy:AddBlockClearBonus()
    proxy:CheckPenalty()
    deathState = proxy:GetDeathState()
    isDeathPenaltySkip = 0
    proxy:InitDeathState()
    proxy:OnCharacterDead(99999, LOCAL_PLAYER, "OnEvent_4000", everytime)
    proxy:NotNetMessage_begin()
    proxy:OnCharacterHP(99999, LOCAL_PLAYER, "OnEvent_4000_Hp", 0, once)
    proxy:NotNetMessage_end()
    proxy:LuaCall(4090, INVADE_TYPE_None, "OnDeadEvent_HostDead", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_NormalWhite, "OnDeadEvent_WhiteDead", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_NormalBlack, "OnDeadEvent_NormalBlackDead", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_ForceJoinBlack, "OnDeadEvent_ForceJoinBlackDead", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_DetectBlack, "OnDeadEvent_dummy", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_WhiteRescue, "OnDeadEvent_dummy", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_BlackRescue, "OnDeadEvent_dummy", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_Nito, "OnDeadEvent_InvadeNitoDead", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_ThievesGuild, "OnDeadEvent_ThievesGuildDead", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_OtoutoUmbasa, "OnDeadEvent_OtoutoUmbasaDead", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_Dragonewt, "OnDeadEvent_DragonewtDead", everytime)
    proxy:LuaCall(4090, INVADE_TYPE_InvadeBounty, "OnDeadEvent_InvadeBounty", everytime)
    proxy:LuaCall(4091, 0, "OnNitoInvadeItemLot", everytime)
    proxy:LuaCall(4000, 1, "HostDead", everytime)
    proxy:OnCheckEzStateMessage(4001, LOCAL_PLAYER, "OnEvent_4001", LOCAL_PLAYER)
    proxy:OnActionCheckKey(4002, LOCAL_PLAYER, "OnEvent_4002", 1, 1)
    proxy:OnActionCheckKey(4003, LOCAL_PLAYER, "OnEvent_4003", 2, 1)
    proxy:OnCheckEzStateMessage(4004, LOCAL_PLAYER, "OnEvent_4004", 0)
    proxy:OnCheckEzStateMessage(4005, LOCAL_PLAYER, "OnEvent_4005", 1)
    proxy:LuaCall(4010, 1, "OnEvent_4010_1", everytime)
    proxy:LuaCall(4010, 2, "OnEvent_4010_2", everytime)
    proxy:LuaCall(4010, 11, "OnEvent_4010_11", everytime)
    proxy:LuaCall(4010, 12, "OnEvent_4010_12", everytime)
    proxy:LuaCall(4012, 1, "OnEvent_4012", everytime)
    proxy:LuaCall(4013, 1, "OnEvent_4013", everytime)
    proxy:LuaCall(4014, 1, "OnEvent_4014", everytime)
    proxy:CustomLuaCall(4013, "SynchroAnim_4013", everytime)
    proxy:CustomLuaCall(4014, "SynchroAnim_4014", everytime)
    proxy:LuaCall(4015, 1, "OnEvent_4015", everytime)
    proxy:LuaCall(4016, 1, "OnEvent_4016", everytime)
    proxy:LuaCall(4017, 1, "OnEvent_4017", everytime)
    proxy:LuaCall(4018, 1, "OnEvent_4018", everytime)
    proxy:LuaCall(4019, 1, "OnEvent_4019", everytime)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4030, "g_second_Initialize", 0.1, 0, 1, once)
    proxy:OnSelfBloodMark(4032, "SelfBloodMark", ATTR_LIVE + ATTR_GREY, everytime)
    proxy:OnSelfHeroBloodMark(4077, "", ATTR_LIVE + ATTR_GREY, everytime)
    proxy:OnSessionJustIn(4034, "OnEvent_4034", everytime)
    proxy:OnSessionJustOut(4035, "OnEvent_4035", everytime)
    proxy:NotNetMessage_end()
    proxy:NotNetMessage_begin()
    proxy:OnSessionInfo(4038, "OnEvent_4038")
    proxy:NotNetMessage_end()
    proxy:CustomLuaCall(4050, "BlockClear2", everytime)
    proxy:LuaCall(4050, 20, "BlockClearSynchroAnime", everytime)
    proxy:LuaCall(4050, 30, "BlockClearSynchroInvalid", everytime)
    proxy:CustomLuaCall(4078, "MediumBossDestroy", everytime)
    proxy:LuaCall(4041, 1, "SummonInfoMsg_White", everytime)
    proxy:LuaCall(4041, 2, "SummonInfoMsg_Black", everytime)
    proxy:LuaCall(4041, 3, "SummonInfoMsg_ForceJoinBlack", everytime)
    proxy:LuaCall(4041, 4, "SummonInfoMsg_ForceSummonBlack", everytime)
    proxy:LuaCall(4041, 7, "SummonInfoMsg_InvadeNito", everytime)
    proxy:LuaCall(4041, 10, "SummonInfoMsg_Dragonewt", everytime)
    proxy:LuaCall(4041, 11, "SummonInfoMsg_InvadeBounty", everytime)
    proxy:LuaCall(4041, 12, "SummonInfoMsg_Coliseum", everytime)
    proxy:LuaCall(4042, 1, "DeadInfoMsg_White", everytime)
    proxy:LuaCall(4042, 2, "DeadInfoMsg_Black", everytime)
    proxy:LuaCall(4042, 3, "DeadInfoMsg_Host", everytime)
    proxy:LuaCall(4042, 4, "dummy", everytime)
    proxy:LuaCall(4042, 5, "dummy", everytime)
    proxy:LuaCall(4042, 6, "DeadInfoMsg_ForceJoinBlack", everytime)
    proxy:LuaCall(4042, 7, "DeadInfoMsg_InvadeNito", everytime)
    proxy:LuaCall(4042, 10, "DeadInfoMsg_Dragonewt", everytime)
    proxy:LuaCall(4042, 11, "DeadInfoMsg_InvadeBounty", everytime)
    proxy:NotNetMessage_begin()
    proxy:LuaCall(4043, 1, "OnLeavePlayer", everytime)
    proxy:LuaCall(4043, 2, "OnLeavePlayer", everytime)
    proxy:LuaCall(4043, 3, "OnLeavePlayer", everytime)
    proxy:LuaCall(4043, 4, "dummy", everytime)
    proxy:LuaCall(4043, 5, "dummy", everytime)
    proxy:NotNetMessage_end()
    proxy:LuaCall(4044, 1, "OnKickOut", everytime)
    proxy:LuaCall(4044, 2, "OnThxKickOut", everytime)
    proxy:LuaCall(4068, 1, "ReportBossArea", everytime)
    proxy:LuaCall(4046, 1, "LeaveMessage", everytime)
    proxy:LuaCall(4046, 2, "LeaveMessage", everytime)
    proxy:LuaCall(4046, 3, "dummy", everytime)
    proxy:LuaCall(4055, 1, "WhiteReviveCount", everytime)
    proxy:LuaCall(4058, 1, "Call_WhiteSos", everytime)
    proxy:LuaCall(4058, 2, "Call_BlackSos", everytime)
    proxy:LuaCall(4058, 3, "Call_Dragonewt", everytime)
    proxy:CustomLuaCall(4063, "OnGameLeave", everytime)
    proxy:LuaCall(4064, 1, "OnDisableInvincible", everytime)
    proxy:LuaCall(4064, 2, "OnEnableDraw", everytime)
    proxy:LuaCall(4064, 3, "OnMatchingCheck", everytime)
    proxy:LuaCall(4064, 4, "OnMatchingError", everytime)
    proxy:CustomLuaCall(4065, "OnEnterRideObj", everytime)
    proxy:CustomLuaCall(4066, "OnLeaveRideObj", everytime)
    proxy:NotNetMessage_begin()
    proxy:OnCheckEzStateMessage(4080, LOCAL_PLAYER, "OnEvent_BonfireFirstLvUp", 30)
    proxy:OnCheckEzStateMessage(4081, LOCAL_PLAYER, "OnEvent_BonfireLvUp", 10)
    proxy:OnCheckEzStateMessage(4082, LOCAL_PLAYER, "OnEvent_BonfireRespawn", 20)
    proxy:OnCheckEzStateMessage(4085, LOCAL_PLAYER, "Lua_Warp_1", 40)
    proxy:NotNetMessage_end()
    proxy:CustomLuaCall(10000, "AI_TadareBoss_SetNearEventPoint_CallByAi", everytime)
    proxy:SetPartyRestrictNum(4)
    print("g_Initialize global_event end")
    
end

function InGameStart(proxy, param)
    print("InGameStart begin")
    proxy:LuaCallStart(4064, 1)
    if proxy:IsAliveMotion() == true then
        print("Condition_AliveMotion Alive")
        proxy:SetTextEffect(TEXT_TYPE_Revival)
        proxy:PlayAnimation(LOCAL_PLAYER, 6950)
        proxy:SetAliveMotion(false)
    else
        print("Condition_AliveMotion Not Alive")
        if proxy:IsLivePlayer() == true then
            proxy:PlayAnimation(LOCAL_PLAYER, 6950)
            proxy:NotNetMessage_begin()
            proxy:OnChrAnimEnd(SYSTEM_WARP, LOCAL_PLAYER, 6950, "AliveMotion_End", once)
            proxy:NotNetMessage_end()
        elseif proxy:IsGreyGhost() == true then
            if proxy:IsReviveWait() == true then
                proxy:RevivePlayer()
            end
            proxy:PlayAnimation(LOCAL_PLAYER, 6950)
            proxy:NotNetMessage_begin()
            proxy:OnChrAnimEnd(SYSTEM_WARP, LOCAL_PLAYER, 6950, "AliveMotion_End", once)
            proxy:NotNetMessage_end()
        elseif proxy:IsWhiteGhost() == true then
            print("IsWhiteAnim")
            proxy:NotNetMessage_begin()
            proxy:OnKeyTime2(4013, "SummonMotion_wait", 0.1, 0, 0, once)
            proxy:NotNetMessage_end()
        elseif proxy:IsBlackGhost() == true then
            print("IsBlackAnim")
            proxy:NotNetMessage_begin()
            proxy:OnKeyTime2(4013, "SummonMotion_wait", 0.1, 0, 0, once)
            proxy:NotNetMessage_end()
            if proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
                proxy:SetEventSpecialEffect(LOCAL_PLAYER, 10180)
                REMO_FLAG = 2
                proxy:LuaCallStart(5500, 0)
                proxy:LuaCallStart(1030, 2)
            end
        elseif proxy:IsIntruder() == true then
            print("IsIntruder")
            proxy:NotNetMessage_begin()
            proxy:OnKeyTime2(4013, "SummonMotion_wait", 0.1, 0, 0, once)
            proxy:NotNetMessage_end()
        elseif proxy:IsColiseumGhost() == true then
            print("IsColiseum")
            proxy:NotNetMessage_begin()
            proxy:OnKeyTime2(4013, "SummonMotion_wait", 0.1, 0, 0, once)
            proxy:NotNetMessage_end()
        end
    end
    proxy:ParamInitialize()
    if proxy:IsGreyGhost() == true or proxy:IsWhiteGhost() == true then
        print("SetEventSpecialEffect LOCAL_PLAYER, 101")
        proxy:SetEventSpecialEffect(LOCAL_PLAYER, 101)
    elseif proxy:IsBlackGhost() == true or proxy:IsIntruder() == true or proxy:IsIntruder() == true then
        print("SetEventSpecialEffect LOCAL_PLAYER, 102")
        proxy:SetEventSpecialEffect(LOCAL_PLAYER, 102)
    end
    print("InGameStart end")
    
end

function OnDisableInvincible(proxy, param)
    print("OnDisableInvincible begin")
    if param:IsNetMessage() == false then
        proxy:EnableInvincible(LOCAL_PLAYER, false)
    else
        proxy:EnableInvincible(param:GetPlayID() + NET_PLAYER, false)
    end
    print("OnDisableInvincible end")
    
end

function OnEnableDraw(proxy, param)
    print("OnEnableDraw begin")
    if param:IsNetMessage() == false then
        print("OnEnableDraw IsNetMessage false")
        if proxy:IsArenaActive() == false then
            proxy:SetDrawEnable(LOCAL_PLAYER, true)
        else
            proxy:SetDrawEnable(LOCAL_PLAYER, false)
        end
    else
        print("OnEnableDraw BEFORE DRAW CHECK")
        if proxy:IsArenaActive() == false then
            proxy:SetDrawEnable(param:GetPlayID() + NET_PLAYER, true)
            print("OnEnableDraw WRONG")
        else
            proxy:SetDrawEnable(param:GetPlayID() + NET_PLAYER, false)
        end
        if proxy:IsClient() == false then
            SummonSuccess(proxy, param:GetPlayID())
        end
    end
    print("OnEnableDraw end")
    
end

function OnMatchingCheck(proxy, param)
    print("OnMatchingCheck begin")
    if param:IsNetMessage() then
        if proxy:IsClient() == false and proxy:IsMatchingMultiPlay(proxy:GetHostPlayerNo(), param:GetPlayID()) == false then
            proxy:LuaCallStartPlus(4064, 4, param:GetPlayID())
        end
    elseif proxy:IsMatchingMultiPlay(proxy:GetHostPlayerNo(), param:GetPlayID()) == false then
        proxy:LeaveSession()
    end
    print("OnMatchingCheck end")
    
end

function OnMatchingError(proxy, param)
    print("OnMatchingError begin")
    if proxy:GetLocalPlayerId() == param:GetParam3() then
        print("OnMatchingError LeaveSession")
        proxy:LeaveSession()
    end
    print("OnMatchingError end")
    
end

function AliveMotion_wait(proxy, param)
    print("AliveMotion_wait begin")
    proxy:LuaCallStart(4064, 2)
    if proxy:IsClient() == true then
        proxy:LuaCallStart(4064, 3)
    end
    proxy:CustomLuaCallStartPlus(4013, LOCAL_PLAYER, 6950)
    proxy:NotNetMessage_begin()
    proxy:OnChrAnimEnd(SYSTEM_WARP, LOCAL_PLAYER, 6950, "AliveMotion_End", once)
    proxy:NotNetMessage_end()
    print("AliveMotion_wait end")
    
end

function SummonMotion_wait(proxy, param)
    print("SummonMotion_wait begin")
    proxy:LuaCallStart(4064, 2)
    if proxy:IsClient() == true then
        proxy:LuaCallStart(4064, 3)
    end
    local summonAnimId = proxy:GetSummonAnimId()
    proxy:CustomLuaCallStartPlus(4013, LOCAL_PLAYER, summonAnimId)
    proxy:NotNetMessage_begin()
    proxy:OnChrAnimEnd(SYSTEM_WARP, LOCAL_PLAYER, summonAnimId, "AliveMotion_End", once)
    proxy:NotNetMessage_end()
    print("SummonMotion_wait end")
    
end

function AliveMotion_End(proxy, param)
    print("AliveMotion_End begin")
    if proxy:IsClient() == true then
        proxy:ChrResetAnimation(LOCAL_PLAYER)
    end
    if proxy:HavePartyMember() == true then
        print("summnParam ", proxy:GetTempSummonParam())
        if proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_White then
            proxy:LuaCallStartPlus(4041, 1, proxy:GetLocalPlayerId())
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Black then
            proxy:LuaCallStartPlus(4041, 2, proxy:GetLocalPlayerId())
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_FroceJoinBlack then
            proxy:LuaCallStartPlus(4041, 3, proxy:GetLocalPlayerId())
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_DetectBlack then
            proxy:LuaCallStartPlus(4041, 3, proxy:GetLocalPlayerId())
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeNito then
            proxy:LuaCallStartPlus(4041, 7, proxy:GetLocalPlayerId())
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Dragonewt then
            proxy:LuaCallStartPlus(4041, 10, proxy:GetLocalPlayerId())
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeBounty then
            proxy:LuaCallStartPlus(4041, 11, proxy:GetLocalPlayerId())
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Coliseum then
            proxy:LuaCallStartPlus(4041, 12, proxy:GetLocalPlayerId())
        elseif proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
            proxy:LuaCallStartPlus(4041, 4, proxy:GetLocalPlayerId())
        end
        print("AliveMotion_End end")
    else
        proxy:ResetSummonParam()
        proxy:WARN("AliveMotion_End 既にルームが無い")
    end
    
end

function g_second_Initialize(proxy, param)
    print("g_second_Initialize begin")
    print("PK監視追加")
    proxy:OnPlayerKill(4030, "PlayerKill_4030_sub", everytime)
    proxy:CustomLuaCall(4030, "PlayerKill_4030", everytime)
    proxy:CustomLuaCall(4071, "MoveTravelItem", everytime)
    print("g_second_Initialize end")
    
end

function OnEvent_4000_Hp(proxy, param)
    print("OnEvent_4000_Hp begin")
    proxy:SaveRequest_Profile()
    print("OnEvent_4000_Hp end")
    
end

function OnEvent_4000(proxy, param)
    if proxy:IsCompleteEvent(4000) == true then
        return
    end
    if param:IsNetMessage() == true then
        return
    end
    print("OnEvent_4000 begin")
    if proxy:GetReturnState() > 0 then
        return
    end
    proxy:SetEventFlag(4000, true)
    local Live = proxy:IsLivePlayer()
    local Grey = proxy:IsGreyGhost()
    local White = proxy:IsWhiteGhost()
    local Black = proxy:IsBlackGhost()
    if proxy:IsIntruder() == true then
        Black = true
    end
    local Host = proxy:IsHost()
    local Party = proxy:HavePartyMember()
    local dead = false
    deathState = proxy:GetDeathState()
    isDeathPenaltySkip = proxy:IsDeathPenaltySkip()
    DeadtextEffectId = TEXT_TYPE_Dead
    local isTextEffect = false
    if proxy:IsCompleteEvent(4067) == false then
        isTextEffect = true
        if deathState == DEATH_STATE_Normal then
            print("Normal")
            if Live == true or Grey == true then
                DeadtextEffectId = TEXT_TYPE_Dead
                if isDeathPenaltySkip == false then
                    DeadtextEffectId = TEXT_TYPE_GhostDead
                end
            elseif proxy:IsPrevGreyGhost() == true then
                DeadtextEffectId = TEXT_TYPE_GhostDead
            else
                DeadtextEffectId = TEXT_TYPE_Dead
            end
        elseif deathState == DEATH_STATE_MagicResurrection then
            print("DEATH_STATE_MagicResurrection")
            DeadtextEffectId = TEXT_TYPE_MagicResurrection
        elseif deathState == DEATH_STATE_RingNormalResurrection then
            print("DEATH_STATE_RingNormalResurrection")
            DeadtextEffectId = TEXT_TYPE_RingNormalResurrection
        elseif deathState == DEATH_STATE_RingCurseResurrection then
            print("DEATH_STATE_RingCurseResurrection")
            DeadtextEffectId = TEXT_TYPE_RingCurseResurrection
        end
        proxy:SetTextEffect(DeadtextEffectId)
    end
    if (Live == true or Grey == true) and Party == true and Host == true then
        proxy:AddDeathCount()
        proxy:NotNetMessage_begin()
        if isTextEffect then
            proxy:OnTextEffectEnd(4000, DeadtextEffectId, "SoloPlayDeath")
        else
            proxy:OnKeyTime2(4000, "SoloPlayDeath", 0, 0, 1, once)
        end
        proxy:NotNetMessage_end()
        proxy:LuaCallStart(4000, 1)
        dead = true
    end
    if Black == true or White == true then
        proxy:NotNetMessage_begin()
        proxy:OnKeyTime2(4000, "PartyGhostDeath", 0, 0, 1, once)
        proxy:NotNetMessage_end()
        dead = true
    end
    if (Live == true or Grey == true) and Party == false then
        proxy:AddDeathCount()
        proxy:NotNetMessage_begin()
        if isTextEffect then
            proxy:OnTextEffectEnd(4000, DeadtextEffectId, "SoloPlayDeath")
        else
            proxy:OnKeyTime2(4000, "SoloPlayDeath", 0, 0, 1, once)
        end
        proxy:NotNetMessage_end()
        dead = true
    end
    if dead == false then
        print("Check ChrType!!")
        proxy:NotNetMessage_begin()
        if isTextEffect then
            proxy:OnTextEffectEnd(4000, DeadtextEffectId, "SoloPlayDeath")
        else
            proxy:OnKeyTime2(4000, "SoloPlayDeath", 0, 0, 1, once)
        end
        proxy:NotNetMessage_end()
        proxy:SetEventFlag(4020, true)
    end
    proxy:SetEventFlag(4000, true)
    print("OnEvent_4000 end")
    
end

function HostDead(proxy, param)
    if proxy:IsHost() == true or proxy:IsGreyGhost() == true then
        return
    end
    proxy:OnHostDead()
    if proxy:IsBlackGhost() == true then
        proxy:NotNetMessage_begin()
        proxy:SetEventFlag(4047, true)
        proxy:SetLoadWait()
        proxy:NotNetMessage_end()
        proxy:SetTextEffect(TEXT_TYPE_TargetClear)
        proxy:NotNetMessage_begin()
        proxy:OnTextEffectEnd(4059, TEXT_TYPE_TargetClear, "TextEffectEnd_PK_Success")
        proxy:NotNetMessage_end()
        return
    end
    proxy:SetEventFlag(4047, true)
    proxy:SetLoadWait()
    print("HostDead begin")
    if isKill_host == true then
        return
    end
    MissionFailed(proxy, param)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4000, "HostDead_1", 5, 0, 0, once)
    proxy:OnKeyTime2(4000, "EventMenuBrake", 5, 1, 1, once)
    proxy:NotNetMessage_end()
    print("HostDead end")
    
end

function HostDead_1(proxy, param)
    print("HostDead_1 begin")
    proxy:SetFlagInitState(2)
    proxy:SetSosSignWarp()
    proxy:SetDefaultMapUid(-1)
    proxy:WarpNextStageKick()
    proxy:SetChrTypeDataGreyNext()
    print("HostDead_1 end")
    
end

function OnEvent_4000_3(proxy, param)
    print("マルチ解散")
    proxy:ReturnMapSelect()
    
end

function SoloPlayDeath(proxy, param)
    print("SoloPlayDeath SetRestart")
    proxy:LuaCallStartPlus(4090, proxy:GetLocalPlayerInvadeType(), proxy:GetLocalPlayerVowType())
    if proxy:GetBountyRankPoint() > 0 and proxy:GetPartyMemberNum_InvadeType(INVADE_TYPE_InvadeBounty) > 0 then
        proxy:SetBountyRankPoint(-1)
    end
    proxy:SetEventFlag(4047, true)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4000, "SoloPlayDeath_TextWait", 1, 0, 3, once)
    proxy:NotNetMessage_end()
    if proxy:IsInParty_EnemyMember() then
        proxy:NotNetMessage_begin()
        proxy:RepeatMessage_begin()
        proxy:OnRevengeMenuClose(4000, "SoloPlayDeath_1", 0, false, once)
        proxy:RepeatMessage_end()
        proxy:NotNetMessage_end()
    else
        SoloPlayDeath_1(proxy, param)
    end
    print("SoloPlayDeath end")
    
end

function SoloPlayDeath_TextWait(proxy, param)
    print("SoloPlayDeath_TextWait begin")
    MissionDeadFailed(proxy, param)
    if proxy:IsCompleteEvent(4030) == false then
        proxy:LuaCallStartPlus(4042, 3, proxy:GetLocalPlayerId())
    end
    print("SoloPlayDeath_TextWait end")
    
end

function SoloPlayDeath_1(proxy, param)
    print("SoloPlayDeath_1 begin")
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4000, "SoloPlayDeath_2", 2.5, 0, 3, once)
    proxy:NotNetMessage_end()
    print("SoloPlayDeath_1 end")
    
end

function SoloPlayDeath_2(proxy, param)
    print("SoloPlayDeath_2 begin")
    proxy:SetFlagInitState(1)
    proxy:ClearMyWorldState()
    proxy:SetDefaultMapUid(999 - proxy:GetLastBlockId())
    proxy:WarpNextStageKick()
    proxy:SetEventFlag(4000, false)
    proxy:RequestFullRecover()
    if isDeathPenaltySkip == false then
        proxy:UpDateBloodMark()
    else
        print("DeathPenaltySkip")
    end
    proxy:SaveRequest_Profile()
    print("SoloPlayDeath_2 end")
    
end

function PartyGhostDeath(proxy, param)
    print("PartyGhostDeath begin")
    proxy:SetEventFlag(4047, true)
    proxy:SetEventFlag(4047, true)
    proxy:CustomLuaCallStart(4063, proxy:GetLocalPlayerId())
    proxy:SetFlagInitState(2)
    proxy:NotNetMessage_begin()
    proxy:OnTextEffectEnd(4059, DeadtextEffectId, "PartyGhostDeath_wait")
    proxy:NotNetMessage_end()
    proxy:SetEventFlag(4000, true)
    print("PartyGhostDeath end")
    
end

function PartyGhostDeath_wait(proxy, param)
    print("PartyGhostDeath_wait begin")
    proxy:LuaCallStartPlus(4090, proxy:GetLocalPlayerInvadeType(), proxy:GetLocalPlayerVowType())
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4000, "PartyGhostDeath_1", 2, 0, 3, once)
    proxy:NotNetMessage_end()
    print("PartyGhostDeath_wait end")
    
end

function PartyGhostDeath_1(proxy, param)
    print("PartyGhostDeath_1 begin")
    if proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
        proxy:LuaCallStartPlus(4042, 2, proxy:GetLocalPlayerId())
        proxy:LuaCallStartPlus(4042, 5, proxy:GetLocalPlayerId())
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_White then
        proxy:LuaCallStartPlus(4042, 1, proxy:GetLocalPlayerId())
        proxy:LuaCallStartPlus(4042, 4, proxy:GetLocalPlayerId())
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Black then
        proxy:LuaCallStartPlus(4042, 2, proxy:GetLocalPlayerId())
        proxy:LuaCallStartPlus(4042, 4, proxy:GetLocalPlayerId())
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_FroceJoinBlack then
        proxy:LuaCallStartPlus(4042, 6, proxy:GetLocalPlayerId())
        proxy:LuaCallStartPlus(4042, 4, proxy:GetLocalPlayerId())
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_DetectBlack then
        proxy:LuaCallStartPlus(4042, 6, proxy:GetLocalPlayerId())
        proxy:LuaCallStartPlus(4042, 4, proxy:GetLocalPlayerId())
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeNito then
        proxy:LuaCallStartPlus(4042, 7, proxy:GetLocalPlayerId())
        proxy:LuaCallStartPlus(4042, 4, proxy:GetLocalPlayerId())
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Dragonewt then
        proxy:LuaCallStartPlus(4042, 10, proxy:GetLocalPlayerId())
        proxy:LuaCallStartPlus(4042, 4, proxy:GetLocalPlayerId())
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeBounty then
        proxy:LuaCallStartPlus(4042, 11, proxy:GetLocalPlayerId())
        proxy:LuaCallStartPlus(4042, 4, proxy:GetLocalPlayerId())
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Coliseum then
        proxy:LuaCallStartPlus(4042, 12, proxy:GetLocalPlayerId())
        proxy:LuaCallStartPlus(4042, 4, proxy:GetLocalPlayerId())
    end
    if ClearBoss == false then
        MissionDeadFailed(proxy, param)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4000, "PartyGhostDeath_2")
        proxy:NotNetMessage_end()
    else
    end
    print("PartyGhostDeath_1 end")
    
end

function PartyGhostDeath_2(proxy, param)
    print("PartyGhostDeath_2 begin")
    proxy:OnDeadEvent()
    proxy:ClearMyWorldState()
    if ClearBoss == false then
        if isDeathPenaltySkip == false and (proxy:IsBlackGhost() == true or proxy:IsIntruder() == true) then
            local invadeType = proxy:GetLocalPlayerInvadeType()
            if invadeType == INVADE_TYPE_ForceJoinBlack or invadeType == INVADE_TYPE_NormalBlack then
                proxy:UpDateBloodMark()
                print("赤侵入　or　吸魂鬼デスペナルティ！！")
            end
        end
        proxy:RequestFullRecover()
        proxy:SetDefaultMapUid(999 - proxy:GetLastBlockId())
        proxy:WarpNextStageKick()
        proxy:SetChrTypeDataGreyNext()
        proxy:SaveRequest_Profile()
    else
    end
    print("PartyGhostDeath_2 end")
    
end

function DeadInfoMsg_White(proxy, param)
    print("DeadInfoMsg_White begin")
    if param:IsNetMessage() == true then
        if proxy:IsGreyGhost() == true or proxy:IsLivePlayer() == true then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(0, 140181)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(0, 140181)
        else
            proxy:WARN("PCNameのタグ差し替え失敗")
        end
    else
    end
    print("DeadInfoMsg_White end")
    
end

function DeadInfoMsg_Black(proxy, param)
    print("DeadInfoMsg_Black begin")
    if param:IsNetMessage() == true then
        if proxy:IsGreyGhost() == true or proxy:IsLivePlayer() == true then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(0, 140192)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(0, 140192)
        else
            proxy:WARN("PCNameのタグ差し替え失敗")
        end
    else
    end
    print("DeadInfoMsg_Black end")
    
end

function DeadInfoMsg_Host(proxy, param)
    print("DeadInfoMsg_Host begin")
    if param:IsNetMessage() == true then
    end
    print("DeadInfoMsg_Host end")
    
end

function DeadInfoMsg_ForceJoinBlack(proxy, param)
    print("DeadInfoMsg_ForceJoinBlack begin")
    if param:IsNetMessage() == true then
        if proxy:IsGreyGhost() == true or proxy:IsLivePlayer() == true then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(0, 140193)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(0, 140193)
        else
            proxy:WARN("PCNameタグの差し替えに失敗")
        end
    else
    end
    print("DeadInfoMsg_ForceJoinBlack end")
    
end

function DeadInfoMsg_InvadeNito(proxy, param)
    print("DeadInfoMsg_InvadeNito begin")
    if param:IsNetMessage() == true then
        if proxy:IsGreyGhost() == true or proxy:IsLivePlayer() == true then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(0, 140197)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(0, 140197)
        else
            proxy:WARN("PCNameタグの差し替えに失敗")
        end
    else
    end
    print("DeadInfoMsg_InvadeNito end")
    
end

function DeadInfoMsg_Dragonewt(proxy, param)
    print("DeadInfoMsg_Dragonewt begin")
    if param:IsNetMessage() == true then
        if proxy:IsGreyGhost() == true or proxy:IsLivePlayer() == true then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(0, 140198)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(0, 140198)
        else
            proxy:WARN("PCNameタグの差し替えに失敗")
        end
    else
    end
    print("DeadInfoMsg_Dragonewt end")
    
end

function DeadInfoMsg_InvadeBounty(proxy, param)
    print("DeadInfoMsg_InvadeBounty begin")
    if param:IsNetMessage() == true then
        if proxy:IsGreyGhost() == true or proxy:IsLivePlayer() == true then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(0, 140199)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_deadChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(0, 140199)
        else
            proxy:WARN("PCNameタグの差し替えに失敗")
        end
    else
    end
    print("DeadInfoMsg_InvadeBounty end")
    
end

function PlayerKill_4030_sub(proxy, param)
    local nDeadPlayerNo = proxy:VariableExpand_22_param1(param:GetParam2())
    local nKillPlayerNo = proxy:VariableExpand_22_param2(param:GetParam2())
    local nDeadPlayerSummonParam = param:GetParam3()
    local nThisPlayerNo = proxy:GetLocalPlayerId()
    local nThisSummonParam = proxy:GetTempSummonParam()
    if nThisPlayerNo == nKillPlayerNo then
        local nOrderParam = proxy:VariableOrder_22(nDeadPlayerSummonParam, nThisSummonParam)
        proxy:CustomLuaCallStartPlus(4030, param:GetParam2(), nOrderParam)
        if nDeadPlayerNo == proxy:GetHostPlayerNo() then
            isKill_host = true
        end
    end
    
end

function PlayerKill_4030(proxy, param)
    print("PlayerKill_4030 begin")
    local nDeadPlayerNo = proxy:VariableExpand_22_param1(param:GetParam2())
    local nKillPlayerNo = proxy:VariableExpand_22_param2(param:GetParam2())
    local nDeadPlayerSummonParam = proxy:VariableExpand_22_param1(param:GetParam3())
    local nKillPlayerSummonParam = proxy:VariableExpand_22_param2(param:GetParam3())
    local nThisPlayerNo = proxy:GetLocalPlayerId()
    local nHostNo = proxy:GetHostPlayerNo()
    print("nDeadPlayerNo = ", nDeadPlayerNo)
    print("nKillPlayerNo = ", nKillPlayerNo)
    print("nDeadPlayerSummonParam = ", nDeadPlayerSummonParam)
    print("nKillPlayerSummonParam = ", nKillPlayerSummonParam)
    print("nThisPlayerNo = ", nThisPlayerNo)
    print("nHostNo = ", nHostNo)
    local f32_local0 = nil
    if nHostNo == nDeadPlayerNo then
        f32_local0 = true
    else
        f32_local0 = false
    end
    local IsWhite = proxy:IsWhiteGhost()
    local IsBlack = proxy:IsBlackGhost()
    local IsIntruder = proxy:IsIntruder()
    local IsMurder = false
    if nThisPlayerNo == nKillPlayerNo then
        IsMurder = true
    end
    print("PlayerNo:<", nDeadPlayerNo, "> が ", "PlayerNo:<", nKillPlayerNo, "> に 殺された")
    print("LocalPlayerNo:<", nThisPlayerNo, "> LocalPlayerType<", proxy:GetLocalPlayerChrType(), ">")
    print("HostNo<", nHostNo, ">　IsHostDead<", f32_local0, ">　IsWhite<", IsWhite, ">　IsBlack<", IsBlack, ">　IsMurder<", IsMurder, ">")
    if proxy:IsCompleteEvent(4047) == true then
        print("EventFlag4047 is true return")
        return
    end
    if nThisPlayerNo ~= nDeadPlayerNo then
        if f32_local0 and IsMurder and IsBlack == true then
            print("ホストPK　QWC　black > host")
        end
        if f32_local0 and IsMurder and IsWhite == true then
        end
        if f32_local0 and IsMurder and IsIntruder == true then
            print("ホストPK　Intruder > host")
            isKill_host = true
        end
        if f32_local0 == true and IsMurder == false then
        end
        if f32_local0 == false and IsMurder == false then
        end
        if f32_local0 and IsMurder then
            print("自分が生存以外を殺した")
            print("NetChrType = ", proxy:GetNetPlayerChrType(nDeadPlayerNo))
            if proxy:IsWhiteGhost_NetPlayer(nDeadPlayerNo) == true then
                print("ホワイト殺しQWC")
            elseif proxy:IsBlackGhost_NetPlayer(nDeadPlayerNo) == true then
                print("ブラック殺しQWC")
                if IsWhite == false then
                    proxy:SetTextEffect(TEXT_TYPE_BlackClear)
                end
                proxy:AddKillBlackGhost()
            end
            print("NetChrSummonParam = ", nDeadPlayerSummonParam)
            print("nDeadPlayerNo = ", nDeadPlayerNo)
            if nDeadPlayerSummonParam == SUMMONPARAM_TYPE_White then
            elseif nDeadPlayerSummonParam <= SUMMONPARAM_TYPE_Black or nDeadPlayerSummonParam > SUMMONPARAM_TYPE_None then
                if proxy:IsLivePlayer() or proxy:IsGreyGhost() then
                    print("PKSuccess Live > Black")
                elseif proxy:IsWhiteGhost() then
                    print("PKSuccess White > Black")
                end
            end
        end
    else
        isDeathPenaltySkip = proxy:IsDeathPenaltySkip()
        if isDeathPenaltySkip == true then
            print("ペナルティスキップ成功！")
            return
        end
        print("PlayerKill_4030 ThisDead")
        proxy:SetEventFlag(4030, true)
        if proxy:IsLivePlayer() == true or proxy:IsGreyGhost() == true then
            print("PlayerKill_4030 LiveDead")
            if nKillPlayerSummonParam == SUMMONPARAM_TYPE_Black then
            elseif nKillPlayerSummonParam == SUMMONPARAM_TYPE_FroceJoinBlack then
            elseif nKillPlayerSummonParam == SUMMONPARAM_TYPE_DetectBlack then
            end
        end
        if proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_White then
            print("PlayerKill_4030 WhiteDead")
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Black then
            print("PlayerKill_4030 BlackDead")
            if nKillPlayerSummonParam == SUMMONPARAM_TYPE_None or nKillPlayerSummonParam == SUMMONPARAM_TYPE_White then
                print("PKPenalty Live or White -> Black")
            end
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_FroceJoinBlack then
            print("PlayerKill_4030 ForceJoinDead")
            if nKillPlayerSummonParam == SUMMONPARAM_TYPE_None or nKillPlayerSummonParam == SUMMONPARAM_TYPE_White then
                print("PKPenalty Live or White -> ForceJoinBlack")
            end
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_DetectBlack then
            print("PlayerKill_4030 IntruderDead")
            if nKillPlayerSummonParam == SUMMONPARAM_TYPE_None or nKillPlayerSummonParam == SUMMONPARAM_TYPE_White then
                print("PKPenalty Live or White -> IntruderBlack")
            end
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeNito then
            print("PlayerKill_4030 InvadeNitoDead")
            if nKillPlayerSummonParam == SUMMONPARAM_TYPE_None or nKillPlayerSummonParam == SUMMONPARAM_TYPE_White then
                print("PKPenalty Live or White -> InvadeNito")
            end
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Dragonewt then
            print("PlayerKill_4030 DragonewtDead")
            if nKillPlayerSummonParam == SUMMONPARAM_TYPE_None or nKillPlayerSummonParam == SUMMONPARAM_TYPE_White then
                print("PKPenalty Live or White -> Dragonewt")
            end
        elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeBounty then
            print("PlayerKill_4030 InvadeBountyDead")
            if nKillPlayerSummonParam == SUMMONPARAM_TYPE_None or nKillPlayerSummonParam == SUMMONPARAM_TYPE_White then
                print("PKPenalty Live or White -> InvadeBounty")
            end
        end
    end
    print("PlayerKill_4030 end")
    
end

function PlayerKill_4030_1(proxy, param)
    print("PlayerKill_4030_1 begin")
    proxy:SetFlagInitState(2)
    proxy:EraseEventSpecialEffect(LOCAL_PLAYER, 101)
    proxy:SetSosSignWarp()
    proxy:SetDefaultMapUid(-1)
    proxy:RequestFullRecover()
    proxy:WarpNextStageKick()
    print("PlayerKill_4030_1 end")
    
end

function MoveTravelItem(proxy, param)
    print("MoveTravelItem begin")
    local itemParamId = param:GetParam2()
    proxy:NotNetMessage_begin()
    proxy:RepeatMessage_begin()
    proxy:OnKeyTime2(param:GetParam1(), "MoveTravelItem2", 1, 0, itemParamId, once)
    proxy:RepeatMessage_end()
    proxy:NotNetMessage_end()
    print("MoveTravelItem end")
    
end

function MoveTravelItem2(proxy, param)
    print("MoveTravelItem2 begin")
    if isKill_host == true then
        local itemParamId = param:GetParam3()
        proxy:AddInventoryItem(itemParamId, TYPE_GOODS, 1)
    end
    print("MoveTravelItem2 end")
    
end

function TextEffectEnd_PK_Success(proxy, param)
    print("TextEffectEnd_PK_Success begin")
    proxy:NotNetMessage_begin()
    proxy:RepeatMessage_begin()
    proxy:OnRevengeMenuClose(4059, "PK_Sucess_RevengeMenuWait", 0, true, once)
    proxy:RepeatMessage_end()
    proxy:NotNetMessage_end()
    if proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
        proxy:GetRateItem(16581)
    end
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4059, "MissionSuccessed", 2, 0, 3, once)
    proxy:NotNetMessage_end()
    print("TextEffectEnd_PK_Success end")
    
end

function TextEffectEnd_NPCPK_Success(proxy, param)
    print("TextEffectEnd_NPCPK_Success begin")
    proxy:GetRateItem_IgnoreMultiPlay(5020)
    proxy:NotNetMessage_begin()
    proxy:RepeatMessage_begin()
    proxy:OnRevengeMenuClose(4059, "PK_Sucess_RevengeMenuWait", 0, true, once)
    proxy:RepeatMessage_end()
    proxy:NotNetMessage_end()
    if proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
        proxy:GetRateItem(16581)
    end
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4059, "MissionSuccessed", 2, 0, 3, once)
    proxy:NotNetMessage_end()
    print("TextEffectEnd_NPCPK_Success end")
    
end

function PK_Sucess_RevengeMenuWait(proxy, param)
    print("PK_Sucess_RevengeMenuWait begin")
    if proxy:IsRevengeRequested() == true then
        local invadeType = proxy:GetLocalPlayerInvadeType()
        if invadeType == INVADE_TYPE_ForceJoinBlack or invadeType == INVADE_TYPE_ThievesGuild or invadeType == INVADE_TYPE_Dragonewt then
            proxy:SetBountyRankPoint(1)
            proxy:RecallMenuEvent(0, 100100)
        end
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "RequestMenuEnd_PK_Sucess")
        proxy:NotNetMessage_end()
    else
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "RequestMenuEnd_PK_Sucess")
        proxy:NotNetMessage_end()
    end
    print("PK_Sucess_RevengeMenuWait end")
    
end

function RequestMenuEnd_PK_Sucess(proxy, param)
    print("RequestMenuEnd_PK_Sucess begin")
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4030, "PlayerKill_4030_1", 2, 0, 3, once)
    proxy:NotNetMessage_end()
    print("RequestMenuEnd_PK_Sucess end")
    
end

function SelfBloodMark(proxy, param)
    print("SelfBloodMark begin")
    local isStatue = param:GetParam2()
    local eventFlagId = param:GetParam1()
    if isStatue > 0 then
        proxy:NotNetMessage_begin()
        proxy:RepeatMessage_begin()
        proxy:OnSelectMenu(eventFlagId, "SelfBloodMark_1", 10010725, 0, 6, -1, 2, once)
        proxy:RepeatMessage_end()
        proxy:NotNetMessage_end()
    else
        proxy:SetTextEffect(TEXT_TYPE_SoulGet)
        proxy:InvalidMyBloodMarkInfo()
    end
    print("SelfBloodMark end")
    
end

function SelfBloodMark_1(proxy, param)
    
end

function SelfHeroBloodMark(proxy, param)
    print("SelfHeroBloodMark begin")
    local isStatue = param:GetParam2()
    local eventFlagId = param:GetParam1()
    if isStatue > 0 then
        proxy:NotNetMessage_begin()
        proxy:RepeatMessage_begin()
        proxy:OnSelectMenu(eventFlagId, "SelfHeroBloodMark_1", 10010725, 0, 6, -1, 2, once)
        proxy:RepeatMessage_end()
        proxy:NotNetMessage_end()
    else
        if proxy:IsGreyGhost() then
        end
        proxy:SetTextEffect(TEXT_TYPE_SoulGet)
        proxy:InvalidMyHeroBloodMarkInfo()
    end
    print("SelfHeroBloodMark end")
    
end

function SelfHeroBloodMark_1(proxy, param)
    
end

function OnIrregularLeaveSession(proxy, param)
    print("OnIrregularLeaveSession begin")
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4033, "OnIrregularLeaveSession_1", 0.1, 0, 1, once)
    proxy:NotNetMessage_end()
    print("OnIrregularLeaveSession end")
    
end

function OnIrregularLeaveSession_1(proxy, param)
    print("OnIrregularLeaveSession_1 begin")
    if proxy:IsLivePlayer() == true or proxy:IsGreyGhost() == true then
        return
    end
    if proxy:IsCompleteEvent(4047) == true then
        print("EventFlag4047 is true return")
        return
    end
    proxy:SetEventFlag(4047, true)
    proxy:SetLoadWait()
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20001001)
    proxy:SetFlagInitState(2)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4046, "OnLeave_Limit", 5, 0, 2, once)
    proxy:NotNetMessage_end()
    print("OnIrregularLeaveSession_1 end")
    
end

function OnServerError_Unavailable(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnServerError_Unavailable begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBufferPlus(50524, MSG_CATEGORY_DIALOG)
    proxy:WARN("サーバが使用不可能.")
    RegistReturnTitle(proxy, param)
    print("OnServerError_Unavailable end")
    
end

function OnServerError_Busy(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnServerError_Busy begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBufferPlus(50523, MSG_CATEGORY_DIALOG)
    proxy:WARN("サーバがBusy状態")
    RegistReturnTitle(proxy, param)
    print("OnServerError_Busy end")
    
end

function OnServerError_Maintenance(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnServerError_Maintenance begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBufferPlus(50522, MSG_CATEGORY_DIALOG)
    proxy:WARN("サーバメンテナンス中")
    RegistReturnTitle(proxy, param)
    print("OnServerError_Maintenance end")
    
end

function OnServerError_Unknown(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnServerError_Unknown begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBufferPlus(50525, MSG_CATEGORY_DIALOG)
    proxy:WARN("サーバから原因不明のエラー")
    RegistReturnTitle(proxy, param)
    print("OnServerError_Unknown end")
    
end

function OnServerError_ServiceStop(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnServerError_ServiceStop begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBufferPlus(51201, MSG_CATEGORY_DIALOG)
    proxy:WARN("サーバのサービス期間外")
    RegistReturnTitle(proxy, param)
    print("OnServerError_ServiceStop end")
    
end

function OnServerError_TimeOut(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnServerError_TimeOut begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBufferPlus(51300, MSG_CATEGORY_DIALOG)
    proxy:WARN("サーバからの応答が無い")
    RegistReturnTitle(proxy, param)
    print("OnServerError_TimeOut end")
    
end

function OnSummonResult_Empty(proxy, param)
    print("OnResultEmpty begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000105)
    print("OnResultEmpty end")
    
end

function OnSummonResult_Move(proxy, param)
    print("OnSummonResult_Move begin")
    print("OnSummonResult_Move end")
    
end

function OnSummonResult_TimeOut(proxy, param)
    print("OnSummonResult_TimeOut begin")
    print("OnSummonResult_TimeOut end")
    
end

function OnSummonResult_OtherError(proxy, param)
    
end

function OnEvent_4038(proxy, param)
    print("OnEvent_4038 begin")
    local info = param:GetParam2()
    if info == 20 then
        ForceSummonSuccess(proxy, param)
    elseif info == 21 then
        ForceSummonFail(proxy, param)
    elseif info == 22 then
        ForceSummonFail(proxy, param)
    elseif info == 23 then
        ForceSummonFail(proxy, param)
    elseif info == 24 then
        ForceSummonFail(proxy, param)
    end
    print("OnEvent_4038 end")
    
end

function ForceSummonFail(proxy, param)
    print("ForceSummonFail begin")
    if proxy:IsClient() == false then
        OnEvent_1090(proxy, param)
    end
    proxy:DeleteEvent(1090)
    proxy:SetEventFlag(1090, true)
    print("ForceSummonFail end")
    
end

function ForceSummonSuccess(proxy, param)
    print("ForceSummonSuccess begin")
    print("ForceSummonSuccess end")
    
end

function BlockClear2(proxy, param)
    if param:IsNetMessage() == true then
        return
    end
    if proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
        return
    end
    print("BlockClear2 begin")
    ClearBossId = -1
    ClearBossId = param:GetParam2()
    if proxy:IsWhiteGhost() == true then
        proxy:LuaCallStart(4055, 1)
    end
    proxy:ClearSosSign()
    proxy:SetClearSesiionCount()
    proxy:ClearBossGauge()
    proxy:SetClearBonus(ClearBossId)
    if proxy:IsLivePlayer() == true then
        proxy:OnBossDestroyed()
        proxy:SetTextEffect(TEXT_TYPE_KillDemon)
        if proxy:IsAlive(LOCAL_PLAYER) == true then
            proxy:SetEventFlag(4047, true)
            proxy:SetEventFlag(4000, true)
            proxy:SetEventFlag(4092, true)
        end
    elseif proxy:IsGreyGhost() == true then
        proxy:OnBossDestroyed()
        proxy:SetTextEffect(TEXT_TYPE_KillDemon)
        if proxy:IsAlive(LOCAL_PLAYER) == true then
            proxy:SetEventFlag(4047, true)
            proxy:SetEventFlag(4000, true)
            proxy:SetEventFlag(4092, true)
        end
    elseif proxy:IsWhiteGhost() == true then
        ClearBoss = true
        proxy:OnBossDestroyed()
        proxy:SetTextEffect(TEXT_TYPE_KillDemon)
        proxy:SetLoadWait()
        if proxy:IsAlive(LOCAL_PLAYER) == true then
            proxy:SetEventFlag(4047, true)
            proxy:SetEventFlag(4000, true)
            proxy:SetEventFlag(4092, true)
        end
        proxy:IncrementCoopPlaySuccessCount()
        proxy:CalcExcuteMultiBonus(proxy:GetLocalPlayerId(), 0, 1)
    elseif proxy:IsBlackGhost() == true or proxy:IsIntruder() == true then
        proxy:OnBossDestroyed()
        proxy:CustomLuaCallStart(4063, proxy:GetLocalPlayerId())
        MissionFailed(proxy, param)
        proxy:SetLoadWait()
        if proxy:IsAlive(LOCAL_PLAYER) == true then
            proxy:SetEventFlag(4047, true)
            proxy:SetEventFlag(4000, true)
            proxy:SetEventFlag(4092, true)
        end
        proxy:LeaveSession()
    end
    proxy:LockSession()
    if proxy:IsInParty_FriendMember() == true then
        proxy:SetSubMenuBrake(true)
    end
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4050, "BlockClear2_1", 5, 0, 2, once)
    proxy:NotNetMessage_end()
    if proxy:IsWhiteGhost() == true or proxy:IsBlackGhost() == true or proxy:IsIntruder() == true then
        proxy:RepeatMessage_begin()
        proxy:NotNetMessage_begin()
        proxy:OnRegistFunc(4050, "Check_BlockClearAnim", "BlockClearAnim", 20, once)
        proxy:NotNetMessage_end()
        proxy:RepeatMessage_end()
    end
    print("BlockClear2 end")
    
end

function BlockClear2_1(proxy, param)
    print("BlockClear2_1 begin")
    proxy:SetEventFlag(4200, true)
    if proxy:IsInParty_FriendMember() == true then
        if proxy:IsLivePlayer() == true or proxy:IsWhiteGhost() == true or proxy:IsGreyGhost() == true then
            if proxy:GetPartyMemberNum_VowType(3, true) > 0 then
                print("クリア時に太陽アンバサが居たので成功報酬")
                proxy:GetRateItem_IgnoreMultiPlay(5030)
            end
            proxy:NotNetMessage_begin()
            proxy:RepeatMessage_begin()
            proxy:OnKeyTime2(4050, "BlockClear2_2", CLEAR_GETSOUL_DELAYTIME, 0, 0, once)
            proxy:RepeatMessage_end()
            proxy:NotNetMessage_end()
        elseif proxy:IsBlackGhost() == true or proxy:IsIntruder() == true then
            proxy:SetFlagInitState(2)
            proxy:SetSosSignWarp()
            proxy:SetDefaultMapUid(-1)
            proxy:WarpNextStageKick()
            proxy:SetChrTypeDataGreyNext()
        end
    elseif proxy:IsLivePlayer() == true then
        proxy:NotNetMessage_begin()
        proxy:OnKeyTime2(4050, "SoloBlockClear", 3, 0, 6, once)
        proxy:NotNetMessage_end()
    elseif proxy:IsGreyGhost() == true then
        proxy:NotNetMessage_begin()
        proxy:OnKeyTime2(4050, "SoloBlockClear", 6, 0, 6, once)
        proxy:NotNetMessage_end()
    elseif proxy:IsWhiteGhost() == true then
        proxy:GetClearBonus(ClearBossId)
        proxy:NotNetMessage_begin()
        proxy:OnKeyTime2(4050, "BlockClear2_3", CLEAR_LEAVE_DELAYTIME, 0, 0, once)
        proxy:NotNetMessage_end()
    elseif proxy:IsBlackGhost() == true or proxy:IsIntruder() == true then
        proxy:SetFlagInitState(2)
        proxy:SetSosSignWarp()
        proxy:SetDefaultMapUid(-1)
        proxy:WarpNextStageKick()
        proxy:SetChrTypeDataGreyNext()
    end
    print("BlockClear2_1 end")
    
end

function BlockClear2_2(proxy, param)
    print("BlockClear2_2 begin")
    if proxy:IsWhiteGhost() == true then
        proxy:CustomLuaCallStart(4063, proxy:GetLocalPlayerId())
    end
    proxy:GetClearBonus(ClearBossId)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4050, "BlockClear2_3", CLEAR_LEAVE_DELAYTIME, 0, 0, once)
    proxy:OnKeyTime2(4050, "BlockClear2_3Leave", 2, 0, 3, once)
    proxy:NotNetMessage_end()
    MissionSuccessed(proxy, param)
    print("BlockClear2_2 end")
    
end

function BlockClear2_3Leave(proxy, param)
    print("BlockClear2_3Leave begin")
    proxy:LeaveSession()
    print("BlockClear2_3Leave end")
    
end

function BlockClear2_3(proxy, param)
    proxy:SetSubMenuBrake(false)
    if proxy:IsLivePlayer() == true or proxy:IsGreyGhost() == true then
        proxy:SetEventFlag(4047, false)
        proxy:SetEventFlag(4000, false)
    end
    if proxy:IsWhiteGhost() == true then
        proxy:SetFlagInitState(2)
        proxy:RequestFullRecover()
        proxy:SetSosSignWarp()
        proxy:SetDefaultMapUid(-1)
        proxy:WarpNextStageKick()
    end
    
end

function SoloBlockClear(proxy, param)
    print("SoloBlockClear begin")
    proxy:SetSubMenuBrake(false)
    proxy:GetSoloClearBonus(ClearBossId)
    if proxy:IsCompleteEvent(4092) == true then
        proxy:SetEventFlag(4047, false)
        proxy:SetEventFlag(4000, false)
        proxy:SetEventFlag(4092, false)
    end
    proxy:LeaveSession()
    print("SoloBlockClear end")
    
end

function Check_BlockClearAnim(proxy, param)
    proxy:ChrFadeOut(LOCAL_PLAYER, 2, 1)
    return true
    
end

function BlockClearAnim(proxy, param)
    print("BlockClearAnim begin")
    proxy:LuaCallStart(4050, 20)
    proxy:StopPlayer()
    print("BlockClearAnim end")
    
end

function BlockClearSynchroAnime(proxy, param)
    if param:IsNetMessage() == true then
        print("BlockClearSynchroAnime Net begin")
        proxy:ChrFadeOut(param:GetPlayID() + NET_PLAYER, 1.5, 1)
        proxy:EnableLogic(param:GetPlayID() + NET_PLAYER, false)
        print("BlockClearSynchroAnime Net end")
        return
    end
    print("BlockClearSynchroAnime begin")
    proxy:NotNetMessage_begin()
    proxy:RepeatMessage_begin()
    proxy:OnKeyTime2(4050, "BlockClearAnim_1", 2, 0, 0, once)
    proxy:RepeatMessage_end()
    proxy:NotNetMessage_end()
    print("BlockClearSynchroAnime end")
    
end

function BlockClearAnim_1(proxy, param)
    print("BlockClearAnim_1 begin")
    proxy:LuaCallStart(4050, 30)
    print("BlockClearAnim_1 end")
    
end

function BlockClearSynchroInvalid(proxy, param)
    if param:IsNetMessage() == true then
        print("BlockClearSynchroInvalid Net begin")
        InvalidCharactor(proxy, param:GetPlayID() + NET_PLAYER)
        print("BlockClearSynchroInvalid Net end")
        return
    else
        print("BlockClearSynchroInvalid begin")
        proxy:SetDrawEnable(LOCAL_PLAYER, false)
        print("BlockClearSynchroInvalid end")
    end
    
end

function MediumBossDestroy(proxy, param)
    print("MediumBossDestroy begin")
    if param:IsNetMessage() == true then
        return
    end
    if proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
        return
    end
    proxy:ClearBossGauge()
    if proxy:IsLivePlayer() == true then
        proxy:SetTextEffect(TEXT_TYPE_KillDemon)
    elseif proxy:IsGreyGhost() == true then
        proxy:SetTextEffect(TEXT_TYPE_KillDemon)
    elseif proxy:IsWhiteGhost() == true then
        proxy:SetTextEffect(TEXT_TYPE_KillDemon)
    elseif proxy:IsBlackGhost() == true or proxy:IsIntruder() == true then
        proxy:SetTextEffect(TEXT_TYPE_KillDemon)
    end
    print("MediumBossDestroy end")
    
end

function OnEvent_4001(proxy, param)
    if param:IsNetMessage() == true then
        return
    end
    proxy:ActionEnd(LOCAL_PLAYER)
    
end

function OnEvent_4002(proxy, param)
    if param:IsNetMessage() == true then
        return
    end
    if proxy:IsAction(LOCAL_PLAYER, 0) == false then
        proxy:SetEventCommand(LOCAL_PLAYER, 2)
    else
        proxy:SetEventCommand(LOCAL_PLAYER, 4)
    end
    
end

function OnEvent_4003(proxy, param)
    if param:IsNetMessage() == true then
        return
    end
    if proxy:IsAction(LOCAL_PLAYER, 1) == false then
        proxy:SetEventCommand(LOCAL_PLAYER, 3)
    else
        proxy:SetEventCommand(LOCAL_PLAYER, 5)
    end
    
end

function OnEvent_4004(proxy, param)
    if param:IsNetMessage() == true then
        return
    end
    print("OnEvent_4004 begin")
    proxy:AddActionCount(LOCAL_PLAYER, 4)
    print("OnEvent_4004 end")
    
end

function OnEvent_4005(proxy, param)
    if param:IsNetMessage() == true then
        return
    end
    print("OnEvent_4005 begin")
    proxy:SubActionCount(LOCAL_PLAYER, 5)
    print("OnEvent_4005 end")
    
end

function EventMenuBrake(proxy, param)
    if param:IsNetMessage() == true then
        return
    end
    proxy:CloseMenu()
    proxy:SetMenuBrake()
    
end

function EventMenuClose(proxy, param)
    if param:IsNetMessage() == true then
        return
    end
    proxy:CloseMenu()
    
end

function LadderDown(proxy, param)
    proxy:CloseMenu()
    proxy:SetMenuBrake()
    
end

function Luafunc_PlaySynchroAnimation(proxy, param, nAnimeID)
    print("Luafunc_PlaySynchroAnimation begin")
    if param:IsNetMessage() == true then
        proxy:SetDrawEnable(param:GetPlayID() + NET_PLAYER, true)
        proxy:PlayAnimation(param:GetPlayID() + NET_PLAYER, nAnimeID)
        print("Luafunc_PlaySynchroAnimation return true")
        return true
    end
    proxy:SetDrawEnable(LOCAL_PLAYER, true)
    proxy:PlayAnimation(LOCAL_PLAYER, nAnimeID)
    print("Luafunc_PlaySynchroAnimation return false")
    return false
    
end

function CheckAlpha(proxy, param)
    print("CheckAlpha" + param:GetParam3())
    chrId = param:GetParam3()
    
end

function Luafunc_ForcePlaySynchroAnimationHighPrio(proxy, param, nAnimeID)
    print("Luafunc_ForcePlaySynchroAnimationHighPrio begin")
    if param:IsNetMessage() == true then
        proxy:SetDrawEnable(param:GetPlayID() + NET_PLAYER, true)
        proxy:ForcePlayAnimationHighPrio(param:GetPlayID() + NET_PLAYER, nAnimeID)
        proxy:OnKeyTime2(4201, "CheckAlpha", 4, 0, param:GetPlayID(), once)
        print("Luafunc_ForcePlaySynchroAnimationHighPrio return true")
        return true
    end
    proxy:SetDrawEnable(LOCAL_PLAYER, true)
    proxy:ForcePlayAnimationHighPrio(LOCAL_PLAYER, nAnimeID)
    print("Luafunc_ForcePlaySynchroAnimationHighPrio return false")
    return false
    
end

function Luafunc_PlaySynchroAnimation_forSummon(proxy, param, nAnimeID)
    print("Luafunc_PlaySynchroAnimation_forSummon begin")
    if param:IsNetMessage() == true then
        if proxy:IsAppearancePlayer(param:GetPlayID() + NET_PLAYER) == false then
            proxy:PlayAnimation(param:GetPlayID() + NET_PLAYER, nAnimeID)
            print("Luafunc_PlaySynchroAnimation_forSummon return true")
        end
        return true
    end
    proxy:PlayAnimation(LOCAL_PLAYER, nAnimeID)
    print("Luafunc_PlaySynchroAnimation_forSummon return false")
    return false
    
end

function Luafunc_ForcePlaySynchroAnimation(proxy, param, nAnimeID)
    print("Luafunc_ForcePlaySynchroAnimation begin")
    if param:IsNetMessage() == true then
        proxy:ForcePlayAnimation(param:GetPlayID() + NET_PLAYER, nAnimeID)
        print("Luafunc_ForcePlaySynchroAnimation return true")
        return true
    end
    proxy:ForcePlayAnimation(LOCAL_PLAYER, nAnimeID)
    print("Luafunc_ForcePlaySynchroAnimation return false")
    return false
    
end

function OnEvent_4010_1(proxy, param)
    print("OnEvent_4010_1 begin")
    if Luafunc_PlaySynchroAnimation(proxy, param, ANIMEID_PICK) == true then
        print("return true ")
        print("OnEvent_4010_1 end")
        return true
    end
    print("return false ")
    print("OnEvent_4010_1 end")
    return false
    
end

function OnEvent_4010_2(proxy, param)
    print("OnEvent_4010_2 begin")
    if Luafunc_ForcePlaySynchroAnimation(proxy, param, ANIMEID_PICK) == true then
        print("return true ")
        print("OnEvent_4010_2 end")
        return true
    end
    print("return false ")
    print("OnEvent_4010_2 end")
    return false
    
end

function OnEvent_4010_11(proxy, param)
    print("OnEvent_4010_11 begin")
    if Luafunc_PlaySynchroAnimation(proxy, param, ANIMEID_COFFER_PICK) == true then
        print("return true ")
        print("OnEvent_4010_11 end")
        return true
    end
    print("return false ")
    print("OnEvent_4010_11 end")
    return false
    
end

function OnEvent_4010_12(proxy, param)
    print("OnEvent_4010_12 begin")
    if Luafunc_ForcePlaySynchroAnimation(proxy, param, ANIMEID_COFFER_PICK) == true then
        print("return true ")
        print("OnEvent_4010_12 end")
        return true
    end
    print("return false ")
    print("OnEvent_4010_12 end")
    return false
    
end

function OnEvent_4012(proxy, param)
    print("OnEvent_4012 begin")
    if Luafunc_PlaySynchroAnimation(proxy, param, ANIMEID_WALK) == true then
        return
    end
    print("OnEvent_4012 end")
    
end

function OnEvent_4013(proxy, param)
    print("OnEvent_4013 begin")
    if Luafunc_PlaySynchroAnimation(proxy, param, param:GetParam3()) == true then
        print("return true ")
        print("OnEvent_4013 end")
        return true
    end
    print("return false ")
    print("OnEvent_4013 end")
    return false
    
end

function OnEvent_4014(proxy, param)
    print("OnEvent_4014 begin")
    if Luafunc_PlaySynchroAnimation_forSummon(proxy, param, param:GetParam3()) == true then
        print("return true ")
        print("OnEvent_4014 end")
        return true
    end
    print("return false ")
    print("OnEvent_4014 end")
    
end

function SynchroAnim_4013(proxy, param)
    print("SynchroAnim_4013 begin")
    local targetId = param:GetParam2()
    local animId = param:GetParam3()
    print("Target :", targetId, " animId :", animId)
    if targetId >= LOCAL_PLAYER then
        Luafunc_ForcePlaySynchroAnimationHighPrio(proxy, param, animId)
    else
        proxy:PlayAnimation(targetId, animId)
    end
    
end

function SynchroAnim_4014(proxy, param)
    print("SynchroAnim_4014 begin")
    local targetId = param:GetParam2()
    local animId = param:GetParam3()
    print("Target :", targetId, " animId :", animId)
    if targetId >= LOCAL_PLAYER then
        Luafunc_PlaySynchroAnimation_forSummon(proxy, param, animId)
    else
        proxy:PlayAnimation(targetId, animId)
    end
    
end

function OnEvent_4015(proxy, param)
    local set_id = LOCAL_PLAYER
    if param:IsNetMessage() == true then
        set_id = NET_PLAYER + param:GetPlayID()
    end
    proxy:SetSuperArmor(set_id, true)
    
end

function OnEvent_4016(proxy, param)
    local set_id = LOCAL_PLAYER
    if param:IsNetMessage() == true then
        set_id = NET_PLAYER + param:GetPlayID()
    end
    proxy:SetSuperArmor(set_id, false)
    
end

function OnEvent_4017(proxy, param)
    local set_id = LOCAL_PLAYER
    if param:IsNetMessage() == true then
        set_id = NET_PLAYER + param:GetPlayID()
    end
    proxy:EnableInvincible(set_id, true)
    
end

function OnEvent_4018(proxy, param)
    local set_id = LOCAL_PLAYER
    if param:IsNetMessage() == true then
        set_id = NET_PLAYER + param:GetPlayID()
    end
    proxy:EnableInvincible(set_id, false)
    
end

function OnEvent_4019(proxy, param)
    local set_id = LOCAL_PLAYER
    if param:IsNetMessage() == true then
        set_id = NET_PLAYER + param:GetPlayID()
    end
    proxy:DisableMapHit(set_id, false)
    proxy:SetDisableGravity(set_id, false)
    proxy:DisableMove(set_id, false)
    
end

function InvalidCharactor(proxy, eventId)
    proxy:SetDisable(eventId, true)
    proxy:CharacterAllAttachSys(eventId)
    
end

function ValidCharactor(proxy, eventId)
    proxy:SetDisable(eventId, false)
    
end

function SummonSuccess_White(proxy, param)
    print("SummonSuccess_White begin")
    print("SummonSuccess_White end")
    
end

function SummonSuccess_Black(proxy, param)
    print("SummonSuccess_Black begin")
    print("SummonSuccess_Black end")
    
end

function MissionSuccessed(proxy, param)
    print("MissionSuccessed begin")
    print("summonParam ", proxy:GetTempSummonParam())
    if proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_White then
        proxy:RecallMenuEvent(0, 140133)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Black then
        if proxy:IsPrevGreyGhost() == true then
            proxy:RecallMenuEvent(0, 140134)
        else
            proxy:RecallMenuEvent(0, 140135)
        end
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_FroceJoinBlack then
        local invadeType = proxy:GetLocalPlayerInvadeType()
        if invadeType == INVADE_TYPE_ForceJoinBlack then
            proxy:RecallMenuEvent(0, 140135)
        elseif invadeType == INVADE_TYPE_ThievesGuild then
            proxy:RecallMenuEvent(0, 141130)
        elseif invadeType == INVADE_TYPE_OtoutoUmbasa then
            proxy:RecallMenuEvent(0, 140139)
        end
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_DetectBlack then
        proxy:RecallMenuEvent(0, 140135)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeNito then
        proxy:RecallMenuEvent(0, 140137)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Dragonewt then
        proxy:RecallMenuEvent(0, 140138)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeBounty then
        proxy:RecallMenuEvent(0, 140139)
    elseif proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
        proxy:RecallMenuEvent(0, 140135)
    end
    proxy:SaveRequest_Profile()
    print("MissionSuccessed end")
    
end

function MissionFailed(proxy, param)
    print("MissionFailed begin")
    if proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_White then
        proxy:RecallMenuEvent(0, 140143)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Black then
        proxy:RecallMenuEvent(0, 140144)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_FroceJoinBlack then
        local invadeType = proxy:GetLocalPlayerInvadeType()
        if invadeType == INVADE_TYPE_ForceJoinBlack then
            proxy:RecallMenuEvent(0, 140145)
        elseif invadeType == INVADE_TYPE_ThievesGuild then
            proxy:RecallMenuEvent(0, 140145)
        elseif invadeType == INVADE_TYPE_OtoutoUmbasa then
            proxy:RecallMenuEvent(0, 140145)
        end
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_DetectBlack then
        proxy:RecallMenuEvent(0, 140145)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeNito then
        proxy:RecallMenuEvent(0, 140147)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Dragonewt then
        proxy:RecallMenuEvent(0, 140148)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeBounty then
        proxy:RecallMenuEvent(0, 140149)
    elseif proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
        proxy:RecallMenuEvent(0, 140146)
    end
    proxy:RequestFullRecover()
    proxy:SaveRequest_Profile()
    print("MissionFailed end")
    
end

function MissionDeadFailed(proxy, param)
    print("MissionDeadFailed begin")
    if proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_White then
        proxy:RecallMenuEvent(0, 140153)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Black then
        proxy:RecallMenuEvent(0, 140154)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_FroceJoinBlack then
        local invadeType = proxy:GetLocalPlayerInvadeType()
        if invadeType == INVADE_TYPE_ForceJoinBlack then
            proxy:RecallMenuEvent(0, 140155)
        elseif invadeType == INVADE_TYPE_ThievesGuild then
            proxy:RecallMenuEvent(0, 140155)
        elseif invadeType == INVADE_TYPE_OtoutoUmbasa then
            proxy:RecallMenuEvent(0, 140155)
        end
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_DetectBlack then
        proxy:RecallMenuEvent(0, 140155)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeNito then
        proxy:RecallMenuEvent(0, 140157)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_Dragonewt then
        proxy:RecallMenuEvent(0, 140158)
    elseif proxy:GetTempSummonParam() == SUMMONPARAM_TYPE_InvadeBounty then
        proxy:RecallMenuEvent(0, 140159)
    elseif proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
        proxy:RecallMenuEvent(0, 140156)
    end
    print("MissionDeadFailed end")
    
end

function OnEvent_Delete_SOS(proxy, param)
    print("OnEvent_Delete_SOS begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000000)
    print("OnEvent_Delete_SOS end")
    
end

function OnEvent_Delete_WhiteSOS(proxy, param)
    print("OnEvent_Delete_WhiteSOS begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000000)
    proxy:SetInfomationPriority(USER_ID_Event_SosLost_White)
    print("OnEvent_Delete_WhiteSOS end")
    
end

function OnEvent_Delete_BlackSOS(proxy, param)
    print("OnEvent_Delete_BlackSOS begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000001)
    proxy:SetInfomationPriority(USER_ID_Event_SosLost_Red)
    print("OnEvent_Delete_BlackSOS end")
    
end

function OnEvent_Delete_ForceJoinSOS(proxy, param)
    print("OnEvent_Delete_ForceJoinSOS begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000002)
    proxy:SetInfomationPriority(USER_ID_Event_SosLost_Black)
    print("OnEvent_Delete_ForceJoinSOS end")
    
end

function OnLanCutError(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnLanCutError begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(4194)
    proxy:NotNetMessage_begin()
    RegistReturnTitle(proxy, param)
    proxy:WARN("OnLanCutError!")
    proxy:NotNetMessage_end()
    print("OnLanCutError end")
    
end

function OnPartyChatError(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnPartyCatError begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20001011)
    proxy:NotNetMessage_begin()
    RegistReturnTitle(proxy, param)
    proxy:WARN("OnPartyCatError!")
    proxy:NotNetMessage_end()
    print("OnPartyCatError end")
    
end

function OnNpServerSignOut(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnNpServerSignOut begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(51100)
    proxy:WARN("NpSignOut Error")
    RegistReturnTitle(proxy, param)
    print("OnNpServerSignOut end")
    
end

function OnNpResumeFromSuspend(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnNpResumeFromSuspend begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(4104)
    proxy:WARN("OnNpResumeFromSuspend Error")
    RegistReturnTitle(proxy, param)
    print("OnNpResumeFromSuspend end")
    
end

function OnFpsDisconnection(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnFpsDisconnection begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20001017)
    proxy:WARN("FpsDisconnection Error")
    RegistReturnTitle(proxy, param)
    print("OnFpsDisconnection end")
    
end

function OnDSServerDisconnection(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnDSServerDisconnect begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(4102)
    proxy:WARN("DSServer Disconnected")
    RegistReturnTitle(proxy, param)
    print("OnDSServerDisconnect end")
    
end

function OnMultiplayerPrivilegesLost(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnMultiplayerPrivilegesLost begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(4132)
    proxy:WARN("Not authorized to play online")
    RegistReturnTitle(proxy, param)
    print("OnMultiplayerPrivilegesLost end")
    
end

function OnP2PTimeOut(proxy, param)
    print("OnP2PTimeOut begin")
    if proxy:IsCompleteEvent(4047) == true then
        return
    end
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20001015)
    if proxy:IsLivePlayer() == false and proxy:IsGreyGhost() == false then
        proxy:NotNetMessage_begin()
        proxy:OnKeyTime2(4046, "OnLeave_Limit", 5, 0, 1, once)
        proxy:SetEventFlag(4047, true)
        proxy:SetLoadWait()
        proxy:NotNetMessage_end()
    end
    print("OnP2PTimeOut end")
    
end

function OnFailedGetBlockNum(proxy, param)
    if proxy:IsCompleteEvent(4093) == true then
        return
    end
    proxy:SetEventFlag(4093, true)
    print("OnFailedGetBlockNum begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20001002)
    proxy:WARN("OnFailedGetBlockNum Error")
    RegistReturnTitle(proxy, param)
    print("OnFailedGetBlockNum end")
    
end

function OnLeavePlayer(proxy, param)
    print("OnLeavePlayer begin")
    if proxy:IsCompleteEvent(4047) == true then
        return
    end
    if proxy:IsCompleteEvent(4044) == true then
        proxy:SetEventFlag(4044, false)
        return
    end
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTimeMsgTag(20001020, TAG_IDX_leaveChara, param:GetParam3())
    if param:GetParam3() == proxy:GetLocalPlayerId() then
        proxy:NotNetMessage_begin()
        proxy:OnKeyTime2(4046, "OnLeave_Limit", 5, 0, 1, once)
        proxy:SetEventFlag(4047, true)
        proxy:SetLoadWait()
        proxy:NotNetMessage_end()
    end
    print("OnLeavePlayer end")
    
end

function OnGameLeave(proxy, param)
    if param:GetParam2() ~= proxy:GetLocalPlayerId() then
        print("OnGameLeave begin")
        local leavePlayer = param:GetParam2()
        print("LeavePlayer :", leavePlayer)
        proxy:ReqularLeavePlayer(leavePlayer)
        print("OnGameLeave begin")
    end
    
end

function OnBeKickOut(proxy, param)
    print("OnBeKickOut begin")
    if proxy:IsCompleteEvent(4047) == true then
        return
    end
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000420)
    proxy:OnKeyTime2(4046, "OnLeave_Limit", 5, 0, 1, once)
    proxy:SetEventFlag(4047, true)
    proxy:SetLoadWait()
    print("OnBeKickOut end")
    
end

function OnBeThxKickOut(proxy, param)
    print("OnBeThxKickOut begin")
    if proxy:IsCompleteEvent(4047) == true then
        return
    end
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000421)
    proxy:OnKeyTime2(4046, "OnLeave_Limit", 5, 0, 1, once)
    proxy:SetEventFlag(4047, true)
    proxy:SetLoadWait()
    print("OnBeThxKickOut end")
    
end

function OnKickOut(proxy, param)
    print("OnKickOut begin")
    if proxy:IsHost() == true then
        proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
        proxy:AddInfomationTosBuffer(20000415)
        proxy:SetEventFlag(4044, true)
    elseif proxy:GetLocalPlayerId() ~= param:GetParam3() then
        proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
        proxy:AddInfomationTimeMsgTag(20000425, TAG_IDX_leaveChara, param:GetParam3())
    end
    print("OnKickOut end")
    
end

function OnThxKickOut(proxy, param)
    print("OnThxKickOut begin")
    if proxy:IsHost() == true then
        proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
        proxy:AddInfomationTosBuffer(20000416)
        proxy:SetEventFlag(4044, true)
    elseif proxy:GetLocalPlayerId() ~= param:GetParam3() then
        proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
        proxy:AddInfomationTimeMsgTag(20000425, TAG_IDX_leaveChara, param:GetParam3())
    end
    print("OnThxKickOut end")
    
end

function OnLeaveMagic(proxy, param)
    print("OnLeaveMagic begin")
    if proxy:IsCompleteEvent(4047) == true then
        return
    end
    proxy:CustomLuaCallStart(4063, proxy:GetLocalPlayerId())
    OnLeaveMenu_Yes(proxy, param)
    print("OnLeaveMagic end")
    
end

function OnForceJoinBlack(proxy, param)
    print("OnForceJoinBlack begin")
    if proxy:IsCompleteEvent(4047) == true then
        return
    end
    proxy:SetForceJoinBlackRequest()
    print("OnForceJoinBlack end")
    
end

function OnCancelForceJoinBlack(proxy, param)
    print("OnCancelForceJoinBlack begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000002)
    print("OnCancelForceJoinBlack end")
    
end

function OnFailedCreateSession(proxy, param)
    print("OnFailedCreateSession begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20001003)
    print("OnFailedCreateSession end")
    
end

function OnFailedJoinSession(proxy, param)
    print("OnFailedJoinSession begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20001004)
    print("OnFailedJoinSession end")
    
end

function OnJoinClearedRoom(proxy, param)
    print("OnJoinClearedRoom begin")
    proxy:RecallMenuEvent(0, 140070)
    print("OnJoinClearedRoom end")
    
end

function OnBeBlackKickOut(proxy, param)
    print("OnBeBlackKickOut begin")
    if proxy:IsWhiteGhost() == true then
        return
    end
    proxy:CustomLuaCallStart(4063, proxy:GetLocalPlayerId())
    proxy:LeaveSession()
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000450)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4046, "OnLeave_Limit", 5, 0, 1, once)
    proxy:SetEventFlag(4047, true)
    proxy:SetLoadWait()
    proxy:NotNetMessage_end()
    print("OnBeBlackKickOut end")
    
end

function OnRoomTimeOut(proxy, param)
    print("OnRoomTimeOut begin")
    print("OnRoomTimeOut end")
    
end

function OnSummonTimeOut(proxy, param)
    print("OnSummonTimeOut begin")
    print("OnSummonTimeOut end")
    
end

function OnBeJoinStart_White(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_White begin")
        proxy:RecallMenuEvent(0, 140023)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_White end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_White 既に死んでる")
    end
    
end

function OnBeJoinStart_Black(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_Black begin")
        proxy:RecallMenuEvent(0, 140024)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_Black end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_Black 既に死んでる")
    end
    
end

function OnBeJoinStart_ForceJoin(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_ForceJoin begin")
        proxy:RecallMenuEvent(0, 140025)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_ForceJoin end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_ForceJoin 既に死んでる")
    end
    
end

function OnBeJoinStart_InvadeNito(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_InvadeNito begin")
        proxy:RecallMenuEvent(0, 140027)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_InvadeNito end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_InvadeNito 既に死んでる")
    end
    
end

function OnBeJoinStart_Dragonewt(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_Dragonewt begin")
        proxy:RecallMenuEvent(0, 140028)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_Dragonewt end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_Dragonewt 既に死んでる")
    end
    
end

function OnBeJoinStart_InvadeBounty(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_InvadeBounty begin")
        proxy:RecallMenuEvent(0, 140029)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_InvadeBounty end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_InvadeBounty 既に死んでる")
    end
    
end

function OnBeJoinStart_Coliseum(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_Coliseum begin")
        proxy:ClearRecallData()
        proxy:RecallMenuEvent(3, 506002)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_Coliseum end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_Coliseum 既に死んでる")
    end
    
end

function OnBeJoinStart_Detect(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_Detect begin")
        proxy:RecallMenuEvent(0, 140025)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_Detect end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_Detect 既に死んでる")
    end
    
end

function OnBeJoinStart_WhiteRescue(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_WhiteRescue begin")
        proxy:RecallMenuEvent(0, 140023)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_WhiteRescue end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_WhiteRescue 既に死んでる")
    end
    
end

function OnBeJoinStart_BlackRescue(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_BlackRescue begin")
        proxy:RecallMenuEvent(0, 140025)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "SummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_BlackRescue end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_BlackRescue 既に死んでる")
    end
    
end

function OnBeJoinStart_ForceSummon(proxy, param)
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        print("OnBeJoinStart_ForceSummon begin")
        proxy:RecallMenuEvent(0, 140026)
        proxy:NotNetMessage_begin()
        proxy:OnRequestMenuEnd(4059, "ForceSummonReloadStart")
        proxy:NotNetMessage_end()
        print("OnBeJoinStart_ForceSummon end")
    else
        proxy:LeaveSession()
        proxy:ResetSummonParam()
        proxy:WARN("OnBeJoinStart_ForceSummon 既に死んでる")
    end
    
end

function SummonReloadStart(proxy, param)
    print("SummonReloadStart begin")
    if proxy:HavePartyMember() == true then
        if proxy:IsAlive(LOCAL_PLAYER) == true then
            proxy:SummonedMapReload()
        else
            proxy:LeaveSession()
            proxy:ResetSummonParam()
            proxy:WARN("SummonReloadStart ルーム有り死んでる")
        end
    elseif proxy:IsAlive(LOCAL_PLAYER) == false then
        proxy:ResetSummonParam()
        proxy:WARN("SummonReloadStart ルーム無し死んでる")
    end
    print("SummonReloadStart end")
    
end

function ForceSummonReloadStart(proxy, param)
    print("ForceSummonReloadStart begin")
    if proxy:HavePartyMember() == true then
        if proxy:IsAlive(LOCAL_PLAYER) == true then
            proxy:RequestFullRecover()
            proxy:SummonedMapReload()
        else
            proxy:LeaveSession()
            proxy:ResetSummonParam()
            proxy:WARN("ForceSummonReloadStart ルーム有り死んでる")
        end
    elseif proxy:IsAlive(LOCAL_PLAYER) == false then
        proxy:ResetSummonParam()
        proxy:WARN("ForceSummonReloadStart ルーム無し死んでる")
    end
    print("ForceSummonReloadStart end")
    
end

function OnJoinMutiplay(proxy, param)
    print("OnJoinMutiplay begin")
    print("OnJoinMutiplay end")
    
end

function JoinSession_White(proxy, param)
    print("JoinSession_White begin")
    proxy:RecallMenuEvent(0, 140013)
    print("JoinSession_White end")
    
end

function JoinSession_Black(proxy, param)
    print("JoinSession_Black begin")
    proxy:RecallMenuEvent(0, 140014)
    print("JoinSession_Black end")
    
end

function JoinSession_ForceJoin(proxy, param)
    print("JoinSession_ForceJoin begin")
    proxy:RecallMenuEvent(0, 140015)
    print("JoinSession_ForceJoin end")
    
end

function JoinSession_Detect(proxy, param)
    print("JoinSession_Detect begin")
    proxy:RecallMenuEvent(0, 9602)
    print("JoinSession_Detect end")
    
end

function JoinSession_InvadeNito(proxy, param)
    print("JoinSession_InvadeNito begin")
    proxy:RecallMenuEvent(0, 140017)
    print("JoinSession_InvadeNito end")
    
end

function JoinSession_Dragonewt(proxy, param)
    print("JoinSession_Dragonewt begin")
    proxy:RecallMenuEvent(0, 140018)
    print("JoinSession_Dragonewt end")
    
end

function JoinSession_InvadeBounty(proxy, param)
    print("JoinSession_InvadeBounty begin")
    proxy:RecallMenuEvent(0, 140019)
    print("JoinSession_InvadeBounty end")
    
end

function JoinSession_Coliseum(proxy, param)
    print("JoinSession_Coliseum begin")
    proxy:RecallMenuEvent(0, 150020)
    print("JoinSession_Coliseum end")
    
end

function JoinSession_ForceSummon(proxy, param)
    print("JoinSession_ForceSummon begin")
    proxy:RecallMenuEvent(0, 140016)
    print("JoinSession_ForceSummon end")
    
end

function OnEvent_Call_SOS(proxy, param)
    print("OnEvent_Call_SOS begin")
    proxy:LuaCallStartPlus(4058, 1, proxy:GetLocalPlayerId())
    print("OnEvent_Call_SOS end")
    
end

function OnEvent_Call_BlackSOS(proxy, param)
    print("OnEvent_Call_BlackSOS begin")
    proxy:LuaCallStartPlus(4058, 2, proxy:GetLocalPlayerId())
    print("OnEvent_Call_BlackSOS end")
    
end

function OnEvent_Call_DragonewtSOS(proxy, param)
    print("OnEvent_Call_DragonewtSOS begin")
    proxy:LuaCallStartPlus(4058, 3, proxy:GetLocalPlayerId())
    print("OnEvent_Call_DragonewtSOS end")
    
end

function Call_WhiteSos(proxy, param)
    print("Call_WhiteSos begin")
    proxy:RecallMenuEvent(0, 140010)
    print("Call_WhiteSos end")
    
end

function Call_BlackSos(proxy, param)
    print("Call_BlackSos begin")
    proxy:RecallMenuEvent(0, 140011)
    print("Call_BlackSos end")
    
end

function Call_Dragonewt(proxy, param)
    print("Call_Dragonewt begin")
    proxy:RecallMenuEvent(0, 140012)
    print("Call_Dragonewt end")
    
end

function OnEvent_SendSosSign_Dummy(proxy, param)
    
end

function OnEvent_SendSoulSign_ForceJoin(proxy, param)
    print("OnEvent_SendSoulSign_ForceJoin begin")
    proxy:RecallMenuEvent(0, 140005)
    print("OnEvent_SendSoulSign_ForceJoin end")
    
end

function OnEvent_SendSoulSign_InvaderNito(proxy, param)
    print("OnEvent_SendSoulSign_InvaderNito begin")
    proxy:RecallMenuEvent(0, 140006)
    proxy:SetMiniBlockIndex()
    proxy:SetSosSignPos()
    print("OnEvent_SendSoulSign_InvaderNito end")
    
end

function OnEvent_SendSosSign_InvadeBounty(proxy, param)
    print("OnEvent_SendSosSign_InvadeBounty begin")
    proxy:RecallMenuEvent(0, 141005)
    print("OnEvent_SendSosSign_InvadeBounty end")
    
end

function OnEvent_SendSosSign_Coliseum(proxy, param)
    print("OnEvent_SendSosSign_Coliseum begin")
    proxy:RecallMenuEvent(0, 141005)
    print("OnEvent_SendSosSign_Coliseum end")
    
end

function OnReviveMagic(proxy, param)
    print("OnReviveMagic begin")
    if proxy:IsBlackGhost() == true then
        return
    end
    if proxy:IsWhiteGhost() == true then
        proxy:LuaCallStart(4055, 1)
    end
    proxy:CustomLuaCallStart(4063, proxy:GetLocalPlayerId())
    proxy:LeaveSession()
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000445)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4046, "OnReviveMagic_1", 5, 0, 1, once)
    proxy:SetEventFlag(4047, true)
    proxy:SetLoadWait()
    proxy:NotNetMessage_end()
    print("OnReviveMagic end")
    
end

function OnReviveMagic_1(proxy, param)
    print("OnReviveMagic_1 begin")
    proxy:SetAliveMotion(true)
    proxy:RequestFullRecover()
    proxy:SetFlagInitState(2)
    proxy:EraseEventSpecialEffect(LOCAL_PLAYER, 101)
    proxy:SetSosSignWarp()
    proxy:SetDefaultMapUid(-1)
    proxy:RevivePlayerNext()
    proxy:WarpNextStageKick()
    print("OnReviveMagic_1 end")
    
end

function OnSpEffectRevive(proxy, param)
    print("OnSpEffectRevive begin")
    proxy:RevivePlayer()
    proxy:SetTextEffect(TEXT_TYPE_Revival)
    print("OnSpEffectRevive end")
    
end

function OnLeaveMenu_Yes(proxy, param)
    print("OnLeaveMenu_Yes begin")
    if proxy:IsCompleteEvent(4047) == true then
        return
    end
    proxy:OnIntendedSessionLeave()
    proxy:SetLoadWait()
    if proxy:IsWhiteGhost() == true then
        proxy:LuaCallStartPlus(4046, 1, proxy:GetLocalPlayerId())
    elseif proxy:IsBlackGhost() == true or proxy:IsIntruder() == true then
        proxy:LuaCallStartPlus(4046, 2, proxy:GetLocalPlayerId())
        if proxy:GetTempSummonParam() > SUMMONPARAM_TYPE_None then
            proxy:LuaCallStartPlus(4046, 3, proxy:GetLocalPlayerId())
        else
        end
    end
    proxy:LeaveSession()
    if proxy:IsLivePlayer() == true then
        print("OnLeaveMenu_Yes return end")
        return
    end
    proxy:SetEventFlag(4047, true)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(4046, "OnLeave_Limit", 5, 0, 1, once)
    proxy:NotNetMessage_end()
    print("OnLeaveMenu_Yes end")
    
end

function LeaveMessage(proxy, param)
    print("LeaveMessage begin")
    if proxy:GetLocalPlayerId() == param:GetParam3() then
        print("LeaveMessage NO INPUT", proxy:IsNoInputLeave())
        if proxy:IsNoInputLeave() == false then
            print("LeaveMessage IsNoInputLeave false")
            proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
            proxy:AddInfomationTosBuffer(20000430)
        end
    else
        proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
        proxy:AddInfomationTimeMsgTag(20000435, TAG_IDX_leaveChara, param:GetParam3())
    end
    print("LeaveMessage end")
    
end

function OnLeave_Limit(proxy, param)
    print("OnLeave_Limit begin")
    if proxy:IsAlive(LOCAL_PLAYER) == true then
        proxy:SetFlagInitState(2)
        proxy:SetSosSignWarp()
        proxy:SetDefaultMapUid(-1)
        proxy:WarpNextStageKick()
        proxy:SetChrTypeDataGreyNext()
    end
    print("OnLeave_Limit end")
    
end

function OnRoomDisappeared(proxy, param)
    print("OnRoomDisappeared begin")
    if proxy:IsCompleteEvent(4047) == true then
        return
    end
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20001000)
    if proxy:IsLivePlayer() == false and proxy:IsGreyGhost() == false then
        proxy:NotNetMessage_begin()
        proxy:OnKeyTime2(4046, "OnLeave_Limit", 5, 0, 1, once)
        proxy:SetEventFlag(4047, true)
        proxy:SetLoadWait()
        proxy:SetFlagInitState(2)
        proxy:NotNetMessage_end()
    end
    print("OnRoomDisappeared end")
    
end

function SummonInfoMsg_White(proxy, param)
    if param:IsNetMessage() == true then
        if proxy:IsClient() == false then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(0, 140030)
            else
                proxy:WARN("ゴースト名取得できなかった")
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(0, 140043)
        else
            proxy:WARN("ホスト名取得できなかった")
        end
    elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_liveChara, proxy:GetHostPlayerNo()) == true then
        proxy:RecallMenuEvent(0, 140033)
    end
    
end

function SummonInfoMsg_Black(proxy, param)
    if param:IsNetMessage() == true then
        if proxy:IsClient() == false then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(0, 140031)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(0, 140044)
        end
    elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_liveChara, proxy:GetHostPlayerNo()) == true then
        proxy:RecallMenuEvent(0, 140034)
    end
    
end

function SummonInfoMsg_ForceJoinBlack(proxy, param)
    if param:IsNetMessage() == true then
        if proxy:IsGameClient() == false then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(1, 140032)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(1, 140045)
        end
    elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_liveChara, proxy:GetHostPlayerNo()) == true then
        proxy:RecallMenuEvent(1, 140035)
    else
        proxy:RecallMenuEvent(1, 140035)
    end
    
end

function SummonInfoMsg_ForceSummonBlack(proxy, param)
    if param:IsNetMessage() == true then
        if proxy:IsClient() == false then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(1, 140046)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(1, 140046)
        end
    elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_liveChara, proxy:GetHostPlayerNo()) == true then
        proxy:RecallMenuEvent(1, 140036)
    end
    
end

function SummonInfoMsg_InvadeNito(proxy, param)
    if param:IsNetMessage() == true then
        if proxy:IsClient() == false then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(1, 140047)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(1, 140047)
        end
    elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_liveChara, proxy:GetHostPlayerNo()) == true then
        proxy:RecallMenuEvent(1, 140037)
    end
    
end

function SummonInfoMsg_Dragonewt(proxy, param)
    if param:IsNetMessage() == true then
        if proxy:IsClient() == false then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(1, 140048)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(1, 140048)
        end
    elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_liveChara, proxy:GetHostPlayerNo()) == true then
        proxy:RecallMenuEvent(1, 140038)
    end
    
end

function SummonInfoMsg_InvadeBounty(proxy, param)
    if param:IsNetMessage() == true then
        if proxy:IsClient() == false then
            if proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
                proxy:RecallMenuEvent(1, 140049)
            end
        elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_joinChara, param:GetParam3()) == true then
            proxy:RecallMenuEvent(1, 140049)
        end
    elseif proxy:EventTagInsertString_forPlayerNo(TAG_IDX_liveChara, proxy:GetHostPlayerNo()) == true then
        proxy:RecallMenuEvent(1, 140039)
    else
        proxy:RecallMenuEvent(1, 140039)
    end
    
end

function SummonInfoMsg_Coliseum(proxy, param)
    
end

function SummonSuccess(proxy, playerId)
    proxy:SummonSuccess(playerId)
    
end

function LiveSide_SummonTimeOut(proxy, param)
    print("LiveSide_SummonTimeOut begin")
    proxy:InfomationMenu(INFOMENU_TYPE_LIST, -1, 0, -1, 1)
    proxy:AddInfomationTosBuffer(20000100)
    print("LiveSide_SummonTimeOut end")
    
end

function ReportBossArea(proxy, param)
    print("ReportBossArea begin")
    if param:IsNetMessage() == true then
        proxy:OnEnterBossArena()
        local summonParam = proxy:GetTempSummonParam()
        if summonParam == SUMMONPARAM_TYPE_White then
            proxy:NotNetMessage_begin()
            proxy:RepeatMessage_begin()
            proxy:OnSelectMenu(4061, "MenuClose", 9500, 1, 6, -1, 3, once)
            proxy:RepeatMessage_end()
            proxy:NotNetMessage_end()
        elseif summonParam == SUMMONPARAM_TYPE_Black then
            Failed_BossAreaMission(proxy, param)
        elseif summonParam == SUMMONPARAM_TYPE_FroceJoinBlack then
            Failed_BossAreaMission(proxy, param)
        elseif summonParam == SUMMONPARAM_TYPE_DetectBlack then
            Failed_BossAreaMission(proxy, param)
        elseif summonParam == SUMMONPARAM_TYPE_InvadeNito then
            Failed_BossAreaMission(proxy, param)
        elseif summonParam == SUMMONPARAM_TYPE_Dragonewt then
            Failed_BossAreaMission(proxy, param)
        elseif summonParam == SUMMONPARAM_TYPE_InvadeBounty then
            Failed_BossAreaMission(proxy, param)
        end
    else
        print("ボス部屋侵入を通知しました")
    end
    print("ReportBossArea end")
    
end

function Failed_BossAreaMission(proxy, param)
    print("Failed_BossAreaMission begin")
    if proxy:IsCompleteEvent(4047) == false then
        proxy:ClearBossGauge()
        MissionFailed(proxy, param)
        proxy:SetEventFlag(4047, true)
        proxy:SetLoadWait()
        proxy:SetEventFlag(4000, true)
        proxy:LeaveSession()
        proxy:NotNetMessage_begin()
        proxy:OnKeyTime2(4045, "Failed_BossAreaMission_LeaveMap", 5, 0, 0, once)
        proxy:NotNetMessage_end()
        if proxy:IsBlackGhost() == true then
            proxy:RepeatMessage_begin()
            proxy:NotNetMessage_begin()
            proxy:OnRegistFunc(4050, "Check_BlockClearAnim", "BlockClearAnim", 20, once)
            proxy:NotNetMessage_end()
            proxy:RepeatMessage_end()
        end
    end
    print("Failed_BossAreaMission end")
    
end

function Failed_BossAreaMission_LeaveMap(proxy, param)
    print("Failed_BossAreaMission_LeaveMap begin")
    proxy:SetFlagInitState(2)
    proxy:SetSosSignWarp()
    proxy:SetDefaultMapUid(-1)
    proxy:WarpNextStageKick()
    proxy:SetChrTypeDataGreyNext()
    print("Failed_BossAreaMission_LeaveMap end")
    
end

function OnEvent_4034(proxy, param)
    print("OnEvent_4034 begin")
    if proxy:IsClient() == false then
        proxy:SaveRequest_Profile()
        local areaNo = proxy:GetCurrentMapAreaNo()
        if areaNo == 99 then
            print("テストマップなのでマルチ壁処理無し")
        else
            proxy:SetMultiWallMapUid()
            local wallNum = proxy:GetMultiWallNum()
            print("wallNum ", wallNum)
            if wallNum > 0 then
                for f187_local0 = 1, wallNum, 1 do
                    local f187_local3 = {1994, 1996, 1998, 1988, 1986, 1984, 1982, 1980, 1978, 1976, 1974}
                    local MultiWall = proxy:CalcGetMultiWallEntityId(f187_local3[f187_local0])
                    Lua_MultiWall(proxy, MultiWall)
                end
            end
        end
    end
    print("OnEvent_4034 end")
    
end

function Lua_MultiWall(proxy, id)
    print("マルチ時魔法壁ON")
    local MultiSfx = id + 1
    print("マルチ壁ID: ", id)
    print("マルチ壁SFX　ID:", MultiSfx)
    proxy:SetColiEnable(id, true)
    proxy:SetDrawEnable(id, true)
    proxy:ValidSfx(MultiSfx)
    
end

function Lua_MultiWall2(proxy, id)
    print("マルチ時魔法壁ON")
    local MultiSfx = id - 99
    print("マルチ壁ID: ", id)
    print("マルチ壁SFX　ID:", MultiSfx)
    proxy:SetColiEnable(id, true)
    proxy:SetDrawEnable(id, true)
    proxy:ValidSfx(MultiSfx)
    
end

function OnEvent_4035(proxy, param)
    print("OnEvent_4035 begin")
    if proxy:IsClient() == false then
        proxy:SaveRequest_Profile()
        local areaNo = proxy:GetCurrentMapAreaNo()
        if areaNo == 99 then
            print("テストマップなのでマルチ壁処理無し")
        else
            local wallNum = proxy:GetMultiWallNum()
            print("wallNum ", wallNum)
            if wallNum > 0 and proxy:CalcGetMultiWallEntityId(0) > 0 then
                for f190_local0 = 1, wallNum, 1 do
                    local f190_local3 = {1994, 1996, 1998, 1988, 1986, 1984, 1982, 1980, 1978, 1976, 1974}
                    local MultiWall = proxy:CalcGetMultiWallEntityId(f190_local3[f190_local0])
                    Lua_InvalidMultiWall(proxy, MultiWall)
                end
            end
        end
    end
    print("OnEvent_4035 end")
    
end

function Lua_InvalidMultiWall(proxy, id)
    print("マルチ時魔法壁OFF")
    local MultiSfx = id + 1
    print("マルチ壁ID: ", id)
    print("マルチ壁SFX　ID:", MultiSfx)
    proxy:SetColiEnable(id, false)
    proxy:SetDrawEnable(id, false)
    proxy:InvalidSfx(MultiSfx, true)
    
end

function WhiteReviveCount(proxy, param)
    print("WhiteReviveCount begin")
    if proxy:IsClient() == false then
        print("WhiteReviveCount AddQWC AddHelpWhiteCount")
        proxy:AddHelpWhiteGhost()
    end
    print("WhiteReviveCount end")
    
end

function dummy(proxy, param)
    
end

function MenuClose(proxy, param)
    print("MenuClose begin")
    proxy:CloseGenDialog()
    print("MenuClose end")
    
end

function RegistReturnTitle(proxy, param)
    print("RegistReturnTitle begin")
    proxy:SaveRequest()
    proxy:StopPlayer()
    proxy:SetEventFlag(4047, true)
    proxy:SetLoadWait()
    proxy:NotNetMessage_begin()
    proxy:RepeatMessage_begin()
    proxy:OnKeyTime2(4062, "ReturnTitle_wait", 0.5, 0, 20, once)
    proxy:RepeatMessage_end()
    proxy:NotNetMessage_end()
    print("RegistReturnTitle end")
    
end

function ReturnTitle_wait(proxy, param)
    proxy:NotNetMessage_begin()
    proxy:RepeatMessage_begin()
    proxy:OnRegistFunc(4062, "Check_ReturnTitle", "OnReturnTitle", 1, once)
    proxy:RepeatMessage_end()
    proxy:NotNetMessage_end()
    
end

function Check_ReturnTitle(proxy)
    if proxy:IsShowMenu_InfoMenu() == true then
        return false
    end
    return true
    
end

function OnReturnTitle(proxy, param)
    print("OnReturnTitle begin")
    proxy:ReturnMapSelect()
    print("OnReturnTitle end")
    
end

function OnEnterRideObj(proxy, param)
    if param:IsNetMessage() == true then
        print("OnEnterRideObj begin")
        local obj = param:GetParam2()
        local sysidx = param:GetParam3()
        print("obj :", obj, " sysidx :", sysidx)
        proxy:SetSyncRideObjInfo(param:GetPlayID() + NET_PLAYER, obj, sysidx)
        print("OnEnterRideObj end")
    end
    
end

function OnLeaveRideObj(proxy, param)
    if param:IsNetMessage() == true then
        print("OnLeaveRideObj begin")
        proxy:ResetSyncRideObjInfo(param:GetPlayID() + NET_PLAYER)
        print("OnLeaveRideObj end")
    end
    
end

function Lua_MultiDoping(proxy, eneId)
    proxy:NotNetMessage_begin()
    print("Lua_MultiDoping begin")
    proxy:ApplyMultiDoping(eneId)
    proxy:OnKeyTime2(4070, "ForceUpdate", 0.1, 0, eneId, once)
    proxy:NotNetMessage_end()
    
end

function ForceUpdate(proxy, param)
    proxy:MultiDoping_AllEventBody()
    proxy:ForceUpdateNextFrame(param:GetParam3())
    
end

function Regist_LadderAction(proxy, actionId1, actionId2, targetId)
    proxy:OnDistanceActionDmyPoly(actionId1, LOCAL_PLAYER, targetId, 194, "OnEvent_LadderDawn", LadderDist_A, LadderAngle_A, HELPID_DOWN, everytime)
    proxy:OnDistanceActionDmyPoly(actionId2, LOCAL_PLAYER, targetId, 195, "OnEvent_LadderUp", LadderDist_A, LadderAngle_A, HELPID_UP, everytime)
    
end

function OnEvent_LadderDawn(proxy, param)
    print("OnEvent_LadderDawn begin")
    if param:IsNetMessage() == true then
        print("IsNetMessage true")
        print("OnEvent_LadderDawn end")
        return
    end
    local actionId = param:GetParam1()
    local targetId = param:GetParam2()
    proxy:BeginAction(LOCAL_PLAYER, 2, 2, 2)
    proxy:HoverMoveValDmy(LOCAL_PLAYER, targetId, 192, LadderTime_A)
    proxy:SetEventCommand(LOCAL_PLAYER, 1)
    proxy:SetKeepCommandIndex(LOCAL_PLAYER, 0, 1, 3)
    proxy:DisableMove(LOCAL_PLAYER, 1)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(actionId, "OnEvent_LadderDawn_1", LadderTime_A, 0, targetId, once)
    proxy:NotNetMessage_end()
    print("OnEvent_LadderDawn end")
    
end

function OnEvent_LadderDawn_1(proxy, param)
    print("OnEvent_LadderDawn_1 begin")
    if proxy:GetEventMode(LOCAL_PLAYER) == 0 then
        print("Local Player GetEventMode 0")
        print("OnEvent_LadderDawn_1 end")
        return
    end
    local maxCount = proxy:GetLadderCount(param:GetParam3(), 193, 194)
    proxy:BeginAction(LOCAL_PLAYER, 1, maxCount + 1, maxCount)
    proxy:DisableMove(LOCAL_PLAYER, 0)
    print("OnEvent_LadderDawn_1 end")
    
end

function OnEvent_LadderUp(proxy, param)
    print("OnEvent_LadderUp begin")
    if param:IsNetMessage() == true then
        print("IsNetMessage true")
        print("OnEvent_LadderUp end")
        return
    end
    local actionId = param:GetParam1()
    local targetId = param:GetParam2()
    proxy:BeginAction(LOCAL_PLAYER, 2, -1, 1)
    proxy:HoverMoveValDmy(LOCAL_PLAYER, targetId, 191, LadderTime_A)
    proxy:SetEventCommand(LOCAL_PLAYER, 0)
    proxy:SetKeepCommandIndex(LOCAL_PLAYER, 0, 1, 3)
    proxy:DisableMove(LOCAL_PLAYER, 1)
    proxy:NotNetMessage_begin()
    proxy:OnKeyTime2(actionId, "OnEvent_LadderUp_1", LadderTime_A, 0, targetId, once)
    proxy:NotNetMessage_end()
    print("OnEvent_LadderUp end")
    
end

function OnEvent_LadderUp_1(proxy, param)
    print("OnEvent_LadderUp_1 begin")
    if proxy:GetEventMode(LOCAL_PLAYER) == 0 then
        print("Local Player GetEventMode 0")
        print("OnEvent_LadderUp_1 end")
        return
    end
    local maxCount = proxy:GetLadderCount(param:GetParam3(), 193, 194)
    proxy:BeginAction(LOCAL_PLAYER, 1, -1, maxCount)
    proxy:DisableMove(LOCAL_PLAYER, 0)
    print("OnEvent_LadderUp_1 end")
    
end

function AI_TadareBoss_SetNearEventPoint_CallByAi(proxy, param)
    local DEF_TadareBoss_EntityId = 1410600
    local moveablePointListListIdx = param:GetParam2()
    local moveablePointListLIstBeginIdx = 1
    local findType = param:GetParam3()
    local f208_local0 = {{1412700, 1412701, 1412702, 1412703, 1412704, 1412705, 1412706, 1412710, 1412711, 1412712, 1412713, 1412714, 1412720}, {1412730}, {1412730}, {1412730}}
    local minListListIdx = 1
    local minListIdx = 1
    local minDist = 10000
    if findType == 1 then
        minDist = 0
    end
    for f208_local1 = moveablePointListLIstBeginIdx, moveablePointListListIdx, 1 do
        local listNum = table.getn(f208_local0[f208_local1])
        for f208_local4 = 1, listNum, 1 do
            local pointEntityId = f208_local0[f208_local1][f208_local4]
            local dist = proxy:GetDistance(LOCAL_PLAYER, pointEntityId)
            if findType == 0 then
                if dist < minDist then
                    minDist = dist
                    minListListIdx = f208_local1
                    minListIdx = f208_local4
                end
            elseif findType == 1 and minDist < dist then
                minDist = dist
                minListListIdx = f208_local1
                minListIdx = f208_local4
            end
        end
    end
    TadareBoss_EntityId = DEF_TadareBoss_EntityId
    local nearPointEntityId = f208_local0[minListListIdx][minListIdx]
    proxy:SetMovePoint(TadareBoss_EntityId, nearPointEntityId, 0.05)
    return true
    

end

warpPointEntityId = -1

function Lua_Warp(proxy, bonfireEntityId, warpTargetBonfireEntityId)
    print("Lua_Warp begin")
    print("bonfire=" .. bonfireEntityId .. " target=" .. warpTargetBonfireEntityId)
    warpPointEntityId = warpTargetBonfireEntityId - 980
    proxy:NoAnimeTurnCharactor(LOCAL_PLAYER, bonfireEntityId, TURNSKIP_ANGLE)
    proxy:ForcePlayAnimation(LOCAL_PLAYER, 7725)
    proxy:SetEventFlag(4079, true)
    print("Lua_Warp end")
    
end

function Lua_Warp_1(proxy, param)
    print("Lua_Warp_1 begin")
    proxy:ForcePlayAnimation(10000, 8284)
    proxy:WarpNextStage_Bonfire(warpPointEntityId)
    print("Lua_Warp_1 end")
    
end

randomAnimOffset = 0
luaTempBonfireEntityId = 0

function Lua_BonfireLoopAnimBegin(proxy, bonfireEntityId)
    print("Lua_BonfireLoopAnimBegin begin")
    luaTempBonfireEntityId = bonfireEntityId
    proxy:SetEventFlag(4079, false)
    proxy:SetEventFlag(4083, false)
    proxy:SetEventFlag(4084, false)
    proxy:NoAnimeTurnCharactor(LOCAL_PLAYER, bonfireEntityId, TURNSKIP_ANGLE)
    randomAnimOffset = 0
    randomAnimOffset = proxy:GetRandom(0, 2)
    local bonfireAnimId = 7700 + 10 * randomAnimOffset
    proxy:SetFootIKInterpolateType(LOCAL_PLAYER, bonfireAnimId, 2)
    proxy:PlayAnimation(LOCAL_PLAYER, bonfireAnimId)
    proxy:NotNetMessage_begin()
    proxy:OnChrAnimEnd(SYSTEM_WARP, LOCAL_PLAYER, 7595, "Lua_BonfireLoopAnimBegin_1", once)
    proxy:NotNetMessage_end()
    print("Lua_BonfireLoopAnimBegin end")
    
end

function Lua_BonfireLoopAnimBegin_1(proxy, param)
    if proxy:IsCompleteEvent(4079) == false then
        print("Lua_BonfireLoopAnimBegin_1 begin")
        proxy:SetFootIKInterpolateType(LOCAL_PLAYER, -1, 3)
        if proxy:IsCompleteEvent(4083) == false then
            proxy:SetEventFlag(4084, true)
            local bonfireAnimId = 7701 + 10 * randomAnimOffset
            proxy:PlayLoopAnimation(LOCAL_PLAYER, bonfireAnimId)
        else
            proxy:SetEventFlag(4084, true)
            Lua_BonfireLoopAnimEnd(proxy)
        end
        print("Lua_BonfireLoopAnimBegin_1 end")
    end
    
end

function Lua_BonfireLoopAnimEnd(proxy)
    print("Lua_BonfireLoopAnimEnd begin")
    if proxy:IsCompleteEvent(4084) == true then
        proxy:SetEventFlag(4079, true)
        proxy:StopLoopAnimation(LOCAL_PLAYER)
        local bonfireAnimId = 7702 + 10 * randomAnimOffset
        proxy:SetFootIKInterpolateType(LOCAL_PLAYER, bonfireAnimId, 1)
        proxy:ForcePlayAnimation(LOCAL_PLAYER, bonfireAnimId)
        proxy:NotNetMessage_begin()
        proxy:OnChrAnimEnd(SYSTEM_WARP, LOCAL_PLAYER, bonfireAnimId, "Lua_BonfireLoopAnimEnd_1", once)
        proxy:NotNetMessage_end()
    else
        proxy:SetEventFlag(4083, true)
    end
    print("Lua_BonfireLoopAnimEnd end")
    
end

function Lua_BonfireLoopAnimEnd_1(proxy)
    print("Lua_BonfireLoopAnimEnd_1 begin")
    proxy:SetFootIKInterpolateType(LOCAL_PLAYER, -1, 0)
    print("Lua_BonfireLoopAnimEnd_1 end")
    
end

function Lua_BonfireCovenantAnim(proxy, bonfireEntityId)
    print("Lua_BonfireCovenantAnim begin")
    luaTempBonfireEntityId = bonfireEntityId
    proxy:NoAnimeTurnCharactor(LOCAL_PLAYER, bonfireEntityId, 10)
    proxy:PlayAnimation(LOCAL_PLAYER, 7905)
    print("Lua_BonfireCovenantAnim end")
    
end

function Lua_BonfireFirstInjectAnim(proxy, bonfireEntityId)
    print("Lua_BonfireFirstInjectAnim begin")
    luaTempBonfireEntityId = bonfireEntityId
    proxy:NoAnimeTurnCharactor(LOCAL_PLAYER, bonfireEntityId, 10)
    proxy:PlayAnimation(LOCAL_PLAYER, 7698)
    print("Lua_BonfireFirstInjectAnim end")
    
end

function Lua_BonfireInjectAnim(proxy, bonfireEntityId)
    print("Lua_BonfireInjectAnim begin")
    proxy:ForcePlayAnimation(LOCAL_PLAYER, 7699)
    randomAnimOffset = 0
    proxy:NotNetMessage_begin()
    proxy:OnChrAnimEnd(SYSTEM_WARP, LOCAL_PLAYER, 7595, "Lua_BonfireLoopAnimBegin_1", once)
    proxy:NotNetMessage_end()
    print("Lua_BonfireInjectAnim end")
    
end

function OnEvent_BonfireFirstLvUp(proxy, param)
    print("OnEvent_BonfireFirstLvUp begin")
    proxy:Util_RequestLevelUpFirst(luaTempBonfireEntityId)
    local textEffectTime = 0.6
    proxy:NotNetMessage_begin()
    proxy:RepeatMessage_begin()
    proxy:OnKeyTime2(4080, "OnEvent_BonfireFirstLvUp_TextEffect", textEffectTime, 0, 0, once)
    proxy:RepeatMessage_end()
    proxy:NotNetMessage_end()
    print("OnEvent_BonfireFirstLvUp end")
    
end

function OnEvent_BonfireFirstLvUp_TextEffect(proxy, param)
    print("OnEvent_BonfireFirstLvUp_TextEffect begin")
    proxy:SetTextEffect(TEXT_TYPE_Bonfire)
    print("OnEvent_BonfireFirstLvUp_TextEffect end")
    
end

function OnEvent_BonfireLvUp(proxy, param)
    print("OnEvent_BonfireLvUp begin")
    proxy:Util_RequestLevelUp(luaTempBonfireEntityId)
    print("OnEvent_BonfireLvUp end")
    
end

function OnEvent_BonfireRespawn(proxy, param)
    print("OnEvent_BonfireRespawn begin")
    proxy:Util_RequestRegene(luaTempBonfireEntityId)
    proxy:Util_RequestRespawn(luaTempBonfireEntityId)
    print("OnEvent_BonfireRespawn end")
    
end

function OnDeadEvent_dummy(proxy, param)
    if param:IsNetMessage() then
        print("OnDeadEvent_dummy begin")
        print("自分以外の誰かが死んだ")
        print("OnDeadEvent_dummy end")
    else
        print("OnDeadEvent_dummy begin")
        print("死んだのは自分なので報酬は無い")
        print("OnDeadEvent_dummy end")
    end
    
end

function OnDeadEvent_HostDead(proxy, param)
    local deadManVowType = param:GetParam3()
    local nitoInvadeMulti = false
    if deadManVowType == 6 and proxy:GetPartyMemberNum_InvadeType(INVADE_TYPE_Nito) > 0 then
        nitoInvadeMulti = true
    end
    if param:IsNetMessage() then
        print("OnDeadEvent_HostDead begin")
        local invadeType = proxy:GetLocalPlayerInvadeType()
        local soulRate = 0
        if nitoInvadeMulti then
            print("ホスト(ニト)死亡 自分:", invadeType)
            soulRate = SOUL_RATE_S
        else
            print("ホスト死亡 自分:", invadeType)
            soulRate = SOUL_RATE_S
        end
        if invadeType == INVADE_TYPE_NormalBlack then
            print("自分:赤侵入はボーナスSOUL倍率*", soulRate, "と人間性+1をゲット")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), soulRate, 1)
        elseif invadeType == INVADE_TYPE_ForceJoinBlack then
            print("自分:吸魂鬼はボーナスSOUL倍率*", soulRate, "人間性+1をゲット")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), soulRate, 1)
        elseif invadeType == INVADE_TYPE_DetectBlack then
            print("自分:探知ゴーストは保留")
        elseif invadeType == INVADE_TYPE_WhiteRescue then
            print("自分:白救援は保留")
        elseif invadeType == INVADE_TYPE_BlackRescue then
            print("自分:黒救援は保留")
        elseif invadeType == INVADE_TYPE_Nito then
            print("自分:ニト侵入はボーナスSOUL倍率*", soulRate, "をゲット")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), soulRate, 0)
        elseif invadeType == INVADE_TYPE_ThievesGuild then
            print("自分:盗賊団侵入はボーナスSOUL倍率*", soulRate, "と金品アイテムをゲット")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), soulRate, 0)
            proxy:GetRateItem_IgnoreMultiPlay(5000)
            proxy:IncrementThiefInvadePlaySuccessCount()
            proxy:AddCurrentVowRankPoint()
        elseif invadeType == INVADE_TYPE_OtoutoUmbasa then
            print("自分:弟アンバサ侵入はボーナスSOUL倍率*", soulRate, "と復讐の証をゲット")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), soulRate, 0)
            proxy:GetRateItem_IgnoreMultiPlay(5020)
        elseif invadeType == INVADE_TYPE_Dragonewt then
            print("自分:ドラゴンニュートはボーナスSOUL倍率*", soulRate, "と竜のうろこをゲット")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), soulRate, 0)
            proxy:GetRateItem_IgnoreMultiPlay(5040)
        elseif invadeType == INVADE_TYPE_InvadeBounty then
            print("自分:賞金首侵入はボーナスSOUL倍率*", soulRate, "と復讐の証をゲット")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), soulRate, 0)
            proxy:GetRateItem_IgnoreMultiPlay(5020)
        end
        proxy:OnDeadEvent_HostDead(invadeType)
        print("OnDeadEvent_HostDead end")
    elseif nitoInvadeMulti then
        print("OnDeadEvent_HostDead begin")
        print("ニト侵入マルチで殺されたのでアイテム抽選を開始")
        proxy:LuaCallStartPlus(4091, 0, proxy:GetPlayerNo_LotNitoMultiItem())
        print("OnDeadEvent_HostDead end")
    end
    
end

function OnNitoInvadeItemLot(proxy, param)
    if proxy:GetLocalPlayerId() == param:GetParam3() then
        print("OnNitoInvadeItemLot begin")
        proxy:GetRateItem_IgnoreMultiPlay(5010)
        print("OnNitoInvadeItemLot end")
    end
    
end

function OnDeadEvent_WhiteDead(proxy, param)
    if param:IsNetMessage() then
        print("OnDeadEvent_WhiteDead begin")
        local invadeType = proxy:GetLocalPlayerInvadeType()
        print("通常召喚死亡 自分:", invadeType)
        if invadeType ~= INVADE_TYPE_None and invadeType ~= INVADE_TYPE_NormalWhite then
            if invadeType == INVADE_TYPE_Nito then
                print("自分:ニト侵入はホスト以外では報酬無し")
            else
                print("クライアントプレイヤーが死亡したので報酬ボーナスSOUL倍率*", SOUL_RATE_S, "入手")
                proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_S, 0)
            end
        end
        print("OnDeadEvent_WhiteDead end")
    end
    
end

function OnDeadEvent_NormalBlackDead(proxy, param)
    if param:IsNetMessage() then
        print("OnDeadEvent_NormalBlackDead begin")
        local invadeType = proxy:GetLocalPlayerInvadeType()
        print("赤侵入死亡 自分:", invadeType)
        if proxy:IsGameClient() == false then
            print("自分：ホスト報酬ボーナスSOUL倍率*", SOUL_RATE_L, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L, 0)
            print("赤侵入者が人間性血痕を落とします")
            proxy:CreateHeroBloodStain(param:GetPlayID())
        else
            print("自分：クライアント報酬ボーナスSOUL倍率*", SOUL_RATE_L * 0.5, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L * 0.5, 0)
        end
        print("OnDeadEvent_NormalBlackDead end")
    end
    
end

function OnDeadEvent_ForceJoinBlackDead(proxy, param)
    if param:IsNetMessage() then
        print("OnDeadEvent_ForceJoinBlackDead begin")
        local invadeType = proxy:GetLocalPlayerInvadeType()
        print("吸魂鬼死亡 自分:", invadeType)
        if proxy:IsGameClient() == false then
            print("自分：ホスト報酬ボーナスSOUL倍率*", SOUL_RATE_L, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L, 0)
            print("吸魂鬼が人間性血痕を落とします")
            proxy:CreateHeroBloodStain(param:GetPlayID())
        else
            print("自分：クライアント報酬ボーナスSOUL倍率*", SOUL_RATE_L * 0.5, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L * 0.5, 0)
        end
        proxy:IncrementInvadersKilledCount()
        print("OnDeadEvent_ForceJoinBlackDead end")
    end
    
end

function OnDeadEvent_InvadeNitoDead(proxy, param)
    if param:IsNetMessage() then
        print("OnDeadEvent_InvadeNitoDead begin")
        local invadeType = proxy:GetLocalPlayerInvadeType()
        print("ニト侵入死亡 自分:", invadeType)
        if proxy:IsGameClient() == false then
            print("自分：ホスト報酬ボーナスSOUL倍率*", SOUL_RATE_L, "と墓王の瞳入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_S, 0)
            proxy:GetRateItem_IgnoreMultiPlay(5010)
        else
        end
        print("OnDeadEvent_InvadeNitoDead end")
    end
    
end

function OnDeadEvent_ThievesGuildDead(proxy, param)
    if param:IsNetMessage() then
        print("OnDeadEvent_ThievesGuildDead begin")
        local invadeType = proxy:GetLocalPlayerInvadeType()
        print("盗賊団侵入死亡 自分:", invadeType)
        if proxy:IsGameClient() == false then
            print("自分：ホスト報酬ボーナスSOUL倍率*", SOUL_RATE_L, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L, 0)
        elseif invadeType ~= INVADE_TYPE_ThievesGuild then
            print("自分：クライアント報酬ボーナスSOUL倍率*", SOUL_RATE_L * 0.5, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L * 0.5, 0)
        end
        print("OnDeadEvent_ThievesGuildDead end")
    end
    
end

function OnDeadEvent_OtoutoUmbasaDead(proxy, param)
    if param:IsNetMessage() then
        print("OnDeadEvent_OtoutoUmbasaDead begin")
        local invadeType = proxy:GetLocalPlayerInvadeType()
        print("弟アンバサ侵入死亡 自分:", invadeType)
        if proxy:IsGameClient() == false then
            print("自分：ホスト報酬ボーナスSOUL倍率*", SOUL_RATE_L, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L, 0)
        elseif invadeType ~= INVADE_TYPE_OtoutoUmbasa then
            print("自分：クライアント報酬ボーナスSOUL倍率*", SOUL_RATE_L * 0.5, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L * 0.5, 0)
        end
        print("OnDeadEvent_OtoutoUmbasaDead end")
    end
    
end

function OnDeadEvent_DragonewtDead(proxy, param)
    if param:IsNetMessage() then
        print("OnDeadEvent_DragonewtDead begin")
        local invadeType = proxy:GetLocalPlayerInvadeType()
        print("ドラゴンニュート侵入死亡 自分:", invadeType)
        if proxy:IsGameClient() == false then
            print("自分：ホスト報酬ボーナスSOUL倍率*", SOUL_RATE_L, "と竜のうろこ入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L, 0)
            proxy:GetRateItem_IgnoreMultiPlay(5040)
        elseif invadeType ~= INVADE_TYPE_Dragonewt then
            print("自分：クライアント報酬ボーナスSOUL倍率*", SOUL_RATE_L * 0.5, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L * 0.5, 0)
        end
        print("OnDeadEvent_DragonewtDead end")
    end
    
end

function OnDeadEvent_InvadeBounty(proxy, param)
    if param:IsNetMessage() then
        print("OnDeadEvent_InvadeBounty begin")
        local invadeType = proxy:GetLocalPlayerInvadeType()
        print("賞金首侵入死亡 自分:", invadeType)
        if proxy:IsGameClient() == false then
            print("自分：ホスト報酬ボーナスSOUL倍率*", SOUL_RATE_L, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L, 0)
        elseif invadeType ~= INVADE_TYPE_InvadeBounty then
            print("自分：クライアント報酬ボーナスSOUL倍率*", SOUL_RATE_L * 0.5, "入手")
            proxy:CalcExcuteMultiBonus(param:GetPlayID(), SOUL_RATE_L * 0.5, 0)
        end
        print("OnDeadEvent_InvadeBounty end")
    end
    
end


