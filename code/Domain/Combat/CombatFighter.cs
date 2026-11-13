namespace GeXingzhou.Domain;

public enum CombatState { Idle, Attack, Charging, Guard, Dodge, Hurt, Down, Rising, Recovery, Dead }
public readonly record struct CombatInput(double MoveX = 0, double MoveY = 0, bool LightPressed = false,
    bool HeavyPressed = false, bool HeavyHeld = false, bool HeavyReleased = false, bool GuardHeld = false, bool DodgePressed = false);
public readonly record struct CombatHit(bool Applied, int Damage, bool Blocked, bool GuardBroken, bool KnockedDown);

public sealed class CombatFighter
{
    public string Id { get; }
    public string Name { get; }
    public CombatStats Stats { get; }
    public CombatEnemy? Enemy { get; }
    public double X { get; set; }
    public double Y { get; set; }
    public int Facing { get; set; } = 1;
    public int Health { get; private set; }
    public double Stamina { get; private set; }
    public CombatState State { get; private set; }
    public int StateFrame { get; private set; }
    public int AttackFrame => StateFrame;
    public CombatMove? Attack { get; private set; }
    public int SecondHitOffset { get; private set; }
    public int LastActiveOffset => SecondHitOffset + (Attack?.Active ?? 0);
    public int AttackSequence { get; private set; }
    public bool FinishedAttack { get; private set; }
    public bool FinishedRecovery { get; private set; }
    public int Cooldown { get; set; }
    public int WaitingFrames { get; set; }
    public int AttackCount { get; set; }
    public int RoamSide { get; set; } = 1;
    public bool IsMoving { get; private set; }
    public bool Protected => protectionFrames > 0 || State is CombatState.Down or CombatState.Rising or CombatState.Dead ||
        State == CombatState.Dodge && StateFrame >= 4 && StateFrame < 11;
    public bool CanAct => State is CombatState.Idle or CombatState.Guard;
    public int ActivePulse
    {
        get
        {
            if (State != CombatState.Attack || Attack == null) return -1;
            var elapsed = StateFrame - Attack.Startup;
            if (elapsed >= 0 && elapsed < Attack.Active) return 0;
            return SecondHitOffset > 0 && elapsed >= SecondHitOffset && elapsed < SecondHitOffset + Attack.Active ? 1 : -1;
        }
    }

    private int remainingFrames, protectionFrames, staminaDelay, dodgeCooldown, continuousStun, leaderKnockdown;
    private double dodgeX, dodgeY;
    private readonly HashSet<string> struck = new(StringComparer.Ordinal);

    public CombatFighter(string id, string name, CombatStats stats, double x, double y, CombatEnemy? enemy = null)
    {
        Id = id; Name = name; Stats = stats; X = x; Y = y; Enemy = enemy;
        Health = stats.Health; Stamina = stats.Stamina;
    }

    public static CombatFighter FromEnemy(string id, CombatEnemy enemy, double x, double y) =>
        new(id, enemy.Name, new(enemy.Health, enemy.Attack, enemy.Defense, 100, 24, enemy.Speed, .34, new CombatItem()), x, y, enemy);

    public void Move(double x, double y, double factor = 1, bool turn = true)
    {
        var length = Math.Sqrt(x * x + y * y);
        if (length > 1) { x /= length; y /= length; }
        X += x * Stats.MoveSpeed * factor / 60; Y += y * Stats.MoveSpeed * factor / 60;
        IsMoving = Math.Abs(x) + Math.Abs(y) > .01;
        if (turn && Math.Abs(x) > .01) Facing = Math.Sign(x);
        ClampPosition();
    }

    public void ClampPosition() { X = Math.Clamp(X, 92, 1188); Y = Math.Clamp(Y, 390, 624); }

    public bool TryAttack(CombatMove move, int secondHitOffset = 0)
    {
        if (State is not (CombatState.Idle or CombatState.Guard or CombatState.Charging) || Stamina < move.Cost) return false;
        Spend(move.Cost); Attack = move; SecondHitOffset = secondHitOffset;
        AttackSequence++; struck.Clear(); Enter(CombatState.Attack); return true;
    }

    public void Charge() { if (CanAct) Enter(CombatState.Charging); }
    public void CancelCharge() { if (State == CombatState.Charging) Recover(11); }
    public void SetGuard(bool held)
    {
        if (held && State == CombatState.Idle) Enter(CombatState.Guard);
        else if (!held && State == CombatState.Guard) Enter(CombatState.Idle);
    }

    public bool TryDodge(double x, double y)
    {
        if (!CanAct || dodgeCooldown > 0 || Stamina < 24) return false;
        if (Math.Abs(x) + Math.Abs(y) < .01) x = Facing;
        var length = Math.Sqrt(x * x + y * y);
        dodgeX = x / length; dodgeY = y / length;
        Spend(24); dodgeCooldown = 39; remainingFrames = Math.Max(11, (int)Math.Round(Stats.DodgeSeconds * 60));
        Enter(CombatState.Dodge); return true;
    }

