using Godot;
using GeXingzhou.Domain;
public partial class DialogueController : Control
{
    private readonly DialogueSession dialogue=new();private Label text=null!;private double elapsed;private Action? finish;
    public bool IsOpen {get;private set;}
    public override void _Ready()
    {
        var panel=new PanelContainer{Position=new Vector2(60,410),Size=new Vector2(1160,260)};AddChild(panel);
        panel.AddThemeStyleboxOverride("panel",UiStyles.Panel());
        text=new Label{CustomMinimumSize=new Vector2(1100,210),AutowrapMode=TextServer.AutowrapMode.WordSmart};panel.AddChild(text);Visible=false;
    }
    public bool Open(string id,Action? onFinished=null)
    {
        var s=GetNode<GameSession>("/root/GameSession");if(!dialogue.Start(id,s.Catalog!).Success)return false;
        Begin(onFinished);return true;
    }
    public void ShowText(string title,string body,Action? onFinished=null)
    {
        dialogue.Start("notice",new ContentCatalog{Dialogues=new Dictionary<string,DialogueNode>{{"notice",new(title,new[]{body})}}});Begin(onFinished);
    }
    private void Begin(Action? callback){IsOpen=true;Visible=true;elapsed=0;finish=callback;GetNode<GameSession>("/root/GameSession").Flow=FlowState.Dialogue;}
    public override void _Process(double delta)
    {
        if(!IsOpen||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Dialogue)return;
        elapsed+=delta;dialogue.Tick(delta,GetNode<GameSession>("/root/GameSession").Options.TextSpeed);text.Text=dialogue.Speaker+"\n\n"+dialogue.Text+"\n\n[E / Enter 继续 · Esc 返回]";
    }
    public void HandleKey(Key key)
    {
        if(key==Key.Escape){dialogue.Cancel();Close(false);}
        else if(key==Key.E||key==Key.Enter){if(dialogue.Advance(elapsed).Finished)Close(true);}
    }
    private void Close(bool completed){Visible=false;IsOpen=false;var callback=finish;finish=null;GetNode<GameSession>("/root/GameSession").Flow=FlowState.Field;if(completed)callback?.Invoke();}
}
