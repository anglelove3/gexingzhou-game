using Godot;
using GeXingzhou.Domain;

public partial class SoupSeatController : Node
{
    private MainView main=null!;private PlayerController? player;private AnimatedSprite2D? art;
    private Marker2D? seat,stand;private Sprite2D? front;private Node2D? props;
    private Action? finished,approached;private int generation;private string pose="";private bool suspended;
    public bool IsActive=>player!=null;
    public bool IsApproaching=>IsActive&&pose=="approach";
    public bool IsActing=>IsActive&&pose!="seated";
    public Position2 SavePosition=>!IsApproaching&&stand!=null&&GodotObject.IsInstanceValid(stand)?WorldPoint(stand.GlobalPosition):new(main.World.Player.Position.X,main.World.Player.Position.Y);
    private Position2 WorldPoint(Vector2 point){var local=main.World.ToLocal(point);return new(local.X,local.Y);}
    public void Configure(MainView owner)=>main=owner;
    public bool Begin(Action seated)
    {
        if(IsActive||main.Rest.IsActive||main.World.SceneId!="soup_shop"||main.World.HasMeta("binding_error")||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field)return false;
        try
        {
            seat=SceneBindings.Require<Marker2D>(main.World,"DepthLayers/Props/Chairs/LeftChair/SeatSurface");stand=main.World.GetAnchor("stand");
            front=SceneBindings.Require<Sprite2D>(main.World,"Foreground/TableEdge");props=SceneBindings.Require<Node2D>(main.World,"ActionProps");
            if(front.Texture==null||!seat.Position.IsFinite()||!stand.Position.IsFinite()||!NavigationGeometry.CanStand(main.World.Navigation,new(stand.Position.X,stand.Position.Y)))throw new InvalidOperationException("汤店座位图像或站位基准无效。");
            var foot=new Position2(main.World.Player.Position.X,main.World.Player.Position.Y);var destination=WorldPoint(stand.GlobalPosition);
            if(NavigationGeometry.Distance(foot,destination)>40||!NavigationGeometry.CanTraverse(main.World.Navigation,foot,destination))return false;
            player=main.World.Player;art=player.GetNode<AnimatedSprite2D>("Artwork");generation++;approached=seated;
            if(NavigationGeometry.Distance(foot,destination)<.01f)StartSit();else{pose="approach";Sync();}return true;
        }
        catch(InvalidOperationException ex){Cancel();SceneBindings.ReportFailure(this,ex.Message);return false;}
    }
    private void StartSit(){front!.Visible=true;var callback=approached;approached=null;StartPose("sit_down",()=>{SetSeated();callback?.Invoke();});}
    public override void _PhysicsProcess(double delta)
    {
        if(!IsApproaching||suspended||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field)return;
        var current=new Position2(player!.Position.X,player.Position.Y);var destination=WorldPoint(stand!.GlobalPosition);
        var next=player.Position.MoveToward(new(destination.X,destination.Y),112*(float)delta);
        if(!NavigationGeometry.CanTraverse(main.World.Navigation,current,destination)||!NavigationGeometry.CanTraverse(main.World.Navigation,current,new(next.X,next.Y)))
        {Cancel();main.ShowNotice("桌边","这边暂时过不去，换个位置再坐吧。");return;}
        player.Position=next;GetNode<GameSession>("/root/GameSession").UpdatePosition(new(next.X,next.Y));
        if(next.DistanceTo(new(destination.X,destination.Y))<.01f)StartSit();
    }
    public bool PlayAction(string code,Action completed)
    {
        if(!IsActive||IsActing||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field||code is not ("eat" or "set_chopsticks" or "check_phone"))return false;
        suspended=false;
        main.Audio.PlayCue(code=="eat"?AudioCue.Bowl:code=="set_chopsticks"?AudioCue.Chopsticks:AudioCue.PhoneMessage);
        StartPose(code,()=>{SetSeated();completed();});return true;
    }
    private void StartPose(string name,Action done)
    {
        Detach();pose=name;var token=generation;var expectedArt=art!;
        finished=()=>
        {
            if(token!=generation||!IsActive||suspended||!GodotObject.IsInstanceValid(expectedArt)||expectedArt.Animation!=name||pose!=name)return;
            Detach();done();
        };
        expectedArt.AnimationFinished+=finished;Sync();
    }
    private void SetSeated(){pose="seated";Sync();}
    public void Stand(Action? completed=null)
    {
        if(!IsActive||IsActing||suspended)return;
        main.Choices.Close();StartPose("stand_up",()=>{Cancel();completed?.Invoke();});
    }
    public bool HandleKey(Key key)
    {
        if(!IsActive||suspended)return false;
        if(key==Key.Escape){Stand();return true;}
        if(key is Key.E or Key.Enter){if(!IsActing&&main.World.GetTarget("seat") is {} target)main.HandleInteraction(target);return true;}
        return false;
    }
    public void Suspend(){if(IsActive){suspended=true;Sync();}}
    public void Resume(){if(IsActive){suspended=false;Sync();}}
    public override void _Process(double delta)
    {
        if(!IsActive)return;
        if(!GodotObject.IsInstanceValid(player)||!GodotObject.IsInstanceValid(seat)||!GodotObject.IsInstanceValid(front)){Cancel();return;}
        suspended=GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field;Sync();
    }
    private void Sync()
    {
        if(player==null||seat==null)return;
        if(IsApproaching){player.SetApproachPose(stand!.GlobalPosition-player.GlobalPosition,suspended);return;}
        player.SetSoupPose(pose,seat.GlobalPosition-player.GlobalPosition,suspended);
        if(props!=null)
        {
            foreach(var child in props.GetChildren().OfType<CanvasItem>())child.Visible=false;
            var name=pose=="check_phone"?"PhoneGlow":pose=="set_chopsticks"?"Chopsticks":"BowlSteam";
            if(props.GetNodeOrNull<Node2D>(name) is {} prop)
            {
                prop.Visible=IsActing&&pose is not ("sit_down" or "stand_up")&&
                    (pose!="set_chopsticks"||art!.Frame==art.SpriteFrames.GetFrameCount(pose)-1)&&
                    (pose!="check_phone"||art!.Frame==1);
                var texture=art!.SpriteFrames.GetFrameTexture(pose,art.Frame);
                if(texture is AtlasTexture frame&&frame.HasMeta("action_contact"))
                    prop.Position=props.ToLocal(art.ToGlobal(frame.GetMeta("action_contact").AsVector2()-frame.GetSize()/2));
            }
        }
    }
    private void Detach(){if(art!=null&&GodotObject.IsInstanceValid(art)&&finished!=null)art.AnimationFinished-=finished;finished=null;}
    public void Cancel()
    {
        generation++;Detach();
        if(player!=null&&GodotObject.IsInstanceValid(player)){if(!IsApproaching&&stand!=null&&GodotObject.IsInstanceValid(stand))player.GlobalPosition=stand.GlobalPosition;player.ClearSoupPose();}
        if(front!=null&&GodotObject.IsInstanceValid(front))front.Visible=false;
        if(props!=null&&GodotObject.IsInstanceValid(props))foreach(var child in props.GetChildren().OfType<CanvasItem>())child.Visible=false;
        player=null;art=null;seat=null;stand=null;front=null;props=null;pose="";suspended=false;approached=null;
    }
    public override void _ExitTree()=>Cancel();
}
