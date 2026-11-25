using Godot;
using GeXingzhou.Domain;
using System.Text.Json;

public partial class CombatArena : Node2D
{
    private CombatMatch? match;
    public CombatMatch? Match
    {
        get => match;
        set { if (!ReferenceEquals(match, value)) { floating.Clear(); impacts.Clear(); } match = value; QueueRedraw(); }
    }
    private Texture2D court = null!, player = null!, opponents = null!, items = null!, footwork = null!;
    private AtlasData playerFrames = null!, opponentFrames = null!, itemFrames = null!, footworkFrames = null!;
    private Font font = null!;
    private readonly List<FloatingHit> floating = new();
    private const int FloatingTextSize = 20;
    private const float OpponentBarWidth = 78;
    private const float StandardFighterHeight = 136;
    private const int OpponentLabelSize = 16;
    private const int StartupCueSize = 22;
    private const string GuardHint = "守势 · 可蓄力破势";

    private sealed record FrameData(double X, double Y, double Width, double Height, double PivotX, double PivotY, double VisibleHeight)
    {
        public Rect2 Region => new((float)X, (float)Y, (float)Width, (float)Height);
    }
    private sealed record AtlasData(FrameData[] Frames);
    private sealed class FloatingHit
    {
        public required CombatEvent Event;
        public double Age;
        public float OffsetX;
        public int? SweepCast;
    }

    public override void _Ready()
    {
        const string root = "res://assets/art/combat-v1/";
        court = GD.Load<Texture2D>(root + "practice-court.png"); player = GD.Load<Texture2D>(root + "player-poses.png");
        opponents = GD.Load<Texture2D>(root + "opponent-poses-v2.png"); items = GD.Load<Texture2D>(root + "equipment.png");
        playerFrames = ReadAtlas(root + "player-poses.json"); opponentFrames = ReadAtlas(root + "opponent-poses-v2.json");
        itemFrames = ReadAtlas(root + "equipment.json");
        footwork = GD.Load<Texture2D>(root + "player-footwork-v1.png");
        footworkFrames = ReadAtlas(root + "player-footwork-v1.json");
        font = GD.Load<Font>("res://assets/fonts/SourceHanSerifSC-SemiBold.otf");
    }

    private static AtlasData ReadAtlas(string path) => JsonSerializer.Deserialize<AtlasData>(Godot.FileAccess.GetFileAsString(path))
        ?? throw new InvalidOperationException("战斗图集元数据缺失。");

    public Texture2D ItemIcon(string id, CombatCatalog c)
    {
        var index = c.Items.FindIndex(i => i.Id == id);
        return new AtlasTexture { Atlas = items, Region = itemFrames.Frames[index].Region };
    }

    public void ShowEvents(IEnumerable<CombatEvent> events)
    {
        foreach (var ev in events)
        {
            if (ev.Kind is "hit" or "block" or "down" or "break" or "tired" or "restore" or "perfect" or "riposte" or "break-bonus" or "cancel" or "area-double-hit")
                AddFloatingHit(ev);
            if (ev.Kind is "hit" or "block" or "down" or "break")
            {
                if (impacts.Count >= 24) impacts.RemoveAt(0);
                impacts.Add(new() { Event = ev });
            }
        }
    }

    private void AddFloatingHit(CombatEvent ev)
    {
        var hit = new FloatingHit { Event = ev };
        floating.Add(hit);
        if (ev.MoveId == CombatMove.AreaSkillId && ev.SourceId == "player" && ev.TargetId.Length > 0 && Match is { } current)
        {
            hit.SweepCast = current.Player.AttackSequence;
            SpaceSweepNumbers(current.Player.AttackSequence);
        }
    }

    private void SpaceSweepNumbers(int cast)
    {
        var hits = floating.Where(hit => hit.SweepCast == cast)
            .OrderBy(hit => hit.Event.X).ThenBy(hit => hit.Event.Y).ToArray();
        const float gap = 8;
        hits[0].OffsetX = 0;
        for (var i = 1; i < hits.Length; i++)
        {
            var previous = hits[i - 1];
            var width = font.GetStringSize(FloatingText(previous.Event), fontSize: FloatingTextSize).X;
            var separation = (float)(hits[i].Event.X - previous.Event.X);
            hits[i].OffsetX = Math.Max(0, previous.OffsetX + width + gap - separation);
        }
        // Keep the group centered on the contacts after making room for each label.
        var centerOffset = hits.Average(hit => hit.OffsetX);
        foreach (var hit in hits) hit.OffsetX -= centerOffset;
    }

