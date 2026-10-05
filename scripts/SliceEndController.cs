using Godot;
using GeXingzhou.Domain;

public partial class SliceEndController : Control
{
    private MainView main=null!;private Label message=null!;private bool shown;
    public bool IsOpen {get;private set;}
    private const string Prefix="Panel/Scroll/Content/";
    public void Configure(MainView owner)=>main=owner;
    public override void _Ready()
    {
        try
        {
            message=SceneBindings.Require<Label>(this,Prefix+"Message");
            SceneBindings.Require<Button>(this,Prefix+"Continue").Pressed+=Close;
            SceneBindings.Require<Button>(this,Prefix+"Menu").Pressed+=()=>TryReturnToMenu();
            SceneBindings.Require<Button>(this,Prefix+"Restart").Pressed+=()=>TryRestart();
            Visible=false;
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public void ShowCompleted()
    {
        if(shown||main==null||HasMeta("binding_error"))return;
        var s=GetNode<GameSession>("/root/GameSession");
        if(s.Snapshot.Stage!=SliceStage.SliceComplete||s.Snapshot.SceneId=="memory_soup_table")return;
        shown=true;IsOpen=true;Visible=true;message.Text="";s.Flow=FlowState.EndCard;
        main.Audio.SetPaused(true);SceneBindings.Require<Button>(this,Prefix+"Continue").GrabFocus();
    }
    public void Close()
    {
        if(!IsOpen)return;
        IsOpen=false;Visible=false;GetNode<GameSession>("/root/GameSession").Flow=FlowState.Field;main.Audio.SetPaused(false);
    }
    public bool TryReturnToMenu()
    {
        if(!IsOpen)return false;
        var result=main.Pause.TryReturnToMenu();if(!result)message.Text=main.Pause.NavigationMessage;return result;
    }
    public bool TryRestart()
    {
        if(!IsOpen)return false;
        var s=GetNode<GameSession>("/root/GameSession");
        var automatic=s.Saves.PreserveForNewGame();var manual=s.ManualSaves.PreserveForNewGame();
        if(!automatic.Success||!manual.Success){message.Text=automatic.Message+"\n"+manual.Message;return false;}
        main.Audio.StopTransient();s.NewGame();
        var error=GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
        if(error==Error.Ok)return true;message.Text="新游戏未能打开，旧档副本仍已保留。";return false;
    }
}
