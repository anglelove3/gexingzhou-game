using System.Text.Json;
namespace GeXingzhou.Domain;
public sealed class SaveV3Codec : ISaveCodec
{
    private readonly NavigationCatalog navigation;
    private static readonly HashSet<string> RootFields=new(StringComparer.Ordinal){
        "schema_version","content_version","scene_id","player_position","stage","game_day","time_block","candy_count",
        "completed_actions","discovered_ids","phone_state","invitation_state","choice_codes","memory_state","return_context",
        "memory_ordinal","memory_visit_ordinal","playthrough_id","scene_active_milliseconds","settings"};
    public SaveV3Codec(NavigationCatalog navigation)=>this.navigation=navigation??throw new ArgumentNullException(nameof(navigation));
    public LoadResult Read(string json)
    {
        // Check the version before interpreting a future protocol's fields; inspect raw arrays before HashSet erases duplicates.
        try {
            using var document=JsonDocument.Parse(json);var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object||!root.TryGetProperty("schema_version",out var schema)||!schema.TryGetInt32(out var version)||
                !root.TryGetProperty("content_version",out var content)||content.ValueKind!=JsonValueKind.String)return Corrupt("存档版本字段缺失或类型错误。");
            if(version!=3||content.GetString()!="vs01-0.3")return new(LoadStatus.UnsupportedVersion,null,"不支持的存档版本，原档已保留。");
            var names=new HashSet<string>(StringComparer.Ordinal);
            foreach(var field in root.EnumerateObject())if(!RootFields.Contains(field.Name)||!names.Add(field.Name))return Corrupt("存档含未知或重复字段。");
            if(root.TryGetProperty("discovered_ids",out var discovered)){
                if(discovered.ValueKind!=JsonValueKind.Array)return Corrupt("发现记录类型错误。");
                var ids=new HashSet<string>(StringComparer.Ordinal);
                foreach(var id in discovered.EnumerateArray())if(id.ValueKind!=JsonValueKind.String||!ids.Add(id.GetString()!))return Corrupt("发现记录包含重复或错误的ID。");
            }
        }catch(JsonException){return Corrupt("存档无法解析，原档已保留。");}
        catch(InvalidOperationException){return Corrupt("存档字段类型错误。");}
        var parsed=SaveJsonReader.Read(json,3,"vs01-0.3",s=>ValidateCore(s,true));
        if(parsed.Status!=LoadStatus.Loaded)return parsed;
        var snapshot=parsed.Snapshot!;var moved=false;
        if(snapshot.SceneId!="memory_soup_table"&&!NavigationGeometry.CanStand(navigation.Profiles[snapshot.SceneId],snapshot.PlayerPosition)){
            snapshot=snapshot with{PlayerPosition=navigation.Profiles[snapshot.SceneId].Anchors["safe"]};moved=true;
        }
        if(snapshot.ReturnContext is {} context&&!NavigationGeometry.CanStand(navigation.Profiles["soup_shop"],context.Position)){
            snapshot=snapshot with{ReturnContext=context with{Position=navigation.Profiles["soup_shop"].Anchors["safe"]}};moved=true;
        }
        return new(LoadStatus.Loaded,snapshot,moved?"家具占位已调整，角色回到同场景安全位置。":"已读取存档。");
    }
    public string? Validate(WorldSnapshot s)=>ValidateCore(s,false);
    private string? ValidateCore(WorldSnapshot s,bool allowBlocked)
    {
        if(s==null)return "空存档。";
        if(s.SchemaVersion!=3||s.ContentVersion!="vs01-0.3")return "不支持的存档版本。";
        var error=SaveRepository.ValidateStory(s,3);if(error!=null)return error;
        if(s.SceneId=="memory_soup_table"){
            if(!float.IsFinite(s.PlayerPosition.X)||s.PlayerPosition.X<8||s.PlayerPosition.X>632||s.PlayerPosition.Y!=280)return "回忆虚拟坐标异常。";
        }else if(!ValidPosition(s.SceneId,s.PlayerPosition,allowBlocked))return "存档位置不在可行走地面。";
        if(s.ReturnContext is {} context&&!ValidPosition("soup_shop",context.Position,allowBlocked))return "回忆返回位置不在可行走地面。";
        return null;
    }
    private bool ValidPosition(string scene,Position2 position,bool allowBlocked)=>navigation.Profiles.TryGetValue(scene,out var p)&&
        (NavigationGeometry.CanStand(p,position)||allowBlocked&&p.Mode==WorldMode.Depth2D&&NavigationGeometry.InsideGround(p,position));
    private static LoadResult Corrupt(string message)=>new(LoadStatus.Corrupt,null,message);
    public static WorldSnapshot CreateNew(GameSettings settings,NavigationCatalog navigation)=>new(){SchemaVersion=3,ContentVersion="vs01-0.3",Settings=settings.Normalize(),PlayerPosition=navigation.Profiles["community_gate"].Anchors["safe"]};
}