    private static string FloatingText(CombatEvent ev) => ev.Kind switch
    {
        "tired" => "留出耐力", "block" => "格挡 " + ev.Damage, "break" => "破防 " + ev.Damage,
        "restore" => "连段回气", "perfect" => "及时格挡", "riposte" => "反击", "break-bonus" => "破势", "cancel" => "闪避衔接",
        _ => ev.Critical ? $"暴击 {ev.Damage}" : ev.Damage.ToString()
    };

    public override void _Process(double delta)
    {
        if (!Animating) return;
        foreach (var hit in floating) hit.Age += delta;
        foreach (var hit in impacts) hit.Age += delta;
        impacts.RemoveAll(h => h.Age > .22);
        floating.RemoveAll(h => h.Age > .7); QueueRedraw();
    }

    public override void _Draw()
    {
        if (court == null) return;
        DrawTextureRect(court, new Rect2(0, 0, 1280, 720), false);
        if (Match == null) return;
        DrawAreaSkillRange(Match.Player);
        var (quickOffset, roamerOffset) = GetFourthWaveNameplateOffsets();
        var all = Match.Enemies.Append(Match.Player).OrderBy(f => f.Y).ThenBy(f => f.Id, StringComparer.Ordinal);
        var fifthWave = Match.Wave.Id == "c1";
        foreach (var fighter in all)
            DrawFighter(fighter, fighter.Enemy?.Id switch { "quick" => quickOffset, "roamer" => roamerOffset, _ => 0 }, fifthWave);
        if (fifthWave)
            foreach (var (fighter, offset) in GetFifthWaveNameplateOffsets())
                DrawFifthWaveNameplate(fighter, offset);
        if (!ReducedMotion) foreach (var hit in impacts) DrawImpact(hit);
        foreach (var hit in floating)
        {
            var ev = hit.Event; var alpha = (float)(1 - hit.Age / .7);
            if (ev.Kind == "area-double-hit")
            {
                var center = new Vector2((float)ev.X - 60, Math.Min((float)ev.Y + 50, 646));
                var cue = Match.Wave.Id == "c1" ? "横扫多目标" : "横扫双中";
                DrawStringOutline(font, center, cue, HorizontalAlignment.Center, 120, 22, 4, new Color(.08f, .06f, .025f, alpha));
                DrawString(font, center, cue, HorizontalAlignment.Center, 120, 22, new Color(1, .92f, .64f, alpha));
                continue;
            }
            var text = FloatingText(ev);
            var color = ev.Kind == "block" ? new Color(.65f, .85f, .95f, alpha) :
                ev.Critical ? new Color(1, .72f, .18f, alpha) : new Color(1, .84f, .45f, alpha);
            var height = ev.Kind is "restore" or "perfect" or "riposte" or "break-bonus" ? 138 : 108;
            var position = new Vector2((float)ev.X - 22 + hit.OffsetX, (float)(ev.Y - height - (ReducedMotion ? 0 : hit.Age * 42)));
            if (ev.Critical) DrawStringOutline(font, position, text, HorizontalAlignment.Left, -1, FloatingTextSize, 3, new Color(.08f, .06f, .025f, alpha));
            DrawString(font, position, text, HorizontalAlignment.Left, -1, FloatingTextSize, color);
        }
        var (startup, sidestep) = GetBorrowPositionCues(quickOffset);
        if (startup.HasArea()) DrawOpponentCue(startup, "！起手", new Color(1, .76f, .38f));
        if (sidestep.HasArea()) DrawOpponentCue(sidestep, "绕侧", new Color(.62f, .92f, .98f));
    }

    private (float Quick, float Roamer) GetFourthWaveNameplateOffsets()
    {
        if (Match is not { Wave.Id: "b2" } round) return default;
        var quick = round.Enemies.Find(f => f.Enemy?.Id == "quick");
        var roamer = round.Enemies.Find(f => f.Enemy?.Id == "roamer");
        if (quick is not { Health: > 0 } || roamer is not { Health: > 0 }) return default;

        var separation = new Vector2((float)(roamer.X - quick.X), (float)(roamer.Y - quick.Y)) +
            BodyOffset(roamer) - BodyOffset(quick);
        const float nameplateHeight = 30;
        const float depthTransition = 14;
        const float clearance = 8;
        var depthOverlap = 1 - Mathf.Clamp((Math.Abs(separation.Y) - nameplateHeight) / depthTransition, 0, 1);
        var shift = Math.Max(0, OpponentBarWidth + clearance - Math.Abs(separation.X)) * depthOverlap / 2;
        return separation.X >= 0 ? (-shift, shift) : (shift, -shift);
    }

