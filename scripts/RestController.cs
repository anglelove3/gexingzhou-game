using Godot;
using GeXingzhou.Domain;
public partial class RestController : Node
{
    private readonly RestStateMachine state=new();
    private MainView main=null!;private RestOptionsController menu=null!;
    private BenchView? bench;private PlayerController? player;private AnimatedSprite2D? art;
    private Action? finished;private int generation;private bool focused=true,approaching;
    private ApproachPath? path;private int waypoint;private double approachSeconds;
    private RestPhase boundPhase;private SpriteFrames? boundFrames;
    public RestPhase Phase=>state.Phase;
    public bool IsApproaching=>approaching;
    public bool IsActive=>approaching||Phase!=RestPhase.Standing;
    private Position2 WorldPoint(Vector2 point){var p=main.World.ToLocal(point);return new(p.X,p.Y);}
    public Position2 SavePosition {
        get {
            var foot=new Position2(main.World.Player.Position.X,main.World.Player.Position.Y);
            if(!approaching&&bench!=null&&GodotObject.IsInstanceValid(bench)){
                var stand=WorldPoint(bench.StandAnchor.GlobalPosition);if(NavigationGeometry.CanStand(main.World.Navigation,stand))return stand;
            }
            return NavigationGeometry.CanStand(main.World.Navigation,foot)?foot:main.World.Navigation.Anchors["safe"];
        }
    }
    public override void _Ready()
    {
        main=(MainView)GetParent();
        try{menu=SceneBindings.Require<RestOptionsController>(main,"RestOptions");}
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public override void _Notification(int what)
    {
        if(what==NotificationApplicationFocusOut){focused=false;Suspend();}
        else if(what==NotificationApplicationFocusIn){focused=true;if(GetNode<GameSession>("/root/GameSession").Flow==FlowState.Field)Resume();}
    }
    public bool Begin(BenchView value)
    {
        if(!focused||IsActive||main.SoupSeat?.IsActive==true||HasMeta("binding_error")||main.World.HasMeta("binding_error")||
           !GodotObject.IsInstanceValid(value)||value.HasMeta("binding_error")||!value.IsInsideTree()||!main.World.IsAncestorOf(value)||!value.IsActive||!value.IsVisibleInTree()||
           GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field)return false;
        var from=new Position2(main.World.Player.Position.X,main.World.Player.Position.Y);var to=WorldPoint(value.StandAnchor.GlobalPosition);
        if(NavigationGeometry.Distance(from,to)>48)return false;
        var planned=ShortApproachPolicy.TryPlan(main.World.Navigation,from,to);if(planned==null)return false;
        bench=value;player=main.World.Player;art=player.GetNode<AnimatedSprite2D>("Artwork");path=planned;waypoint=1;approachSeconds=0;
        if(planned.Length==0)StartSit();else{approaching=true;Sync();}return true;
    }
    private void StartSit(){approaching=false;path=null;if(!state.TrySit()){Cancel();return;}Sync();}
    private void Detach()
    {
        generation++;if(art!=null&&GodotObject.IsInstanceValid(art)&&finished!=null)art.AnimationFinished-=finished;
        finished=null;boundFrames=null;
    }
    private void BindCompletion(SpriteFrames frames)
    {
        Detach();boundFrames=frames;boundPhase=Phase;
        if(Phase==RestPhase.Seated)return;
        var current=generation;var expected=Phase;var expectedArt=art!;
        finished=()=>{
            if(current!=generation||!IsActive||state.Suspended||!focused||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field||
               !GodotObject.IsInstanceValid(expectedArt)||Phase!=expected||expectedArt.SpriteFrames!=frames)return;
            state.AnimationFinished();Sync();
        };
        expectedArt.AnimationFinished+=finished;
    }
    public override void _PhysicsProcess(double delta)
    {
        if(!approaching||!focused||state.Suspended||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field)return;
        if(player==null||bench==null||path==null||!GodotObject.IsInstanceValid(player)||!GodotObject.IsInstanceValid(bench)||!main.World.IsAncestorOf(bench)){Cancel();return;}
        approachSeconds+=delta;if(approachSeconds>2){Cancel();return;}
        float distance=112*(float)delta;
        while(distance>0&&waypoint<path.Points.Count){
            var destination=path.Points[waypoint];var current=new Position2(player.Position.X,player.Position.Y);
            var next=player.Position.MoveToward(new(destination.X,destination.Y),distance);
            if(!NavigationGeometry.CanTraverse(main.World.Navigation,current,destination)||!NavigationGeometry.CanTraverse(main.World.Navigation,current,new(next.X,next.Y))){Cancel();return;}
            distance-=player.Position.DistanceTo(next);player.Position=next;GetNode<GameSession>("/root/GameSession").UpdatePosition(new(next.X,next.Y));
            if(next.DistanceTo(new(destination.X,destination.Y))<.001f)waypoint++;else break;
        }
        if(waypoint==path.Points.Count)StartSit();else Sync();
    }
    public bool HandleKey(Key key)
    {
        if(!IsActive||state.Suspended||!focused)return false;
        if(approaching){if(key==Key.Escape)Cancel();return key is Key.E or Key.Enter or Key.Escape;}
        if(key==Key.Escape){state.HandleEscape();Sync();return true;}
        if(key is Key.E or Key.Enter){if(menu.IsOpen)menu.ActivateFocused();else if(state.ReopenMenu())Sync();return true;}
        return false;
    }
    private void Choose(RestChoice choice)
    {
        if(!state.TryChoose(choice))return;
        menu.Close();Sync();if(choice==RestChoice.Rest&&bench!=null)main.ShowObservation(bench);
    }
    public void Suspend(){if(!IsActive)return;state.Suspend();Sync();}
    public void Resume(){if(!state.Suspended||!focused)return;state.Resume();Sync();}
    public override void _Process(double delta)
    {
        if(!IsActive)return;
        if(!focused||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field){if(!state.Suspended)Suspend();else Sync();return;}
        if(state.Suspended)Resume();else Sync();
    }
    private void Sync()
    {
        if(!IsActive){Cancel();return;}
        if(player==null||bench==null||!GodotObject.IsInstanceValid(player)||!GodotObject.IsInstanceValid(bench)||!main.World.IsAncestorOf(bench)){Cancel();return;}
        if(approaching){var point=path!.Points[Math.Min(waypoint,path.Points.Count-1)];player.SetApproachPose(new Vector2(point.X,point.Y)-player.Position,state.Suspended);return;}
        var frames=GetNode<GameSession>("/root/GameSession").Options.ReducedMotion?player.ReducedRestFrames:player.RestFrames;
        if(boundFrames!=frames||boundPhase!=Phase)BindCompletion(frames);
        var animation=Phase switch{RestPhase.SittingDown=>"sit_down",RestPhase.Seated=>"seated",RestPhase.Smoking=>"smoke",_=>"stand_up"};
        player.SetRestPose(animation,bench.SeatAnchor.GlobalPosition-player.GlobalPosition,state.Suspended);
        bench.GetNode<Sprite2D>("BenchForeground").Visible=true;
        if(state.MenuOpen&&!state.Suspended)menu.Open(Choose);else if(menu.IsOpen)menu.Close();
    }
    public void Cancel()
    {
        approaching=false;path=null;state.Cancel();Detach();
        if(player!=null&&GodotObject.IsInstanceValid(player))player.ClearRestPose();
        if(bench!=null&&GodotObject.IsInstanceValid(bench)&&bench.GetNodeOrNull<Sprite2D>("BenchForeground") is {} foreground)foreground.Visible=false;
        if(menu!=null&&GodotObject.IsInstanceValid(menu))menu.Close();
        art=null;player=null;bench=null;
    }
    public override void _ExitTree()=>Cancel();
}
