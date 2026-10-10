namespace GeXingzhou.Domain;
public static class SaveV3Adapter
{
    public static LoadResult Upgrade(WorldSnapshot source,NavigationCatalog frozenV2,NavigationCatalog current)
    {
        if(source==null)return new(LoadStatus.Corrupt,null,"空存档。");
        if(source.SchemaVersion==1&&source.ContentVersion=="vs01-0.1"){
            var legacy=LegacySaveAdapter.Upgrade(source,frozenV2);if(legacy.Status!=LoadStatus.Loaded)return legacy;
            source=legacy.Snapshot!;
        }else if(source.SchemaVersion!=2||source.ContentVersion!="vs01-0.2")return new(LoadStatus.UnsupportedVersion,null,"不支持的旧档版本，原档已保留。");
        var error=new SaveV2Codec(frozenV2).Validate(source);if(error!=null)return new(LoadStatus.Corrupt,null,error);
        var position=source.SceneId=="community_gate"?new Position2(source.PlayerPosition.X,460):source.PlayerPosition;
        var relocated=false;
        if(source.SceneId=="community_gate"){
            if(!current.Profiles.TryGetValue("community_gate",out var profile))return new(LoadStatus.Corrupt,null,"缺少小区导航，暂不能升级。");
            if(!NavigationGeometry.CanStand(profile,position)){position=profile.Anchors["safe"];relocated=true;}
        }
        var candidate=source with{
            SchemaVersion=3,ContentVersion="vs01-0.3",PlayerPosition=position,
            CompletedActions=new(source.CompletedActions),ChoiceCodes=new(source.ChoiceCodes),DiscoveredIds=new(source.DiscoveredIds),
            SceneActiveMilliseconds=new(source.SceneActiveMilliseconds),Settings=source.Settings with{},InvitationState=source.InvitationState with{},
            ReturnContext=source.ReturnContext is {} context?context with{}:null,
            MemoryState=source.MemoryState is {} memory?memory with{PushedCoinIds=new(memory.PushedCoinIds)}:null};
        error=new SaveV3Codec(current).Validate(candidate);
        return error==null?new(LoadStatus.Loaded,candidate,relocated?"旧档已复制升级；家具占位已调整，回到小区安全位置。":"旧档已转换为v3副本，原档未修改。"):new(LoadStatus.Corrupt,null,error);
    }
}
