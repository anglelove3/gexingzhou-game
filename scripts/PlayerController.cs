using Godot;
using GeXingzhou.Domain;
public partial class PlayerController : CharacterBody2D
{
    private bool locked; private AnimatedSprite2D artwork=null!;
    [Export] public SpriteFrames RestFrames {get;set;}=null!;
    [Export] public SpriteFrames ReducedRestFrames {get;set;}=null!;
    [Export] public SpriteFrames SoupFrames {get;set;}=null!;
    [Export] public SpriteFrames ReducedSoupFrames {get;set;}=null!;
    private bool soupActive;
    private MainView? owner;private float stepDistance;
    [Export] public float RestStandingPixels {get;set;}=307;
    private SpriteFrames walkingFrames=null!;private bool restActive;private Vector2 seatOffset;
    public override void _Ready()
    {
        try
        {
            for(Node? parent=GetParent();parent!=null;parent=parent.GetParent())if(parent is MainView main){owner=main;break;}
            SceneBindings.Require<CollisionShape2D>(this,"CollisionShape2D");
            SceneBindings.Require<Camera2D>(this,"Camera2D");
            artwork=SceneBindings.Require<AnimatedSprite2D>(this,"Artwork");
            if(artwork.SpriteFrames==null||!artwork.SpriteFrames.HasAnimation("idle")||!artwork.SpriteFrames.HasAnimation("walk"))
                throw new InvalidOperationException("主角缺少待机/行走动画资源。");
            ValidateRestFrames(RestFrames,"休息");ValidateRestFrames(ReducedRestFrames,"减少动效休息");
            ValidateSoupFrames(SoupFrames);ValidateSoupFrames(ReducedSoupFrames);
            if(!float.IsFinite(RestStandingPixels)||RestStandingPixels<=0)
                throw new InvalidOperationException("休息动作站立基准必须大于零。");
            artwork.FrameChanged+=AnchorArtwork;artwork.AnimationChanged+=AnchorArtwork;
            walkingFrames=artwork.SpriteFrames;artwork.Play("idle");AnchorArtwork();
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public void SetInputLocked(bool value) {locked=value;if(value)Velocity=Vector2.Zero;}
    private static void ValidateRestFrames(SpriteFrames? frames,string label)
    {
        foreach(var (name,count) in new[]{("sit_down",3),("seated",1),("smoke",6),("stand_up",3)})
        {
            if(frames==null||!frames.HasAnimation(name)||frames.GetFrameCount(name)<count||
               (name!="seated"&&frames.GetAnimationLoopMode(name)!=SpriteFrames.LoopMode.None)||
               !double.IsFinite(frames.GetAnimationSpeed(name))||frames.GetAnimationSpeed(name)<=0)
                throw new InvalidOperationException($"主角{label}资源缺失或动作无效：{name}。");
            for(int i=0;i<frames.GetFrameCount(name);i++)
            {
                if(frames.GetFrameTexture(name,i) is not AtlasTexture atlas||atlas.Atlas==null||
                   !atlas.HasMeta("seat_pivot")||!atlas.HasMeta("stand_pivot")||
                   atlas.GetMeta("seat_pivot").VariantType!=Variant.Type.Vector2||atlas.GetMeta("stand_pivot").VariantType!=Variant.Type.Vector2||
                   !atlas.GetMeta("seat_pivot").AsVector2().IsFinite()||!atlas.GetMeta("stand_pivot").AsVector2().IsFinite())
                    throw new InvalidOperationException($"主角{label}动作缺少有效图像或坐姿基准：{name}/{i}。");
            }
        }
    }
    public void SetRestPose(string animation,Vector2 visualOffset,bool paused)
    {
        soupActive=false;
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
        restActive=false;soupActive=false;artwork.SpriteFrames=walkingFrames;artwork.Play("idle");AnchorArtwork();
    }
    private static void ValidateSoupFrames(SpriteFrames? frames)
    {
        foreach(var name in new[]{"sit_down","seated","eat","set_chopsticks","check_phone","stand_up"})
        {
            if(frames==null||!frames.HasAnimation(name)||frames.GetFrameCount(name)<(name=="seated"?1:3)||
                (name!="seated"&&frames.GetAnimationLoopMode(name)!=SpriteFrames.LoopMode.None)||!double.IsFinite(frames.GetAnimationSpeed(name))||frames.GetAnimationSpeed(name)<=0)
                throw new InvalidOperationException("主角汤店动作资源无效："+name);
            for(int i=0;i<frames.GetFrameCount(name);i++)
            {
                if(frames.GetFrameTexture(name,i) is not AtlasTexture atlas||atlas.Atlas==null||
                    !atlas.HasMeta("seat_pivot")||!atlas.HasMeta("stand_pivot")||!atlas.HasMeta("pose_scale")||
                    atlas.GetMeta("seat_pivot").VariantType!=Variant.Type.Vector2||atlas.GetMeta("stand_pivot").VariantType!=Variant.Type.Vector2||
                    !atlas.GetMeta("seat_pivot").AsVector2().IsFinite()||!atlas.GetMeta("stand_pivot").AsVector2().IsFinite()||
                    !double.IsFinite(atlas.GetMeta("pose_scale").AsDouble())||atlas.GetMeta("pose_scale").AsDouble()<=0)
                    throw new InvalidOperationException("汤店动作缺少有效图像基准："+name+"/"+i);
            }
        }
    }
    public void SetSoupPose(string animation,Vector2 visualOffset,bool paused)
    {
        soupActive=true;restActive=true;seatOffset=visualOffset;Velocity=Vector2.Zero;artwork.FlipH=false;
        var frames=GetNode<GameSession>("/root/GameSession").Options.ReducedMotion?ReducedSoupFrames:SoupFrames;
        if(artwork.SpriteFrames!=frames){artwork.SpriteFrames=frames;artwork.Animation=animation;artwork.Frame=0;}
        if(artwork.Animation!=animation){artwork.Play(animation);artwork.Frame=0;}
        artwork.SpeedScale=1;if(paused)artwork.Pause();else if(!artwork.IsPlaying())artwork.Play(animation);AnchorArtwork();
    }
    public void ClearSoupPose()=>ClearRestPose();
    public override void _PhysicsProcess(double delta)
    {
        var session=GetNode<GameSession>("/root/GameSession");
        float axis=(Input.IsPhysicalKeyPressed(Key.D)||Input.IsPhysicalKeyPressed(Key.Right)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)||Input.IsPhysicalKeyPressed(Key.Left)?1:0);
        var p=session.Catalog!.Parameters;
        float vx=MovementModel.Step(Velocity.X,axis,Input.IsPhysicalKeyPressed(Key.Shift),locked||restActive||(session.Flow!=FlowState.Field), (float)delta,(float)p["move.walk_speed"],(float)p["move.run_speed"],(float)p["move.acceleration"],(float)p["move.deceleration"]);
        var before=Position;Velocity=new Vector2(vx,0);MoveAndSlide();
        if(!locked&&!restActive&&session.Flow==FlowState.Field)
        {
            stepDistance+=Math.Abs(Position.X-before.X);
            while(stepDistance>=32){stepDistance-=32;owner?.Audio?.PlayCue(AudioCue.Footstep);}
        }
        if(restActive)
        {
            MainView? main=null;for(Node? pnode=GetParent();pnode!=null;pnode=pnode.GetParent())if(pnode is MainView m){main=m;break;}
            session.UpdatePosition(main?.SafeSavePosition??new(Position.X,Position.Y));return;
        }
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
            float scaleRest=soupActive?(float)atlas.GetMeta("pose_scale").AsDouble():68f/RestStandingPixels;artwork.Scale=Vector2.One*scaleRest;
            float t=artwork.Animation=="sit_down"?(float)artwork.Frame/(artwork.SpriteFrames.GetFrameCount(artwork.Animation)-1):
                artwork.Animation=="stand_up"?1f-(float)artwork.Frame/(artwork.SpriteFrames.GetFrameCount(artwork.Animation)-1):1;
            var pivot=atlas.GetMeta("stand_pivot").AsVector2().Lerp(atlas.GetMeta("seat_pivot").AsVector2(),t);
            artwork.Position=seatOffset*t+(texture.GetSize()/2-pivot)*scaleRest;return;
        }
        float scale=68f/(texture.GetHeight()-4);artwork.Scale=Vector2.One*scale;
        artwork.Position=new Vector2(0,-(texture.GetHeight()/2f-2)*scale);
    }
}
