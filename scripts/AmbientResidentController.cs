using Godot;
using GeXingzhou.Domain;

// Ambient residents bind only authored scene nodes. They never dispatch story or save actions.
public partial class AmbientResidentController : Node2D
{
    [Export] public float StepSpeed {get;set;}=8;
    [Export] public double PauseSeconds {get;set;}=4;
    [Export] public float IdlePhase {get;set;}
    private Sprite2D artwork=null!;
    private Vector2 origin,artOrigin,route;
    private double elapsed,pause;
    private bool returning;
    private bool configured,focused=true;
    public override void _Ready()
    {
        try{
            artwork=SceneBindings.Require<Sprite2D>(this,"Artwork");
            route=SceneBindings.Require<Marker2D>(this,"Waypoint").Position;
            origin=Position;artOrigin=artwork.Position;pause=PauseSeconds*.25;
        }catch(InvalidOperationException error){SceneBindings.ReportFailure(this,error.Message);}
    }
    // Static route validation also works before a candidate world enters the tree.
    public void ValidateRoute(WorldView world)
    {
        if(SceneBindings.Require<Sprite2D>(this,"Artwork").Texture==null)
            throw new InvalidOperationException("背景居民图像缺失："+Name);
        var start=world.ToLocal(ToGlobal(Vector2.Zero));
        var end=world.ToLocal(ToGlobal(SceneBindings.Require<Marker2D>(this,"Waypoint").Position));
        var nav=world.Navigation??NavigationSceneReader.Read(world);
        if(!float.IsFinite(StepSpeed)||StepSpeed<=0||!double.IsFinite(PauseSeconds)||PauseSeconds<0||
            !NavigationGeometry.CanStand(nav,new(start.X,start.Y))||!NavigationGeometry.CanStand(nav,new(end.X,end.Y))||
            !NavigationGeometry.CanTraverse(nav,new(start.X,start.Y),new(end.X,end.Y)))
            throw new InvalidOperationException("背景居民路线不可通行："+Name);
    }
    public void Configure(WorldView world){ValidateRoute(world);focused=GetNode<GameSession>("/root/GameSession").ApplicationFocused;configured=true;}
    public override void _Notification(int what)
    {
        if(what==NotificationApplicationFocusOut)focused=false;
        else if(what==NotificationApplicationFocusIn)focused=true;
    }
    public override void _Process(double delta)
    {
        if(!configured||!focused||artwork==null||HasMeta("binding_error"))return;
        var session=GetNode<GameSession>("/root/GameSession");
        if(session.Flow!=FlowState.Field||session.Options.ReducedMotion)return;
        elapsed+=delta;bool moving=false;
        if(pause>0)pause=Math.Max(0,pause-delta);
        else{
            var destination=origin+(returning?Vector2.Zero:route);
            var direction=destination-Position;
            if(direction.Length()>.05f){Position=Position.MoveToward(destination,StepSpeed*(float)delta);artwork.FlipH=direction.X<0;moving=true;}
            else{returning=!returning;pause=PauseSeconds;}
        }
        artwork.Position=artOrigin+new Vector2(0,(float)Math.Sin(elapsed*(moving?7:1.7)+IdlePhase)*(moving?.45f:.22f));
        artwork.Rotation=(float)Math.Sin(elapsed*1.4+IdlePhase)*.003f;
    }
}
