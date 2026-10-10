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
            Shape=ValidateBindings();
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    // Read-only and detached-safe: upgrade preflight and _Ready enforce the same authored contract.
    public Polygon2D ValidateBindings()
    {
        if(!DiscoveryPolicy.IsKnown(DiscoveryId))throw new InvalidOperationException("未知观察ID："+DiscoveryId);
        SceneBindings.Require<Node2D>(this,VisualTarget.ToString());
        var shape=SceneBindings.Require<Polygon2D>(this,HitPolygon.ToString());var points=shape.Polygon;
        if(points.Length<3||points.Any(p=>!float.IsFinite(p.X)||!float.IsFinite(p.Y)))throw new InvalidOperationException("观察物件缺少有效轮廓："+Id);
        double area=0;for(int i=0;i<points.Length;i++){var a=points[i];var b=points[(i+1)%points.Length];area+=(double)a.X*b.Y-(double)b.X*a.Y;}
        if(Math.Abs(area)<.00001)throw new InvalidOperationException("观察物件轮廓退化："+Id);
        return shape;
    }
    public bool ContainsWorldPoint(Vector2 point)=>IsActive&&IsVisibleInTree()&&!HasMeta("binding_error")&&Geometry2D.IsPointInPolygon(Shape.ToLocal(point),Shape.Polygon);
    public Vector2[] OutlineWorld()=>Shape.Polygon.Select(p=>Shape.ToGlobal(p)).ToArray();
}
