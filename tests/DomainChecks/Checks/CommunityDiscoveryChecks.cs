using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class CommunityDiscoveryChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        // Break: missing community IDs makes a completed optional observation disappear from the current save.
        tests.Add(("CommunityDiscovery","KnownAndIdempotent",()=>{
            var s=SaveV3Codec.CreateNew(new(),CommunityFixtures.Current());
            foreach(var id in new[]{"community.sign","community.notice","community.planter"}){
                Check.True(DiscoveryPolicy.IsKnown(id));var next=DiscoveryPolicy.Mark(s,id);
                Check.True(next.DiscoveredIds.Contains(id));Check.True(ReferenceEquals(next,DiscoveryPolicy.Mark(next,id)));
                Check.Equal(0,s.DiscoveredIds.Count);Check.Equal(s.Stage,next.Stage);Check.True(s.CompletedActions.SetEquals(next.CompletedActions));
            }
            foreach(var id in new[]{"COMMUNITY.SIGN","community.fake",""})Check.True(!DiscoveryPolicy.IsKnown(id));
        }));
        // Break: unordered or soup-only projection gives discoveries the wrong place and duplicates retained history.
        tests.Add(("CommunityDiscovery","JournalSixOrderAndLocation",()=>{
            var s=new WorldSnapshot{DiscoveredIds=new(){"soup.note","community.planter","soup.sign","community.notice","soup.menu","community.sign"},CompletedActions=new(){"invitation.meeting_complete:invitation-1","candy.hey.delivered:hey-1","soup.meet:soup-seat-1","memory.return:soup-1","memory.food.resolve:soup-1","soup.payment:soup-payment-1","slice.complete:slice-1"}};
            var before=SaveMigrationFixtures.Json(s);var view=JournalProjection.Build(s);
            Check.Equal("community.sign,community.notice,community.planter,soup.sign,soup.menu,soup.note",string.Join(',',view.Discoveries.Select(x=>x.Id)));
            Check.Equal("meeting,hey,soup,memory,payment,slice",string.Join(',',view.History.Select(x=>x.Id)));
            foreach(var item in view.Discoveries)Check.True(item.Body.Contains(item.Id.StartsWith("community.")?"安置小区":"鸭血粉丝汤店"));
            Check.Equal(before,SaveMigrationFixtures.Json(s));s.DiscoveredIds.Clear();Check.Equal(6,view.Discoveries.Count);
            Check.Throws<NotSupportedException>(()=>((IList<JournalEntry>)view.Discoveries).Clear());
        }));
        // Break: expanding runtime policy must not silently expand the frozen v2 protocol.
        tests.Add(("CommunityDiscovery","FrozenV2StillRejects",()=>{
            var codec=SaveMigrationFixtures.Codec();Check.True(codec.Validate(SaveMigrationFixtures.Depth() with{DiscoveredIds=new(){"community.sign"}})!=null);
        }));
    }
}
