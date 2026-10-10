using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class CommunityFixtures
{
    public static NavigationCatalog FrozenV2()=>NavigationCatalog.Parse(File.ReadAllText("content/vs01/compat/navigation-v2.json"));
    public static NavigationCatalog Current()
    {
        var profiles=FrozenV2().Profiles.ToDictionary(p=>p.Key,p=>p.Value);
        profiles["community_gate"]=new("community_gate",WorldMode.Depth2D,1600,540,8,
            Rect(24,300,1576,520),new[]{Rect(240,340,330,400),Rect(1080,340,1150,412),Rect(870,340,1050,414),Rect(530,386,548,414),Rect(660,386,678,414)},
            new Dictionary<string,Position2>{["entry"]=new(120,460),["exit"]=new(1480,460),["safe"]=new(320,460),["stand"]=new(960,440),["seat"]=new(960,380)});
        return new(profiles);
    }
    private static Position2[] Rect(float x,float y,float right,float bottom)=>new[]{new Position2(x,y),new(right,y),new(right,bottom),new(x,bottom)};
    public static WorldSnapshot V3()=>new(){SchemaVersion=3,ContentVersion="vs01-0.3",PlayerPosition=new(320,460)};
    public static string Json(WorldSnapshot s)=>SaveMigrationFixtures.Json(s);
    public static string DirectoryFor(string name)=>SaveMigrationFixtures.DirectoryFor("community-"+name);
}
