using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class StreetFixtures
{
    public static NavigationCatalog FrozenV3()=>NavigationCatalog.Parse(File.ReadAllText("content/vs01/compat/navigation-v3.json"));
    public static NavigationCatalog Current()
    {
        var profiles=FrozenV3().Profiles.ToDictionary(p=>p.Key,p=>p.Value);
        profiles["convenience_street"]=new("convenience_street",WorldMode.Depth2D,1600,720,8,
            Rect(24,180,1576,688),new[]{Rect(24,180,340,440),Rect(450,300,1150,440),Rect(1260,180,1576,360),Rect(24,580,580,688),Rect(680,570,940,620),Rect(1040,580,1280,688),Rect(1340,580,1576,688)},
            new Dictionary<string,Position2>{["entry"]=new(100,520),["exit"]=new(1500,520),["safe"]=new(240,520)});
        return new(profiles);
    }
    public static NavigationCatalog CurrentWithStreetBlock(float x)
    {
        var catalog=Current();var profiles=catalog.Profiles.ToDictionary(p=>p.Key,p=>p.Value);var street=profiles["convenience_street"];
        profiles["convenience_street"]=street with{Obstacles=street.Obstacles.Append(Rect(x-12,500,x+12,540)).ToArray()};
        return new(profiles);
    }
    public static WorldSnapshot V4()=>new(){SchemaVersion=4,ContentVersion="vs01-0.4",PlayerPosition=new(320,460)};
    public static string Json(WorldSnapshot s)=>SaveMigrationFixtures.Json(s);
    public static string DirectoryFor(string name)=>SaveMigrationFixtures.DirectoryFor("street-"+name);
    private static Position2[] Rect(float x,float y,float right,float bottom)=>new[]{new Position2(x,y),new(right,y),new(right,bottom),new(x,bottom)};
}
