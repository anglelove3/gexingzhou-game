using Godot;
using GeXingzhou.Domain;

public partial class SoupSeatController : Node
{
    private MainView main=null!;private PlayerController? player;private AnimatedSprite2D? art;
    private Marker2D? seat,stand;private Sprite2D? front;private Node2D? props;
    private Action? finished;private int generation;private string pose="";private bool suspended;
    public bool IsActive=>player!=null;
    public bool IsActing=>IsActive&&pose!="seated";
    public Position2 SavePosition=>stand!=null&&GodotObject.IsInstanceValid(stand)?new(stand.GlobalPosition.X,stand.GlobalPosition.Y):new(main.World.Player.Position.X,main.World.Player.Position.Y);
    public void Configure(MainView owner)=>main=owner;
    public bool Begin(Action seated)
    {
        if(IsActive||main.Rest.IsActive||main.World.SceneId!="soup_shop"||main.World.HasMeta("binding_error")||GetNode<GameSession>("/root/GameSession").Flow!=FlowState.Field)return false;
        try
        {
            seat=SceneBindings.Require<Marker2D>(main.World,"SeatAnchor");stand=SceneBindings.Require<Marker2D>(main.World,"StandAnchor");
            front=SceneBindings.Require<Sprite2D>(main.World,"TableForeground");props=SceneBindings.Require<Node2D>(main.World,"ActionProps");
            if(front.Texture==null||!seat.Position.IsFinite()||!stand.Position.IsFinite()||stand.Position.Y!=280)throw new InvalidOperationException("汤店座位图像或站位基准无效。");
            player=main.World.Player;art=player.GetNode<AnimatedSprite2D>("Artwork");player.GlobalPosition=stand.GlobalPosition;generation++;front.Visible=true;
            StartPose("sit_down",()=>{SetSeated();seated();});return true;
        }
        catch(InvalidOperationException ex){Cancel();SceneBindings.ReportFailure(this,ex.Message);return false;}
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
        player.SetSoupPose(pose,seat.GlobalPosition-player.GlobalPosition,suspended);
        if(props!=null)
        {
            foreach(var child in props.GetChildren().OfType<CanvasItem>())child.Visible=false;
            if(props.GetNodeOrNull<CanvasItem>(pose=="check_phone"?"PhoneGlow":pose=="set_chopsticks"?"Chopsticks":"BowlSteam") is {} prop)prop.Visible=IsActing&&pose is not ("sit_down" or "stand_up");
        }
    }
    private void Detach(){if(art!=null&&GodotObject.IsInstanceValid(art)&&finished!=null)art.AnimationFinished-=finished;finished=null;}
    public void Cancel()
    {
        generation++;Detach();
        if(player!=null&&GodotObject.IsInstanceValid(player)){if(stand!=null&&GodotObject.IsInstanceValid(stand))player.GlobalPosition=stand.GlobalPosition;player.ClearSoupPose();}
        if(front!=null&&GodotObject.IsInstanceValid(front))front.Visible=false;
        if(props!=null&&GodotObject.IsInstanceValid(props))foreach(var child in props.GetChildren().OfType<CanvasItem>())child.Visible=false;
        player=null;art=null;seat=null;stand=null;front=null;props=null;pose="";suspended=false;
    }
    public override void _ExitTree()=>Cancel();
}
