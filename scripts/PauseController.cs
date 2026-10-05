using Godot;
using GeXingzhou.Domain;

public partial class PauseController : Control
{
    private MainView main=null!;private Label message=null!;private Control? previousFocus;
    public bool IsOpen {get;private set;}
    private const string Prefix="Panel/Scroll/Content/";
    public void Configure(MainView owner)=>main=owner;
    public override void _Ready()
    {
        try
        {
            message=SceneBindings.Require<Label>(this,Prefix+"Message");
            SceneBindings.Require<Button>(this,Prefix+"Resume").Pressed+=Close;
            SceneBindings.Require<Button>(this,Prefix+"Save").Pressed+=()=>Save();
            SceneBindings.Require<Button>(this,Prefix+"Settings").Pressed+=()=>main.Settings.Open();
            SceneBindings.Require<Button>(this,Prefix+"Menu").Pressed+=()=>TryReturnToMenu();
            SceneBindings.Require<Button>(this,Prefix+"Quit").Pressed+=()=>TryExit();
            Visible=false;
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public bool Open()
    {
        if(IsOpen||HasMeta("binding_error")||main==null||!main.CanPause)return false;
        previousFocus=GetViewport().GuiGetFocusOwner();IsOpen=true;Visible=true;message.Text="";
        GetNode<GameSession>("/root/GameSession").Flow=FlowState.Paused;main.Audio.SetPaused(true);
        SceneBindings.Require<Button>(this,Prefix+"Resume").GrabFocus();return true;
    }
    public void Close()
    {
        if(!IsOpen)return;
        if(main.Settings.IsOpen)main.Settings.Close();
        IsOpen=false;Visible=false;GetNode<GameSession>("/root/GameSession").Flow=FlowState.Field;main.Audio.SetPaused(false);
        if(GodotObject.IsInstanceValid(previousFocus)&&previousFocus!.IsVisibleInTree())previousFocus.GrabFocus();previousFocus=null;
    }
    public SaveResult Save()
    {
        if(!IsOpen)return new(false,"请先暂停再保存。");
        var session=GetNode<GameSession>("/root/GameSession");session.UpdatePosition(main.SafeSavePosition);
        var result=session.SaveManual();message.Text=result.Message;return result;
    }
    private bool SaveForNavigation()
    {
        if(!IsOpen)return false;
        var session=GetNode<GameSession>("/root/GameSession");session.UpdatePosition(main.SafeSavePosition);
        var result=session.SaveCheckpoint();message.Text=result.Message;return result.Success;
    }
    public bool TryReturnToMenu()
    {
        if(!SaveForNavigation())return false;
        main.Audio.StopTransient();
        var error=GetTree().ChangeSceneToFile("res://scenes/Boot.tscn");
        if(error==Error.Ok)return true;message.Text="菜单未能打开，请继续游戏或重试。";return false;
    }
    public bool TryExit()
    {
        if(!SaveForNavigation())return false;
        main.Audio.StopAll();GetTree().Quit();return true;
    }
}
