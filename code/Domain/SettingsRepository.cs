using System.Text.Json;
namespace GeXingzhou.Domain;
public sealed class SettingsRepository
{
    private readonly string directory,path;public bool HasValidFile {get;private set;}public string Message {get;private set;}="";
    public SettingsRepository(string directory){this.directory=directory;path=Path.Combine(directory,"settings.json");}
    public GameSettings Load()
    {
        try{if(!File.Exists(path))return new();var options=JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(path));if(options==null)throw new JsonException();HasValidFile=true;return options.Normalize();}
        catch(Exception ex) when(ex is JsonException or IOException or UnauthorizedAccessException){Message="设置未读取，使用默认值；原文件保留。";return new();}
    }
    public RecordResult Save(GameSettings options)
    {
        try{Directory.CreateDirectory(directory);if(File.Exists(path))File.Copy(path,Path.Combine(directory,"settings-previous-"+Guid.NewGuid()+".json"));var temp=Path.Combine(directory,"settings.tmp");File.WriteAllText(temp,JsonSerializer.Serialize(options.Normalize()));File.Move(temp,path,true);HasValidFile=true;return new(true,"设置已保存。");}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException){return new(false,"设置未写入："+ex.GetType().Name);}
    }
}
