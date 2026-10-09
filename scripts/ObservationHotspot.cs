using Godot;
using GeXingzhou.Domain;
public partial class ObservationHotspot : Interactable
{
    [Export] public string DiscoveryId {get;set;}="";
    [Export] public NodePath VisualTarget {get;set;}="Visual";
    [Export] public NodePath HitPolygon {get;set;}="Visual/Shape";
    public Polygon2D Shape {get;private set;}=null!;
    public override void _Ready()
    {
        base._Ready();
        try
        {
            if(!DiscoveryPolicy.IsKnown(DiscoveryId))throw new InvalidOperationException("未知观察ID："+DiscoveryId);
            SceneBindings.Require<Node2D>(this,VisualTarget.ToString());
            Shape=SceneBindings.Require<Polygon2D>(this,HitPolygon.ToString());
            if(Shape.Polygon.Length<3)throw new InvalidOperationException("观察物件缺少轮廓："+Id);
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public bool ContainsWorldPoint(Vector2 point)=>IsActive&&IsVisibleInTree()&&!HasMeta("binding_error")&&Geometry2D.IsPointInPolygon(Shape.ToLocal(point),Shape.Polygon);
    public Vector2[] OutlineWorld()=>Shape.Polygon.Select(p=>Shape.ToGlobal(p)).ToArray();
}
