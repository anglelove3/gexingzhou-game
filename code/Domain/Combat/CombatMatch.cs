namespace GeXingzhou.Domain;

public sealed class CombatMatch
{
    public CombatCatalog Catalog { get; }
    public CombatWave Wave { get; }
    public CombatFighter Player { get; }
    public List<CombatFighter> Enemies { get; } = new();
    public List<CombatEvent> Events { get; } = new();
    public CombatStatistics Statistics { get; } = new();
    public CombatResult Result { get; private set; }
    public int TickCount { get; private set; }
    public bool HitStopped => hitstop > 0;
    private int hitstop, nextPermit, queueFrames;
    private string? permitOwner, queued;

    public CombatMatch(CombatCatalog catalog, CombatProgress progress, string waveId)
    {
        if (progress.Validate(catalog) is { } error) throw new ArgumentException(error);
        if (!progress.CanEnter(waveId, catalog)) throw new ArgumentException("波次尚未解锁。");
        Catalog = catalog; Wave = catalog.Wave(waveId);
        Player = new("player", "葛行舟", catalog.Stats(progress), 360, 480);
        for (int i = 0; i < Wave.Enemies.Length; i++)
        {
            var type = catalog.Enemies.First(e => e.Id == Wave.Enemies[i]);
            var e = CombatFighter.FromEnemy("enemy" + i, type, 790 + i * 105, 480 + (i % 2 == 0 ? -24 : 36));
            e.Facing = -1; e.RoamSide = i % 2 == 0 ? 1 : -1;
            if (type.Id == "guard") e.SetGuard(true);
            Enemies.Add(e);
        }
    }

    public void Step(CombatInput input)
    {
        Events.Clear();
        if (Result != CombatResult.Running) return;
        if (hitstop > 0) { hitstop--; BufferInput(input); return; }
        TickCount++; Statistics.ActiveFrames++;
        BufferInput(input);
        Player.Tick();
        foreach (var enemy in Enemies) enemy.Tick();
        PlayerAction(input);
        UpdateEnemies();
        ApplyAttack(Player, Enemies);
        foreach (var enemy in Enemies) ApplyAttack(enemy, new[] { Player });
        if (Player.Health == 0) Result = CombatResult.Lost;
        else if (Enemies.All(e => e.Health == 0)) Result = CombatResult.Won;
        if (Result != CombatResult.Running) Events.Add(new(Result == CombatResult.Won ? "win" : "loss", "player", "", 0, Player.X, Player.Y));
    }

    private void BufferInput(CombatInput input)
    {
        if (input.LightPressed)
        {
            queued = "light1"; queueFrames = 6;
            if (Player.State == CombatState.Attack && Player.Attack is { IsLight: true } attack && attack.Total - Player.AttackFrame <= 8)
            {
                queued = attack.Id switch { "light1" => "light2", "light2" => "light3", _ => "light1" };
                queueFrames = attack.Total - Player.AttackFrame + 2;
            }
        }
        if (input.HeavyPressed) { queued = "charge"; queueFrames = 6; }
        if (input.DodgePressed) { queued = "dodge"; queueFrames = 6; }
    }

    private void PlayerAction(CombatInput input)
    {
        if (Player.State is CombatState.Hurt or CombatState.Down or CombatState.Rising or CombatState.Dead)
        { queued = null; queueFrames = 0; return; }
        if (Player.State == CombatState.Charging)
        {
            if (input.DodgePressed || input.GuardHeld) { Player.CancelCharge(); queued = null; return; }
            Player.Move(input.MoveX, input.MoveY, .4, false);
            if (input.HeavyReleased || !input.HeavyHeld || Player.StateFrame >= 45)
            {
                var charged = Catalog.EquippedMove("charged", Player.Stats);
                var id = Player.StateFrame >= 27 && Player.Stamina >= charged.Cost ? "charged" : "heavy";
                if (!StartPlayerAttack(id)) Player.CancelCharge();
            }
            return;
        }
        if (Player.CanAct)
        {
            if (Player.State == CombatState.Idle && Math.Abs(input.MoveX) > .01) Player.Facing = Math.Sign(input.MoveX);
            Player.SetGuard(input.GuardHeld);
            if (queueFrames > 0 && queued != null)
            {
                switch (queued)
                {
                    case "dodge":
                        if (Player.TryDodge(input.MoveX, input.MoveY))
                        { Statistics.Dodges++; Events.Add(new("dodge", Player.Id, "", 0, Player.X, Player.Y)); }
                        else EmptyStamina();
                        break;
                    case "charge": Player.Charge(); break;
                    default: StartPlayerAttack(queued); break;
                }
                queued = null; queueFrames = 0;
            }
            if (Player.CanAct) Player.Move(input.MoveX, input.MoveY, input.GuardHeld ? .4 : 1, !input.GuardHeld);
        }
        if (queueFrames > 0 && --queueFrames == 0) queued = null;
    }

    private bool StartPlayerAttack(string id)
    {
        var move = Catalog.EquippedMove(id, Player.Stats);
        if (!Player.TryAttack(move)) { EmptyStamina(); return false; }
        if (move.IsLight) Statistics.LightAttacks++; else Statistics.HeavyAttacks++;
        Events.Add(new("attack", Player.Id, "", 0, Player.X, Player.Y)); return true;
    }

