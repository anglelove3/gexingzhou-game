using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class StreetDiscoveryChecks
{
    public static readonly string[] Ids={"street.doorplate","street.newsstand","street.bike_shed","street.drain","street.wall_paint","street.laundry_rack","street.stool"};
    public static void Register(List<(string,string,Action)> tests)
    {
        // Break: leaving runtime's six-ID whitelist makes legitimate street reads disappear.
        tests.Add(("StreetDiscovery","KnownIdempotentNoStoryMutation",()=>{
            var initial=StreetFixtures.V4() with{CandyCount=2,ChoiceCodes=new(){["hey-1"]="stay"},CompletedActions=new(){"observation.community.first"}};
            var s=initial;
            foreach(var id in Ids){Check.True(DiscoveryPolicy.IsKnown(id));s=DiscoveryPolicy.Mark(s,id);Check.True(s.DiscoveredIds.Contains(id));Check.True(ReferenceEquals(s,DiscoveryPolicy.Mark(s,id)));}
            Check.Equal(0,initial.DiscoveredIds.Count);Check.Equal(7,s.DiscoveredIds.Count);
            Check.Equal(initial.Stage,s.Stage);Check.Equal(2,s.CandyCount);Check.Equal(initial.PlayerPosition,s.PlayerPosition);
            Check.True(initial.CompletedActions.SetEquals(s.CompletedActions));Check.Equal("stay",s.ChoiceCodes["hey-1"]);
            foreach(var bad in new[]{"STREET.DRAIN","street.drain.extra","street.fake",""})Check.True(ReferenceEquals(s,DiscoveryPolicy.Mark(s,bad)));
        }));
        // Break: appending the street after soup or using the soup location misrepresents the walk.
        tests.Add(("StreetDiscovery","ThirteenOrderAndLocation",()=>{
            var all=new[]{"community.sign","community.notice","community.planter"}.Concat(Ids).Concat(new[]{"soup.sign","soup.menu","soup.note"}).ToArray();
            var s=StreetFixtures.V4() with{DiscoveredIds=new(all.Reverse())};var before=StreetFixtures.Json(s);
            var view=JournalProjection.Build(s);Check.Equal(string.Join(',',all),string.Join(',',view.Discoveries.Select(x=>x.Id)));
            foreach(var entry in view.Discoveries.Where(x=>x.Id.StartsWith("street.")))Check.Equal("在便利店街看过，可回到场景再次观察",entry.Body);
            Check.Equal(before,StreetFixtures.Json(s));Check.Equal(0,view.History.Count);
        }));
        // Break: runtime expansion must never expand the frozen v2/v3 save protocols.
        tests.Add(("StreetDiscovery","FrozenProtocolsAndCurrentRoundTrip",()=>{
            var current=new SaveV4Codec(StreetFixtures.Current());
            foreach(var id in Ids){
                Check.True(SaveMigrationFixtures.Codec().Validate(SaveMigrationFixtures.Depth() with{DiscoveredIds=new(){id}})!=null);
                Check.True(new SaveV3Codec(StreetFixtures.FrozenV3()).Validate(SaveV3Codec.CreateNew(new(),StreetFixtures.FrozenV3()) with{DiscoveredIds=new(){id}})!=null);
            }
            var all=StreetFixtures.V4() with{DiscoveredIds=new(Ids)};
            Check.Equal(LoadStatus.Loaded,current.Read(StreetFixtures.Json(all)).Status);
            Check.True(current.Read(StreetFixtures.Json(all)).Snapshot!.DiscoveredIds.SetEquals(Ids));
        }));
    }
}
