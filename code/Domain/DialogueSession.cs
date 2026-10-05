using System.Text.Json;
using System.Text.Json.Serialization;
namespace GeXingzhou.Domain;
[JsonConverter(typeof(DialogueLineKindConverter))]
public enum DialogueLineKind { Spoken, Thought, Narration }
public sealed class DialogueLineKindConverter : JsonConverter<DialogueLineKind>
{
    public override DialogueLineKind Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)
    {
        if(reader.TokenType!=JsonTokenType.String || !Enum.TryParse<DialogueLineKind>(reader.GetString(),false,out var kind) || !Enum.IsDefined(kind))
            throw new JsonException("对白Kind必须为Spoken、Thought或Narration。");
        return kind;
    }
    public override void Write(Utf8JsonWriter writer,DialogueLineKind value,JsonSerializerOptions options)=>writer.WriteStringValue(value.ToString());
}
public sealed record DialogueLineMetadata(string? Speaker=null,DialogueLineKind Kind=DialogueLineKind.Spoken);
public sealed record DialogueNode(string Speaker,string[] Lines,DialogueLineMetadata[]? LineMetadata=null)
{
    [JsonIgnore] public bool IsValid=>!string.IsNullOrWhiteSpace(Speaker)&&Lines is {Length:>0}&&!Lines.Any(string.IsNullOrWhiteSpace)&&
        (LineMetadata==null || (LineMetadata.Length==Lines.Length && LineMetadata.All(m=>m!=null&&Enum.IsDefined(m.Kind)&&(m.Speaker==null||!string.IsNullOrWhiteSpace(m.Speaker)))));
}
public sealed record DialogueStartResult(bool Success,string? ErrorCode);
public sealed record DialogueStepResult(bool Applied,bool Finished,string Text);
public sealed class DialogueSession
{
    private string[] lines=Array.Empty<string>();private int index;private double visible;private double lastAdvance;private bool active;
    public string Speaker {get;private set;}="";
    public DialogueLineKind Kind {get;private set;}=DialogueLineKind.Spoken;
    private DialogueNode? node;
    private void ApplyLineMetadata()
    {
        var metadata=node!.LineMetadata?[index];
        Speaker=metadata?.Speaker??node.Speaker;Kind=metadata?.Kind??DialogueLineKind.Spoken;
    }
    public string Text => active?lines[index][..Math.Min(lines[index].Length,(int)visible)]:"";
    public DialogueStartResult Start(string nodeId,ContentCatalog catalog)
    {
        active=false;
        if(!catalog.Dialogues.TryGetValue(nodeId,out var candidate)||candidate==null||!candidate.IsValid)return new(false,"missing_node");
        node=candidate;lines=node.Lines;index=0;visible=0;lastAdvance=0;active=true;ApplyLineMetadata();return new(true,null);
    }
    public DialogueStepResult Advance(double activeTime)
    {
        if(!active||activeTime-lastAdvance<.15-1e-8)return new(false,false,Text);lastAdvance=activeTime;
        if(visible<lines[index].Length){visible=lines[index].Length;return new(true,false,Text);}
        if(index+1==lines.Length){active=false;return new(true,true,"");}
        index++;visible=0;ApplyLineMetadata();return new(true,false,Text);
    }
    public DialogueStepResult Cancel(){var applied=active;active=false;return new(applied,true,"");}
    public void Tick(double dt,int speed){if(active&&dt>0)visible=speed==0?lines[index].Length:Math.Min(lines[index].Length,visible+dt*speed);}
}
