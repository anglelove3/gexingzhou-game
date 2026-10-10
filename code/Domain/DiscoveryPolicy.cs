namespace GeXingzhou.Domain;
public static class DiscoveryPolicy
{
    public static bool IsKnown(string id)=>id is "community.sign" or "community.notice" or "community.planter" or "soup.sign" or "soup.menu" or "soup.note";
    public static WorldSnapshot Mark(WorldSnapshot s,string id)=>!IsKnown(id)||s.DiscoveredIds.Contains(id)?s:s with{DiscoveredIds=new HashSet<string>(s.DiscoveredIds){id}};
}
