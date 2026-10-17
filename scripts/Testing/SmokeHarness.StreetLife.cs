using Godot;
using GeXingzhou.Domain;
public partial class SmokeHarness
{
    private async Task StreetLifeChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){TextSpeed=0,ReducedMotion=false},false);
        var main=await NewPolishMain();
        try
        {
            Require(main.ChangeWorld("convenience_street",new(800,520)),"Street life entry failed");await Frames(3);
            var world=main.World;
            // Break: _Ready can reject a required artwork after detached preflight already wrote v4.
            var fixture=UpgradeFixture();WriteStreetV3(fixture,false,false);fixture.Flow=FlowState.Phone;
            var old=CommunityFileState(fixture.V3Directory);var current=CommunityFileState(fixture.SaveDirectory);var snapshot=fixture.Snapshot;
            var resources=(Dictionary<string,PackedScene>)DiagnosticField(fixture,"sceneResources")!;
            const string worldPath="res://scenes/world/ConvenienceStreet.tscn";var originalWorld=resources[worldPath];
            var missing=originalWorld.Instantiate<WorldView>();missing.GetNode("DepthLayers/Actors/ResidentNorth/Artwork").Free();
            using var broken=new PackedScene();Require(broken.Pack(missing)==Error.Ok,"Missing resident artwork fixture pack failed");missing.Free();resources[worldPath]=broken;
            try{
                var result=fixture.TryUpgradePrevious(false);
                Require(result.Status==LoadStatus.Corrupt&&CommunityFileState(fixture.V3Directory)==old&&CommunityFileState(fixture.SaveDirectory)==current&&ReferenceEquals(fixture.Snapshot,snapshot)&&fixture.Flow==FlowState.Phone,"Missing resident artwork wrote candidate save or changed source/runtime");
            }finally{resources[worldPath]=originalWorld;fixture.Free();}
            // Break: a detached candidate misses FocusOut, then enters the tree in the background.
            var delayed=GD.Load<PackedScene>("res://scenes/world/ConvenienceStreet.tscn").Instantiate<WorldView>();
            s.Notification((int)NotificationApplicationFocusOut);AddChild(delayed);
            try{
                foreach(var actor in delayed.FindChildren("*","",true,false).OfType<AmbientResidentController>()){
                    actor.SetProcess(false);var at=actor.Position;actor._Process(30);actor._Process(30);
                    Require(actor.Position==at,"Resident entering after FocusOut moved in background");
                }
            }finally{delayed.Free();s.Notification((int)NotificationApplicationFocusIn);}
            foreach(var name in new[]{"ResidentNorth","ResidentSouth"})
            {
                var resident=world.GetNode<Node2D>("DepthLayers/Actors/"+name) as AmbientResidentController;
                Require(resident!=null,"Street resident still static "+name);
                resident!.SetProcess(false);resident._Notification((int)NotificationApplicationFocusIn);
                Require(!resident.FindChildren("*","CollisionObject2D",true,false).Any()&&world.GetNode<Node2D>("DepthLayers/Actors/"+name) is not Interactable,"Resident became blocking or interactive");
                var before=StreetLifeSnapshot(s);var start=resident.Position;var end=start+resident.GetNode<Marker2D>("Waypoint").Position;
                Require(NavigationGeometry.CanTraverse(world.Navigation,new(start.X,start.Y),new(end.X,end.Y)),"Resident authored route blocked");
                resident._Process(2);resident._Process(.2);Require(resident.Position.DistanceTo(start)>0,"Resident did not walk");
                var at=resident.Position;var art=resident.GetNode<Sprite2D>("Artwork");var artAt=art.Position;var rotation=art.Rotation;
                foreach(var flow in new[]{FlowState.Dialogue,FlowState.Phone,FlowState.Paused,FlowState.Transition,FlowState.EndCard})
                {s.Flow=flow;resident._Process(30);Require(resident.Position==at&&art.Position==artAt&&art.Rotation==rotation,"Resident moved during modal "+flow);}
                s.Flow=FlowState.Field;resident._Notification((int)NotificationApplicationFocusOut);resident._Process(30);Require(resident.Position==at,"Resident moved unfocused");
                resident._Notification((int)NotificationApplicationFocusIn);s.SetOptions(s.Options with{ReducedMotion=true},false);resident._Process(30);Require(resident.Position==at&&art.Position==artAt,"Reduced motion reset or advanced resident");
                s.SetOptions(s.Options with{ReducedMotion=false},false);resident._Process(.1);Require(resident.Position.DistanceTo(at)<1.01f&&resident.Position!=at,"Resume teleported or failed");
                for(int i=0;i<400;i++){resident._Process(.5);Require(NavigationGeometry.CanStand(world.Navigation,new(resident.Position.X,resident.Position.Y)),"Resident left legal route");}
                Require(before==StreetLifeSnapshot(s),"Resident wrote story/save state");
            }
            var ambience=main.Audio.GetNode<AudioStreamPlayer>("Ambience");
            main.Audio.SetScene("convenience_street");main.Audio._Process(2);main.Audio._Process(2);
            Require(ambience.Stream?.ResourcePath=="res://assets/audio/vs03/ambience-street-soft.wav","Street still uses generic outdoor audio");
            foreach(var masterMute in new[]{false,true}){
                s.SetOptions(s.Options with{MasterVolume=masterMute?0:1,EnvironmentVolume=masterMute?1:0},false);main.Audio.ApplySettings(s.Options);
                Require(ambience.StreamPaused&&ambience.VolumeLinear==0,"Street zero gain did not silence real stream");
                s.SetOptions(s.Options with{MasterVolume=1,EnvironmentVolume=.3},false);main.Audio.ApplySettings(s.Options);main.Audio._Process(2);
                Require(!ambience.StreamPaused&&ambience.Playing&&ambience.VolumeLinear>0,"Street sound failed to resume");
            }
            main.Audio.SetScene("soup_shop");main.Audio._Process(2);main.Audio._Process(2);Require(ambience.Stream==main.Audio.IndoorStream,"Street stream leaked into soup");
            main.Audio.SetScene("community_gate");main.Audio._Process(2);main.Audio._Process(2);Require(ambience.Stream==main.Audio.OutdoorStream,"Street stream leaked into community");
        }
        finally{main.Free();await Frames(3);}
        GD.Print("STREET_LIFE_PASS Routes Modal Focus Reduced Resume NoStory IndependentAudio RealMute SceneSwitch");
    }
    private static string StreetLifeSnapshot(GameSession s)=>System.Text.Json.JsonSerializer.Serialize(s.Snapshot);
}
