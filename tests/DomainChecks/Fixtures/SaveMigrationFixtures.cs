using GeXingzhou.Domain;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace GeXingzhou.DomainChecks;
public static class SaveMigrationFixtures
{
    public static NavigationCatalog Navigation()=>new(new Dictionary<string,NavigationProfile>{
        ["soup_shop"]=NavigationFixtures.Soup(),
        ["community_gate"]=Horizontal("community_gate",1600),["convenience_street"]=Horizontal("convenience_street",1280)});
    private static NavigationProfile Horizontal(string id,float width)=>new(id,WorldMode.Horizontal,width,358,8,Array.Empty<Position2>(),Array.Empty<Position2[]>(),
        new Dictionary<string,Position2>{["entry"]=new(120,280),["exit"]=new(48,280),["safe"]=new(120,280)});
    public static WorldSnapshot LegacyMemory(FoodChoice? food,bool replay)
    {
        var context=new SceneReturnContext("soup_shop",new(440,280),"soup.return",true);
        var snapshot=new WorldSnapshot{SceneId="memory_soup_table",Stage=replay?SliceStage.SliceComplete:SliceStage.MemoryActive,
            PlayerPosition=new(320,280),ReturnContext=context,MemoryOrdinal=1,MemoryVisitOrdinal=1,
            CompletedActions=new(){"soup.meet:soup-seat-1","soup.response:soup-response-1","memory.soup.enter:soup-1"},
            ChoiceCodes=new(){["soup-seat-1"]="sit",["soup-response-1"]="eat",["soup-1"]="enter"},
            SceneActiveMilliseconds=new(){["soup_shop"]=1234,["memory_soup_table"]=450},Settings=new(){SubtitleSize=32,MusicVolume=.2}};
        if(replay){snapshot=snapshot with {MemoryOrdinal=1,CompletedActions=new(snapshot.CompletedActions){"slice.complete:slice-1"},ChoiceCodes=new(snapshot.ChoiceCodes){["slice-1"]="completed"}};}
        var memory=MemorySession.Begin(snapshot,context,replay);
        memory=MemorySession.PushCoin(memory,"c4");
        if(food!=null){foreach(var coin in new[]{"c1","c2","c3"})memory=MemorySession.PushCoin(memory,coin);memory=MemorySession.ResolveFood(memory,food.Value);}
        return snapshot with {MemoryState=memory,MemoryOrdinal=int.Parse(memory.InstanceId[5..])};
    }
    public static string DirectoryFor(string name){var path=Path.GetFullPath(Path.Combine("test-output","migration",name+"-"+Guid.NewGuid()));Directory.CreateDirectory(path);return path;}
    public static string Json(WorldSnapshot snapshot)=>JsonSerializer.Serialize(snapshot,new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,Converters={new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,false)}});
    public static SaveV2Codec Codec()=>new(Navigation());
    public static WorldSnapshot Depth()=>SaveV2Codec.CreateNew(new()) with {SceneId="soup_shop",PlayerPosition=new(400,430)};
}
