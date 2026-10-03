namespace GeXingzhou.Domain;
public sealed record DialogueNode(string Speaker,string[] Lines);
public sealed record DialogueStartResult(bool Success,string? ErrorCode);
public sealed record DialogueStepResult(bool Applied,bool Finished,string Text);
public sealed class DialogueSession
{
    private string[] lines=Array.Empty<string>();private int index;private double visible;private double lastAdvance;private bool active;
    public string Speaker {get;private set;}="";
    public string Text => active?lines[index][..Math.Min(lines[index].Length,(int)visible)]:"";
    public DialogueStartResult Start(string nodeId,ContentCatalog catalog)
    {
        active=false;
        if(!catalog.Dialogues.TryGetValue(nodeId,out var node)||node.Lines==null||node.Lines.Length==0||node.Lines.Any(string.IsNullOrWhiteSpace))return new(false,"missing_node");
        lines=node.Lines;Speaker=node.Speaker;index=0;visible=0;lastAdvance=0;active=true;return new(true,null);
    }
    public DialogueStepResult Advance(double activeTime)
    {
        if(!active||activeTime-lastAdvance<.15-1e-8)return new(false,false,Text);lastAdvance=activeTime;
        if(visible<lines[index].Length){visible=lines[index].Length;return new(true,false,Text);}
        if(index+1==lines.Length){active=false;return new(true,true,"");}
        index++;visible=0;return new(true,false,Text);
    }
    public DialogueStepResult Cancel(){var applied=active;active=false;return new(applied,true,"");}
    public void Tick(double dt,int speed){if(active&&dt>0)visible=speed==0?lines[index].Length:Math.Min(lines[index].Length,visible+dt*speed);}
}