    public bool InRange(CombatFighter target, CombatMove move)
    {
        var forward = (target.X - X) * Facing;
        return forward >= 0 && forward <= move.Reach && Math.Abs(target.Y - Y) <= move.Depth;
    }

    public bool RegisterTarget(CombatFighter target) => struck.Add(target.Id + ":" + ActivePulse);

    public CombatHit ReceiveHit(CombatFighter source, CombatMove move)
    {
        if (Protected) return default;
        int damage = CombatCatalog.Damage(source.Stats.Attack, move.Power, Stats.Defense);
        bool front = (source.X - X) * Facing >= 0;
        bool blocking = State == CombatState.Guard && front && !(Enemy != null && move.Id == "charged");
        if (blocking && Enemy != null)
        {
            damage = (int)Math.Floor(damage * .2); Health = Math.Max(0, Health - damage);
            if (Health == 0) Enter(CombatState.Dead);
            return new(true, damage, true, false, false);
        }
        bool broken = false;
        if (blocking)
        {
            int cost = (int)Math.Ceiling(damage * 1.5);
            broken = Stamina < cost;
            Spend(Math.Min(Stamina, cost));
            if (!broken)
            {
                damage = (int)Math.Floor(damage * .2); Health = Math.Max(0, Health - damage);
                if (Health == 0) Enter(CombatState.Dead);
                return new(true, damage, true, false, false);
            }
        }
        Health = Math.Max(0, Health - damage);
        if (Health == 0) { Enter(CombatState.Dead); return new(true, damage, false, broken, false); }
        bool knocked = move.Id == "charged";
        if (Enemy?.Id == "leader")
        {
            if (move.Id is "heavy" or "charged") leaderKnockdown++;
            knocked = leaderKnockdown >= 2;
            if (knocked) leaderKnockdown = 0;
        }
        X += source.Facing * move.Push; ClampPosition();
        if (knocked) { remainingFrames = 42; Enter(CombatState.Down); continuousStun = 0; }
        else
        {
            var stun = broken ? 39 : (int)Math.Ceiling(move.Stun * 60);
            if (Enemy?.Id == "leader") stun = Math.Min(stun, 10);
            if (broken) { remainingFrames = stun; continuousStun = 0; }
            else
            {
                var added = Math.Min(stun, 36 - continuousStun);
                continuousStun += added;
                var previousStun = State == CombatState.Hurt ? remainingFrames : 0;
                remainingFrames = Math.Max(previousStun, added);
                if (continuousStun >= 36) protectionFrames = remainingFrames + 21;
            }
            Enter(CombatState.Hurt);
        }
        return new(true, damage, false, broken, knocked);
    }

    private void Spend(double amount) { Stamina -= amount; staminaDelay = 42; }
    public void Recover(int frames) { remainingFrames = frames; Enter(CombatState.Recovery); }
    private void Enter(CombatState state) { State = state; StateFrame = 0; IsMoving = false; }

    public void Tick()
    {
        FinishedAttack = false; FinishedRecovery = false; IsMoving = false;
        if (State == CombatState.Dead) return;
        if (protectionFrames > 0) protectionFrames--;
        if (dodgeCooldown > 0) dodgeCooldown--;
        if (staminaDelay > 0) staminaDelay--;
        if (Cooldown > 0) Cooldown--;
        StateFrame++;
        switch (State)
        {
            case CombatState.Attack:
                if (StateFrame >= Attack!.Total + SecondHitOffset)
                { FinishedAttack = true; Enter(CombatState.Idle); }
                break;
            case CombatState.Dodge:
                var ticks = Math.Max(11, (int)Math.Round(Stats.DodgeSeconds * 60));
                X += dodgeX * 72 / ticks; Y += dodgeY * 72 / ticks; ClampPosition();
                if (--remainingFrames <= 0) Enter(CombatState.Idle);
                break;
            case CombatState.Hurt:
            case CombatState.Recovery:
                if (--remainingFrames <= 0)
                {
                    FinishedRecovery = State == CombatState.Recovery;
                    Enter(CombatState.Idle); continuousStun = 0;
                }
                break;
            case CombatState.Down:
                if (--remainingFrames <= 0) { Enter(CombatState.Rising); remainingFrames = 21; }
                break;
            case CombatState.Rising:
                if (--remainingFrames <= 0) { Enter(CombatState.Idle); protectionFrames = 21; }
                break;
        }
        if (staminaDelay == 0 && State is CombatState.Idle or CombatState.Guard)
            Stamina = Math.Min(Stats.Stamina, Stamina + Stats.Recovery / 60 * (State == CombatState.Guard ? .5 : 1));
    }
}
