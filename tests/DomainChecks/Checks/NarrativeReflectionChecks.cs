using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class NarrativeReflectionChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        // Break: a completed title without its read-only context loses the approved journal reflection.
        tests.Add(("NarrativeReflection","HistoryHasContextOnlyForCompletedFacts",()=>{
            var s=StreetFixtures.V4() with{Stage=SliceStage.SliceComplete,CompletedActions=new(){"candy.hey.delivered:hey-1","soup.meet:soup-seat-1"}};
            var before=StreetFixtures.Json(s);var view=JournalProjection.Build(s);
            Check.Equal("hey,soup",string.Join(',',view.History.Select(x=>x.Id)));
            Check.True(view.History.All(x=>x.Body.Length>0));Check.True(view.History[0].Body.Contains("喜糖"));Check.True(view.History[1].Body.Contains("坐"));
            Check.Equal(before,StreetFixtures.Json(s));
            Check.Equal(0,JournalProjection.Build(s with{CompletedActions=new()}).History.Count);
        }));
        // Break: reading a replay's live choice or stage alone invents/rewrites first-memory history.
        tests.Add(("NarrativeReflection","ReplayAndPartialMemoryStayFactBased",()=>{
            var first=StreetFixtures.V4() with{CompletedActions=new(){"memory.return:soup-1","memory.food.resolve:soup-1"},MemoryState=new(){InstanceId="soup-2",Replay=true,FoodChoice=FoodChoice.Share}};
            var before=JournalProjection.Build(first).History.Single().Body;Check.True(before.Length>0);
            Check.Equal(before,JournalProjection.Build(first with{MemoryState=new(){InstanceId="soup-3",Replay=true,FoodChoice=FoodChoice.Wait}}).History.Single().Body);
            Check.Equal(0,JournalProjection.Build(first with{CompletedActions=new(){"memory.return:soup-1"}}).History.Count);
        }));
    }
}
