using Godot;
using GeXingzhou.Domain;

// Ambient residents bind only authored scene nodes. They never dispatch story or save actions.
public partial class AmbientResidentController : Node2D
{
    [Export] public float StepSpeed {get;set;}=28;
    [Export] public double PauseSeconds {get;set;}=4;
    [Export] public float IdlePhase {get;set;}
    [Export] public SpriteFrames WalkFrames {get;set;}=null!;
    [Export] public float WorldHeight {get;set;}=92;
    [Export] public float StrideLength {get;set;}=32;
    private Sprite2D artwork=null!;
    private Vector2 origin,route;
    private double pause;
    private float walked;
    private bool returning;
    private bool configured,focused=true;
    public override void _Ready()
    {
        try{
            artwork=SceneBindings.Require<Sprite2D>(this,"Artwork");
            route=SceneBindings.Require<Marker2D>(this,"Waypoint").Position;
            origin=Position;pause=PauseSeconds*(.25+(IdlePhase%1)*.2);
            ValidateFrames();ShowPose(false);
        }catch(InvalidOperationException error){SceneBindings.ReportFailure(this,error.Message);}
    }
    // Static route validation also works before a candidate world enters the tree.
    public void ValidateRoute(WorldView world)
    {
        if(SceneBindings.Require<Sprite2D>(this,"Artwork").Texture==null)
            throw new InvalidOperationException("背景居民图像缺失："+Name);
        ValidateFrames();
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
        bool moving=false;
        if(pause>0)pause=Math.Max(0,pause-delta);
        else{
            var destination=origin+(returning?Vector2.Zero:route);
            var direction=destination-Position;
            if(direction.Length()>.05f){var before=Position;Position=Position.MoveToward(destination,StepSpeed*(float)delta);walked+=Position.DistanceTo(before);artwork.FlipH=direction.X<0;moving=true;}
            else{returning=!returning;pause=PauseSeconds;}
        }
        ShowPose(moving);
    }
    private void ValidateFrames()
    {
        if(!float.IsFinite(WorldHeight)||WorldHeight<=0||!float.IsFinite(StrideLength)||StrideLength<=0)
            throw new InvalidOperationException("居民比例或步幅无效："+Name);
        foreach(var animation in new[]{"idle","walk"}){
            if(WalkFrames==null||!WalkFrames.HasAnimation(animation)||WalkFrames.GetFrameCount(animation)<(animation=="walk"?8:1))
                throw new InvalidOperationException("居民缺少完整待机/步态："+Name);
            for(int i=0;i<WalkFrames.GetFrameCount(animation);i++){
                if(WalkFrames.GetFrameTexture(animation,i) is not AtlasTexture frame||frame.Atlas==null||
                    !frame.HasMeta("foot_pivot")||frame.GetMeta("foot_pivot").VariantType!=Variant.Type.Vector2||
                    !frame.GetMeta("foot_pivot").AsVector2().IsFinite()||!frame.HasMeta("body_height")||
                    !double.IsFinite(frame.GetMeta("body_height").AsDouble())||frame.GetMeta("body_height").AsDouble()<=0)
                    throw new InvalidOperationException("居民步态缺少脚底基准："+Name);
            }
        }
    }
    private void ShowPose(bool moving)
    {
        var animation=moving?"walk":"idle";
        var index=moving?(int)(walked/StrideLength*WalkFrames.GetFrameCount(animation))%WalkFrames.GetFrameCount(animation):0;
        var frame=(AtlasTexture)WalkFrames.GetFrameTexture(animation,index);
        var scale=WorldHeight/(float)frame.GetMeta("body_height").AsDouble();
        var offset=(frame.GetSize()/2-frame.GetMeta("foot_pivot").AsVector2())*scale;
        if(artwork.FlipH)offset.X=-offset.X;
        artwork.Texture=frame;artwork.Scale=Vector2.One*scale;artwork.Position=offset;artwork.Rotation=0;
    }
}
