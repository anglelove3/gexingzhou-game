$ErrorActionPreference='Stop'
$projectRoot=Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Collections.Generic;
public static class CombatAtlasScan {
    public static object[] Scan(string path, int columns, int rows) {
        var result=new List<object>();
        using(var bitmap=new Bitmap(path)) {
            for(int index=0;index<columns*rows;index++) {
                int left=index%columns*bitmap.Width/columns;
                int right=(index%columns+1)*bitmap.Width/columns;
                int top=index/columns*bitmap.Height/rows;
                int bottom=(index/columns+1)*bitmap.Height/rows;
                int minX=right,minY=bottom,maxX=left,maxY=top;
                for(int y=top;y<bottom;y++) for(int x=left;x<right;x++) {
                    if(bitmap.GetPixel(x,y).A<64)continue;
                    minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);
                }
                if(minX>maxX||minY>maxY)throw new Exception("Empty combat atlas cell "+index);
                int footMin=right,footMax=left;
                for(int y=Math.Max(minY,maxY-12);y<=maxY;y++)for(int x=minX;x<=maxX;x++) {
                    if(bitmap.GetPixel(x,y).A<128)continue;
                    footMin=Math.Min(footMin,x);footMax=Math.Max(footMax,x);
                }
                result.Add(new {X=left,Y=top,Width=right-left,Height=bottom-top,
                    PivotX=(footMin+footMax)/2.0-left,PivotY=maxY-top,
                    VisibleHeight=maxY-minY+1,Margin=Math.Min(Math.Min(minX-left,right-1-maxX),Math.Min(minY-top,bottom-1-maxY))});
            }
        }
        return result.ToArray();
    }
}
'@
$root=Join-Path $projectRoot 'assets/art/combat-v1'
foreach($name in @('player-poses','opponent-poses-v2','equipment')) {
    $columns=4;$rows=3
    if($name -eq 'equipment'){$columns=3}
    $source=Join-Path $root ($name+'.png')
    $frames=[CombatAtlasScan]::Scan($source,$columns,$rows)
    if(@($frames|Where-Object {$_.Margin -lt 1}).Count -gt 0){throw "COMBAT_ATLAS_CELL_CLIPPED $name"}
    $data=[ordered]@{SourceSha256=(Get-FileHash -LiteralPath $source).Hash;Columns=$columns;Rows=$rows;Frames=$frames}
    [IO.File]::WriteAllText((Join-Path $root ($name+'.json')),($data|ConvertTo-Json -Depth 6),[Text.UTF8Encoding]::new($false))
    Write-Output "COMBAT_ATLAS $name frames=$($frames.Count) margins=$($frames.Margin -join ',')"
}
