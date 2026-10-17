namespace GeXingzhou.Domain;
public static class DiscoveryPolicy
{
    public static bool IsKnown(string id)=>id is "community.sign" or "community.notice" or "community.planter" or "soup.sign" or "soup.menu" or "soup.note"
        or "street.doorplate" or "street.newsstand" or "street.bike_shed" or "street.drain" or "street.wall_paint" or "street.laundry_rack" or "street.stool";
    public static WorldSnapshot Mark(WorldSnapshot s,string id)=>!IsKnown(id)||s.DiscoveredIds.Contains(id)?s:s with{DiscoveredIds=new HashSet<string>(s.DiscoveredIds){id}};
}
