namespace GeXingzhou.Domain;
public record InteractionCandidate(string Id,float X,bool Reachable);
public class InteractionPolicy
{
    private readonly float radius,hysteresis; private readonly double guard; private double last=double.NegativeInfinity;
    public InteractionPolicy(float radius=40,float hysteresis=8,double guard=0.25) {this.radius=radius;this.hysteresis=hysteresis;this.guard=guard;}
    public string? Select(IReadOnlyList<InteractionCandidate> targets,string? currentId,float playerX)
    {
        var current=targets.FirstOrDefault(t=>t.Id==currentId && t.Reachable && Math.Abs(t.X-playerX)<=radius+hysteresis);
        return current?.Id ?? targets.Where(t=>t.Reachable && Math.Abs(t.X-playerX)<=radius).OrderBy(t=>Math.Abs(t.X-playerX)).ThenBy(t=>t.Id,StringComparer.Ordinal).FirstOrDefault()?.Id;
    }
    public bool TryActivate(string id,double activeTime)
    {
        if(string.IsNullOrWhiteSpace(id)||!double.IsFinite(activeTime)||activeTime-last<guard) return false;
        last=activeTime;return true;
    }
}