    private Dictionary<CombatFighter, float> GetFifthWaveNameplateOffsets()
    {
        const float gap = 8;
        const float edge = 8;
        const float courtWidth = 1280;
        var plates = Match!.Enemies.Where(fighter => fighter.Health > 0)
            .Select(fighter => (Fighter: fighter, Anchor: FifthWaveNameplateAnchor(fighter)))
            .OrderBy(plate => plate.Anchor.X).ThenBy(plate => plate.Anchor.Y)
            .ThenBy(plate => plate.Fighter.Id, StringComparer.Ordinal)
            .Select(plate => (plate.Fighter, Bounds: FifthWaveNameplateBounds(plate.Fighter, plate.Anchor)))
            .ToArray();
        var offsets = new float[plates.Length];
        var groups = Enumerable.Range(0, plates.Length).ToArray();

        bool SharesDepth(int left, int right) =>
            plates[left].Bounds.Position.Y < plates[right].Bounds.End.Y &&
            plates[right].Bounds.Position.Y < plates[left].Bounds.End.Y;
        float RequiredShift(int left, int right) =>
            plates[left].Bounds.End.X + offsets[left] + gap - plates[right].Bounds.Position.X;

        // Merge groups only when their labels would collide, including after a group moves in from a court edge.
        for (var left = 0; left < plates.Length; left++)
        {
            for (var right = left + 1; right < plates.Length; right++)
            {
                if (groups[left] == groups[right] || !SharesDepth(left, right) ||
                    RequiredShift(left, right) <= offsets[right] + .001f) continue;

                var first = groups[left];
                var last = Array.LastIndexOf(groups, groups[right]);
                float shiftSum = 0, minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
                for (var i = first; i <= last; i++)
                {
                    groups[i] = first;
                    offsets[i] = 0;
                    for (var j = first; j < i; j++)
                        if (SharesDepth(j, i)) offsets[i] = Math.Max(offsets[i], RequiredShift(j, i));
                    shiftSum += offsets[i];
                    minX = Math.Min(minX, plates[i].Bounds.Position.X + offsets[i]);
                    maxX = Math.Max(maxX, plates[i].Bounds.End.X + offsets[i]);
                }
                var translation = Mathf.Clamp(-shiftSum / (last - first + 1), edge - minX, courtWidth - edge - maxX);
                for (var i = first; i <= last; i++) offsets[i] += translation;

                // Each restart joins at least two groups, so at most two restarts are needed in C1.
                left = -1;
                break;
            }
        }
        return plates.Select((plate, index) => (plate.Fighter, Offset: offsets[index]))
            .ToDictionary(plate => plate.Fighter, plate => plate.Offset);
    }

    private Vector2 FifthWaveNameplateAnchor(CombatFighter fighter) =>
        new Vector2((float)fighter.X, (float)fighter.Y - StandardFighterHeight - 19) + BodyOffset(fighter);

    private Rect2 FifthWaveNameplateBounds(CombatFighter fighter, Vector2 anchor)
    {
        var left = anchor.X - OpponentBarWidth / 2;
        var top = anchor.Y - 7 - font.GetAscent(OpponentLabelSize);
        var right = Math.Max(left + font.GetStringSize(fighter.Name, fontSize: OpponentLabelSize).X, anchor.X + OpponentBarWidth / 2);
        var (cue, baseline, size) = FifthWaveCue(fighter);
        if (cue.Length > 0)
        {
            var halfWidth = font.GetStringSize(cue, fontSize: size).X / 2;
            left = Math.Min(left, anchor.X - halfWidth);
            right = Math.Max(right, anchor.X + halfWidth);
            top = anchor.Y + baseline - font.GetAscent(size);
        }
        return new Rect2(left, top, right - left, anchor.Y + 5 - top);
    }

    private static (string Text, float Baseline, int Size) FifthWaveCue(CombatFighter fighter)
    {
        if (fighter.Enemy?.Id == "guard" && fighter.State == CombatState.Guard)
            return (GuardHint, -25, OpponentLabelSize);
        if (fighter.State == CombatState.Attack && fighter.Attack is { IsAreaSkill: false } attack && fighter.AttackFrame < attack.Startup)
            return (attack.Id == "enemy_heavy" ? "重击！" : "！", -36, StartupCueSize);
        return ("", 0, 0);
    }

