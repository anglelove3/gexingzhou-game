using Godot;
using GeXingzhou.Domain;
public partial class DialogueController : Control
{
    private readonly DialogueSession dialogue=new();
    private Label text=null!,nameLabel=null!;
    private PanelContainer panel=null!;private StyleBox authoredStyle=null!;
    [Export(PropertyHint.Range,"0.5,1.0,0.01")] public float MonologueOpacity {get;set;}=.82f;
    private double elapsed;private Action? finish;
    private FlowState source;private Control? previousFocus;
    public bool IsOpen {get;private set;}
    public override void _Ready()
    {
        try
        {
            panel=SceneBindings.Require<PanelContainer>(this,"Panel");authoredStyle=panel.GetThemeStylebox("panel");
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
        if(dialogue.Speaker=="葛行舟"&&authoredStyle is StyleBoxTexture raster)
        {
            var lighter=(StyleBoxTexture)raster.Duplicate();var tint=lighter.ModulateColor;
            tint.A*=Math.Clamp(MonologueOpacity,.5f,1f);lighter.ModulateColor=tint;
            panel.AddThemeStyleboxOverride("panel",lighter);
        }
        else panel.AddThemeStyleboxOverride("panel",authoredStyle);
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
