namespace GeXingzhou.Domain;

public static class SaveV4Adapter
{
    public static LoadResult Upgrade(WorldSnapshot source,NavigationCatalog frozenV2,NavigationCatalog frozenV3,NavigationCatalog current)
    {
        if(source==null)return new(LoadStatus.Corrupt,null,"空存档。");
        if(source.SchemaVersion is 1 or 2)
        {
            var upgraded=SaveV3Adapter.Upgrade(source,frozenV2,frozenV3);
            if(upgraded.Status!=LoadStatus.Loaded)return upgraded;
            source=upgraded.Snapshot!;
        }
        else if(source.SchemaVersion!=3||source.ContentVersion!="vs01-0.3")return new(LoadStatus.UnsupportedVersion,null,"不支持的旧档版本，原档已保留。");
        var error=new SaveV3Codec(frozenV3).Validate(source);
        if(error!=null)return new(LoadStatus.Corrupt,null,error);
        var position=source.SceneId=="convenience_street"?new Position2(source.PlayerPosition.X,520):source.PlayerPosition;
        var relocated=false;
        if(source.SceneId=="convenience_street"&&!NavigationGeometry.CanStand(current.Profiles["convenience_street"],position))
        {position=current.Profiles["convenience_street"].Anchors["safe"];relocated=true;}
        var candidate=source with
        {
            SchemaVersion=4,ContentVersion="vs01-0.4",PlayerPosition=position,
            CompletedActions=new(source.CompletedActions),ChoiceCodes=new(source.ChoiceCodes),DiscoveredIds=new(source.DiscoveredIds),
            SceneActiveMilliseconds=new(source.SceneActiveMilliseconds),Settings=source.Settings with{},InvitationState=source.InvitationState with{},
            ReturnContext=source.ReturnContext is {} context?context with{}:null,
            MemoryState=source.MemoryState is {} memory?memory with{PushedCoinIds=new(memory.PushedCoinIds)}:null
        };
        error=new SaveV4Codec(current).Validate(candidate);
        return error==null?new(LoadStatus.Loaded,candidate,relocated?"旧档已复制升级；家具占位已调整，回到便利店街安全位置。":"旧档已转换为v4副本，原档未修改。"):new(LoadStatus.Corrupt,null,error);
    }
}
