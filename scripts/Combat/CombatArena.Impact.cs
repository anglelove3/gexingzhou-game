using Godot;
using GeXingzhou.Domain;

public partial class CombatArena
{
    public bool Animating { get; set; } = true;
    private bool reducedMotion;
    public bool ReducedMotion
    {
        get => reducedMotion;
        set { if (reducedMotion == value) return; reducedMotion = value; QueueRedraw(); }
    }
    private readonly List<Impact> impacts = new();
    private sealed class Impact
    {
        public required CombatEvent Event;
        public double Age;
    }

    private static float Weight(string move) => move switch
    {
        "light2" => 1.15f, "light3" => 1.5f, "sweep" => 1.65f, "heavy" or "enemy_heavy" => 1.8f, "charged" => 2.3f, _ => 1
    };

    public Vector2 CameraOffset
    {
        get
        {
            if (ReducedMotion || !Animating || impacts.Count == 0) return Vector2.Zero;
            var hit = impacts.Where(h => h.Event.Kind != "block").MaxBy(h => Weight(h.Event.MoveId) * Math.Max(0, 1 - h.Age / .15));
            if (hit == null || hit.Age >= .15) return Vector2.Zero;
            float strength = Math.Max(0, Weight(hit.Event.MoveId) - 1) * 2.2f * (1 - (float)hit.Age / .15f);
            return new Vector2(Mathf.Sin((float)hit.Age * 110) * strength * hit.Event.Facing,
                Mathf.Sin((float)hit.Age * 81) * strength * .4f);
        }
    }

    private Vector2 BodyOffset(CombatFighter fighter)
    {
        if (ReducedMotion) return Vector2.Zero;
        var hit = impacts.LastOrDefault(h => h.Event.TargetId == fighter.Id);
        if (hit == null || hit.Event.Kind == "block") return Vector2.Zero;
        float t = Mathf.Clamp((float)hit.Age / .12f, 0, 1);
        // Smooth the already applied logical push; range and collision remain authoritative.
        float x = -(float)hit.Event.Displacement * (1 - t) * (1 - t);
        if (Math.Abs(hit.Event.Displacement) < .01) x += Mathf.Sin(t * Mathf.Pi) * hit.Event.Facing * 5;
        return new Vector2(x, -Mathf.Sin(t * Mathf.Pi) * 2);
    }

    private void DrawImpact(Impact hit)
    {
        float t = (float)(hit.Age / .22); if (t >= 1) return;
        var ev = hit.Event; float weight = Weight(ev.MoveId);
        bool sweep = ev.MoveId == "sweep";
        bool frontKick = ev.MoveId == "light3" && Match!.Wave.Footwork;
        var center = new Vector2((float)ev.X, (float)ev.Y - (sweep ? 15 : frontKick ? 63 : 74));
        bool blocked = ev.Kind == "block";
        var color = blocked ? new Color(.66f, .86f, 1, (1 - t) * .85f) : new Color(1, .86f, .56f, (1 - t) * .9f);
        if (blocked)
        {
            DrawArc(center, 13 + t * 14, -1.2f, 1.2f, 14, color, 2, true); return;
        }
        if (sweep)
        {
            for (int i = 0; i < 5; i++)
            {
                var dust = center + new Vector2((i - 2) * (7 + 10 * t), -i * (1 + t));
                DrawCircle(dust, (4 + i) * (1 + t), new Color(.9f, .74f, .51f, (1 - t) * .5f));
            }
        }
        float radius = (8 + t * 18) * weight;
        DrawArc(center, radius * .55f, -.8f, .8f + Mathf.Pi, 16, color with { A = color.A * .45f }, 1.5f, true);
        for (int i = 0; i < 5; i++)
        {
            float angle = -.95f + i * .47f;
            var direction = new Vector2(Mathf.Cos(angle) * ev.Facing, Mathf.Sin(angle));
            DrawLine(center + direction * radius * .35f, center + direction * radius, color, i == 2 ? 3 : 1.5f, true);
        }
        DrawLine(center - new Vector2(ev.Facing * 11, 6) * (1 - t), center + new Vector2(ev.Facing * 11, 6) * (1 - t), color, 2, true);
    }

    private void DrawAttackTrail(CombatFighter fighter, Vector2 feet)
    {
        if (fighter.ActivePulse < 0 || fighter.Attack == null) return;
        float weight = Weight(fighter.Attack.Id);
        var origin = feet + new Vector2(fighter.Facing * 22, fighter.Attack.Id switch
        {
            "sweep" => -16,
            "light3" when Match!.Wave.Footwork && fighter.Enemy == null => -64,
            _ => -76
        });
        float start = fighter.Facing > 0 ? -.9f : Mathf.Pi - .9f;
        DrawArc(origin, 16 + weight * 7, start, start + 1.7f, 12,
            new Color(.96f, .84f, .61f, .35f), Math.Min(weight, 2), true);
    }
}
