using System.Text.Json;
using System.Text.Json.Serialization;
namespace GeXingzhou.Domain;
public enum LoadStatus{Loaded,NotFound,Corrupt,UnsupportedVersion,IoError}
public sealed record SaveResult(bool Success,string Message);
public sealed record LoadResult(LoadStatus Status,WorldSnapshot? Snapshot,string Message);
public sealed class SaveRepository
{
    private readonly string directory,path,backup;private bool replacePreserved;
    private static readonly JsonSerializerOptions Options=new(){PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,WriteIndented=true,Converters={new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,false)}};
    public SaveRepository(string directory,string slot="auto")
    {
        this.directory=Path.GetFullPath(directory);var name=slot=="manual"?"manual":"save";path=Path.Combine(this.directory,name+".json");backup=Path.Combine(this.directory,name+".bak.json");
    }
    public SaveResult Save(WorldSnapshot snapshot)
    {
        string? temporary=null;
        try
        {
            var error=Validate(snapshot);if(error!=null)return new(false,error);
            Directory.CreateDirectory(directory);var current=Load();
            if(current.Status is not (LoadStatus.Loaded or LoadStatus.NotFound)&&!replacePreserved)return new(false,"原档损坏或版本未知；请先保留副本再新建/恢复。");
            temporary=Path.Combine(directory,"write-"+Guid.NewGuid()+".tmp");File.WriteAllText(temporary,JsonSerializer.Serialize(snapshot,Options));
            if(Read(temporary).Status!=LoadStatus.Loaded)return new(false,"临时存档校验失败。");
            if(current.Status==LoadStatus.Loaded)File.Replace(temporary,path,backup,true);else File.Move(temporary,path,true);
            temporary=null;replacePreserved=false;return new(true,"已保存。");
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException){return new(false,"本次未能保存，可继续游戏或重试。"+ex.GetType().Name);}
        finally{if(temporary!=null&&File.Exists(temporary))try{File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    public LoadResult Load()=>Read(path);
    public LoadResult LoadBackup()=>Read(backup);
    public SaveResult PreserveForNewGame()
    {
        try{Directory.CreateDirectory(directory);foreach(var source in new[]{path,backup})if(File.Exists(source))File.Copy(source,Path.Combine(directory,"preserved-"+Guid.NewGuid()+".json"));replacePreserved=true;return new(true,"原档副本已保留。");}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return new(false,"未能保留旧档："+ex.GetType().Name);}
    }
    private static LoadResult Read(string source)
    {
        try
        {
            if(!File.Exists(source))return new(LoadStatus.NotFound,null,"暂无存档。");
            var text=File.ReadAllText(source);using var json=JsonDocument.Parse(text);var root=json.RootElement;
            if(!root.TryGetProperty("schema_version",out var schema)||!schema.TryGetInt32(out var version)||!root.TryGetProperty("content_version",out var content))return new(LoadStatus.Corrupt,null,"存档缺少版本字段，原档已保留。");
            if(version!=1||content.GetString()!="vs01-0.1")return new(LoadStatus.UnsupportedVersion,null,"不支持的存档版本，原档已保留。");
            if(!root.TryGetProperty("scene_id",out _)||!root.TryGetProperty("stage",out _))return new(LoadStatus.Corrupt,null,"存档缺少场景或阶段。");
            var snapshot=JsonSerializer.Deserialize<WorldSnapshot>(text,Options);var error=snapshot==null?"空存档":Validate(snapshot);
            return error==null?new(LoadStatus.Loaded,snapshot,"已读取存档。"):new(LoadStatus.Corrupt,null,error);
        }
        catch(JsonException){return new(LoadStatus.Corrupt,null,"存档无法解析，原档已保留。");}
        catch(InvalidOperationException){return new(LoadStatus.Corrupt,null,"存档字段类型错误。");}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return new(LoadStatus.IoError,null,"无法读取存档："+ex.GetType().Name);}
    }
    public static string? Validate(WorldSnapshot s)
    {
        if(s.SchemaVersion!=1||s.ContentVersion!="vs01-0.1")return "不支持的存档版本。";
        if(s.SceneId is not ("community_gate" or "convenience_street" or "soup_shop" or "memory_soup_table")||!Enum.IsDefined(s.Stage)||s.CompletedActions==null||s.ChoiceCodes==null||s.InvitationState==null||s.Settings==null||s.SceneActiveMilliseconds==null||string.IsNullOrWhiteSpace(s.PlaythroughId))return "存档内容不完整。";
        if(!float.IsFinite(s.PlayerPosition.X)||!float.IsFinite(s.PlayerPosition.Y)||s.PlayerPosition.X<0||s.PlayerPosition.X>1600||s.PlayerPosition.Y!=280||s.GameDay!=1||s.CandyCount is <0 or >1)return "存档数值异常。";
        if(s.Stage==SliceStage.CandyHeyPending&&s.CandyCount!=1||s.Stage>SliceStage.CandyHeyPending&&s.CandyCount!=0)return "喜糖与任务阶段不一致。";
        if(!double.IsFinite(s.InvitationState.Elapsed)||s.InvitationState.Elapsed<0)return "邀请计时异常。";
        if(s.MemoryState is {} m)
        {
            if(m.PushedCoinIds==null||m.PushedCoinIds.Any(id=>id is not ("c1" or "c2" or "c3" or "c4"))||m.PushedTotal!=m.PushedCoinIds.Sum(id=>id=="c4"?2:1)||m.Completed!=(m.FoodChoice!=null)||m.Completed&&m.PushedTotal!=5||m.FoodChoice!=null&&!Enum.IsDefined(m.FoodChoice.Value))return "回忆进度异常。";
            if(m.InstanceId==null||!m.InstanceId.StartsWith("soup-")||!int.TryParse(m.InstanceId[5..],out var ordinal)||ordinal<1||s.MemoryOrdinal!=ordinal)return "回忆实例异常。";
        }
        if(s.SceneId=="memory_soup_table"||s.Stage is SliceStage.MemoryActive or SliceStage.MemoryReturned)
        {if(s.MemoryState==null||s.ReturnContext is not {SceneId:"soup_shop",IsAdult:true,DialogueNodeId:"soup.return"} c||!float.IsFinite(c.Position.X)||c.Position.X<0||c.Position.X>960||c.Position.Y!=280)return "回忆返回上下文缺失。";}
        return null;
    }
}
public static class ResumePolicy
{
    public static WorldSnapshot Normalize(WorldSnapshot snapshot)=>snapshot with {PhoneState="closed"};
}
