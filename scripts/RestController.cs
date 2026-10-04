using Godot;
using GeXingzhou.Domain;
public partial class RestController : Node
{
    private readonly RestStateMachine state=new();
    private MainView main=null!;private RestOptionsController menu=null!;
    private BenchView? bench;private PlayerController? player;private AnimatedSprite2D? art;
    private Action? finished;private int generation;
    public RestPhase Phase=>state.Phase;
    public bool IsActive=>Phase!=RestPhase.Standing;
    public Position2 SavePosition=>bench!=null&&GodotObject.IsInstanceValid(bench)?
        new(bench.StandAnchor.GlobalPosition.X,bench.StandAnchor.GlobalPosition.Y):
        new(main.World.Player.Position.X,main.World.Player.Position.Y);
    public override void _Ready()
    {
        main=(MainView)GetParent();
        try{menu=SceneBindings.Require<RestOptionsController>(main,"RestOptions");}
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public bool Begin(BenchView value)
    {
        if(HasMeta("binding_error")||main.World.HasMeta("binding_error")||value.HasMeta("binding_error")||value.GetParent()!=main.World||!value.IsActive||
           GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field||!state.TrySit())return false;
        bench=value;player=main.World.Player;player.GlobalPosition=value.StandAnchor.GlobalPosition;
        art=player.GetNode<AnimatedSprite2D>("Artwork");int current=++generation;
        finished=()=>{
            if(current!=generation||!IsActive||state.Suspended||
               GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field)return;
            var expected=Phase switch{RestPhase.SittingDown=>"sit_down",RestPhase.Smoking=>"smoke",RestPhase.StandingUp=>"stand_up",_=>""};
            if(art.Animation!=expected)return;
            state.AnimationFinished();Sync();
        };
        art.AnimationFinished+=finished;Sync();return true;
    }
    public bool HandleKey(Key key)
    {
        if(!IsActive||state.Suspended)return false;
        if(key==Key.Escape){state.HandleEscape();Sync();return true;}
        if(key is Key.E or Key.Enter)
        {if(menu.IsOpen)menu.ActivateFocused();else if(state.ReopenMenu())Sync();return true;}
        return false;
    }
    private void Choose(RestChoice choice)
    {
        if(!state.TryChoose(choice))return;
        menu.Close();Sync();
        if(choice==RestChoice.Rest&&bench!=null)main.ShowObservation(bench);
    }
    public void Suspend(){if(!IsActive)return;state.Suspend();Sync();}
    public void Resume(){if(!state.Suspended)return;state.Resume();Sync();}
    public override void _Process(double delta)
    {
        if(!IsActive)return;
        var flow=GetNode<GameSession>("/root/GameSession").Flow;
        if(flow!=FlowState.Field){if(!state.Suspended)Suspend();return;}
        if(state.Suspended)Resume();else Sync();
    }
    private void Sync()
    {
        if(!IsActive){Cancel();return;}
        if(player==null||bench==null||!GodotObject.IsInstanceValid(player)||!GodotObject.IsInstanceValid(bench)){Cancel();return;}
        var animation=Phase switch{RestPhase.SittingDown=>"sit_down",RestPhase.Seated=>"seated",RestPhase.Smoking=>"smoke",_=>"stand_up"};
        player.SetRestPose(animation,bench.SeatAnchor.GlobalPosition-player.GlobalPosition,state.Suspended);
        bench.GetNode<Sprite2D>("BenchForeground").Visible=true;
        if(state.MenuOpen&&!state.Suspended)menu.Open(Choose);else if(menu.IsOpen)menu.Close();
    }
    public void Cancel()
    {
        generation++;state.Cancel();
        if(art!=null&&GodotObject.IsInstanceValid(art)&&finished!=null)art.AnimationFinished-=finished;
        finished=null;
        if(player!=null&&GodotObject.IsInstanceValid(player))player.ClearRestPose();
        if(bench!=null&&GodotObject.IsInstanceValid(bench)&&bench.GetNodeOrNull<Sprite2D>("BenchForeground") is {} foreground)foreground.Visible=false;
        if(menu!=null&&GodotObject.IsInstanceValid(menu))menu.Close();
        art=null;player=null;bench=null;
    }
    public override void _ExitTree()=>Cancel();
}
