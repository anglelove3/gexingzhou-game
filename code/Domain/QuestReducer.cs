namespace GeXingzhou.Domain;
public static class QuestReducer
{
    public static TransitionResult Apply(WorldSnapshot state,StoryAction action)
    {
        var key=action.Id+":"+action.OpportunityId;
        if(state.CompletedActions.Contains(key))return new(false,state,"duplicate");
        WorldSnapshot? next=action.Id switch
        {
            "invitation.answer" when state.Stage==SliceStage.InvitationPending&&state.InvitationState.PhoneRinging => state with {Stage=SliceStage.InvitationResolved,InvitationState=state.InvitationState with {Resolution=InvitationResolution.Answered,PhoneRinging=false}},
            "invitation.meeting_complete" when (state.Stage==SliceStage.InvitationPending||state.Stage==SliceStage.InvitationResolved)&&state.InvitationState.CarArrived => state with {Stage=SliceStage.CandyHeyPending,CandyCount=1,InvitationState=state.InvitationState with {Resolution=state.InvitationState.Resolution??InvitationResolution.Arrived,PhoneRinging=false}},
            "candy.hey.delivered" when state.Stage==SliceStage.CandyHeyPending&&state.CandyCount==1 => state with {Stage=SliceStage.CandyHeyDelivered,CandyCount=0},
            "soup.meet" when state.Stage==SliceStage.CandyHeyDelivered => state with {Stage=SliceStage.SoupMeet},
            "soup.response" when state.Stage==SliceStage.SoupMeet&&action.ChoiceCode is "eat" or "set_chopsticks" or "check_phone" => state,
            "memory.soup.enter" when state.Stage==SliceStage.SoupMeet&&state.MemoryState!=null&&state.ChoiceCodes.ContainsKey("soup-response-1") => state with {Stage=SliceStage.MemoryActive},
            "memory.coin.push" when state.MemoryState is {Completed:false} m&&action.ChoiceCode is "c1" or "c2" or "c3" or "c4"&&!m.PushedCoinIds.Contains(action.ChoiceCode) => state with {MemoryState=MemorySession.PushCoin(m,action.ChoiceCode)},
            "memory.food.resolve" when state.MemoryState is {Completed:false,PushedTotal:5} m&&Enum.TryParse<FoodChoice>(action.ChoiceCode,true,out var food)&&Enum.IsDefined(food) => state with {MemoryState=MemorySession.ResolveFood(m,food)},
            "memory.return" when state.Stage==SliceStage.MemoryActive&&state.MemoryState is {Completed:true} => state with {Stage=SliceStage.MemoryReturned},
            "memory.return" when state.MemoryState is {Replay:true,Completed:true} => state,
            "slice.complete" when state.Stage==SliceStage.MemoryReturned => state with {Stage=SliceStage.SliceComplete},
            _ => null
        };
        if(next==null)return new(false,state,"invalid_stage_or_action");
        var completed=new HashSet<string>(state.CompletedActions){key};var choices=new Dictionary<string,string>(state.ChoiceCodes){[action.OpportunityId]=action.ChoiceCode};
        return new(true,next with {CompletedActions=completed,ChoiceCodes=choices});
    }
}
