using System.Collections.ObjectModel;
using System.Text.Json;
namespace GeXingzhou.Domain;
public enum WorldMode { Horizontal, Depth2D }
public sealed record NavigationProfile(string SceneId,WorldMode Mode,float Width,float Height,float FootRadius,Position2[] Ground,Position2[][] Obstacles,IReadOnlyDictionary<string,Position2> Anchors);
public sealed class NavigationCatalog
{
    public IReadOnlyDictionary<string,NavigationProfile> Profiles {get;}
    public NavigationCatalog(IReadOnlyDictionary<string,NavigationProfile> profiles)
    {
        if(profiles==null||profiles.Count is <1 or >3)throw new ArgumentException("Missing navigation profiles");
        foreach(var pair in profiles){
            if(pair.Value==null||pair.Key!=pair.Value.SceneId)throw new ArgumentException("Navigation scene key mismatch");
            ValidateProfile(pair.Value);
        }
        Profiles=new ReadOnlyDictionary<string,NavigationProfile>(profiles.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal));
    }
    public static NavigationCatalog Parse(string json)
    {
        try{
            using var document=JsonDocument.Parse(json);
            var root=document.RootElement;Object(root,"schema_version","profiles");
            if(root.GetProperty("schema_version").GetInt32()!=1)throw new ArgumentException("Unsupported navigation schema");
            var profiles=new Dictionary<string,NavigationProfile>(StringComparer.Ordinal);
            var items=root.GetProperty("profiles");
            if(items.ValueKind!=JsonValueKind.Array||items.GetArrayLength() is <1 or >3)throw new ArgumentException("Invalid navigation profiles");
            foreach(var item in items.EnumerateArray()){
                Object(item,"scene_id","mode","width","height","foot_radius","ground","obstacles","anchors");
                var id=item.GetProperty("scene_id").GetString()??"";
                var mode=item.GetProperty("mode").GetString() switch{"horizontal"=>WorldMode.Horizontal,"depth2d"=>WorldMode.Depth2D,_=>throw new ArgumentException("Unknown navigation mode")};
                var ground=Polygon(item.GetProperty("ground"));
                var obstacleItems=item.GetProperty("obstacles");
                if(obstacleItems.ValueKind!=JsonValueKind.Array||obstacleItems.GetArrayLength()>64)throw new ArgumentException("Invalid obstacle list");
                var obstacles=obstacleItems.EnumerateArray().Select(Polygon).ToArray();
                var anchors=new Dictionary<string,Position2>(StringComparer.Ordinal);
                var anchorObject=item.GetProperty("anchors");
                Object(anchorObject,"entry","exit","stand","memory_return","safe","seat");
                foreach(var anchor in anchorObject.EnumerateObject())anchors.Add(anchor.Name,Point(anchor.Value));
                var profile=new NavigationProfile(id,mode,item.GetProperty("width").GetSingle(),item.GetProperty("height").GetSingle(),item.GetProperty("foot_radius").GetSingle(),ground,obstacles,anchors);
                if(!profiles.TryAdd(id,profile))throw new ArgumentException("Duplicate navigation scene");
            }
            return new(profiles);
        }
        catch(Exception ex) when(ex is JsonException or InvalidOperationException or KeyNotFoundException or OverflowException or FormatException){throw new ArgumentException("Invalid navigation data",ex);}
    }
    private static void Object(JsonElement element,params string[] allowed)
    {
        if(element.ValueKind!=JsonValueKind.Object)throw new ArgumentException("Expected navigation object");
        var names=new HashSet<string>(StringComparer.Ordinal);
        foreach(var property in element.EnumerateObject())
            if(!allowed.Contains(property.Name,StringComparer.Ordinal)||!names.Add(property.Name))throw new ArgumentException("Unknown or duplicate navigation field: "+property.Name);
    }
    private static Position2 Point(JsonElement element){Object(element,"x","y");return new(element.GetProperty("x").GetSingle(),element.GetProperty("y").GetSingle());}
    private static Position2[] Polygon(JsonElement element)
    {
        if(element.ValueKind!=JsonValueKind.Array||element.GetArrayLength()>128)throw new ArgumentException("Invalid polygon");
        return element.EnumerateArray().Select(Point).ToArray();
    }
    private static void ValidateProfile(NavigationProfile p)
    {
        if(p.SceneId is not ("community_gate" or "convenience_street" or "soup_shop")||!Enum.IsDefined(p.Mode)||
            !float.IsFinite(p.Width)||!float.IsFinite(p.Height)||p.Width<=16||p.Height<280||p.Width>100000||p.Height>100000||
            !float.IsFinite(p.FootRadius)||p.FootRadius<=0||p.FootRadius>=Math.Min(p.Width,p.Height)/2||p.Ground==null||p.Obstacles==null||p.Obstacles.Length>64||p.Anchors==null)
            throw new ArgumentException("Invalid navigation profile dimensions or scene");
        if(p.Mode==WorldMode.Depth2D&&(!NavigationGeometry.IsSimplePolygon(p.Ground,p.Width,p.Height)||p.Obstacles.Any(o=>!NavigationGeometry.IsSimplePolygon(o,p.Width,p.Height))))
            throw new ArgumentException("Navigation polygon is empty, outside bounds or self-intersecting");
        if(p.Mode==WorldMode.Horizontal&&(p.Ground.Length!=0||p.Obstacles.Length!=0))throw new ArgumentException("Horizontal profile must retain legacy floor rules");
        var required=p.SceneId=="soup_shop"&&p.Mode==WorldMode.Depth2D?new[]{"entry","exit","stand","memory_return","safe","seat"}:new[]{"entry","exit","safe"};
        if(required.Any(key=>!p.Anchors.ContainsKey(key)))throw new ArgumentException("Missing navigation anchor");
        foreach(var (key,value) in p.Anchors){
            if(key is not ("entry" or "exit" or "stand" or "memory_return" or "safe" or "seat")||!float.IsFinite(value.X)||!float.IsFinite(value.Y)||value.X<0||value.X>p.Width||value.Y<0||value.Y>p.Height)
                throw new ArgumentException("Invalid navigation anchor: "+key);
            if(key!="seat"&&!NavigationGeometry.CanStand(p,value))throw new ArgumentException("Blocked navigation anchor: "+key);
        }
    }
}
