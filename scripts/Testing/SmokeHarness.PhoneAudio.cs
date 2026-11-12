using Godot;
using GeXingzhou.Domain;

public partial class SmokeHarness
{
    private async Task PhoneAudioChecks()
    {
        var session=GetNode<GameSession>("/root/GameSession");
        session.SetOptions(new(){TextSpeed=0,ReducedMotion=true},false);
        var failures=new List<string>();
        foreach(var answer in new[]{true,false})
        {
            var main=await NewPolishMain();var audio=main.Audio;
            // Sharing a long WAV makes cancellation by cue identity observable: cancelling
            // the ringtone must not cancel message/coin cues that use the same resource.
            using var shared=new AudioStreamWav{MixRate=44100,Format=AudioStreamWav.FormatEnum.Format16Bits,Data=new byte[44100*20]};
            audio.PhoneMessageStream=shared;audio.PhoneRingStream=shared;audio.CoinStream=shared;
            var slots=audio.GetNode("Effects").GetChildren().OfType<AudioStreamPlayer>().ToArray();
            audio.StopTransient();session.AdvanceClock(65);await Frames(2);
            Require(slots.Count(p=>p.Playing)==2&&slots[0].Playing&&slots[1].Playing,"Message and ringtone fixture did not start");
            var message=slots[0];var ring=slots[1];
            main.Phone.Open("messages");await Frames(2);main.Phone.Close();await Frames(2);
            Require(session.Snapshot.InvitationState.PhoneRinging&&ring.Playing,"Closing an unanswered phone changed call resolution");
            Require(audio.PlayCue(AudioCue.Coin),"Concurrent unrelated cue rejected");var coin=slots[2];
            if(answer){main.Phone.Open("messages");await Frames(2);KeyPress(Key.Enter);await Frames(3);}
            else{Require(session.TryDispatch(new("invitation.meeting_complete","meeting","invitation-1")).Applied,"Meeting fixture rejected");await Frames(3);}
            Require(!session.Snapshot.InvitationState.PhoneRinging&&session.Snapshot.InvitationState.Resolution==(answer?InvitationResolution.Answered:InvitationResolution.Arrived),"Resolution or existing story route changed");
            var mode=answer?"KeyboardAnswer":"MeetWithoutAnswer";
            if(ring.Playing){failures.Add(mode+": ringtone continued after call resolved");GD.Print("PHONE_AUDIO_FAIL "+mode);}
            Require(message.Playing&&coin.Playing,"Resolving a call stopped unrelated cues sharing the WAV");
            Require(audio.GetNode<AudioStreamPlayer>("Music").Playing&&audio.GetNode<AudioStreamPlayer>("Ambience").Playing,"Resolving a call stopped background audio");
            if(!ring.Playing)
            {
                Require(audio.PlayCue(AudioCue.Coin)&&ring.Playing,"Retired ringtone slot was not reusable");await Frames(3);
                Require(ring.Playing,"Repeated resolved-state updates stopped a reused coin slot");
            }
            main.Free();await Frames(3);GD.Print("PHONE_AUDIO_CASE_DONE "+mode);
        }
        if(failures.Count>0)throw new Exception(string.Join("; ",failures));
        GD.Print("PHONE_AUDIO_PASS KeyboardAnswer Meeting SharedWav UnrelatedCues SlotReuse UnansweredClose");
    }
}
