using GeXingzhou.Domain;

namespace GeXingzhou.DomainChecks;

public static class CombatAutomation
{
    public static CombatInput Decide(CombatMatch match)
    {
        var p = match.Player;
        var e = match.Enemies.Where(f => f.Health > 0).OrderBy(f => Math.Abs(f.X - p.X) + Math.Abs(f.Y - p.Y) * 2).FirstOrDefault();
        if (e == null) return default;
        double dx = e.X - p.X, dy = e.Y - p.Y;
        double x = Math.Abs(dx) > 42 ? Math.Sign(dx) : p.Facing != Math.Sign(dx) ? Math.Sign(dx) * .05 : 0;
        double y = Math.Abs(dy) > 7 ? Math.Clamp(dy / 18, -1, 1) : 0;
        if (p.CanAct && p.Stamina < 42)
            return new CombatInput(MoveX: p.X < 140 ? 1 : p.X > 1140 ? -1 : -Math.Sign(dx), MoveY: match.TickCount % 300 < 150 ? 1 : -1);
        if (p.State == CombatState.Charging)
            return new CombatInput(MoveX: x, MoveY: y, HeavyHeld: p.StateFrame < 28, HeavyReleased: p.StateFrame >= 28);
        if (p.State == CombatState.Attack)
            return new CombatInput(MoveX: x, MoveY: y, LightPressed: p.Attack!.IsLight && p.Attack.Total - p.AttackFrame <= 7 && Math.Abs(dx) < 75);
        if (!p.CanAct) return default;
        var incoming = match.Enemies.FirstOrDefault(f => f.Enemy?.Id == "leader" && f.State == CombatState.Attack &&
            f.AttackFrame < f.Attack!.Startup + f.LastActiveOffset && Math.Abs(f.X - p.X) <= 75 && Math.Abs(f.Y - p.Y) <= 26);
        if (incoming != null)
            return new CombatInput(MoveX: p.Facing != Math.Sign(incoming.X - p.X) ? Math.Sign(incoming.X - p.X) * .05 : 0, GuardHeld: true);
        bool close = Math.Abs(dx) <= 64 && Math.Abs(dy) <= 18;
        bool heavy = e.State is CombatState.Guard or CombatState.Down or CombatState.Rising || e.Enemy?.Id == "leader" &&
            (e.State == CombatState.Recovery || e.State == CombatState.Attack && e.AttackFrame >= e.Attack!.Startup + e.LastActiveOffset);
        return new CombatInput(MoveX: x, MoveY: y, LightPressed: close && !heavy, HeavyPressed: close && heavy, HeavyHeld: close && heavy);
    }
}
