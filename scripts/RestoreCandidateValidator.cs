using Godot;
using GeXingzhou.Domain;
// A detached read-only candidate: no _Ready, Restore, scene/audio change, progress mutation or file write.
public static class RestoreCandidateValidator
{
    public static string? Validate(GameSession session,WorldSnapshot candidate)
    {
        if(session.Saves.ValidateSnapshot(candidate) is {} error)return error;
        WorldView? world=null;SoupMemoryController? memory=null;
        try {
            var scene=candidate.SceneId=="memory_soup_table"?"soup_shop":candidate.SceneId;
            var file=scene switch{"community_gate"=>"CommunityGate","convenience_street"=>"ConvenienceStreet","soup_shop"=>"SoupShop",_=>throw new InvalidOperationException("未知候选场景。")};
            var resource=session.GetScene("res://scenes/world/"+file+".tscn")??throw new InvalidOperationException("候选场景资源缺失。");
            world=resource.Instantiate<WorldView>();
            if(world.SceneId!=scene||!NavigationSceneReader.Matches(NavigationSceneReader.Read(world),session.Navigation.Profiles[scene]))return "候选场景导航与保存数据不一致，原档保留。";
            var player=SceneBindings.Require<PlayerController>(world,world.Mode==WorldMode.Depth2D?"DepthLayers/Actors/Player":"Player");
            SceneBindings.Require<InteractionController>(world,"Interactions");SceneBindings.Require<Camera2D>(player,"Camera2D");
            if(SceneBindings.Require<Sprite2D>(world,"Backdrop").Texture==null)return "候选背景图像缺失。";
            player.ValidateBindings(world.Mode);
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var target in world.GetTargets()){
                if(string.IsNullOrWhiteSpace(target.Id)||!ids.Add(target.Id))return "候选交互绑定异常。";
                if(target is BenchView bench&&(bench.SeatAnchor==null||bench.StandAnchor==null||bench.GetNodeOrNull<Sprite2D>("BenchForeground")?.Texture==null))return "候选长椅绑定缺失。";
            }
            if(scene=="community_gate"&&world.GetTarget("cannon")==null)return "候选小区会面目标缺失。";
            foreach(var sprite in world.FindChildren("*","Sprite2D",true,false).OfType<Sprite2D>())if(sprite.Texture==null)return "候选家具或角色图像缺失。";
            if(candidate.SceneId=="memory_soup_table"){
                var packed=session.GetScene("res://scenes/world/MemorySoupTable.tscn")??throw new InvalidOperationException("候选回忆资源缺失。");
                memory=packed.Instantiate<SoupMemoryController>();memory.ValidateBindings();
                foreach(var image in memory.FindChildren("*","TextureRect",true,false).OfType<TextureRect>())if(image.Texture==null)return "候选回忆图像缺失。";
            }
            return null;
        }catch(Exception ex) when(ex is InvalidOperationException or ArgumentException){return "候选场景预检失败，原档保留："+ex.Message;}
        finally {memory?.Free();world?.Free();}
    }
}