    private void DrawFifthWaveNameplate(CombatFighter fighter, float offset)
    {
        var anchor = FifthWaveNameplateAnchor(fighter);
        DrawOpponentNameplate(fighter, anchor, offset);
        var (cue, baseline, size) = FifthWaveCue(fighter);
        if (cue.Length == 0) return;
        var halfWidth = font.GetStringSize(cue, fontSize: size).X / 2;
        var color = fighter.State == CombatState.Guard ? new Color(.73f, .89f, 1) : new Color(1, .73f, .3f);
        DrawString(font, anchor + new Vector2(offset - halfWidth, baseline), cue, HorizontalAlignment.Left, -1, size, color);
    }

    private (Rect2 Startup, Rect2 Sidestep) GetBorrowPositionCues(float quickOffset)
    {
        if (Match is not { Wave.LaneBlocking: true } lane) return default;
        var quick = lane.Enemies.Find(f => f.Enemy?.Id == "quick");
        var roamer = lane.Enemies.Find(f => f.Enemy?.Id == "roamer");
        var startup = quick is { Health: > 0, State: CombatState.Attack, Attack: { } attack } && quick.AttackFrame < attack.Startup
            ? new Rect2((float)quick.X + BodyOffset(quick).X + quickOffset - 36, (float)quick.Y - 200, 72, 26)
            : default;
        var sidestep = roamer is { Health: > 0 } && lane.IsSidestepping(roamer)
            ? new Rect2((float)roamer.X - 31, (float)roamer.Y + 8, 62, 26)
            : default;
        if (startup.HasArea() && sidestep.HasArea() && startup.Intersects(sidestep))
            sidestep.Position = new Vector2(sidestep.Position.X, startup.End.Y + 4);
        return (startup, sidestep);
    }

    private void DrawOpponentCue(Rect2 bounds, string text, Color color)
    {
        DrawRect(bounds, new Color(.06f, .07f, .08f, .96f));
        DrawRect(bounds, color, false, 1);
        const int size = 17;
        var baseline = bounds.Position.Y + (bounds.Size.Y + font.GetAscent(size) - font.GetDescent(size)) / 2;
        DrawString(font, new Vector2(bounds.Position.X, baseline), text, HorizontalAlignment.Center, bounds.Size.X, size, color);
    }

    private void DrawFighter(CombatFighter f, float nameplateOffset, bool fifthWave)
    {
        var atlas = f.Enemy == null ? playerFrames : opponentFrames;
        var texture = f.Enemy == null ? player : opponents;
        var footworkPose = f.Enemy == null && (Match!.Wave.Footwork || f.Attack?.IsAreaSkill == true) ? FootworkPose(f) : null;
        if (footworkPose.HasValue)
        {
            atlas = footworkFrames;
            texture = footwork;
        }
        var pose = footworkPose ?? Pose(f);
        if (f.State == CombatState.Down && f.StateFrame < 5 && impacts.Any(h => h.Event.TargetId == f.Id && h.Event.MoveId == "sweep"))
            pose = 7;
        var frame = atlas.Frames[pose];
        var height = f.Enemy?.Id == "leader" ? 148f : StandardFighterHeight;
        var scale = height / (float)atlas.Frames[0].VisibleHeight;
        var point = new Vector2((float)f.X, (float)f.Y) + BodyOffset(f);
        DrawSetTransform(point, 0, new Vector2(1, .23f));
        DrawCircle(Vector2.Zero, f.Enemy?.Id == "leader" ? 24 : 21, new Color(.025f, .03f, .02f, .3f));
        DrawSetTransform(point, 0, new Vector2(f.Facing, 1));
        var tint = f.Enemy?.Id switch
        {
            "guard" => new Color(.82f, .85f, 1), "roamer" => new Color(1, .85f, .78f),
            "leader" => new Color(.8f, .78f, .72f), _ => Colors.White
        };
        if (f.State == CombatState.Dead) tint.A = .65f;
        if (!ReducedMotion && impacts.LastOrDefault(h => h.Event.TargetId == f.Id) is { Age: < .065 } impact && impact.Event.Kind != "block")
            tint = tint.Lerp(new Color(1, .87f, .67f, tint.A), .65f);
        var rect = new Rect2(-(float)frame.PivotX * scale, -(float)frame.PivotY * scale, (float)frame.Width * scale, (float)frame.Height * scale);
        DrawTextureRectRegion(texture, rect, frame.Region, tint);
        if (!footworkPose.HasValue && f.Enemy == null && f.Stats.Weapon.Id == "stick" && f.State is CombatState.Attack or CombatState.Charging or CombatState.Guard)
        {
            DrawSetTransform(point + new Vector2(f.Facing * 23, -70), f.Facing * .55f, new Vector2(f.Facing, 1));
            DrawTextureRectRegion(items, new Rect2(-12, -25, 58, 58), itemFrames.Frames[2].Region);
        }
        DrawSetTransform(Vector2.Zero);
        if (!ReducedMotion && f.Attack?.IsAreaSkill != true) DrawAttackTrail(f, point);
        if (f.Health <= 0) return;
        if (!fifthWave && f.Enemy != null)
            DrawOpponentNameplate(f, new Vector2(point.X, point.Y - height - 19), nameplateOffset);
        if (f.State == CombatState.Attack && !f.Attack!.IsAreaSkill && f.AttackFrame < f.Attack.Startup)
        {
            var text = f.Enemy == null ? "" : f.Attack.Id == "enemy_heavy" ? "重击！" : "！";
            if (!fifthWave && text.Length > 0 && !(Match!.Wave.LaneBlocking && f.Enemy?.Id == "quick"))
                DrawString(font, point + new Vector2(-16, -height - 55), text, HorizontalAlignment.Left, -1, 22, new Color(1, .73f, .3f));
            var portion = f.AttackFrame / (float)f.Attack.Startup;
            DrawArc(point, 29, -Mathf.Pi, -Mathf.Pi + portion * Mathf.Tau, 24, new Color(1, .65f, .3f, .85f), 2, true);
        }
        if (f.Enemy?.Id == "guard" && f.State == CombatState.Guard)
        {
            DrawArc(point + new Vector2(f.Facing * 27, -65), 25, -1.2f, 1.2f, 18, new Color(.6f, .83f, .96f, .8f), 3, true);
            if (!fifthWave)
                DrawString(font, point + new Vector2(-45, -height - 38), GuardHint, HorizontalAlignment.Left, -1, OpponentLabelSize, new Color(.73f, .89f, 1));
        }
    }

