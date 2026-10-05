using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task ExperienceAudioChecks()
    {
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
        audio.ApplySettings(settings);audio.SetScene("community_gate");var gain=music.VolumeLinear;
        audio.SetPaused(true);Require(Math.Abs(music.VolumeLinear-gain*.35)<.001,"Paused music gain wrong");
        Require(Math.Abs(ambience.VolumeLinear-.9*.3*.35)<.001,"Paused ambience gain wrong");
        Require(!audio.PlayCue(AudioCue.Footstep),"Footstep played while paused");audio.SetPaused(false);
        Require(Math.Abs(music.VolumeLinear-gain)<.001,"Audio gain did not restore");
        for(int i=0;i<20;i++)audio.PlayCue(AudioCue.Coin);
        Require(slots.Count(p=>p.Playing)==8,"Effect pool overflowed or dropped first eight");
        audio.StopTransient();Require(slots.All(p=>!p.Playing&&p.Stream==null),"Transient streams survived stop");
        audio.SetScene("soup_shop");Require(ambience.Stream==two,"Scene retained old environment");
        audio.ApplySettings(settings with {MasterVolume=0});
        Require(music.VolumeLinear==0&&ambience.VolumeLinear==0&&slots.All(p=>p.VolumeLinear==0),"Mute has nonzero actual player gain");
        main.Free();await Task.Delay(150);await Frames(2);GC.Collect();GC.WaitForPendingFinalizers();await Frames(2);
        Require(FindChildren("*","AudioDirector",true,false).Count==0,"Director leaked after root release");
    }
}
