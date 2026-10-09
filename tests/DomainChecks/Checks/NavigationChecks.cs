using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;

public static class NavigationChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        // Break: independent axis speeds would make diagonal speed exceed the axial cap.
        tests.Add(("Navigation","DiagonalSpeed",()=>{
            foreach(var (fast,want) in new[]{(false,112f),(true,168f)}){
                var v=Movement2DModel.Step(new(0,0),new(1,1),fast,false,1);
                Check.True(Math.Abs(NavigationGeometry.Distance(new(0,0),v)-want)<.001f);
                Check.True(Math.Abs(v.X-v.Y)<.001f);
            }
        }));
        tests.Add(("Navigation","VerticalAccelerationAndStop",()=>{
            Check.Equal(new Position2(0,80),Movement2DModel.Step(new(0,0),new(0,1),false,false,.1f));
            Check.Equal(new Position2(0,112),Movement2DModel.Step(new(0,80),new(0,1),false,false,1));
            Check.Equal(new Position2(0,52),Movement2DModel.Step(new(0,112),new(0,0),false,false,.05f));
            Check.Equal(new Position2(0,0),Movement2DModel.Step(new(0,112),new(0,0),false,false,.1f));
            Check.Equal(new Position2(0,-168),Movement2DModel.Step(new(0,0),new(0,-1),true,false,1));
        }));
        tests.Add(("Navigation","InvalidInput",()=>{
            var zero=new Position2(0,0);
            Check.Equal(zero,Movement2DModel.Step(new(10,10),new(1,1),false,true,1));
            Check.Equal(new Position2(20,30),Movement2DModel.Step(new(20,30),new(1,1),false,false,0));
            Check.Equal(zero,Movement2DModel.Step(new(float.NaN,1),new(1,1),false,false,1));
            Check.Equal(zero,Movement2DModel.Step(new(10,0),new(0,float.PositiveInfinity),false,false,1));
            Check.Equal(zero,Movement2DModel.Step(new(10,0),new(1,0),false,false,float.NaN));
            Check.Equal(zero,Movement2DModel.Step(new(10,0),new(1,0),false,false,-1));
            var huge=Movement2DModel.Step(zero,new(float.MaxValue,float.MaxValue),false,false,1);
            Check.True(float.IsFinite(huge.X)&&float.IsFinite(huge.Y));
            Check.True(Math.Abs(NavigationGeometry.Distance(zero,huge)-112)<.001f);
        }));
        // Break: point-only containment would allow the foot circle to overlap tables/edges.
        tests.Add(("Navigation","FootClearance",()=>{
            var p=NavigationFixtures.Soup();
            Check.True(NavigationGeometry.CanStand(p,new(400,430)));
            Check.True(NavigationGeometry.CanStand(p,new(33,480)));
            Check.True(!NavigationGeometry.CanStand(p,new(31,480)));
            Check.True(!NavigationGeometry.CanStand(p,new(455,430)));
            Check.True(!NavigationGeometry.CanStand(p,new(500,430)));
            Check.True(NavigationGeometry.CanStand(p,new(450,430)));
            Check.True(!NavigationGeometry.CanStand(p,new(float.NaN,430)));
        }));
        tests.Add(("Navigation","SweptTableBlock",()=>{
            var p=NavigationFixtures.Soup();
            Check.True(!NavigationGeometry.CanTraverse(p,new(400,430),new(600,430)));
            Check.True(NavigationGeometry.CanTraverse(p,new(400,480),new(600,480)));
            Check.True(NavigationGeometry.CanTraverse(p,new(400,430),new(400,480)));
            Check.True(!NavigationGeometry.CanTraverse(p,new(455,430),new(400,480)));
            var concave=p with {Ground=new Position2[]{new(24,360),new(936,360),new(936,520),new(550,520),new(550,460),new(450,460),new(450,520),new(24,520)},Obstacles=Array.Empty<Position2[]>()};
            Check.True(!NavigationGeometry.CanTraverse(concave,new(400,480),new(600,480)));
        }));
        tests.Add(("Navigation","DistanceAndHysteresis",()=>{
            var policy=new Interaction2DPolicy();
            Check.Equal<string?>(null,policy.Select(new[]{new Interaction2DCandidate("above",new(0,41),true)},null,new(0,0)));
            var ts=new[]{new Interaction2DCandidate("b",new(0,40),true),new Interaction2DCandidate("a",new(40,0),true),new Interaction2DCandidate("wall",new(0,0),false)};
            Check.Equal("a",policy.Select(ts,null,new(0,0)));
            Check.Equal("b",policy.Select(new[]{new Interaction2DCandidate("b",new(0,48),true)},"b",new(0,0)));
            Check.Equal<string?>(null,policy.Select(new[]{new Interaction2DCandidate("b",new(0,48.1f),true)},"b",new(0,0)));
            Check.Equal<string?>(null,policy.Select(new[]{new Interaction2DCandidate("wall",new(0,0),false)},null,new(0,0)));
            Check.Equal<string?>(null,policy.Select(ts,null,new(float.NaN,0)));
        }));
        tests.Add(("Navigation","Debounce",()=>{
            var policy=new Interaction2DPolicy();
            Check.True(policy.TryActivate("a",0));Check.True(!policy.TryActivate("b",.2));
            Check.True(policy.TryActivate("b",.25));Check.True(!policy.TryActivate("b",double.NaN));
            Check.True(!policy.TryActivate("",1));Check.True(policy.TryActivate("a",1));
        }));
        tests.Add(("Navigation","MalformedProfile",()=>{
            var parsed=NavigationCatalog.Parse(NavigationFixtures.SoupJson);
            Check.True(NavigationGeometry.CanStand(parsed.Profiles["soup_shop"],new(400,430)));
            foreach(var json in new[]{NavigationFixtures.SoupJson.Replace("\"depth2d\"","\"free\""),
                NavigationFixtures.SoupJson.Replace("\"foot_radius\":8","\"foot_radius\":-1"),
                NavigationFixtures.SoupJson.Replace("\"schema_version\":1","\"schema_version\":999"),
                NavigationFixtures.SoupJson.Replace("\"safe\":{\"x\":120,\"y\":480}","\"safe\":{\"x\":500,\"y\":430}"),
                NavigationFixtures.SoupJson.Replace("\"safe\":{\"x\":120,\"y\":480}","\"safe\":{\"x\":120,\"y\":480},\"safe\":{\"x\":130,\"y\":480}"),
                NavigationFixtures.SoupJson.Replace("{\"x\":936,\"y\":360},{\"x\":936,\"y\":520}","{\"x\":936,\"y\":520},{\"x\":936,\"y\":360}")})
                Check.Throws<ArgumentException>(()=>NavigationCatalog.Parse(json));
        }));
    }
}
