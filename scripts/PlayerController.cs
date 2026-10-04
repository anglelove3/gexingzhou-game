using Godot;
using GeXingzhou.Domain;
public partial class PlayerController : CharacterBody2D
{
    private bool locked; private AnimatedSprite2D artwork=null!;
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
            artwork.Play("idle");AnchorArtwork();
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public void SetInputLocked(bool value) {locked=value;if(value)Velocity=Vector2.Zero;}
    public override void _PhysicsProcess(double delta)
    {
        var session=GetNode<GameSession>("/root/GameSession");
        float axis=(Input.IsPhysicalKeyPressed(Key.D)||Input.IsPhysicalKeyPressed(Key.Right)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)||Input.IsPhysicalKeyPressed(Key.Left)?1:0);
        var p=session.Catalog!.Parameters;
        float vx=MovementModel.Step(Velocity.X,axis,Input.IsPhysicalKeyPressed(Key.Shift),locked||(session.Flow!=FlowState.Field), (float)delta,(float)p["move.walk_speed"],(float)p["move.run_speed"],(float)p["move.acceleration"],(float)p["move.deceleration"]);
        Velocity=new Vector2(vx,0);MoveAndSlide();
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
        float scale=68f/(texture.GetHeight()-4);artwork.Scale=Vector2.One*scale;
        artwork.Position=new Vector2(0,-(texture.GetHeight()/2f-2)*scale);
    }
}
