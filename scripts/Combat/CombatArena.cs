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

    private sealed record FrameData(double X, double Y, double Width, double Height, double PivotX, double PivotY, double VisibleHeight)
    {
        public Rect2 Region => new((float)X, (float)Y, (float)Width, (float)Height);
    }
    private sealed record AtlasData(FrameData[] Frames);
    private sealed class FloatingHit
    {
        public required CombatEvent Event;
        public double Age;
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
            if (ev.Kind is "hit" or "block" or "down" or "break" or "tired" or "restore" or "perfect" or "riposte" or "break-bonus" or "cancel") floating.Add(new() { Event = ev });
            if (ev.Kind is "hit" or "block" or "down" or "break")
            {
                if (impacts.Count >= 24) impacts.RemoveAt(0);
                impacts.Add(new() { Event = ev });
            }
        }
    }

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
        var all = Match.Enemies.Append(Match.Player).OrderBy(f => f.Y).ThenBy(f => f.Id, StringComparer.Ordinal);
        foreach (var fighter in all) DrawFighter(fighter);
        if (!ReducedMotion) foreach (var hit in impacts) DrawImpact(hit);
        foreach (var hit in floating)
        {
            var ev = hit.Event; var alpha = (float)(1 - hit.Age / .7);
            var text = ev.Kind switch
            {
                "tired" => "留出耐力", "block" => "格挡 " + ev.Damage, "break" => "破防 " + ev.Damage,
                "restore" => "连段回气", "perfect" => "及时格挡", "riposte" => "反击", "break-bonus" => "破势", "cancel" => "闪避衔接",
                _ => ev.Critical ? $"暴击 {ev.Damage}" : ev.Damage.ToString()
            };
            var color = ev.Kind == "block" ? new Color(.65f, .85f, .95f, alpha) :
                ev.Critical ? new Color(1, .72f, .18f, alpha) : new Color(1, .84f, .45f, alpha);
            var height = ev.Kind is "restore" or "perfect" or "riposte" or "break-bonus" ? 138 : 108;
            var position = new Vector2((float)ev.X - 22, (float)(ev.Y - height - (ReducedMotion ? 0 : hit.Age * 42)));
            if (ev.Critical) DrawStringOutline(font, position, text, HorizontalAlignment.Left, -1, 20, 3, new Color(.08f, .06f, .025f, alpha));
            DrawString(font, position, text, HorizontalAlignment.Left, -1, 20, color);
        }
    }

    private void DrawFighter(CombatFighter f)
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
        var height = f.Enemy?.Id == "leader" ? 148f : 136f;
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
        if (f.Enemy != null)
        {
            var width = f.Enemy.Id == "leader" ? 102 : 78;
            DrawRect(new Rect2(point.X - width / 2f, point.Y - height - 19, width, 5), new Color(.07f, .08f, .06f, .9f));
            DrawRect(new Rect2(point.X - width / 2f, point.Y - height - 19, width * f.Health / (float)f.Stats.Health, 5), new Color(.8f, .45f, .23f));
            var name = Match!.IsSidestepping(f) ? f.Name + " · 绕侧" : f.Name;
            DrawString(font, point + new Vector2(-width / 2f, -height - 26), name, HorizontalAlignment.Left, -1, 16, new Color(1, .93f, .75f));
        }
        if (f.State == CombatState.Attack && !f.Attack!.IsAreaSkill && f.AttackFrame < f.Attack.Startup)
        {
            var text = f.Enemy == null ? "" : f.Attack.Id == "enemy_heavy" ? "重击！" : "！";
            if (text.Length > 0) DrawString(font, point + new Vector2(-16, -height - 55), text, HorizontalAlignment.Left, -1, 22, new Color(1, .73f, .3f));
            var portion = f.AttackFrame / (float)f.Attack.Startup;
            DrawArc(point, 29, -Mathf.Pi, -Mathf.Pi + portion * Mathf.Tau, 24, new Color(1, .65f, .3f, .85f), 2, true);
        }
        if (f.Enemy?.Id == "guard" && f.State == CombatState.Guard)
        {
            DrawArc(point + new Vector2(f.Facing * 27, -65), 25, -1.2f, 1.2f, 18, new Color(.6f, .83f, .96f, .8f), 3, true);
            DrawString(font, point + new Vector2(-45, -height - 38), "守势 · 可蓄力破势", HorizontalAlignment.Left, -1, 16, new Color(.73f, .89f, 1));
        }
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
