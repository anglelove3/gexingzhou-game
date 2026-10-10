using Godot;
using GeXingzhou.Domain;
using System.Text.Json;
// Offline export reads authored local transforms, never a running session or global physics.
public static class NavigationSceneReader
{
    private static readonly Dictionary<string,string> AnchorNames=new(){
        ["EntryAnchor"]="entry",["ExitAnchor"]="exit",["StandAnchor"]="stand",
        ["SafeAnchor"]="safe",["MemoryReturnAnchor"]="memory_return",["SeatAnchor"]="seat"};
    public static NavigationProfile Read(WorldView world)
    {
        var root=SceneBindings.Require<Node2D>(world,"Navigation/Anchors");
        var anchors=new Dictionary<string,Position2>(StringComparer.Ordinal);
        foreach(var marker in root.GetChildren().OfType<Marker2D>())
        {
            if(!AnchorNames.TryGetValue(marker.Name.ToString(),out var key)||!anchors.TryAdd(key,Point(world,marker,Vector2.Zero)))
                throw new InvalidOperationException("导航锚点名称无效或重复："+marker.Name);
        }
        Position2[] ground=Array.Empty<Position2>();Position2[][] obstacles=Array.Empty<Position2[]>();
        float radius=8;
        if(world.Mode==WorldMode.Depth2D)
        {
            foreach(var path in new[]{"DepthLayers","DepthLayers/Actors"})
                if(SceneBindings.Require<Node2D>(world,path).Transform!=Transform2D.Identity)
                    throw new InvalidOperationException("Depth角色父层必须保持原点、无旋转缩放："+path);
            var player=SceneBindings.Require<PlayerController>(world,"DepthLayers/Actors/Player");
            if(player.CollisionMask!=1||player.CollisionLayer!=2||player.Transform.X!=Vector2.Right||player.Transform.Y!=Vector2.Down)
                throw new InvalidOperationException("Depth角色必须使用layer2/mask1，不能旋转缩放脚圆。");
            var foot=SceneBindings.Require<CollisionShape2D>(world,"DepthLayers/Actors/Player/FootCollisionShape2D");
            if(foot.Shape is not CircleShape2D circle||circle.Radius!=8||foot.Position!=Vector2.Zero||foot.Scale!=Vector2.One)
                throw new InvalidOperationException("Depth脚圆必须为原点半径8。");
            radius=circle.Radius;
            var boundary=SceneBindings.Require<CollisionPolygon2D>(world,"Navigation/GroundBoundary/CollisionPolygon2D");
            var boundaryBody=SceneBindings.Require<StaticBody2D>(world,"Navigation/GroundBoundary");
            if(boundary.Disabled||boundaryBody.CollisionLayer!=1||boundary.BuildMode!=CollisionPolygon2D.BuildModeEnum.Segments)
                throw new InvalidOperationException("地面外边界必须启用layer1/Segments碰撞。");
            ground=Polygon(world,boundary);
            obstacles=SceneBindings.Require<Node2D>(world,"Navigation/Obstacles").GetChildren()
                .OrderBy(n=>n.Name.ToString(),StringComparer.Ordinal).Select(n=>{
                    if(n is not StaticBody2D body||body.CollisionLayer!=1)throw new InvalidOperationException("占位必须为layer1静态刚体。");
                    var shape=SceneBindings.Require<CollisionPolygon2D>(body,"CollisionPolygon2D");
                    if(shape.Disabled||shape.BuildMode!=CollisionPolygon2D.BuildModeEnum.Solids)throw new InvalidOperationException("占位碰撞必须启用Solids。");
                    return Polygon(world,shape);
                }).ToArray();
        }
        if(world.SceneId=="community_gate"&&world.Mode==WorldMode.Depth2D){
            var bench=world.GetTarget("bench") as BenchView??throw new InvalidOperationException("小区长椅缺失。");
            foreach(var binding in new[]{(bench.StandAnchor,"stand"),(bench.SeatAnchor,"seat")})
                if(binding.Item1==null||!anchors.TryGetValue(binding.Item2,out var expected)||Point(world,binding.Item1,Vector2.Zero)!=expected)
                    throw new InvalidOperationException("长椅锚点与导航锚点不一致："+binding.Item2);
        }
        var profile=new NavigationProfile(world.SceneId,world.Mode,world.Width,world.ViewBounds.Size.Y,radius,ground,obstacles,anchors);
        try{_ = new NavigationCatalog(new Dictionary<string,NavigationProfile>{{world.SceneId,profile}});}
        catch(ArgumentException ex){throw new InvalidOperationException("静态导航无效："+ex.Message,ex);}
        return profile;
    }
    private static Position2[] Polygon(WorldView world,CollisionPolygon2D polygon)=>polygon.Polygon.Select(v=>Point(world,polygon,v)).ToArray();
    private static Position2 Point(WorldView world,Node2D node,Vector2 local)
    {
        for(Node? current=node;current!=world;current=current.GetParent())
        {
            if(current==null)throw new InvalidOperationException("导航节点不在本世界。");
            if(current is Node2D spatial)local=spatial.Transform*local;
        }
        return new(local.X,local.Y);
    }
    public static string Canonical(NavigationProfile profile)=>JsonSerializer.Serialize(new{
        scene_id=profile.SceneId,mode=profile.Mode==WorldMode.Depth2D?"depth2d":"horizontal",width=profile.Width,height=profile.Height,
        foot_radius=profile.FootRadius,ground=profile.Ground.Select(v=>new{x=v.X,y=v.Y}),
        obstacles=profile.Obstacles.Select(points=>points.Select(v=>new{x=v.X,y=v.Y})),
        anchors=profile.Anchors.OrderBy(v=>v.Key,StringComparer.Ordinal).ToDictionary(v=>v.Key,v=>new{x=v.Value.X,y=v.Value.Y})});
    public static string Export(IEnumerable<NavigationProfile> profiles)=>"{\"schema_version\":1,\"profiles\":["+
        string.Join(",",profiles.OrderBy(p=>p.SceneId,StringComparer.Ordinal).Select(Canonical))+"]}";
    public static bool Matches(NavigationProfile a,NavigationProfile b)=>Canonical(a)==Canonical(b);
    public static NavigationCatalog LoadCatalog()=>NavigationCatalog.Parse(Godot.FileAccess.GetFileAsString("res://content/vs01/navigation.json"));
}
