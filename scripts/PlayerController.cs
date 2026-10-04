using Godot;
using GeXingzhou.Domain;
public partial class PlayerController : CharacterBody2D
{
    private bool locked; private AnimatedSprite2D artwork=null!;
    [Export] public SpriteFrames RestFrames {get;set;}=null!;
    [Export] public SpriteFrames ReducedRestFrames {get;set;}=null!;
    [Export] public float RestStandingPixels {get;set;}=307;
    private SpriteFrames walkingFrames=null!;private bool restActive;private Vector2 seatOffset;
    public override void _Ready()
    {
        try
        {
            SceneBindings.Require<CollisionShape2D>(this,"CollisionShape2D");
            SceneBindings.Require<Camera2D>(this,"Camera2D");
            artwork=SceneBindings.Require<AnimatedSprite2D>(this,"Artwork");
            if(artwork.SpriteFrames==null||!artwork.SpriteFrames.HasAnimation("idle")||!artwork.SpriteFrames.HasAnimation("walk"))
                throw new InvalidOperationException("主角缺少待机/行走动画资源。");
            artwork.FrameChanged+=AnchorArtwork;artwork.AnimationChanged+=AnchorArtwork;
            walkingFrames=artwork.SpriteFrames;artwork.Play("idle");AnchorArtwork();
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public void SetInputLocked(bool value) {locked=value;if(value)Velocity=Vector2.Zero;}
    public void SetRestPose(string animation,Vector2 visualOffset,bool paused)
    {
        restActive=true;seatOffset=visualOffset;Velocity=Vector2.Zero;artwork.FlipH=false;
        var frames=GetNode<GameSession>("/root/GameSession").Options.ReducedMotion?ReducedRestFrames:RestFrames;
        if(artwork.SpriteFrames!=frames){artwork.SpriteFrames=frames;artwork.Animation=animation;artwork.Frame=0;}
        if(artwork.Animation!=animation){artwork.Play(animation);artwork.Frame=0;}
        artwork.SpeedScale=1;
        if(paused)artwork.Pause();else if(!artwork.IsPlaying())artwork.Play(animation);
        AnchorArtwork();
    }
    public void ClearRestPose()
    {
        restActive=false;artwork.SpriteFrames=walkingFrames;artwork.Play("idle");AnchorArtwork();
    }
    public override void _PhysicsProcess(double delta)
    {
        var session=GetNode<GameSession>("/root/GameSession");
        float axis=(Input.IsPhysicalKeyPressed(Key.D)||Input.IsPhysicalKeyPressed(Key.Right)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)||Input.IsPhysicalKeyPressed(Key.Left)?1:0);
        var p=session.Catalog!.Parameters;
        float vx=MovementModel.Step(Velocity.X,axis,Input.IsPhysicalKeyPressed(Key.Shift),locked||restActive||(session.Flow!=FlowState.Field), (float)delta,(float)p["move.walk_speed"],(float)p["move.run_speed"],(float)p["move.acceleration"],(float)p["move.deceleration"]);
        Velocity=new Vector2(vx,0);MoveAndSlide();
        if(restActive){session.UpdatePosition(new(Position.X,Position.Y));return;}
        if(axis!=0&&!locked&&session.Flow==FlowState.Field)artwork.FlipH=axis<0;
        bool walking=Math.Abs(Velocity.X)>1&&!locked&&session.Flow==FlowState.Field;
        artwork.SpeedScale=walking?Math.Clamp(Math.Abs(Velocity.X)/120f,.6f,1.6f):1;
        var animation=walking?"walk":"idle";if(artwork.Animation!=animation)artwork.Play(animation);
        if(session.Options.ReducedMotion&&!walking){artwork.Stop();artwork.Frame=0;}else if(!artwork.IsPlaying())artwork.Play(animation);
        session.UpdatePosition(new(Position.X,Position.Y));
    }
    private void AnchorArtwork()
    {
        var texture=artwork.SpriteFrames.GetFrameTexture(artwork.Animation,artwork.Frame);
        if(restActive&&texture is AtlasTexture atlas)
        {
            float scaleRest=68f/RestStandingPixels;artwork.Scale=Vector2.One*scaleRest;
            float t=artwork.Animation=="sit_down"?(float)artwork.Frame/(artwork.SpriteFrames.GetFrameCount(artwork.Animation)-1):
                artwork.Animation=="stand_up"?1f-(float)artwork.Frame/(artwork.SpriteFrames.GetFrameCount(artwork.Animation)-1):1;
            var pivot=atlas.GetMeta("stand_pivot").AsVector2().Lerp(atlas.GetMeta("seat_pivot").AsVector2(),t);
            artwork.Position=seatOffset*t+(texture.GetSize()/2-pivot)*scaleRest;return;
        }
        float scale=68f/(texture.GetHeight()-4);artwork.Scale=Vector2.One*scale;
        artwork.Position=new Vector2(0,-(texture.GetHeight()/2f-2)*scale);
    }
}
