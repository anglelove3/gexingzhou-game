using Godot;
using GeXingzhou.Domain;

public partial class SliceEndController : Control
{
    private MainView main=null!;private Label message=null!,reflection=null!;private bool shown;
    public bool IsOpen {get;private set;}
    private const string Prefix="Panel/Scroll/Content/";
    private const string Actions="Panel/Actions/";
    public void Configure(MainView owner)=>main=owner;
    public override void _Ready()
    {
        try
        {
            message=SceneBindings.Require<Label>(this,Prefix+"Message");
            reflection=SceneBindings.Require<Label>(this,Prefix+"Reflection");
            SceneBindings.Require<Button>(this,Actions+"Continue").Pressed+=Close;
            SceneBindings.Require<Button>(this,Actions+"Menu").Pressed+=()=>TryReturnToMenu();
            SceneBindings.Require<Button>(this,Actions+"Restart").Pressed+=()=>TryRestart();
            Visible=false;
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public void ShowCompleted()
    {
        if(shown||main==null||HasMeta("binding_error"))return;
        var s=GetNode<GameSession>("/root/GameSession");
        if(s.Snapshot.Stage!=SliceStage.SliceComplete||s.Snapshot.SceneId=="memory_soup_table")return;
        shown=true;IsOpen=true;Visible=true;message.Text="";reflection.Text=JournalProjection.EndReflection(s.Snapshot);s.Flow=FlowState.EndCard;
        main.Audio.SetPaused(true);SceneBindings.Require<Button>(this,Actions+"Continue").GrabFocus();
    }
    public void Close()
    {
        if(!IsOpen)return;
        IsOpen=false;Visible=false;GetNode<GameSession>("/root/GameSession").Flow=FlowState.Field;main.Audio.SetPaused(false);
    }
    public bool HandleReadingKey(Key key)
    {
        if(!IsOpen)return false;
        var scroll=GetNode<ScrollContainer>("Panel/Scroll");
        if(key==Key.Home)scroll.ScrollVertical=0;
        else if(key==Key.End)scroll.ScrollVertical=(int)scroll.GetVScrollBar().MaxValue;
        else if(key is Key.Pageup or Key.Pagedown)scroll.ScrollVertical+=(key==Key.Pageup?-1:1)*Math.Max(24,(int)scroll.Size.Y-12);
        else return false;
        return true;
    }
    public bool TryReturnToMenu()
    {
        if(!IsOpen)return false;
        var result=main.Pause.TryReturnToMenu();if(!result)ShowFailure(main.Pause.NavigationMessage);return result;
    }
    public bool TryRestart()
    {
        if(!IsOpen)return false;
        var s=GetNode<GameSession>("/root/GameSession");
        var automatic=s.Saves.PreserveForNewGame();var manual=s.ManualSaves.PreserveForNewGame();
        if(!automatic.Success||!manual.Success){ShowFailure(automatic.Message+"\n"+manual.Message);return false;}
        main.Audio.StopTransient();s.NewGame();
        var error=GetTree().ChangeSceneToFile("res://scenes/Main.tscn");
        if(error==Error.Ok)return true;ShowFailure("新游戏未能打开，旧档副本仍已保留。");return false;
    }
    private void ShowFailure(string text){message.Text=text;CallDeferred(nameof(RevealFailure));}
    private void RevealFailure()
    {
        if(IsOpen)GetNode<ScrollContainer>("Panel/Scroll").EnsureControlVisible(message);
    }
}
