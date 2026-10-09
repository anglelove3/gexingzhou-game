using Godot;
using GeXingzhou.Domain;
public partial class PlayerController : CharacterBody2D
{
    private bool locked; private AnimatedSprite2D artwork=null!;
    [Export] public SpriteFrames RestFrames {get;set;}=null!;
    [Export] public SpriteFrames ReducedRestFrames {get;set;}=null!;
    [Export] public SpriteFrames SoupFrames {get;set;}=null!;
    [Export] public SpriteFrames ReducedSoupFrames {get;set;}=null!;
    private bool soupActive,approachActive;
    public string Facing {get;private set;}="side";
    private MainView? owner;private float stepDistance;
    [Export] public float RestStandingPixels {get;set;}=307;
    private SpriteFrames walkingFrames=null!;private bool restActive;private Vector2 seatOffset;
    [Export] public SpriteFrames DepthFrames {get;set;}=null!;
    private NavigationProfile? navigation;private bool focused=true;
    private readonly HashSet<string> suppressed=new();
    private static readonly string[] MovementActions={"move_left","move_right","move_up","move_down","move_fast"};
    public void ApplyNavigation(NavigationProfile profile)
    {
        navigation=profile;bool depth=profile.Mode==WorldMode.Depth2D;
        SceneBindings.Require<CollisionShape2D>(this,"CollisionShape2D").Disabled=depth;
        SceneBindings.Require<CollisionShape2D>(this,"FootCollisionShape2D").Disabled=!depth;
        if(depth)
        {
            foreach(var name in new[]{"idle_front","idle_back","idle_side","walk_front","walk_back","walk_side"})
                if(DepthFrames==null||!DepthFrames.HasAnimation(name)||DepthFrames.GetFrameCount(name)==0)
                    throw new InvalidOperationException("二维朝向资源缺失："+name);
            walkingFrames=DepthFrames;artwork.SpriteFrames=walkingFrames;Facing="side";artwork.Play("idle_side");AnchorArtwork();
        }
    }
    private void SuppressHeld(){foreach(var name in MovementActions)if(Input.IsActionPressed(name))suppressed.Add(name);}
    private float AxisStrength(string name)
    {
        if(!Input.IsActionPressed(name)){suppressed.Remove(name);return 0;}
        return suppressed.Contains(name)?0:Input.GetActionStrength(name);
    }
    public override void _Notification(int what)
    {
        if(what==NotificationApplicationFocusOut){focused=false;SuppressHeld();Velocity=Vector2.Zero;}
        else if(what==NotificationApplicationFocusIn){focused=true;SuppressHeld();}
    }
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
    public void SetInputLocked(bool value) {locked=value;if(value){SuppressHeld();Velocity=Vector2.Zero;}}
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
        restActive=false;soupActive=false;approachActive=false;locked=false;SuppressHeld();artwork.SpriteFrames=walkingFrames;
        artwork.Play(navigation?.Mode==WorldMode.Depth2D?"idle_"+Facing:"idle");AnchorArtwork();
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
        approachActive=false;soupActive=true;restActive=true;seatOffset=visualOffset;Velocity=Vector2.Zero;artwork.FlipH=false;
        var frames=GetNode<GameSession>("/root/GameSession").Options.ReducedMotion?ReducedSoupFrames:SoupFrames;
        if(artwork.SpriteFrames!=frames){artwork.SpriteFrames=frames;artwork.Animation=animation;artwork.Frame=0;}
        if(artwork.Animation!=animation){artwork.Play(animation);artwork.Frame=0;}
        artwork.SpeedScale=1;if(paused)artwork.Pause();else if(!artwork.IsPlaying())artwork.Play(animation);AnchorArtwork();
    }
    public void ClearSoupPose()=>ClearRestPose();
    public void SetApproachPose(Vector2 direction,bool paused)
    {
        approachActive=true;locked=true;restActive=false;Velocity=Vector2.Zero;
        Facing=Math.Abs(direction.X)>=Math.Abs(direction.Y)?"side":direction.Y<0?"back":"front";
        artwork.FlipH=Facing=="side"&&direction.X<0;artwork.SpriteFrames=walkingFrames;
        var name="walk_"+Facing;if(artwork.Animation!=name)artwork.Play(name);
        if(paused)artwork.Pause();else if(!artwork.IsPlaying())artwork.Play(name);AnchorArtwork();
    }
    public override void _PhysicsProcess(double delta)
    {
        var session=GetNode<GameSession>("/root/GameSession");
        if(approachActive){SuppressHeld();Velocity=Vector2.Zero;return;}
        float axis=(Input.IsPhysicalKeyPressed(Key.D)||Input.IsPhysicalKeyPressed(Key.Right)?1:0)-(Input.IsPhysicalKeyPressed(Key.A)||Input.IsPhysicalKeyPressed(Key.Left)?1:0);
        var p=session.Catalog!.Parameters;bool depth=navigation?.Mode==WorldMode.Depth2D;
        bool blocked=locked||restActive||session.Flow!=FlowState.Field||!focused;
        var before=Position;float vertical=0;
        if(depth)
        {
            if(blocked)SuppressHeld();
            axis=AxisStrength("move_right")-AxisStrength("move_left");vertical=AxisStrength("move_down")-AxisStrength("move_up");
            var velocity=Movement2DModel.Step(new(Velocity.X,Velocity.Y),new(axis,vertical),AxisStrength("move_fast")>0,blocked,(float)delta);
            Velocity=new(velocity.X,velocity.Y);
        }
        else
        {
            float vx=MovementModel.Step(Velocity.X,axis,Input.IsPhysicalKeyPressed(Key.Shift),blocked,(float)delta,(float)p["move.walk_speed"],(float)p["move.run_speed"],(float)p["move.acceleration"],(float)p["move.deceleration"]);
            Velocity=new(vx,0);
        }
        MoveAndSlide();
        if(!locked&&!restActive&&session.Flow==FlowState.Field)
        {
            stepDistance+=depth?Position.DistanceTo(before):Math.Abs(Position.X-before.X);
            while(stepDistance>=32){stepDistance-=32;owner?.Audio?.PlayCue(AudioCue.Footstep);}
        }
        if(restActive)
        {
            MainView? main=null;for(Node? pnode=GetParent();pnode!=null;pnode=pnode.GetParent())if(pnode is MainView m){main=m;break;}
            if(session.Snapshot.SceneId==navigation?.SceneId)session.UpdatePosition(main?.SafeSavePosition??new(Position.X,Position.Y));return;
        }
        if(!blocked&&(axis!=0||vertical!=0))
        {
            if(depth){Facing=Math.Abs(axis)>Math.Abs(vertical)?"side":vertical<0?"back":"front";artwork.FlipH=Facing=="side"&&axis<0;}
            else artwork.FlipH=axis<0;
        }
        bool walking=Velocity.Length()>1&&!blocked;
        artwork.SpeedScale=walking?Math.Clamp(Velocity.Length()/120f,.6f,1.6f):1;
        var animation=(walking?"walk":"idle")+(depth?"_"+Facing:"");if(artwork.Animation!=animation)artwork.Play(animation);
        if(session.Options.ReducedMotion&&!walking){artwork.Stop();artwork.Frame=0;}else if(!artwork.IsPlaying())artwork.Play(animation);
        if(session.Snapshot.SceneId==navigation?.SceneId)session.UpdatePosition(new(Position.X,Position.Y));
    }
    private void AnchorArtwork()
    {
        // SpriteFrames emits AnimationChanged while the previous pose name is being replaced.
        if(!artwork.SpriteFrames.HasAnimation(artwork.Animation))return;
        var texture=artwork.SpriteFrames.GetFrameTexture(artwork.Animation,artwork.Frame);
        if(texture==null)return;
        if(restActive&&texture is AtlasTexture atlas)
        {
            float scaleRest=soupActive?(float)atlas.GetMeta("pose_scale").AsDouble():68f/RestStandingPixels;artwork.Scale=Vector2.One*scaleRest;
            float t=artwork.Animation=="sit_down"?(float)artwork.Frame/(artwork.SpriteFrames.GetFrameCount(artwork.Animation)-1):
                artwork.Animation=="stand_up"?1f-(float)artwork.Frame/(artwork.SpriteFrames.GetFrameCount(artwork.Animation)-1):1;
            var pivot=atlas.GetMeta("stand_pivot").AsVector2().Lerp(atlas.GetMeta("seat_pivot").AsVector2(),t);
            artwork.Position=seatOffset*t+(texture.GetSize()/2-pivot)*scaleRest;return;
        }
        if(navigation?.Mode==WorldMode.Depth2D&&texture is AtlasTexture footFrame&&footFrame.HasMeta("foot_pivot"))
        {
            float depthScale=96f/400;artwork.Scale=Vector2.One*depthScale;
            artwork.Position=(texture.GetSize()/2-footFrame.GetMeta("foot_pivot").AsVector2())*depthScale;return;
        }
        float scale=68f/(texture.GetHeight()-4);artwork.Scale=Vector2.One*scale;
        artwork.Position=new Vector2(0,-(texture.GetHeight()/2f-2)*scale);
    }
}
