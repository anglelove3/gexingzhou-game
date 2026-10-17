$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Drawing
# Read alpha geometry only. The generated PNG pixels are preserved byte-for-byte.
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
public static class ResidentAlphaGeometry {
    public static int[] Bounds(Bitmap image,int x0,int y0,int x1,int y1) {
        int left=x1,top=y1,right=x0,bottom=y0;
        for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(image.GetPixel(x,y).A>24){left=Math.Min(left,x);right=Math.Max(right,x+1);top=Math.Min(top,y);bottom=Math.Max(bottom,y+1);}
        if(left>=right||top>=bottom)throw new Exception("Empty resident frame");
        int scan=top+(bottom-top)/2,sum=0,count=0;
        for(int y=scan-2;y<=scan+2;y++)for(int x=left;x<right;x++)if(image.GetPixel(x,y).A>128){sum+=x;count++;}
        return new[]{left,top,right,bottom,count>0?sum/count:(left+right)/2};
    }
    public static int Cut(Bitmap image,int row,int nominal) {
        int best=nominal,score=int.MaxValue;
        for(int x=Math.Max(1,nominal-60);x<Math.Min(image.Width-1,nominal+61);x++){
            int occupied=0;
            for(int y=row*image.Height/2;y<(row+1)*image.Height/2;y++)if(image.GetPixel(x,y).A>24)occupied++;
            int candidate=occupied*1000+Math.Abs(x-nominal);
            if(candidate<score){score=candidate;best=x;}
        }
        if(score>=1000)throw new Exception("No transparent gutter between resident poses");
        return best;
    }
}
'@
$oldImage=[Drawing.Bitmap]::new((Join-Path $projectRoot 'assets/art/vs01-v1/npcs-v1.png'))
try{
    foreach($identity in @('green','apron','beige')){
        $relative=if($identity -eq 'beige'){'assets/art/vs01-v8-feedback/resident-beige-walk-v2.png'}else{"assets/art/vs01-v8-feedback/resident-$identity-walk.png"}
        $image=[Drawing.Bitmap]::new((Join-Path $projectRoot $relative))
        try{
            $frames=@()
            foreach($row in 0..1){
                $cuts=@(0)
                foreach($col in 1..3){$cuts+=[ResidentAlphaGeometry]::Cut($image,$row,[int]($col*$image.Width/4))}
                $cuts+=$image.Width
                foreach($col in 0..3){
                    $x=$cuts[$col];$y=[int]($row*$image.Height/2);$width=$cuts[$col+1]-$x;$height=[int]($image.Height/2)
                    $bounds=[ResidentAlphaGeometry]::Bounds($image,$x,$y,$x+$width,$y+$height)
                    $frames+=@{X=$x;Y=$y;Width=$width;Height=$height;FootX=$bounds[4]-$x;FootY=$bounds[3]-$y;BodyHeight=$bounds[3]-$bounds[1]}
                }
            }
            $bodyHeight=($frames.BodyHeight|Sort-Object)[4]
            $idle=if($identity -eq 'green'){@(707,11,138,497)}elseif($identity -eq 'apron'){@(1184,11,161,500)}else{@(219,11,158,497)}
            $idleBounds=[ResidentAlphaGeometry]::Bounds($oldImage,$idle[0],$idle[1],$idle[0]+$idle[2],$idle[1]+$idle[3])
            $lines=[Collections.Generic.List[string]]::new()
            $lines.Add('[gd_resource type="SpriteFrames" load_steps=12 format=3]')
            $lines.Add('[ext_resource type="Texture2D" path="res://'+$relative+'" id="Walk"]')
            $lines.Add('[ext_resource type="Texture2D" path="res://assets/art/vs01-v1/npcs-v1.png" id="Idle"]')
            for($i=0;$i -lt 8;$i++){
                $f=$frames[$i];$lines.Add('[sub_resource type="AtlasTexture" id="F'+$i+'"]');$lines.Add('atlas=ExtResource("Walk")')
                $lines.Add("region=Rect2($($f.X),$($f.Y),$($f.Width),$($f.Height))");$lines.Add('filter_clip=true')
                $lines.Add("metadata/foot_pivot=Vector2($($f.FootX),$($f.FootY))");$lines.Add("metadata/body_height=$bodyHeight")
            }
            $lines.Add('[sub_resource type="AtlasTexture" id="IdleFrame"]');$lines.Add('atlas=ExtResource("Idle")')
            $lines.Add('region=Rect2('+($idle -join ',')+')');$lines.Add('filter_clip=true')
            $lines.Add("metadata/foot_pivot=Vector2($($idleBounds[4]-$idle[0]),$($idleBounds[3]-$idle[1]))")
            $lines.Add("metadata/body_height=$($idleBounds[3]-$idleBounds[1])")
            $lines.Add('[resource]');$lines.Add('animations=[')
            $lines.Add('{"name":&"idle","loop":true,"speed":1.0,"frames":[{"duration":1.0,"texture":SubResource("IdleFrame")}]},')
            $frameText=(0..7|ForEach-Object {'{"duration":1.0,"texture":SubResource("F'+$_+'")}'}) -join ','
            $lines.Add('{"name":&"walk","loop":true,"speed":8.0,"frames":['+$frameText+']}');$lines.Add(']')
            $output=Join-Path $projectRoot "assets/animations/resident-$identity-walk.tres"
            [IO.File]::WriteAllText($output,($lines -join "`n")+"`n",[Text.UTF8Encoding]::new($false))
            Write-Output "RESIDENT_RESOURCE $identity 8 alpha-separated frames body_height=$bodyHeight"
        }finally{$image.Dispose()}
    }
}finally{$oldImage.Dispose()}
