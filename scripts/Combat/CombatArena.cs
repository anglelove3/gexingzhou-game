using Godot;
using GeXingzhou.Domain;
using System.Text.Json;

public partial class CombatArena : Node2D
{
    public CombatMatch? Match { get; set; }
    private Texture2D court = null!, player = null!, opponents = null!, items = null!;
    private AtlasData playerFrames = null!, opponentFrames = null!, itemFrames = null!;
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
            if (ev.Kind is "hit" or "block" or "down" or "break" or "tired") floating.Add(new() { Event = ev });
    }

    public override void _Process(double delta)
    {
        foreach (var hit in floating) hit.Age += delta;
        floating.RemoveAll(h => h.Age > .7); QueueRedraw();
    }

    public override void _Draw()
    {
        if (court == null) return;
        DrawTextureRect(court, new Rect2(0, 0, 1280, 720), false);
        if (Match == null) return;
        var all = Match.Enemies.Append(Match.Player).OrderBy(f => f.Y).ThenBy(f => f.Id, StringComparer.Ordinal);
        foreach (var fighter in all) DrawFighter(fighter);
        foreach (var hit in floating)
        {
            var ev = hit.Event; var alpha = (float)(1 - hit.Age / .7);
            var text = ev.Kind == "tired" ? "留出耐力" : ev.Kind == "block" ? "格挡 " + ev.Damage : ev.Kind == "break" ? "破防 " + ev.Damage : ev.Damage.ToString();
            var color = ev.Kind == "block" ? new Color(.65f, .85f, .95f, alpha) : new Color(1, .84f, .45f, alpha);
            DrawString(font, new Vector2((float)ev.X - 22, (float)(ev.Y - 108 - hit.Age * 42)), text, HorizontalAlignment.Left, -1, 20, color);
        }
    }

    private void DrawFighter(CombatFighter f)
    {
        var atlas = f.Enemy == null ? playerFrames : opponentFrames;
        var texture = f.Enemy == null ? player : opponents;
        var pose = Pose(f); var frame = atlas.Frames[pose];
        var height = f.Enemy?.Id == "leader" ? 148f : 136f;
        var scale = height / (float)atlas.Frames[0].VisibleHeight;
        var point = new Vector2((float)f.X, (float)f.Y);
        DrawSetTransform(point, 0, new Vector2(1, .23f));
        DrawCircle(Vector2.Zero, f.Enemy?.Id == "leader" ? 24 : 21, new Color(.025f, .03f, .02f, .3f));
        DrawSetTransform(point, 0, new Vector2(f.Facing, 1));
        var tint = f.Enemy?.Id switch
        {
            "guard" => new Color(.82f, .85f, 1), "roamer" => new Color(1, .85f, .78f),
            "leader" => new Color(.8f, .78f, .72f), _ => Colors.White
        };
        if (f.State == CombatState.Dead) tint.A = .65f;
        var rect = new Rect2(-(float)frame.PivotX * scale, -(float)frame.PivotY * scale, (float)frame.Width * scale, (float)frame.Height * scale);
        DrawTextureRectRegion(texture, rect, frame.Region, tint);
        if (f.Enemy == null && f.Stats.Weapon.Id == "stick" && f.State is CombatState.Attack or CombatState.Charging or CombatState.Guard)
        {
            DrawSetTransform(point + new Vector2(f.Facing * 23, -70), f.Facing * .55f, new Vector2(f.Facing, 1));
            DrawTextureRectRegion(items, new Rect2(-12, -25, 58, 58), itemFrames.Frames[2].Region);
        }
        DrawSetTransform(Vector2.Zero);
        if (f.Health <= 0) return;
        if (f.Enemy != null)
        {
            var width = f.Enemy.Id == "leader" ? 102 : 78;
            DrawRect(new Rect2(point.X - width / 2f, point.Y - height - 19, width, 5), new Color(.07f, .08f, .06f, .9f));
            DrawRect(new Rect2(point.X - width / 2f, point.Y - height - 19, width * f.Health / (float)f.Stats.Health, 5), new Color(.8f, .45f, .23f));
            DrawString(font, point + new Vector2(-width / 2f, -height - 26), f.Name, HorizontalAlignment.Left, -1, 16, new Color(1, .93f, .75f));
        }
        if (f.State == CombatState.Attack && f.AttackFrame < f.Attack!.Startup)
        {
            var text = f.Enemy == null ? "" : f.Attack.Id == "enemy_heavy" ? "重击！" : "！";
            if (text.Length > 0) DrawString(font, point + new Vector2(-16, -height - 55), text, HorizontalAlignment.Left, -1, 22, new Color(1, .73f, .3f));
            var portion = f.AttackFrame / (float)f.Attack.Startup;
            DrawArc(point, 29, -Mathf.Pi, -Mathf.Pi + portion * Mathf.Tau, 24, new Color(1, .65f, .3f, .85f), 2, true);
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
}
