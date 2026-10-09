namespace GeXingzhou.Domain;
public static class LegacySaveAdapter
{
    public static LoadResult Upgrade(WorldSnapshot legacy,NavigationCatalog navigation)
    {
        var error=SaveRepository.Validate(legacy);if(error!=null)return new(LoadStatus.Corrupt,null,error);
        if(!navigation.Profiles.TryGetValue("soup_shop",out var soup))return new(LoadStatus.Corrupt,null,"缺少汤店导航，暂不能升级。");
        var upgraded=legacy with {
            SchemaVersion=2,ContentVersion="vs01-0.2",
            PlayerPosition=legacy.SceneId=="soup_shop"?soup.Anchors["safe"]:legacy.PlayerPosition,
            ReturnContext=legacy.ReturnContext is {} context?context with {Position=soup.Anchors["memory_return"]}:null,
            CompletedActions=new(legacy.CompletedActions),ChoiceCodes=new(legacy.ChoiceCodes),DiscoveredIds=new(),
            MemoryState=legacy.MemoryState is {} memory?memory with {PushedCoinIds=new(memory.PushedCoinIds)}:null,
            SceneActiveMilliseconds=new(legacy.SceneActiveMilliseconds),Settings=legacy.Settings with {},InvitationState=legacy.InvitationState with {}};
        error=new SaveV2Codec(navigation).Validate(upgraded);
        return error==null?new(LoadStatus.Loaded,upgraded,"旧档已转换为新版本副本，原档未修改。"):new(LoadStatus.Corrupt,null,error);
    }
}
