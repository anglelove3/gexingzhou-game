using Godot;
using GeXingzhou.Domain;

public enum AudioCue { Footstep, PhoneMessage, PhoneRing, Bowl, Chopsticks, Coin, Candy }
public partial class AudioDirector : Node
{
    [Export] public AudioStream? MusicStream {get;set;}
    [Export] public AudioStream? OutdoorStream {get;set;}
    [Export] public AudioStream? IndoorStream {get;set;}
    [Export] public AudioStream? FootstepStream {get;set;}
    [Export] public AudioStream? PhoneMessageStream {get;set;}
    [Export] public AudioStream? PhoneRingStream {get;set;}
    [Export] public AudioStream? BowlStream {get;set;}
    [Export] public AudioStream? ChopsticksStream {get;set;}
    [Export] public AudioStream? CoinStream {get;set;}
    [Export] public AudioStream? CandyStream {get;set;}
    private AudioStreamPlayer music=null!,ambience=null!;
    private AudioStreamPlayer[] slots=Array.Empty<AudioStreamPlayer>();
    private GameSettings options=new();private bool paused,ready;
    private readonly HashSet<string> missing=new();
    public override void _Ready()
    {
        try
        {
            music=SceneBindings.Require<AudioStreamPlayer>(this,"Music");ambience=SceneBindings.Require<AudioStreamPlayer>(this,"Ambience");
            slots=Enumerable.Range(1,8).Select(i=>SceneBindings.Require<AudioStreamPlayer>(this,"Effects/Slot"+i)).ToArray();
            ready=true;ApplySettings(GetNode<GameSession>("/root/GameSession").Options);
        }
        catch(InvalidOperationException ex){SceneBindings.ReportFailure(this,ex.Message);}
    }
    public override void _Process(double delta)
    {
        if(!ready)return;
        var current=GetNode<GameSession>("/root/GameSession").Options;if(current!=options)ApplySettings(current);
    }
    public void ApplySettings(GameSettings settings)
    {
        options=settings.Normalize();if(!ready)return;
        var attenuation=paused?.35:1;
        music.VolumeLinear=(float)(options.MasterVolume*options.MusicVolume*attenuation);
        ambience.VolumeLinear=(float)(options.MasterVolume*options.EnvironmentVolume*attenuation);
        foreach(var slot in slots)slot.VolumeLinear=(float)(options.MasterVolume*options.EffectsVolume);
    }
    private void Missing(string name){if(missing.Add(name))GD.Print("AUDIO_RESOURCE_MISSING "+name+"; silent fallback");}
    private void Loop(AudioStreamPlayer player,AudioStream? stream,string name)
    {
        if(player.Stream==stream&&player.Playing)return;
        player.Stop();player.Stream=stream;
        if(stream==null){Missing(name);return;}
        if(stream is AudioStreamWav wav){wav.LoopBegin=0;wav.LoopEnd=(int)Math.Round(wav.GetLength()*wav.MixRate);wav.LoopMode=AudioStreamWav.LoopModeEnum.Forward;}
        player.Play();
    }
    public void SetScene(string sceneId)
    {
        if(!ready)return;
        StopTransient();
        Loop(music,MusicStream,"music");
        Loop(ambience,sceneId is "soup_shop" or "memory_soup_table"?IndoorStream:OutdoorStream,"environment:"+sceneId);
        music.PitchScale=sceneId=="memory_soup_table"?.96f:1;
    }
    public bool PlayCue(AudioCue cue)
    {
        if(!ready||paused&&cue==AudioCue.Footstep||options.MasterVolume*options.EffectsVolume==0)return false;
        var stream=cue switch{AudioCue.Footstep=>FootstepStream,AudioCue.PhoneMessage=>PhoneMessageStream,AudioCue.PhoneRing=>PhoneRingStream,AudioCue.Bowl=>BowlStream,AudioCue.Chopsticks=>ChopsticksStream,AudioCue.Coin=>CoinStream,AudioCue.Candy=>CandyStream,_=>null};
        if(stream==null){Missing(cue.ToString());return false;}
        var slot=slots.FirstOrDefault(p=>!p.Playing);if(slot==null)return false;
        slot.Stream=stream;slot.Play();return true;
    }
    public void SetPaused(bool value){if(value==paused)return;paused=value;if(value)StopTransient();ApplySettings(options);}
    public void StopTransient(){foreach(var slot in slots){slot.Stop();slot.Stream=null;}}
    public void StopAll(){StopTransient();if(ready){music.Stop();ambience.Stop();music.Stream=null;ambience.Stream=null;}}
    public override void _ExitTree(){StopAll();ready=false;}
}
