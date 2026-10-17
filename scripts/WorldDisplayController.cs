using Godot;

public partial class WorldDisplayController : SubViewportContainer
{
    private WorldView? world;
    public Rect2 VisibleWorldRect {get;private set;}
    public override void _Ready(){Resized+=UpdateView;}
    public void Configure(WorldView value){world=value;UpdateView();}
    public override void _Process(double delta)=>UpdateView();
    private void UpdateView()
    {
        if(world==null||!GodotObject.IsInstanceValid(world)||world.HasMeta("binding_error")||Size.X<2||Size.Y<2)return;
        var viewport=GetNode<SubViewport>("WorldViewport");
        // Stretch owns the SubViewport size; assigning Size here conflicts with Godot.
        if(viewport.Size.X<1||viewport.Size.Y<1)return;
        var bounds=world.ViewBounds;float aspect=(float)viewport.Size.X/viewport.Size.Y;
        // Frame the community closer while retaining authored foot/furniture and save coordinates.
        var framingBounds=world.SceneId=="community_gate"?new Rect2(bounds.Position,new Vector2(bounds.Size.X,Math.Min(bounds.Size.Y,520))):bounds;
        float preferredHeight=world.SceneId=="community_gate"?400:framingBounds.Size.Y;
        float height=Math.Min(preferredHeight,framingBounds.Size.X/aspect);var viewSize=new Vector2(height*aspect,height);
        var wanted=world.Player.GlobalPosition+new Vector2(0,world.SceneId=="community_gate"?-130:-100);
        var center=new Vector2(Math.Clamp(wanted.X,bounds.Position.X+viewSize.X/2,bounds.End.X-viewSize.X/2),
            Math.Clamp(wanted.Y,framingBounds.Position.Y+viewSize.Y/2,framingBounds.End.Y-viewSize.Y/2));
        VisibleWorldRect=new Rect2(center-viewSize/2,viewSize);
        var camera=world.Player.GetNode<Camera2D>("Camera2D");
        // Clamp explicitly inside authored safe art bounds, independently of collision borders.
        camera.LimitLeft=-100000;camera.LimitTop=-100000;camera.LimitRight=100000;camera.LimitBottom=100000;
        camera.GlobalPosition=center;camera.Zoom=Vector2.One*(viewport.Size.Y/height);
    }
}
