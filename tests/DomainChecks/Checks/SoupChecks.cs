using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class SoupChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        tests.Add(("Soup","RejectsEarlyMeeting",()=>Check.True(!QuestReducer.Apply(new(){Stage=SliceStage.CandyHeyPending,CandyCount=1},new("soup.meet","sit","soup-seat-1")).Applied)));
        foreach(var choice in new[]{"eat","set_chopsticks","check_phone"})tests.Add(("Soup","Merges_"+choice,()=>{
            var before=new WorldSnapshot{Stage=SliceStage.CandyHeyDelivered};
            var seat=QuestReducer.Apply(before,new("soup.meet","sit","soup-seat-1"));Check.True(seat.Applied);Check.Equal(SliceStage.SoupMeet,seat.Next.Stage);
            var r=QuestReducer.Apply(seat.Next,new("soup.response",choice,"soup-response-1"));Check.True(r.Applied);Check.Equal(choice,r.Next.ChoiceCodes["soup-response-1"]);
            Check.True(!QuestReducer.Apply(r.Next,new("soup.response","eat","soup-response-1")).Applied);
            Check.Equal(0,before.CompletedActions.Count);Check.Equal(0,before.ChoiceCodes.Count);Check.Equal(1,r.Next.GameDay);
        }));
    }
}
