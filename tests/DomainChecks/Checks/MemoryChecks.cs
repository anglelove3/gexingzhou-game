using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class MemoryChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        tests.Add(("Return","InterruptedReplayKeepsInstance",()=>{
            var old=MemorySession.PushCoin(new(){InstanceId="soup-2",Replay=true},"c1");
            var resumed=MemorySession.Begin(new(){Stage=SliceStage.SliceComplete,MemoryState=old,MemoryOrdinal=2},new("soup_shop",new(440,280),"soup.return",true),true);
            Check.Equal("soup-2",resumed.InstanceId);Check.Equal(1,resumed.PushedTotal);Check.True(resumed.Replay);
        }));
        tests.Add(("Memory","AllCoinPermutationsAndDuplicates",()=>{
            foreach(var order in Permutations(new[]{"c1","c2","c3","c4"}))
            {var m=new MemoryState();foreach(var id in order){m=MemorySession.PushCoin(m,id);m=MemorySession.PushCoin(m,id);}Check.Equal(5,m.PushedTotal);Check.Equal(4,m.PushedCoinIds.Count);}
        }));
        tests.Add(("Memory","NoEarlyFoodOrUnknownCoin",()=>{
            var m=MemorySession.PushCoin(new(),"bad");Check.Equal(0,m.PushedTotal);Check.True(!MemorySession.ResolveFood(m,FoodChoice.Wait).Completed);
        }));
        foreach(var food in Enum.GetValues<FoodChoice>())tests.Add(("Memory","Food_"+food,()=>{
            var m=new MemoryState();foreach(var id in new[]{"c4","c2","c1","c3"})m=MemorySession.PushCoin(m,id);
            var done=MemorySession.ResolveFood(m,food);Check.True(done.Completed);Check.Equal(food,done.FoodChoice!.Value);
            Check.Equal(food,MemorySession.ResolveFood(done,FoodChoice.Share).FoodChoice!.Value);Check.True(!m.Completed);
        }));
        tests.Add(("Return","ResumeSameInstanceReplayNew",()=>{
            var context=new SceneReturnContext("soup_shop",new(440,280),"soup.return",true);
            var first=MemorySession.Begin(new(){Stage=SliceStage.SoupMeet},context,false);first=MemorySession.PushCoin(first,"c1");
            var resumed=MemorySession.Begin(new(){Stage=SliceStage.MemoryActive,MemoryState=first,MemoryOrdinal=1},context,false);
            Check.Equal("soup-1",resumed.InstanceId);Check.Equal(1,resumed.PushedTotal);
            var replay=MemorySession.Begin(new(){Stage=SliceStage.SliceComplete,MemoryState=first with{Completed=true},MemoryOrdinal=1},context,true);
            Check.Equal("soup-2",replay.InstanceId);Check.Equal(0,replay.PushedTotal);Check.True(replay.Replay);
            Check.Equal("soup_shop",context.SceneId);Check.Equal(440f,context.Position.X);Check.Equal("soup.return",context.DialogueNodeId);Check.True(context.IsAdult);
        }));
    }
    private static IEnumerable<string[]> Permutations(string[] remaining)
    {
        if(remaining.Length==0){yield return Array.Empty<string>();yield break;}
        for(int i=0;i<remaining.Length;i++)foreach(var tail in Permutations(remaining.Where((_,j)=>j!=i).ToArray()))yield return new[]{remaining[i]}.Concat(tail).ToArray();
    }
}