    private void DrawOpponentNameplate(CombatFighter fighter, Vector2 anchor, float offset)
    {
        var width = fighter.Enemy!.Id == "leader" ? 102 : OpponentBarWidth;
        var left = anchor.X + offset - width / 2;
        var bar = new Rect2(left, anchor.Y, width, 5);
        if (Math.Abs(offset) >= 1)
        {
            var start = new Vector2(anchor.X + offset, bar.End.Y);
            var end = anchor + new Vector2(0, 18);
            DrawLine(start, end, new Color(.07f, .08f, .06f, .9f), 3, true);
            DrawLine(start, end, new Color(1, .93f, .75f, .85f), 1, true);
        }
        DrawRect(bar, new Color(.07f, .08f, .06f, .9f));
        bar.Size = new Vector2(width * fighter.Health / fighter.Stats.Health, bar.Size.Y);
        DrawRect(bar, new Color(.8f, .45f, .23f));
        DrawString(font, new Vector2(left, anchor.Y - 7), fighter.Name, HorizontalAlignment.Left, -1, OpponentLabelSize, new Color(1, .93f, .75f));
    }

    private int Pose(CombatFighter f) => f.State switch
    {
        CombatState.Guard => 1,
        CombatState.Charging => 4,
        CombatState.Dodge => 8,
        CombatState.Down or CombatState.Dead => 9,
        CombatState.Rising => 10,
        CombatState.Hurt => 7,
        CombatState.Attack => f.AttackFrame < f.Attack!.Startup ? f.Attack.IsLight || f.Attack.Id == "enemy_jab" ? 2 : 4 :
            f.ActivePulse >= 0 ? f.Attack.IsLight || f.Attack.Id == "enemy_jab" ? 3 : 5 : 6,
        _ => f.IsMoving && Match!.TickCount % 24 < 12 ? 11 : 0
    };

    private static int? FootworkPose(CombatFighter f)
    {
        if (f.State != CombatState.Attack || f.Attack is not { } move) return null;
        var frame = f.AttackFrame;
        var afterContact = frame - move.Startup - move.Active;
        return move.Id switch
        {
            "light1" => frame < move.Startup / 2 ? null : frame < move.Startup ? 1 : afterContact < 0 ? 0 : afterContact < 8 ? 1 : null,
            "light2" => frame < move.Startup ? 1 : afterContact < 0 ? 2 : afterContact < 9 ? 3 : null,
            "light3" => frame < move.Startup ? 4 : afterContact < 0 ? 5 : afterContact < 9 ? 4 : null,
            "sweep" => frame < move.Startup ? 7 : afterContact < 0 ? 6 : afterContact < 11 ? 7 : null,
            CombatMove.AreaSkillId => frame < move.Startup / 2 ? 7 : frame < move.Startup ? 3 :
                afterContact < 0 ? 6 : afterContact < 9 ? 7 : null,
            _ => null
        };
    }
}
