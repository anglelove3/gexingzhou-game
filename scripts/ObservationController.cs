using Godot;
using GeXingzhou.Domain;
public partial class ObservationController : Node
{
    private MainView main=null!;private Control hover=null!;private Line2D outline=null!;private Label title=null!;
    private Vector2 pointer;private bool hasPointer;
    public void Configure(MainView owner)
    {main=owner;hover=SceneBindings.Require<Control>(main,"ObservationHover");outline=hover.GetNode<Line2D>("Outline");title=hover.GetNode<Label>("Title");}
    private bool Available()=>GodotObject.IsInstanceValid(main)&&GodotObject.IsInstanceValid(main.World)&&(main.World.SceneId is "soup_shop" or "community_gate")&&GetNode<GameSession>("/root/GameSession").Flow==FlowState.Field&&!main.SoupSeat.IsActing&&!main.Rest.IsActive;
    public bool CanObserve(ObservationHotspot target)=>Available()&&GodotObject.IsInstanceValid(target)&&main.World.GetTargets().Contains(target)&&target.IsInsideTree()&&target.IsActive&&target.IsVisibleInTree()&&!target.HasMeta("binding_error");
    public bool TryObserve(ObservationHotspot target)
    {
        if(!CanObserve(target))return false;
        var s=GetNode<GameSession>("/root/GameSession");var id=target.DiscoveryId;
        main.Dialogue.ShowText(target.Caption,target.Description,()=>{s.UpdatePosition(main.SafeSavePosition);s.MarkDiscovery(id);},DialogueLineKind.Thought);hover.Visible=false;return true;
    }
    private bool IsUiPoint(Vector2 point)=>main.FindChildren("*","",true,false).OfType<Control>().Any(c=>c!=main&&c!=main.GetNode<Control>("WorldDisplay")&&c.GetViewport()==main.GetViewport()&&c.IsVisibleInTree()&&c.MouseFilter==Control.MouseFilterEnum.Stop&&c.GetGlobalRect().HasPoint(point));
    private bool ForegroundBlocks(Vector2 point)
    {
        var foreground=main.World.GetNodeOrNull<Node2D>("Foreground");if(foreground==null)return false;
        foreach(var item in foreground.FindChildren("*","",true,false).OfType<Node2D>().Where(n=>n.IsVisibleInTree()))
        {
            if(item is Polygon2D p&&Geometry2D.IsPointInPolygon(p.ToLocal(point),p.Polygon))return true;
            if(item is Sprite2D sprite&&sprite.Texture!=null)
            {
                var local=sprite.ToLocal(point)+(sprite.Centered?sprite.Texture.GetSize()/2:Vector2.Zero);
                if(local.X<0||local.Y<0||local.X>=sprite.Texture.GetWidth()||local.Y>=sprite.Texture.GetHeight())continue;
                using var image=sprite.Texture.GetImage();if(image!=null&&image.GetPixel((int)local.X,(int)local.Y).A>=.1)return true;
            }
        }
        return false;
    }
    private ObservationHotspot? Hit(Vector2 point)
    {
        if(!Available()||IsUiPoint(point)||!main.GetNode<Control>("WorldDisplay").GetGlobalRect().HasPoint(point))return null;
        var world=main.UiToWorld(point);if(ForegroundBlocks(world))return null;
        return main.World.GetTargets().OfType<ObservationHotspot>().Where(t=>CanObserve(t)&&t.ContainsWorldPoint(world)).OrderBy(t=>t.Id,StringComparer.Ordinal).FirstOrDefault();
    }
    public override void _Process(double delta)
    {
        if(main==null)return;
        var point=hasPointer?pointer:main.GetViewport().GetMousePosition();var target=Hit(point);hover.Visible=target!=null;if(target==null)return;
        var points=target.OutlineWorld().Select(main.WorldToUi).ToArray();outline.Points=points;
        title.Text=target.Caption+" · 点击看看";
        var x=Math.Clamp(points.Min(p=>p.X),12,Math.Max(12,main.Size.X-240));
        var y=Math.Clamp(points.Min(p=>p.Y)-30,12,main.Size.Y-40);title.Position=new(x,y);
    }
    public override void _Input(InputEvent ev)
    {
        if(ev is InputEventMouseMotion motion){pointer=motion.Position;hasPointer=true;}
        if(main!=null&&ev is InputEventMouseButton{Pressed:true,ButtonIndex:MouseButton.Left} button&&Hit(button.Position) is {} target){main.ShowObservation(target);GetViewport().SetInputAsHandled();}
    }
}
