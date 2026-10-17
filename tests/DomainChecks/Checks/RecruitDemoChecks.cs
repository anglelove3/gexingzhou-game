using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class RecruitDemoChecks
{
    public static void Register(List<(string,string,Action)> tests)
    {
        // Break: generic location-only bodies make different discovered objects indistinguishable.
        tests.Add(("RecruitDemo","DistinctMemoriesOnlyForSeenObjects",()=>{
            var ids=new[]{"community.sign","community.notice","community.planter","street.doorplate","street.newsstand","street.bike_shed","street.drain","street.wall_paint","street.laundry_rack","street.stool","soup.sign","soup.menu","soup.note"};
            var snapshot=StreetFixtures.V4() with{DiscoveredIds=new(ids)};
            var before=StreetFixtures.Json(snapshot);var entries=JournalProjection.Build(snapshot).Discoveries;
            Check.Equal(13,entries.Select(x=>x.Body).Distinct().Count());
            foreach(var id in ids){var one=JournalProjection.Build(snapshot with{DiscoveredIds=new(){id}}).Discoveries;Check.Equal(1,one.Count);Check.Equal(id,one[0].Id);}
            Check.Equal(before,StreetFixtures.Json(snapshot));
            Check.Equal(0,JournalProjection.Build(snapshot with{DiscoveredIds=new(){"unknown"}}).Discoveries.Count);
        }));
        // Break: putting the checklist first hides the closing thought until the player scrolls.
        tests.Add(("RecruitDemo","EndingLeadsWithFactBasedCoda",()=>{
            var complete=StreetFixtures.V4() with{CompletedActions=new(){"slice.complete:slice-1","soup.payment:soup-payment-1"}};
            var text=JournalProjection.EndReflection(complete);var checklist=text.IndexOf("今天留下的几笔",StringComparison.Ordinal);
            Check.True(checklist>0);Check.True(text[..checklist].Contains("明天"));
            Check.True(!JournalProjection.EndReflection(complete with{CompletedActions=new()}).Contains("答应"));
            Check.True(!text.Contains("旧门牌"));
        }));
        // Break: after legitimately walking away, advice must lead back before naming an absent chair/coin.
        tests.Add(("RecruitDemo","ResumeRouteFollowsCurrentScene",()=>{
            foreach(var stage in new[]{SliceStage.SoupMeet,SliceStage.MemoryActive,SliceStage.MemoryReturned}){
                var initial=StreetFixtures.V4() with{Stage=stage,SceneId="community_gate"};
                Check.True(JournalProjection.RouteHint(initial).Contains("右"));
                var street=initial with{SceneId="convenience_street"};
                Check.True(JournalProjection.RouteHint(street).Contains("尽头"));
            }
            Check.True(JournalProjection.RouteHint(StreetFixtures.V4() with{Stage=SliceStage.MemoryActive,SceneId="soup_shop"}).Contains("桌边"));
            Check.True(JournalProjection.RouteHint(StreetFixtures.V4() with{Stage=SliceStage.MemoryActive,SceneId="memory_soup_table"}).Contains("硬币"));
        }));
    }
}
