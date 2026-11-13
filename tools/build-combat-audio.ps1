$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
$directory=Join-Path $projectRoot 'assets/audio/combat-v1'
[IO.Directory]::CreateDirectory($directory)|Out-Null
Add-Type -TypeDefinition @'
using System;
using System.IO;
public static class CombatAudioBuild {
    public static void Write(string file, int kind) {
        int rate=44100;
        double length=kind==0?.18:kind==3?.34:.22;
        int count=(int)(rate*length);
        using(var writer=new BinaryWriter(File.Create(file))) {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);
            writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);
            writer.Write((short)2);writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            var random=new Random(31113+kind);double smooth=0;
            for(int i=0;i<count;i++) {
                double t=i/(double)rate,phase=t/length;
                double noise=random.NextDouble()*2-1;
                smooth=.84*smooth+.16*noise;
                double envelope=Math.Sin(Math.PI*Math.Min(1,phase*12))*Math.Exp(-phase*5);
                double sample=kind==0?noise*.22*Math.Sin(Math.PI*phase):
                    kind==1?(smooth*.8+Math.Sin(2*Math.PI*(110-60*phase)*t)*.35)*envelope:
                    kind==2?(Math.Sin(2*Math.PI*680*t)*.2+noise*.2)*envelope:
                    (smooth*.9+Math.Sin(2*Math.PI*65*t)*.4)*envelope;
                writer.Write((short)(Math.Max(-1,Math.Min(1,sample))*18000));
            }
        }
    }
}
'@
$names=@('swing','hit','block','down')
for($i=0;$i -lt $names.Count;$i++){
    [CombatAudioBuild]::Write((Join-Path $directory ($names[$i]+'.wav')),$i)
}
Write-Output 'COMBAT_AUDIO_BUILT original deterministic PCM 44100Hz mono'
