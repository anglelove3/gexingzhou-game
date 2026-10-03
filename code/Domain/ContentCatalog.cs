using System.Text.Json;
namespace GeXingzhou.Domain;
public sealed record ContentLoadResult(bool Success, ContentCatalog? Catalog, IReadOnlyList<string> Errors);
public sealed class ContentCatalog
{
    public IReadOnlyDictionary<string, double> Parameters { get; init; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<string,DialogueNode> Dialogues {get;set;} = new Dictionary<string,DialogueNode>();
    public static ContentLoadResult Load(string directory) => LoadText(name => File.ReadAllText(Path.Combine(directory,name)));
    public static ContentLoadResult LoadText(Func<string,string> read)
    {
        var errors = new List<string>();
        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string,double>>(read("parameters.json")) ?? new();
            string[] required = {"move.walk_speed","move.run_speed","move.acceleration","move.deceleration","interact.radius","interact.hysteresis","interact.repeat_guard","dialogue.min_advance_delay","dialogue.type_speed","invitation.voice_delay","invitation.call_delay","invitation.car_fallback","memory.fade","phone.max_blocking_fx"};
            foreach (var key in required) if (!values.TryGetValue(key,out var value) || !double.IsFinite(value) || value<=0) errors.Add($"Invalid parameter: {key}");
            using var scenes = JsonDocument.Parse(read("scenes.json"));
            var ids = new HashSet<string>();
            var stages = new HashSet<string>(Enum.GetNames<SliceStage>().Select(n=>JsonNamingPolicy.SnakeCaseLower.ConvertName(n)));
            foreach (var scene in scenes.RootElement.EnumerateArray())
            {
                if (!scene.TryGetProperty("scene_id",out var id) || string.IsNullOrWhiteSpace(id.GetString()) || !ids.Add(id.GetString()!)) errors.Add("Missing or duplicate scene_id");
                if (!scene.TryGetProperty("stage",out var stage) || !stages.Contains(stage.GetString() ?? "")) errors.Add("Unknown stage");
                if (!scene.TryGetProperty("width",out var width) || !width.TryGetInt32(out var w) || w<=0) errors.Add("Invalid scene width");
            }
            if (ids.Count==0) errors.Add("No scenes");
            var events = JsonSerializer.Deserialize<string[]>(read("events.json")) ?? Array.Empty<string>();
            if(events.Length==0 || events.Any(string.IsNullOrWhiteSpace) || events.Distinct().Count()!=events.Length) errors.Add("Empty or duplicate events");
            return new(errors.Count==0,errors.Count==0 ? new ContentCatalog{Parameters=values} : null, errors);
        }
        catch(Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or ArgumentException)
        { return new(false,null,new[]{ex.Message}); }
    }
}
