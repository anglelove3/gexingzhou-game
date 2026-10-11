// Asset compilation only: unpack generated cells and normalize transparent frame canvases.
export async function buildWalkAtlas(fs,PNG,root) {
  const input=PNG.sync.read(await fs.readFile(root+'/assets/art/vs01-v6-polish/player-walk-source.png'));
  const output=new PNG({width:256*8,height:432*3});
  for(let row=0;row<3;row++) {
    const y0=Math.round(row*input.height/3),y1=Math.round((row+1)*input.height/3);
    const runs=[];let start=-1;
    for(let x=0;x<=input.width;x++) {
      let filled=false;
      if(x<input.width)for(let y=y0;y<y1;y++)if(input.data[(y*input.width+x)*4+3]>24){filled=true;break;}
      if(filled&&start<0)start=x;
      if(!filled&&start>=0){runs.push([start,x]);start=-1;}
    }
    if(runs.length!==8)throw new Error('Expected eight transparent separated poses in row '+row);
    for(let col=0;col<8;col++) {
    const [x0,x1]=runs[col];
    let left=x1,right=x0,top=y1,bottom=y0;
    for(let y=y0;y<y1;y++)for(let x=x0;x<x1;x++)if(input.data[(y*input.width+x)*4+3]>24) {
      left=Math.min(left,x);right=Math.max(right,x);top=Math.min(top,y);bottom=Math.max(bottom,y);
    }
    if(bottom<=top)throw new Error('Empty frame '+row+'/'+col);
    const scale=400/(bottom-top+1),width=Math.round((right-left+1)*scale);
    if(width>250)throw new Error('Frame too wide '+row+'/'+col);
    const offset=Math.round((256-width)/2);
    for(let y=0;y<400;y++)for(let x=0;x<width;x++) {
      const source=((top+Math.min(bottom-top,Math.floor(y/scale)))*input.width+left+Math.min(right-left,Math.floor(x/scale)))*4;
      const target=(((row*432)+12+y)*output.width+col*256+offset+x)*4;
      input.data.copy(output.data,target,source,source+4);
    }
    }
  }
  await fs.writeFile(root+'/assets/art/vs01-v6-polish/player-walk.png',PNG.sync.write(output));
  let resource='[gd_resource type="SpriteFrames" load_steps=30 format=3]\n';
  resource+='[ext_resource type="Texture2D" path="res://assets/art/vs01-v6-polish/player-walk.png" id="Walk"]\n';
  resource+='[ext_resource type="SpriteFrames" path="res://assets/animations/player-depth-v4.tres" id="Idle"]\n';
  for(let row=0;row<3;row++)for(let col=0;col<8;col++)resource+=`[sub_resource type="AtlasTexture" id="F${row*8+col}"]\natlas=ExtResource("Walk")\nregion=Rect2(${col*256},${row*432},256,432)\nfilter_clip=true\nmetadata/foot_pivot=Vector2(128,412)\n`;
  // Preserve existing neutral poses/identity; reference their AtlasTexture regions directly.
  const old=await fs.readFile(root+'/assets/animations/player-depth-v4.tres','utf8');
  resource=resource.replace('[ext_resource type="SpriteFrames" path="res://assets/animations/player-depth-v4.tres" id="Idle"]','[ext_resource type="Texture2D" path="res://assets/art/vs01-v4/player-depth.png" id="Idle"]');
  for(const i of [0,4,8]) {const block=old.match(new RegExp(`\\[sub_resource type="AtlasTexture" id="F${i}"\\]([\\s\\S]*?)(?=\\[)`))[1];resource+=`[sub_resource type="AtlasTexture" id="I${i}"]`+block.replace('ExtResource("1")','ExtResource("Idle")');}
  resource+='\n[resource]\nanimations=[\n';
  const directions=['side','front','back'];
  for(let row=0;row<3;row++) {
    resource+=`{ "name": &"idle_${directions[row]}", "loop":true, "speed":1.0, "frames":[{"duration":1.0,"texture":SubResource("I${row*4}")}] },\n`;
    resource+=`{ "name": &"walk_${directions[row]}", "loop":true, "speed":10.0, "frames":[`+Array.from({length:8},(_,i)=>`{"duration":1.0,"texture":SubResource("F${row*8+i}")}`).join(',')+'] },\n';
  }
  resource+='{ "name": &"idle", "loop":true, "speed":1.0, "frames":[{"duration":1.0,"texture":SubResource("I0")}] },\n';
  resource+='{ "name": &"walk", "loop":true, "speed":10.0, "frames":['+Array.from({length:8},(_,i)=>`{"duration":1.0,"texture":SubResource("F${i}")}`).join(',')+'] }\n';
  resource+=']\n';await fs.writeFile(root+'/assets/animations/player-depth-polish.tres',resource);
  return {width:output.width,height:output.height,frames:24};
}
