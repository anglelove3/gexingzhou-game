using Godot;
using GeXingzhou.Domain;
public partial class NavigationBake : Node
{
    public override void _Ready()
    {
        try
        {
            var profiles=new List<NavigationProfile>();
            foreach(var name in new[]{"CommunityGate","ConvenienceStreet","SoupShop"})
            {
                var world=GD.Load<PackedScene>("res://scenes/world/"+name+".tscn").Instantiate<WorldView>();
                try{profiles.Add(NavigationSceneReader.Read(world));}finally{world.Free();}
            }
            var json=NavigationSceneReader.Export(profiles);
            _=NavigationCatalog.Parse(json);
            var output=ProjectSettings.GlobalizePath("res://test-output/navigation-bake/navigation.json");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!);
            System.IO.File.WriteAllText(output,json,new System.Text.UTF8Encoding(false));
            var expectedArg=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--navigation-expected="));
            if(expectedArg!=null)
            {
                var file=expectedArg.Split('=',2)[1];
                var expected=NavigationCatalog.Parse(System.IO.File.ReadAllText(file));
                if(expected.Profiles.Count!=profiles.Count||profiles.Any(p=>!expected.Profiles.TryGetValue(p.SceneId,out var other)||!NavigationSceneReader.Matches(p,other)))
                    throw new InvalidOperationException("编辑场景与预期导航不同。");
                GD.Print("NAVIGATION_MATCH_PASS");
            }
            GD.Print("NAVIGATION_EXPORT_PASS "+output);
            GetTree().Quit();
        }
        catch(Exception ex){GD.Print("NAVIGATION_EXPORT_FAIL "+ex.Message);GetTree().Quit(1);}
    }
}
