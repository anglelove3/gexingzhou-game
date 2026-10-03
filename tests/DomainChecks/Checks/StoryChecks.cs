using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class StoryChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        tests.Add(("Invitation","OperationalClockBoundaries",()=>{
            var s=InvitationClock.Advance(new(),19.99,true);Check.True(!s.VoiceReceived);
            s=InvitationClock.Advance(s,.01,true);Check.True(s.VoiceReceived);Check.True(!s.PhoneRinging);
            s=InvitationClock.Advance(s,15,true);Check.True(s.PhoneRinging);Check.True(!s.CarArrived);
            s=InvitationClock.Advance(s,30,true);Check.True(s.CarArrived);
        }));
        tests.Add(("Invitation","FrozenAndLargeDelta",()=>{
            Check.Equal(0d,InvitationClock.Advance(new(),100,false).Elapsed);
            var s=InvitationClock.Advance(new(),100,true);Check.True(s.VoiceReceived&&s.PhoneRinging&&s.CarArrived);
            Check.Equal(100d,InvitationClock.Advance(s,0,true).Elapsed);
        }));
        tests.Add(("Quest","WrongStageRejected",()=>{
            var s=new WorldSnapshot();var r=QuestReducer.Apply(s,new("candy.hey.delivered","delivered","hey-1"));
            Check.True(!r.Applied);Check.Equal(SliceStage.FreeArrival,r.Next.Stage);
        }));
        tests.Add(("Quest","AnsweredDoesNotGiveCandy",()=>{
            var s=new WorldSnapshot{Stage=SliceStage.InvitationPending,InvitationState=new(){PhoneRinging=true}};
            var r=QuestReducer.Apply(s,new("invitation.answer","answered","invitation-1"));
            Check.True(r.Applied);Check.Equal(0,r.Next.CandyCount);Check.Equal(InvitationResolution.Answered,r.Next.InvitationState.Resolution!.Value);
        }));
        tests.Add(("Quest","MeetingAndHeyOnce",()=>{
            var before=new WorldSnapshot{Stage=SliceStage.InvitationPending,InvitationState=new(){CarArrived=true}};
            var s=QuestReducer.Apply(before,new("invitation.meeting_complete","arrived","invitation-1")).Next;
            Check.Equal(1,s.CandyCount);Check.Equal(SliceStage.CandyHeyPending,s.Stage);Check.Equal(0,before.CompletedActions.Count);
            Check.True(!QuestReducer.Apply(s,new("invitation.meeting_complete","arrived","invitation-1")).Applied);
            s=QuestReducer.Apply(s,new("candy.hey.delivered","delivered","hey-1")).Next;
            Check.Equal(0,s.CandyCount);Check.Equal(SliceStage.CandyHeyDelivered,s.Stage);
            Check.True(!QuestReducer.Apply(s,new("candy.hey.delivered","delivered","hey-1")).Applied);
        }));
        tests.Add(("Dialogue","GuardTypingAndOneStep",()=>{
            var c=new ContentCatalog{Dialogues=new Dictionary<string,DialogueNode>{{"test",new("朋友",new[]{"你好啊","第二句"})}}};
            var s=new DialogueSession();Check.True(s.Start("test",c).Success);
            Check.True(!s.Advance(.1).Applied);Check.True(s.Advance(.15).Applied);Check.Equal("你好啊",s.Text);
            Check.True(!s.Advance(.16).Applied);Check.True(s.Advance(.31).Applied);Check.Equal("",s.Text);
            s.Tick(1,0);Check.Equal("第二句",s.Text);Check.True(s.Advance(.5).Finished);
        }));
        tests.Add(("Dialogue","MissingNodeNeverStarts",()=>{
            var s=new DialogueSession();Check.True(!s.Start("missing",new()).Success);Check.True(!s.Advance(1).Applied);
        }));
        tests.Add(("Dialogue","NullNodeIsRecoverable",()=>{
            var s=new DialogueSession();var c=new ContentCatalog{Dialogues=new Dictionary<string,DialogueNode>{{"bad",null!}}};
            Check.True(!s.Start("bad",c).Success);Check.True(!s.Advance(1).Applied);
        }));
    }
}
