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
            _ => null
        };
        if(next==null)return new(false,state,"invalid_stage_or_action");
        var completed=new HashSet<string>(state.CompletedActions){key};return new(true,next with {CompletedActions=completed});
    }
}
