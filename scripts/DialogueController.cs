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
    private DialogueLineKind? presentedKind;
    private ScrollContainer body=null!;
    public bool IsOpen {get;private set;}
    public override void _Ready()
    {
        try
        {
            panel=SceneBindings.Require<PanelContainer>(this,"Panel");authoredStyle=panel.GetThemeStylebox("panel");
            nameLabel=SceneBindings.Require<Label>(this,"Panel/Content/NameLabel");
            text=SceneBindings.Require<Label>(this,"Panel/Content/Body/Text");
            body=SceneBindings.Require<ScrollContainer>(this,"Panel/Content/Body");
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
    public void ShowText(string title,string body,Action? onFinished=null,DialogueLineKind kind=DialogueLineKind.Spoken)
    {
        if(HasMeta("binding_error"))return;
        dialogue.Start("notice",new ContentCatalog{Dialogues=new Dictionary<string,DialogueNode>{{"notice",new(title,new[]{body},new[]{new DialogueLineMetadata(null,kind)})}}});
        Begin(onFinished);
    }
    private void Begin(Action? callback)
    {
        var s=GetNode<GameSession>("/root/GameSession");
        if(!IsOpen){source=s.Flow;previousFocus=GetViewport().GuiGetFocusOwner();}
        IsOpen=true;Visible=true;elapsed=0;finish=callback;s.Flow=FlowState.Dialogue;
        presentedKind=null;ApplyLinePresentation();text.Text=dialogue.Text;
        GetNode<ScrollContainer>("Panel/Content/Body").ScrollVertical=0;
    }
    private void ApplyLinePresentation()
    {
        nameLabel.Text=dialogue.Kind switch{DialogueLineKind.Thought=>dialogue.Speaker+"·心声",DialogueLineKind.Narration=>"旁白",_=>dialogue.Speaker};
        if(presentedKind==dialogue.Kind)return;
        presentedKind=dialogue.Kind;
        nameLabel.AddThemeColorOverride("font_color",dialogue.Kind==DialogueLineKind.Spoken?new Color(.96f,.84f,.62f):new Color(.82f,.85f,.87f));
        if(dialogue.Kind!=DialogueLineKind.Spoken&&authoredStyle is StyleBoxTexture raster)
        {
            var lighter=(StyleBoxTexture)raster.Duplicate();var tint=lighter.ModulateColor;
            tint.A*=dialogue.Kind==DialogueLineKind.Thought?Math.Clamp(MonologueOpacity,.5f,1f):.94f;lighter.ModulateColor=tint;
            panel.AddThemeStyleboxOverride("panel",lighter);
        }
        else panel.AddThemeStyleboxOverride("panel",authoredStyle);
        if(dialogue.Kind!=DialogueLineKind.Spoken&&authoredStyle is StyleBoxFlat flat)
        {
            var style=(StyleBoxFlat)flat.Duplicate();var tint=style.BgColor;
            if(dialogue.Kind!=DialogueLineKind.Spoken)tint.A*=dialogue.Kind==DialogueLineKind.Thought?Math.Clamp(MonologueOpacity,.5f,1):.94f;
            style.BgColor=tint;panel.AddThemeStyleboxOverride("panel",style);
        }
    }
    public override void _Process(double delta)
    {
        if(!IsOpen||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Dialogue)return;
        elapsed+=delta;dialogue.Tick(delta,GetNode<GameSession>("/root/GameSession").Options.TextSpeed);
        ApplyLinePresentation();text.Text=dialogue.Text;
        UpdateLayout();
    }
    private void UpdateLayout()
    {
        var font=GetNode<GameSession>("/root/GameSession").Options.SubtitleSize;
        float height=Math.Clamp(font*4+44,124,188);
        panel.OffsetTop=-16-height;panel.OffsetBottom=-16;
        if(GetParent() is MainView main&&main.World?.SceneId=="soup_shop"&&main.SoupSeat.IsActive)
        {
            // Use the actual SubViewport/camera transform, never world pixels as screen pixels.
            var a=main.WorldToUi(main.World.ToGlobal(new Vector2(408,326)));
            var b=main.WorldToUi(main.World.ToGlobal(new Vector2(604,410)));
            var safe=new Rect2(a,b-a).Abs().Grow(8);
            if(panel.GetRect().Intersects(safe))
            {panel.OffsetTop=-Size.Y+140;panel.OffsetBottom=panel.OffsetTop+height;}
        }
    }
    public void HandleKey(Key key)
    {
        if(!IsOpen)return;
        if(key is Key.Up or Key.Down or Key.Pageup or Key.Pagedown)
        {body.ScrollVertical+=(key is Key.Up or Key.Pageup?-1:1)*(key is Key.Pageup or Key.Pagedown?Math.Max(24,(int)body.Size.Y-12):32);return;}
        if(key==Key.Escape){dialogue.Cancel();Close(false);}
        else if(key==Key.E||key==Key.Enter){if(dialogue.Advance(elapsed).Finished)Close(true);}
    }
    public override void _GuiInput(InputEvent ev)
    {
        if(IsOpen&&ev is InputEventMouseButton{Pressed:true} button&&button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {body.ScrollVertical+=button.ButtonIndex==MouseButton.WheelUp?-48:48;AcceptEvent();}
    }
    private void Close(bool completed)
    {
        Visible=false;IsOpen=false;var callback=finish;finish=null;
        GetNode<GameSession>("/root/GameSession").Flow=source;
        if(GodotObject.IsInstanceValid(previousFocus)&&previousFocus!.IsVisibleInTree())previousFocus.GrabFocus();
        previousFocus=null;if(completed)callback?.Invoke();
    }
}
