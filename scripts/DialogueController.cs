using Godot;
using GeXingzhou.Domain;
public partial class DialogueController : Control
{
    private readonly DialogueSession dialogue=new();
    private Label text=null!,nameLabel=null!;
    private double elapsed;private Action? finish;
    private FlowState source;private Control? previousFocus;
    public bool IsOpen {get;private set;}
    public override void _Ready()
    {
        try
        {
            nameLabel=SceneBindings.Require<Label>(this,"Panel/Content/NameLabel");
            text=SceneBindings.Require<Label>(this,"Panel/Content/Body/Text");
            SceneBindings.Require<Label>(this,"Panel/Content/ContinueHint");Visible=false;
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public bool Open(string id,Action? onFinished=null)
    {
        if(HasMeta("binding_error"))return false;
        var s=GetNode<GameSession>("/root/GameSession");
        if(s.Catalog==null||!dialogue.Start(id,s.Catalog).Success)return false;
        Begin(onFinished);return true;
    }
    public void ShowText(string title,string body,Action? onFinished=null)
    {
        if(HasMeta("binding_error"))return;
        dialogue.Start("notice",new ContentCatalog{Dialogues=new Dictionary<string,DialogueNode>{{"notice",new(title,new[]{body})}}});
        Begin(onFinished);
    }
    private void Begin(Action? callback)
    {
        var s=GetNode<GameSession>("/root/GameSession");
        if(!IsOpen){source=s.Flow;previousFocus=GetViewport().GuiGetFocusOwner();}
        IsOpen=true;Visible=true;elapsed=0;finish=callback;s.Flow=FlowState.Dialogue;
        nameLabel.Text=dialogue.Speaker;text.Text=dialogue.Text;
        GetNode<ScrollContainer>("Panel/Content/Body").ScrollVertical=0;
    }
    public override void _Process(double delta)
    {
        if(!IsOpen||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Dialogue)return;
        elapsed+=delta;dialogue.Tick(delta,GetNode<GameSession>("/root/GameSession").Options.TextSpeed);
        nameLabel.Text=dialogue.Speaker;text.Text=dialogue.Text;
    }
    public void HandleKey(Key key)
    {
        if(!IsOpen)return;
        if(key==Key.Escape){dialogue.Cancel();Close(false);}
        else if(key==Key.E||key==Key.Enter){if(dialogue.Advance(elapsed).Finished)Close(true);}
    }
    private void Close(bool completed)
    {
        Visible=false;IsOpen=false;var callback=finish;finish=null;
        GetNode<GameSession>("/root/GameSession").Flow=source;
        if(GodotObject.IsInstanceValid(previousFocus)&&previousFocus!.IsVisibleInTree())previousFocus.GrabFocus();
        previousFocus=null;if(completed)callback?.Invoke();
    }
}
