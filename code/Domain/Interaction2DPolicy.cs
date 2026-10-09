namespace GeXingzhou.Domain;
public sealed record Interaction2DCandidate(string Id,Position2 Foot,bool Reachable);
public sealed class Interaction2DPolicy
{
    private readonly float radius,hysteresis;private readonly double guard;
    private double last=double.NegativeInfinity;
    public Interaction2DPolicy(float radius=40,float hysteresis=8,double guard=.25)
    {
        if(!float.IsFinite(radius)||radius<0||!float.IsFinite(hysteresis)||hysteresis<0||!double.IsFinite(guard)||guard<0)
            throw new ArgumentException("Invalid interaction range or repeat guard");
        this.radius=radius;this.hysteresis=hysteresis;this.guard=guard;
    }
    public string? Select(IReadOnlyList<Interaction2DCandidate> targets,string? currentId,Position2 player)
    {
        if(targets==null||!float.IsFinite(player.X)||!float.IsFinite(player.Y))return null;
        var reachable=targets.Where(t=>t!=null&&t.Reachable&&!string.IsNullOrWhiteSpace(t.Id)&&float.IsFinite(t.Foot.X)&&float.IsFinite(t.Foot.Y));
        var current=reachable.FirstOrDefault(t=>t.Id==currentId&&NavigationGeometry.Distance(t.Foot,player)<=radius+hysteresis);
        return current?.Id??reachable.Where(t=>NavigationGeometry.Distance(t.Foot,player)<=radius)
            .OrderBy(t=>NavigationGeometry.Distance(t.Foot,player)).ThenBy(t=>t.Id,StringComparer.Ordinal).FirstOrDefault()?.Id;
    }
    public bool TryActivate(string id,double activeTime)
    {
        if(string.IsNullOrWhiteSpace(id)||!double.IsFinite(activeTime)||activeTime<0||activeTime-last<guard)return false;
        last=activeTime;return true;
    }
}