    private void EmptyStamina()
    {
        Statistics.EmptyStamina++; Events.Add(new("tired", Player.Id, "", 0, Player.X, Player.Y));
    }

    private void UpdateEnemies()
    {
        var owner = Enemies.FirstOrDefault(e => e.Id == permitOwner);
        if (owner != null && (owner.State != CombatState.Attack || owner.AttackFrame >= owner.Attack!.Startup + owner.LastActiveOffset))
        { permitOwner = null; nextPermit = TickCount + 15; }
        foreach (var e in Enemies)
        {
            if (e.Health <= 0) continue;
            if (e.FinishedAttack)
            {
                e.RoamSide *= -1; e.Recover(e.Enemy!.Id == "guard" ? 30 : 22);
            }
            if (e.State == CombatState.Recovery)
            {
                e.Move(-e.Facing, e.Enemy!.Id == "roamer" ? e.RoamSide * .4 : 0, .55, false);
                continue;
            }
            if (e.State == CombatState.Guard)
            {
                if (e.StateFrame >= 48) e.SetGuard(false);
                else continue;
            }
            if (e.State == CombatState.Attack)
            {
                if (e.AttackFrame < e.Attack!.Startup && Math.Abs(Player.X - e.X) > .1) e.Facing = Math.Sign(Player.X - e.X);
                continue;
            }
            if (e.State != CombatState.Idle) continue;
            e.WaitingFrames++;
            if (permitOwner != null && permitOwner != e.Id)
            {
                var side = Enemies.IndexOf(e) % 2 == 0 ? -1 : 1;
                var goalX = Math.Clamp(Player.X + (e.X >= Player.X ? 64 : -64), 92, 1188);
                var goalY = Math.Clamp(Player.Y + side * 44, 390, 624);
                var gapX = goalX - e.X; var gapY = goalY - e.Y;
                var gap = Math.Sqrt(gapX * gapX + gapY * gapY);
                if (gap > 4) e.Move(gapX / gap, gapY / gap, .6);
                continue;
            }
            var dx = Player.X - e.X; var dy = Player.Y - e.Y;
            if (Math.Abs(dx) > .1) e.Facing = Math.Sign(dx);
            if (Math.Abs(dx) > 48 || Math.Abs(dy) > 16)
            {
                var aimY = dy;
                if (e.Enemy!.Id == "roamer" && Math.Abs(dx) > 130) aimY += e.RoamSide * 48;
                var length = Math.Sqrt(dx * dx + aimY * aimY);
                if (length > 0) e.Move(dx / length, aimY / length);
            }
            else if (e.Enemy!.Id == "guard" && e.Cooldown > 0) e.SetGuard(true);
        }
        if (permitOwner != null || TickCount < nextPermit) return;
        var ready = Enemies.Where(e => e.State == CombatState.Idle && e.Cooldown == 0 &&
            Math.Abs(e.X - Player.X) <= 58 && Math.Abs(e.Y - Player.Y) <= 22)
            .OrderByDescending(e => e.WaitingFrames).ThenBy(e => e.Id, StringComparer.Ordinal).FirstOrDefault();
        if (ready == null) return;
        StartEnemyAttack(ready);
    }

    private void StartEnemyAttack(CombatFighter e)
    {
        var type = e.Enemy!; var index = e.AttackCount % 3; bool leader = type.Id == "leader";
        bool heavy = type.Id == "guard" || leader && index == 2;
        int second = leader && index == 1 ? 18 : 0;
        var power = heavy ? (leader ? 1.5 : 1.3) : second > 0 ? .8 : 1;
        var move = new CombatMove
        {
            Id = heavy ? "enemy_heavy" : "enemy_jab", Name = heavy ? "重击预警" : second > 0 ? "二连预警" : "出拳预警",
            Power = power, Startup = (int)Math.Round((leader && heavy ? .85 : type.Warning) * 60),
            Active = heavy ? 6 : 4, Recovery = heavy ? 30 : 20, Cost = 0,
            Reach = 58, Depth = 22, Stun = heavy ? .32 : .2, Push = heavy ? 22 : 10
        };
        if (!e.TryAttack(move, second)) return;
        e.AttackCount++; e.WaitingFrames = 0;
        e.Cooldown = (int)Math.Round((leader && e.Health <= type.Health * .4 ? 1.5 : type.Interval) * 60);
        permitOwner = e.Id; Events.Add(new("warning", e.Id, "player", 0, e.X, e.Y));
    }

    private void ApplyAttack(CombatFighter source, IEnumerable<CombatFighter> targets)
    {
        if (source.ActivePulse < 0) return;
        var move = source.Attack!;
        foreach (var target in targets)
        {
            if (!source.InRange(target, move) || !source.RegisterTarget(target)) continue;
            var hit = target.ReceiveHit(source, move);
            if (!hit.Applied) continue;
            Events.Add(new(hit.Blocked ? "block" : hit.GuardBroken ? "break" : hit.KnockedDown ? "down" : "hit", source.Id, target.Id, hit.Damage, target.X, target.Y));
            hitstop = Math.Max(hitstop, move.IsLight || move.Id == "enemy_jab" ? 2 : 4);
            if (source == Player) Statistics.DamageDealt += hit.Damage;
            if (target == Player)
            {
                Statistics.DamageTaken += hit.Damage;
                if (hit.Blocked) Statistics.Blocks++; else Statistics.HitsTaken++;
            }
        }
    }
}
