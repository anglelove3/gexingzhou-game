using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task StreetChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");session.NewGame();
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();AddChild(main);await Frames(3);
        Require(main.ChangeWorld("convenience_street",session.Navigation.Profiles["convenience_street"].Anchors["safe"]),"Street fixture failed to enter");await Frames(3);
        var world=main.World;
        Require(world.Mode==WorldMode.Depth2D,"Street is not Depth2D");
        Require(world.Width==1600&&world.ViewBounds.Size==new Vector2(1600,720),"Street dimensions are not 1600x720");
        Require(world.GetNodeOrNull<PlayerController>("DepthLayers/Actors/Player")!=null,"Street depth player missing");
        Require(world.GetTarget("hey")?.Position==new Vector2(800,500),"Hey anchor changed");
        var handover=world.GetTarget("hey")!.GetNode<Sprite2D>("HandoverCandy");
        Require(!handover.Visible,"Hey handover fixture started visible");world.ShowCandyHandover();Require(handover.Visible,"Nested Hey handover visual did not start");
        foreach(var pair in new[]{("entry",new Vector2(100,520)),("safe",new Vector2(240,520)),("exit",new Vector2(1500,520))})
            Require(world.GetAnchor(pair.Item1).Position==pair.Item2,"Street anchor mismatch "+pair.Item1);
        var ids=world.GetTargets().OfType<ObservationHotspot>().Select(x=>x.Id).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        Require(string.Join(',',ids)=="bike_shed,doorplate,drain,laundry_rack,newsstand,stool,wall_paint","Street hotspot contract missing");
        var navigation=NavigationSceneReader.Read(world);
        Require(NavigationSceneReader.Matches(navigation,session.Navigation.Profiles["convenience_street"]),"Street navigation not baked");
        Require(navigation.Ground.SequenceEqual(new[]{new Position2(24,180),new Position2(1576,180),new Position2(1576,688),new Position2(24,688)}),"Street ground boundary changed");
        var expectedObstacles=new[]{
            Rect(24,180,340,440),Rect(450,300,1150,440),Rect(1260,180,1576,360),Rect(24,580,580,688),
            Rect(680,570,940,620),Rect(1040,580,1280,688),Rect(1340,580,1576,688)};
        Require(navigation.Obstacles.Length==expectedObstacles.Length&&navigation.Obstacles.Zip(expectedObstacles).All(pair=>pair.First.SequenceEqual(pair.Second)),"Street obstacle contract changed");
        foreach(var route in new[]{
            new[]{new Position2(100,520),new Position2(800,520),new Position2(1500,520)},
            new[]{new Position2(380,520),new Position2(380,220),new Position2(1220,220),new Position2(1220,520)},
            new[]{new Position2(620,520),new Position2(620,650),new Position2(1000,650),new Position2(1000,520)},
            new[]{new Position2(1320,520),new Position2(1380,420),new Position2(1320,520)}})
            for(var i=1;i<route.Length;i++)Require(NavigationGeometry.CanTraverse(navigation,route[i-1],route[i]),$"Street route blocked {route[i-1]}->{route[i]}");
        main.Free();await Frames(2);
    }

    private static Position2[] Rect(float left,float top,float right,float bottom)=>
        new[]{new Position2(left,top),new Position2(right,top),new Position2(right,bottom),new Position2(left,bottom)};
}
