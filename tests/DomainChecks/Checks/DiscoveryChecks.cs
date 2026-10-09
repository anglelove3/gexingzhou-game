using GeXingzhou.Domain;
namespace GeXingzhou.DomainChecks;
public static class DiscoveryChecks
{
    private static bool Known(string id)=>DiscoveryPolicy.IsKnown(id);
    private static WorldSnapshot Mark(WorldSnapshot s,string id)=>DiscoveryPolicy.Mark(s,id);
    public static void Register(List<(string,string,Action)> tests)
    {
        // Break: accepting unknown identities would admit unapproved story discoveries into v2.
        tests.Add(("Discovery","KnownOnly",()=>{foreach(var id in new[]{"soup.sign","soup.menu","soup.note"})Check.True(Known(id));foreach(var id in new[]{"","soup.secret_identity","SOUP.NOTE"})Check.True(!Known(id));var s=SaveV2Codec.CreateNew(new());Check.True(ReferenceEquals(s,Mark(s,"bad")));}));
        // Break: mutating the old set would change an already retained save; repeating must not write again.
        tests.Add(("Discovery","Idempotent",()=>{var s=SaveV2Codec.CreateNew(new());var once=Mark(s,"soup.note");Check.Equal(0,s.DiscoveredIds.Count);Check.Equal(1,once.DiscoveredIds.Count);Check.True(ReferenceEquals(once,Mark(once,"soup.note")));Check.True(Mark(once,"soup.menu").DiscoveredIds.SetEquals(new[]{"soup.note","soup.menu"}));}));
        tests.Add(("Discovery","NoStoryMutation",()=>{var s=SaveV2Codec.CreateNew(new()) with{Stage=SliceStage.CandyHeyDelivered,CandyCount=1,CompletedActions=new(){"candy.hey.delivered"}};var once=Mark(s,"soup.sign");Check.Equal(s.Stage,once.Stage);Check.Equal(1,once.CandyCount);Check.True(once.CompletedActions.SetEquals(s.CompletedActions));Check.Equal(s.PlayerPosition,once.PlayerPosition);Check.Equal(0,once.ChoiceCodes.Count);}));
    }
}
