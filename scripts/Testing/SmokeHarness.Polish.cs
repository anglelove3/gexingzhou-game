using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private static void Require(bool value,string message)
    { if(!value)throw new InvalidOperationException(message); }

    private async Task<MainView> NewPolishMain()
    {
        GetNode<GameSession>("/root/GameSession").NewGame();
        var main=GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<MainView>();
        AddChild(main);await Frames(3);return main;
    }

    // A runtime-only rebuild would lose edited positions, references and actor activation.
    private async Task EditableWorldChecks()
    {
        foreach(var (file,paths) in new[] {
            ("CommunityGate",new[]{"OldSign","Bench/SeatAnchor","Bench/StandAnchor","Cannon","StreetExit"}),
            ("ConvenienceStreet",new[]{"CommunityExit","Hey","SoupExit"}),
            ("SoupShop",new[]{"StreetExit","Seat","Sign","Menu","Counter","Shopkeeper","SoupBowl"}) })
        {
            var world=GD.Load<PackedScene>($"res://scenes/world/{file}.tscn").Instantiate<WorldView>();
            try
            {
                foreach(var path in paths.Concat(new[]{"Backdrop","Player/Artwork","Player/Camera2D","Player/CollisionShape2D","Interactions","Floor/CollisionShape2D","LeftBoundary/CollisionShape2D","RightBoundary/CollisionShape2D"}))
                    Require(world.GetNodeOrNull(path)!=null,$"SavedWorldNodes: {file}/{path} not editable offline");
                var before=world.GetNode<PlayerController>("Player");
                var backdrop=world.GetNode<Sprite2D>("Backdrop");var authored=backdrop.Position+new Vector2(0,2);backdrop.Position=authored;
                if(file=="CommunityGate")world.GetNode<Node2D>("Bench").Position=new Vector2(940,280);
                GetNode<GameSession>("/root/GameSession").NewGame();AddChild(world);await Frames(3);
                Require(ReferenceEquals(before,world.Player),"InspectorChangesSurviveReady: player rebuilt");
                Require(backdrop.Position==authored,"InspectorChangesSurviveReady: background reset");
                var ids=world.GetChildren().OfType<Interactable>().Select(t=>t.Id).ToArray();
                Require(ids.Length==ids.Distinct().Count(),"Duplicate interaction IDs");
                if(file=="CommunityGate")
                {
                    Require(world.GetNode<Node2D>("Bench").Position==new Vector2(940,280),"InspectorChangesSurviveReady: bench reset");
                    var cannon=world.GetNode<Interactable>("Cannon");
                    Require(!cannon.Visible,"QuestActorActivation: cannon already visible");
                    var s=GetNode<GameSession>("/root/GameSession");s.AdvanceClock(65);
                    var refresh=world.GetType().GetMethod("RefreshQuestActors");Require(refresh!=null,"QuestActorActivation: missing state binding");
                    refresh!.Invoke(world,new object[]{s.Snapshot});
                    Require(cannon.Visible,"QuestActorActivation: cannon never arrived");
                }
                GD.Print("EDITABLE_WORLD_PASS "+file);
            }
            finally {world.Free();await Frames(2);}
        }
        // Remove real required nodes from an instantiated authored world; no fake controller.
        foreach(var invalid in new[]{"missing_artwork","duplicate_id"})
        {
            var s=GetNode<GameSession>("/root/GameSession");s.NewGame();
            var host=new Control();var error=new Label{Name="StartupError",Visible=false};host.AddChild(error);AddChild(host);
            var bad=GD.Load<PackedScene>("res://scenes/world/CommunityGate.tscn").Instantiate<WorldView>();
            if(invalid=="missing_artwork")bad.GetNode("Player/Artwork").Free();
            else bad.GetNode<Interactable>("StreetExit").Id="old_sign";
            host.AddChild(bad);await Frames(2);
            Require(error.Visible&&error.Text.Length>0,"MissingBindingStopsInteraction: no readable error "+invalid);
            Require(bad.HasMeta("binding_error"),"MissingBindingStopsInteraction: world still initialized "+invalid);
            var stage=s.Snapshot.Stage;bad.GetNode<Interactable>("OldSign").TryInteract(s);KeyPress(Key.E);await Frames(2);
            Require(s.Snapshot.Stage==stage,"Invalid world advanced story");host.Free();await Frames(2);
        }
        GetNode<GameSession>("/root/GameSession").NewGame();
        GD.Print("EDITABLE_WORLD_PASS MissingBindingStopsInteraction");
    }
}
