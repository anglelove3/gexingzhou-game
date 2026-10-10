using System.Text.Json;
using System.Text.Json.Serialization;
namespace GeXingzhou.Domain;
public interface ISaveCodec
{
    LoadResult Read(string json);
    string? Validate(WorldSnapshot snapshot);
}
public sealed class LegacySaveCodec : ISaveCodec
{
    public LoadResult Read(string json)=>SaveJsonReader.Read(json,1,"vs01-0.1",Validate);
    public string? Validate(WorldSnapshot snapshot)=>SaveRepository.Validate(snapshot);
}
public sealed class SaveV2Codec : ISaveCodec
{
    private readonly NavigationCatalog navigation;
    public SaveV2Codec(NavigationCatalog navigation)=>this.navigation=navigation??throw new ArgumentNullException(nameof(navigation));
    public LoadResult Read(string json)
    {
        // Only a structurally valid snapshot on the ground may be relocated.
        var parsed=SaveJsonReader.Read(json,2,"vs01-0.2",s=>ValidateCore(s,true));
        if(parsed.Status!=LoadStatus.Loaded)return parsed;
        var s=parsed.Snapshot!;var moved=false;
        if(s.SceneId!="memory_soup_table"&&!NavigationGeometry.CanStand(navigation.Profiles[s.SceneId],s.PlayerPosition)){
            s=s with {PlayerPosition=navigation.Profiles[s.SceneId].Anchors["safe"]};moved=true;
        }
        if(s.ReturnContext is {} context&&!NavigationGeometry.CanStand(navigation.Profiles["soup_shop"],context.Position)){
            s=s with {ReturnContext=context with {Position=navigation.Profiles["soup_shop"].Anchors["safe"]}};moved=true;
        }
        return new(LoadStatus.Loaded,s,moved?"家具占位已调整，角色回到同场景安全位置。":"已读取存档。");
    }
    public string? Validate(WorldSnapshot snapshot)=>ValidateCore(snapshot,false);
    private string? ValidateCore(WorldSnapshot s,bool allowBlocked)
    {
        if(s==null)return "空存档。";
        if(s.SchemaVersion!=2||s.ContentVersion!="vs01-0.2")return "不支持的存档版本。";
        var storyError=SaveRepository.ValidateStory(s);if(storyError!=null)return storyError;
        if(s.SceneId=="memory_soup_table"){
            if(!float.IsFinite(s.PlayerPosition.X)||s.PlayerPosition.X<8||s.PlayerPosition.X>632||s.PlayerPosition.Y!=280)return "回忆虚拟坐标异常。";
        }else if(!ValidPosition(s.SceneId,s.PlayerPosition,allowBlocked))return "存档位置不在可行走地面。";
        if(s.ReturnContext is {} context&&!ValidPosition("soup_shop",context.Position,allowBlocked))return "回忆返回位置不在可行走地面。";
        return null;
    }
    private bool ValidPosition(string scene,Position2 position,bool allowBlocked)
    {
        if(!navigation.Profiles.TryGetValue(scene,out var profile))return false;
        return NavigationGeometry.CanStand(profile,position)||allowBlocked&&profile.Mode==WorldMode.Depth2D&&NavigationGeometry.InsideGround(profile,position);
    }
    public static WorldSnapshot CreateNew(GameSettings settings)=>new(){SchemaVersion=2,ContentVersion="vs01-0.2",Settings=settings.Normalize()};
}
internal static class SaveJsonReader
{
    private static readonly JsonSerializerOptions Options=new(){PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,Converters={new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,false)}};
    public static LoadResult Read(string text,int schemaVersion,string contentVersion,Func<WorldSnapshot,string?> validate)
    {
        try{
            using var json=JsonDocument.Parse(text);var root=json.RootElement;
            if(root.ValueKind!=JsonValueKind.Object||!root.TryGetProperty("schema_version",out var schema)||!schema.TryGetInt32(out var version)||!root.TryGetProperty("content_version",out var content))return new(LoadStatus.Corrupt,null,"存档缺少版本字段，原档已保留。");
            if(content.ValueKind!=JsonValueKind.String)return new(LoadStatus.Corrupt,null,"存档版本类型错误。");
            if(version!=schemaVersion||content.GetString()!=contentVersion)return new(LoadStatus.UnsupportedVersion,null,"不支持的存档版本，原档已保留。");
            if(!root.TryGetProperty("scene_id",out _)||!root.TryGetProperty("stage",out _))return new(LoadStatus.Corrupt,null,"存档缺少场景或阶段。");
            var snapshot=JsonSerializer.Deserialize<WorldSnapshot>(text,Options);var error=snapshot==null?"空存档。":validate(snapshot);
            return error==null?new(LoadStatus.Loaded,snapshot,"已读取存档。"):new(LoadStatus.Corrupt,null,error);
        }
        catch(JsonException){return new(LoadStatus.Corrupt,null,"存档无法解析，原档已保留。");}
        catch(InvalidOperationException){return new(LoadStatus.Corrupt,null,"存档字段类型错误。");}
    }
}
internal static class SaveIdPolicy
{
    internal static bool MemoryId(string? id,int ordinal)=>id!=null&&id.StartsWith("soup-",StringComparison.Ordinal)&&int.TryParse(id[5..],out var n)&&n>0&&n<=ordinal&&id=="soup-"+n;
    private static bool CoinId(string id,int ordinal){var parts=id.Split(':');return parts.Length==2&&MemoryId(parts[0],ordinal)&&parts[1] is "c1" or "c2" or "c3" or "c4";}
    internal static string? Validate(WorldSnapshot s,int discoverySchemaVersion=2)
    {
        if(s.DiscoveredIds==null||s.DiscoveredIds.Count>(discoverySchemaVersion==3?6:3)||s.DiscoveredIds.Any(id=>
            id is not ("soup.sign" or "soup.menu" or "soup.note")&&
            !(discoverySchemaVersion==3&&id is "community.sign" or "community.notice" or "community.planter")))return "未知发现记录。";
        if(s.PhoneState is not ("open" or "closed")||s.TimeBlock!="arrival"||s.SceneActiveMilliseconds.Any(p=>p.Key is not ("community_gate" or "convenience_street" or "soup_shop" or "memory_soup_table")||p.Value<0))return "状态或场景计时异常。";
        if(s.CompletedActions.Any(id=>!Action(id,s.MemoryOrdinal))||s.ChoiceCodes.Any(p=>!Choice(p.Key,p.Value,s.MemoryOrdinal)))return "未知剧情事实或选项。";
        return null;
    }
    private static bool Action(string? id,int ordinal)
    {
        if(id=="observation.community.first")return true;
        if(id==null)return false;var split=id.IndexOf(':');if(split<0)return false;var action=id[..split];var opportunity=id[(split+1)..];
        return action switch{
            "invitation.answer" or "invitation.meeting_complete"=>opportunity=="invitation-1",
            "candy.hey.delivered"=>opportunity=="hey-1","soup.meet"=>opportunity=="soup-seat-1","soup.response"=>opportunity=="soup-response-1",
            "soup.payment"=>opportunity=="soup-payment-1","slice.complete"=>opportunity=="slice-1",
            "memory.soup.enter" or "memory.food.resolve" or "memory.return"=>MemoryId(opportunity,ordinal),"memory.coin.push"=>CoinId(opportunity,ordinal),_=>false};
    }
    private static bool Choice(string key,string? code,int ordinal)=>key switch{
        "invitation-1"=>code is "answered" or "meeting","hey-1"=>code=="delivered","soup-seat-1"=>code=="sit",
        "soup-response-1"=>code is "eat" or "set_chopsticks" or "check_phone","soup-payment-1"=>code=="zhang_pays","slice-1"=>code=="completed",
        _=>MemoryId(key,ordinal)?code is "enter" or "return" or "Take" or "Wait" or "Share" or "take" or "wait" or "share":
            CoinId(key,ordinal)&&code==key[(key.LastIndexOf(':')+1)..]};
}
