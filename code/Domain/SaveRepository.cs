using System.Text.Json;
using System.Text.Json.Serialization;
namespace GeXingzhou.Domain;
public enum LoadStatus{Loaded,NotFound,Corrupt,UnsupportedVersion,IoError}
public sealed record SaveResult(bool Success,string Message);
public sealed record LoadResult(LoadStatus Status,WorldSnapshot? Snapshot,string Message);
public sealed class SaveRepository
{
    private readonly string directory,path,backup;private readonly ISaveCodec codec;private bool replacePreserved;
    private static readonly JsonSerializerOptions Options=new(){PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower,WriteIndented=true,Converters={new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower,false)}};
    public SaveRepository(string directory,string slot="auto",ISaveCodec? codec=null)
    {
        this.directory=Path.GetFullPath(directory);this.codec=codec??new LegacySaveCodec();var name=slot=="manual"?"manual":"save";path=Path.Combine(this.directory,name+".json");backup=Path.Combine(this.directory,name+".bak.json");
    }
    public SaveResult Save(WorldSnapshot snapshot)
    {
        string? temporary=null;
        try
        {
            var error=ValidateSnapshot(snapshot);if(error!=null)return new(false,error);
            Directory.CreateDirectory(directory);var current=Load();
            if(current.Status==LoadStatus.IoError)return new(false,"本次未能读取原档或保存，请继续游戏或重试。"+current.Message);
            if(current.Status is not (LoadStatus.Loaded or LoadStatus.NotFound)&&!replacePreserved)return new(false,"原档损坏或版本未知；请先保留副本再新建/恢复。");
            if(current.Status==LoadStatus.NotFound&&LoadBackup().Status!=LoadStatus.NotFound&&!replacePreserved)return new(false,"该槽仍有备份；请明确恢复或保留副本后再新建。");
            temporary=Path.Combine(directory,"write-"+Guid.NewGuid()+".tmp");File.WriteAllText(temporary,JsonSerializer.Serialize(snapshot,Options));
            if(Read(temporary).Status!=LoadStatus.Loaded)return new(false,"临时存档校验失败。");
            if(current.Status==LoadStatus.Loaded)File.Replace(temporary,path,backup,true);else File.Move(temporary,path,replacePreserved);
            temporary=null;replacePreserved=false;return new(true,"已保存。");
        }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException){return new(false,"本次未能保存，可继续游戏或重试。"+ex.GetType().Name);}
        finally{if(temporary!=null&&File.Exists(temporary))try{File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
    public LoadResult Load()=>Read(path);
    public string? ValidateSnapshot(WorldSnapshot snapshot)=>codec.Validate(snapshot);
    public LoadResult LoadBackup()=>Read(backup);
    public SaveResult PreserveForNewGame()
    {
        try{Directory.CreateDirectory(directory);foreach(var source in new[]{path,backup})if(File.Exists(source))File.Copy(source,Path.Combine(directory,"preserved-"+Guid.NewGuid()+".json"));replacePreserved=true;return new(true,"原档副本已保留。");}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return new(false,"未能保留旧档："+ex.GetType().Name);}
    }
    private LoadResult Read(string source)
    {
        try
        {
            return codec.Read(File.ReadAllText(source));
        }
        catch(FileNotFoundException){return new(LoadStatus.NotFound,null,"暂无存档。");}
        catch(DirectoryNotFoundException){
            // A file in place of any parent directory is an IO failure, not permission to import v1.
            var parent=Path.GetDirectoryName(source);
            while(parent!=null&&!Directory.Exists(parent)){
                if(File.Exists(parent))return new(LoadStatus.IoError,null,"存档目录被文件占用，原档已保留。");
                parent=Path.GetDirectoryName(parent);
            }
            return new(LoadStatus.NotFound,null,"暂无存档。");
        }
        catch(JsonException){return new(LoadStatus.Corrupt,null,"存档无法解析，原档已保留。");}
        catch(InvalidOperationException){return new(LoadStatus.Corrupt,null,"存档字段类型错误。");}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return new(LoadStatus.IoError,null,"无法读取存档："+ex.GetType().Name);}
    }
    public static string? Validate(WorldSnapshot s)
    {
        if(s==null)return "空存档。";
        if(s.SchemaVersion!=1||s.ContentVersion!="vs01-0.1")return "不支持的存档版本。";
        var storyError=ValidateStory(s);if(storyError!=null)return storyError;
        if(s.DiscoveredIds.Count!=0)return "旧档不能包含新版发现。";
        var width=s.SceneId switch{"community_gate"=>1600,"convenience_street"=>1280,"soup_shop"=>960,_=>640};
        if(!float.IsFinite(s.PlayerPosition.X)||!float.IsFinite(s.PlayerPosition.Y)||s.PlayerPosition.X<8||s.PlayerPosition.X>width-8||s.PlayerPosition.Y!=280)return "存档位置异常。";
        if(s.ReturnContext is {} context&&(!float.IsFinite(context.Position.X)||context.Position.X<8||context.Position.X>952||context.Position.Y!=280))return "回忆返回位置异常。";
        return null;
    }
    internal static string? ValidateStory(WorldSnapshot s)
    {
        if(s.SceneId is not ("community_gate" or "convenience_street" or "soup_shop" or "memory_soup_table")||!Enum.IsDefined(s.Stage)||s.CompletedActions==null||s.ChoiceCodes==null||s.InvitationState==null||s.Settings==null||s.SceneActiveMilliseconds==null||string.IsNullOrWhiteSpace(s.PlaythroughId))return "存档内容不完整。";
        if(s.GameDay!=1||s.CandyCount is <0 or >1||s.MemoryOrdinal<0||s.MemoryVisitOrdinal<0)return "存档数值异常。";
        var idError=SaveIdPolicy.Validate(s);if(idError!=null)return idError;
        if(s.Stage==SliceStage.CandyHeyPending&&s.CandyCount!=1||s.Stage>SliceStage.CandyHeyPending&&s.CandyCount!=0)return "喜糖与任务阶段不一致。";
        if(!double.IsFinite(s.InvitationState.Elapsed)||s.InvitationState.Elapsed<0||s.InvitationState.Resolution!=null&&!Enum.IsDefined(s.InvitationState.Resolution.Value))return "邀请计时异常。";
        if(s.MemoryState is {} m)
        {
            if(m.PushedCoinIds==null||m.PushedCoinIds.Any(id=>id is not ("c1" or "c2" or "c3" or "c4"))||m.PushedTotal!=m.PushedCoinIds.Sum(id=>id=="c4"?2:1)||m.Completed!=(m.FoodChoice!=null)||m.Completed&&m.PushedTotal!=5||m.FoodChoice!=null&&!Enum.IsDefined(m.FoodChoice.Value))return "回忆进度异常。";
            if(!SaveIdPolicy.MemoryId(m.InstanceId,s.MemoryOrdinal)||m.InstanceId!="soup-"+s.MemoryOrdinal)return "回忆实例异常。";
        }
        if(s.MemoryState!=null&&s.Stage<SliceStage.MemoryActive||s.SceneId=="memory_soup_table"&&s.Stage is not (SliceStage.MemoryActive or SliceStage.SliceComplete))return "回忆与任务阶段不一致。";
        if(s.Stage==SliceStage.MemoryReturned&&s.MemoryState is not {Completed:true,Replay:false}||s.Stage==SliceStage.SliceComplete&&(s.MemoryState==null||!s.MemoryState.Completed&&!s.MemoryState.Replay))return "任务完成但回忆未完成。";
        if(s.MemoryState is {Replay:true}&& (s.Stage!=SliceStage.SliceComplete||!s.CompletedActions.Contains("slice.complete:slice-1")))return "重看缺少已完成主线。";
        if(s.MemoryState!=null||s.Stage is SliceStage.MemoryActive or SliceStage.MemoryReturned or SliceStage.SliceComplete)
        {if(s.MemoryState==null||s.ReturnContext is not {SceneId:"soup_shop",IsAdult:true,DialogueNodeId:"soup.return"})return "回忆返回上下文缺失。";}
        if(s.ReturnContext!=null&&s.ReturnContext is not {SceneId:"soup_shop",IsAdult:true,DialogueNodeId:"soup.return"})return "回忆返回上下文异常。";
        return null;
    }
}
public static class ResumePolicy
{
    public static WorldSnapshot Normalize(WorldSnapshot snapshot)=>snapshot with {PhoneState="closed"};
}
