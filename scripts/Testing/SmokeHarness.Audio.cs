using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task ExperienceAudioChecks()
    {
        await AudioPolishChecks();
        AudioSourceChecks();
        await AudioGameplayChecks();
        var main=await NewPolishMain();
        Require(main.GetNodeOrNull("Audio")!=null,"Missing authored audio director");
        var music=main.GetNode<AudioStreamPlayer>("Audio/Music");
        var ambience=main.GetNode<AudioStreamPlayer>("Audio/Ambience");
        var slots=main.GetNode("Audio/Effects").GetChildren().OfType<AudioStreamPlayer>().ToArray();
        Require(slots.Length==8,"Audio pool is not bounded to eight slots");
        var audio=main.GetNode<AudioDirector>("Audio");
        using var one=new AudioStreamWav{MixRate=44100,Format=AudioStreamWav.FormatEnum.Format16Bits,Data=new byte[88200]};
        using var two=new AudioStreamWav{MixRate=44100,Format=AudioStreamWav.FormatEnum.Format16Bits,Data=new byte[88200]};
        audio.FootstepStream=one;audio.CoinStream=one;audio.MusicStream=one;audio.OutdoorStream=one;audio.IndoorStream=two;
        var settings=new GameSettings{MasterVolume=.9,MusicVolume=.6,EffectsVolume=.7,EnvironmentVolume=.3};
        GetNode<GameSession>("/root/GameSession").SetOptions(settings,false);
        audio.ApplySettings(settings);audio.SetScene("community_gate");audio._Process(2);var gain=music.VolumeLinear;
        audio.SetPaused(true);audio._Process(2);Require(Math.Abs(music.VolumeLinear-gain*.35)<.001,"Paused music gain wrong");
        Require(Math.Abs(ambience.VolumeLinear-.9*.3*.35)<.001,"Paused ambience gain wrong");
        Require(!audio.PlayCue(AudioCue.Footstep),"Footstep played while paused");audio.SetPaused(false);audio._Process(2);
        Require(Math.Abs(music.VolumeLinear-gain)<.001,"Audio gain did not restore");
        for(int i=0;i<20;i++)audio.PlayCue(AudioCue.Coin);
        Require(slots.Count(p=>p.Playing)==8,"Effect pool overflowed or dropped first eight");
        audio.StopTransient();Require(slots.All(p=>!p.Playing&&p.Stream==null),"Transient streams survived stop");
        audio.SetScene("soup_shop");audio._Process(2);audio._Process(2);Require(ambience.Stream==two,"Scene retained old environment");
        audio.ApplySettings(settings with {MasterVolume=0});
        Require(music.VolumeLinear==0&&ambience.VolumeLinear==0&&slots.All(p=>p.VolumeLinear==0),"Mute has nonzero actual player gain");
        main.Free();await Task.Delay(150);await Frames(2);GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
        Require(FindChildren("*","AudioDirector",true,false).Count==0,"Director leaked after root release");
    }
    // Catches abrupt gain jumps and controls that only display zero while an active stream still renders.
    private async Task AudioPolishChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");
        session.SetOptions(new(){TextSpeed=0,MasterVolume=.8,MusicVolume=.5,EffectsVolume=.6,EnvironmentVolume=.4},false);
        var main=await NewPolishMain();var audio=main.Audio;
        var music=audio.GetNode<AudioStreamPlayer>("Music");var ambience=audio.GetNode<AudioStreamPlayer>("Ambience");
        var slots=audio.GetNode("Effects").GetChildren().OfType<AudioStreamPlayer>().ToArray();
        audio._Process(2);Require(music.Playing&&ambience.Playing,"Polished beds are not running");
        var gain=music.VolumeLinear;audio.SetPaused(true);
        Require(Math.Abs(music.VolumeLinear-gain)<.001,"Pause abruptly jumps music gain before a fade can run");
        audio._Process(.1);Require(music.VolumeLinear<gain&&music.VolumeLinear>gain*.35,"Pause has no intermediate fade gain");
        audio._Process(2);Require(Math.Abs(music.VolumeLinear-.14)<.001,"Pause fade did not settle");
        audio.SetPaused(false);audio._Process(2);
        var oldEnvironment=ambience.Stream;audio.SetScene("soup_shop");
        Require(ambience.Stream==oldEnvironment,"Environment is replaced before fading out");
        audio._Process(.1);Require(ambience.Stream==oldEnvironment&&ambience.VolumeLinear>0&&ambience.VolumeLinear<.32,"Environment lacks gradual fade out");
        audio._Process(2);audio._Process(2);Require(ambience.Stream==audio.IndoorStream&&ambience.VolumeLinear>0,"Indoor bed did not fade back in");
        main.Settings.Open();
        var audibleCue=new byte[44100*6];
        for(int i=0;i<audibleCue.Length/2;i++)BitConverter.TryWriteBytes(audibleCue.AsSpan(i*2,2),(short)(4000*Math.Sin(2*Math.PI*330*i/44100)));
        using var longCue=new AudioStreamWav{MixRate=44100,Format=AudioStreamWav.FormatEnum.Format16Bits,Data=audibleCue};
        audio.CoinStream=longCue;
        foreach(var id in new[]{"MasterVolume","MusicVolume","EffectsVolume","EnvironmentVolume"})
        {
            audio.StopTransient();Require(audio.PlayCue(AudioCue.Coin),"Enabled effect does not play before zero test "+id);
            var activeEffects=slots.Where(p=>p.Playing).ToArray();Require(activeEffects.Length==1,"Zero test lacks an active audible cue "+id);
            var slider=main.Settings.GetNode<HSlider>("Panel/Scroll/Content/"+id);var original=slider.Value;slider.Value=0;
            await Frames(2);
            var affectsMusic=id is "MasterVolume" or "MusicVolume";
            var affectsEnvironment=id is "MasterVolume" or "EnvironmentVolume";
            var affectsEffects=id is "MasterVolume" or "EffectsVolume";
            Require(!affectsMusic||music.VolumeLinear==0&&music.StreamPaused,"Actual music stream is not silent at zero "+id);
            Require(!affectsEnvironment||ambience.VolumeLinear==0&&ambience.StreamPaused,"Actual environment stream is not silent at zero "+id);
            Require(!affectsEffects||slots.All(p=>p.VolumeLinear==0)&&activeEffects.All(p=>p.StreamPaused),"Active effects are not silent at zero "+id+"; "+string.Join(", ",slots.Select(p=>$"{p.Name}:gain={p.VolumeLinear},playing={p.Playing},paused={p.StreamPaused}")));
            Require(affectsMusic||music.VolumeLinear>0&&!music.StreamPaused,"Unrelated music output muted by "+id);
            Require(affectsEnvironment||ambience.VolumeLinear>0&&!ambience.StreamPaused,"Unrelated environment output muted by "+id);
            Require(affectsEffects||activeEffects.All(p=>p.VolumeLinear>0&&!p.StreamPaused),"Unrelated effect output muted by "+id);
            if(affectsEffects)Require(!audio.PlayCue(AudioCue.Coin),"New cue played while effect output muted "+id);
            slider.Value=original;await Frames(2);audio._Process(2);
            Require(music.VolumeLinear>0&&!music.StreamPaused&&ambience.VolumeLinear>0&&!ambience.StreamPaused,"Bed did not recover after zero "+id);
            Require(slots.All(p=>p.VolumeLinear>0&&!p.StreamPaused),"Effect output did not recover after zero "+id);
            audio.StopTransient();Require(audio.PlayCue(AudioCue.Coin),"Effect did not recover after zero "+id);
        }
        main.Settings.Close();main.Free();await Frames(2);
        GD.Print("AUDIO_POLISH_PASS pause/environment fades; four real settings sliders zero active stream and restore");
    }
    private void AudioSourceChecks()
    {
        using var assets=System.Text.Json.JsonDocument.Parse(Godot.FileAccess.GetFileAsString("res://assets/manifest.json"));
        Require(assets.RootElement.GetProperty("audio_included").GetBoolean(),"Audio provenance still says excluded");
        foreach(var (version,count) in new[]{("vs01",10),("vs02",3)})
        {
          Require(Godot.FileAccess.FileExists($"res://assets/audio/{version}/manifest.json"),"Missing formal audio manifest "+version);
          using var manifest=System.Text.Json.JsonDocument.Parse(Godot.FileAccess.GetFileAsString($"res://assets/audio/{version}/manifest.json"));
          var files=manifest.RootElement.GetProperty("files");Require(files.GetArrayLength()==count,"Missing audio source item "+version);
          foreach(var item in files.EnumerateArray())
          {
            var path=$"res://assets/audio/{version}/"+item.GetProperty("file").GetString();
            var bytes=Godot.FileAccess.GetFileAsBytes(path);Require(bytes.Length>1000,"Empty WAV "+path);
            Require(BitConverter.ToUInt16(bytes,20)==1&&BitConverter.ToUInt16(bytes,22)==1&&BitConverter.ToUInt32(bytes,24)==44100&&BitConverter.ToUInt16(bytes,34)==16,"Invalid PCM source "+path);
            double peak=0;for(int i=44;i<bytes.Length;i+=2)peak=Math.Max(peak,Math.Abs(BitConverter.ToInt16(bytes,i)/32768d));
            Require(peak>0&&peak<=.95,"Audio peak invalid "+path);
            var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            Require(hash==item.GetProperty("sha256").GetString(),"Source hash mismatch "+path);
            var stream=GD.Load<AudioStreamWav>(path);Require(stream!=null&&stream.MixRate==44100&&!stream.Stereo&&stream.Format==AudioStreamWav.FormatEnum.Format16Bits,"WAV not loadable "+path);
          }
        }
    }
    private async Task AudioGameplayChecks()
    {
        var s=GetNode<GameSession>("/root/GameSession");s.SetOptions(new(){TextSpeed=0,ReducedMotion=true},false);
        var main=await NewPolishMain();var audio=main.Audio;
        Require(audio.MusicStream!=null&&audio.CoinStream!=null,"Formal audio not wired to scene");
        Require(audio.MusicStream!.GetLength()>=64,"Scene still uses short repeating music");
        using var longCue=new AudioStreamWav{MixRate=44100,Format=AudioStreamWav.FormatEnum.Format16Bits,Data=new byte[44100*6]};
        audio.FootstepStream=longCue;audio.PhoneMessageStream=longCue;audio.PhoneRingStream=longCue;
        var slots=audio.GetNode("Effects").GetChildren().OfType<AudioStreamPlayer>().ToArray();
        audio.StopTransient();await Frames(8);Require(slots.All(p=>!p.Playing),"Stationary player played footsteps");
        var start=main.World.Player.Position.X;Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=true});
        for(int i=0;i<40;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        Input.ParseInputEvent(new InputEventKey{PhysicalKeycode=Key.D,Pressed=false});main.World.Player.SetInputLocked(true);
        var distance=main.World.Player.Position.X-start;
        Require(distance>32&&slots.Count(p=>p.Playing)==(int)(distance/32),"Footsteps not tied to actual 32-unit distance");
        audio.StopTransient();s.AdvanceClock(8);await Frames(2);var messageCount=slots.Count(p=>p.Playing);
        Require(messageCount==1,"New message had no single cue");await Frames(8);Require(slots.Count(p=>p.Playing)==messageCount,"Message refreshed twice");
        s.AdvanceClock(15);await Frames(2);var ringCount=slots.Count(p=>p.Playing);Require(ringCount==2,"Ring cue absent");await Frames(8);Require(slots.Count(p=>p.Playing)==ringCount,"Ring refreshed twice");
        main.Free();await Frames(2);
        var soup=await NewSoupMain(true);Require(soup.World.GetNodeOrNull<Sprite2D>("PaymentCoin")?.Texture!=null,"Payment still lacks authored coin art");soup.SoupSeat.Begin(()=>{});await WaitUntil(()=>!soup.SoupSeat.IsActing,"Audio soup seated");
        soup.Audio.BowlStream=longCue;soup.Audio.CoinStream=longCue;soup.Audio.StopTransient();
        Require(soup.SoupSeat.PlayAction("eat",()=>{}),"Audio soup action blocked");
        Require(!soup.SoupSeat.PlayAction("eat",()=>{}),"Repeated action accepted");
        Require(soup.Audio.GetNode("Effects").GetChildren().OfType<AudioStreamPlayer>().Count(p=>p.Playing)==1,"Repeated action stacked cue");
        await WaitUntil(()=>!soup.SoupSeat.IsActing,"Audio action complete");soup.Audio.StopTransient();
        var table=await NewCoinTable(0);table.Main=soup;Require(table.TryPush(0),"Audio coin rejected");Require(!table.TryPush(0),"Duplicate coin accepted");
        Require(soup.Audio.GetNode("Effects").GetChildren().OfType<AudioStreamPlayer>().Count(p=>p.Playing)==1,"Duplicate coin stacked cue");
        table.Free();soup.Free();await Task.Delay(150);await Frames(2);
        s.SetOptions(s.Options with {MasterVolume=0},false);await MemoryPath(false,1,false);
        s.SetOptions(s.Options with {MasterVolume=.8},false);await MemoryPath(false,2,false,missingCoinAudio:true);
        await Task.Delay(150);await Frames(2);
    }
}
