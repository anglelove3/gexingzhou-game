using System.Text.Json;
using System.Text.RegularExpressions;
namespace GeXingzhou.Domain;
public sealed record BehaviorEvent
{
    public int SchemaVersion {get;init;}=1;public string ContentVersion {get;init;}="vs01-0.1";
    public string PlaythroughId {get;init;}="";public long Sequence {get;init;}
    public string SceneId {get;init;}="community_gate";public string EventId {get;init;}="";
    public string OpportunityId {get;init;}="";public string ChoiceCode {get;init;}="";
    public int GameDay {get;init;}=1;public string CheckpointId {get;init;}="arrival";public long ActiveMilliseconds {get;init;}
}
public sealed record RecordResult(bool Success,string Message);
public sealed class LocalEventRecorder
{
    public bool Enabled {get;set;}public int SkippedLines {get;private set;}
    private readonly string directory,path,counterPath;private long sequence;private readonly HashSet<string> seen=new();
    private static readonly JsonSerializerOptions Options=new(){PropertyNamingPolicy=JsonNamingPolicy.SnakeCaseLower};
    private static readonly Dictionary<string,string[]> Allowed=new()
    {
        ["invitation.resolve"]=new[]{"answered","arrived"},["candy.hey.delivered"]=new[]{"delivered"},["soup.memory.enter"]=new[]{"entered"},["memory.coin.complete"]=new[]{"completed"},["memory.food.resolve"]=new[]{"take","wait","share"},["soup.memory.return"]=new[]{"returned"},["slice.complete"]=new[]{"completed"}
    };
    public LocalEventRecorder(string directory,bool enabled=false)
    {
        this.directory=Path.GetFullPath(directory);path=Path.Combine(this.directory,"events.jsonl");counterPath=Path.Combine(this.directory,"counter.txt");Enabled=enabled;
        try{if(File.Exists(counterPath)&&long.TryParse(File.ReadAllText(counterPath),out var count)&&count>0)sequence=count;foreach(var value in ReadValid()){sequence=Math.Max(sequence,value.Sequence);seen.Add(Key(value));}}catch(IOException){}catch(UnauthorizedAccessException){}
    }
    private static string Key(BehaviorEvent e)=>e.PlaythroughId+":"+e.EventId+":"+e.OpportunityId;
    private static bool Valid(BehaviorEvent e)=>e.SchemaVersion==1&&e.ContentVersion=="vs01-0.1"&&e.GameDay==1&&e.ActiveMilliseconds>=0&&e.EventId!=null&&Allowed.TryGetValue(e.EventId,out var codes)&&codes.Contains(e.ChoiceCode)&&e.SceneId is "community_gate" or "convenience_street" or "soup_shop" or "memory_soup_table"&&e.PlaythroughId!=null&&Regex.IsMatch(e.PlaythroughId,"^[a-zA-Z0-9-]{1,64}$")&&e.OpportunityId!=null&&Regex.IsMatch(e.OpportunityId,"^[a-z0-9:_-]{1,128}$")&&e.CheckpointId!=null&&Regex.IsMatch(e.CheckpointId,"^[a-z_]{1,64}$");
    private IEnumerable<BehaviorEvent> ReadValid()
    {
        SkippedLines=0;if(!File.Exists(path))yield break;var unique=new HashSet<string>();long last=0;
        foreach(var line in File.ReadLines(path))
        {
            BehaviorEvent? value=null;try{value=JsonSerializer.Deserialize<BehaviorEvent>(line,Options);}catch(JsonException){}
            if(value==null||!Valid(value)||value.Sequence<=last||!unique.Add(Key(value))){SkippedLines++;continue;}last=value.Sequence;yield return value;
        }
    }
    public RecordResult TryAppend(BehaviorEvent value)
    {
        if(!Enabled)return new(true,"记录已关闭。");if(!Valid(value))return new(false,"不允许的事件字段。");if(seen.Contains(Key(value)))return new(true,"重复机会已忽略。");
        try{Directory.CreateDirectory(directory);var next=value with {Sequence=checked(sequence+1)};File.AppendAllText(path,JsonSerializer.Serialize(next,Options)+Environment.NewLine);sequence=next.Sequence;seen.Add(Key(value));File.WriteAllText(counterPath,sequence.ToString(System.Globalization.CultureInfo.InvariantCulture));return new(true,"已记录。");}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or OverflowException){return new(false,"记录未写入，不影响主线："+ex.GetType().Name);}
    }
    public RecordResult Clear()
    {
        try{if(File.Exists(path)){File.WriteAllText(counterPath,sequence.ToString(System.Globalization.CultureInfo.InvariantCulture));File.WriteAllText(path,"");}seen.Clear();SkippedLines=0;return new(true,"本地记录已清空，存档未改变。");}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return new(false,ex.GetType().Name);}
    }
    public RecordResult Export(string destination)
    {
        try{if(Path.GetFullPath(destination)==path)return new(false,"不能覆盖原记录。");var values=ReadValid().ToArray();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);File.WriteAllLines(destination,values.Select(v=>JsonSerializer.Serialize(v,Options)));return new(true,"已导出允许的字段。");}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException){return new(false,ex.GetType().Name);}
    }
}
