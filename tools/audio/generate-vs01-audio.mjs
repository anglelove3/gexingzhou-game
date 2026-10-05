import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const targetArg=process.argv[process.argv.indexOf('--output')+1];
if(!process.argv.includes('--output')||!targetArg)throw new Error('Use --output assets/audio/vs01 or test-output/subdirectory');
const target=path.resolve(root,targetArg), relative=path.relative(root,target).replaceAll('\\','/');
if(relative!=='assets/audio/vs01'&&!relative.startsWith('test-output/'))throw new Error('Output must stay in this project audio/test-output directory');
const rate=44100,seed=20261005;
let state=seed;
function noise(){state=(Math.imul(state,1664525)+1013904223)>>>0;return state/2147483648-1;}
function samples(seconds){return new Float64Array(Math.round(seconds*rate));}
function pluck(buffer,start,freq,duration,level){
 const period=Math.round(rate/freq),line=Float64Array.from({length:period},()=>noise()*level);
 const at=Math.round(start*rate),length=Math.min(Math.round(duration*rate),buffer.length-at);
 for(let i=0;i<length;i++){
  const cursor=i%period,value=line[cursor];
  line[cursor]=.995*(value+line[(cursor+1)%period])/2;
  const edge=Math.min(1,i/96,(length-i)/160);
  buffer[at+i]+=value*edge*Math.exp(-i/rate/3.2);
 }
}
function texture(buffer,kind){
 let low=0;
 for(let i=0;i<buffer.length;i++){
  const t=i/rate,n=noise();low=.98*low+.02*n;
  const fade=Math.min(1,t/.04,(buffer.length/rate-t)/.08);
  if(kind==='outdoor')buffer[i]=low*.09*fade;
  else if(kind==='indoor')buffer[i]=(low*.045+.001*Math.sin(2*Math.PI*83*t))*fade;
  else if(kind==='footstep')buffer[i]=(.32*low+.035*n)*Math.exp(-t*28)*Math.min(1,t/.008);
  else if(kind==='candy')buffer[i]=n*.045*(.4+.6*Math.sin(t*76)**2)*Math.exp(-t*7)*Math.min(1,t/.01);
  else {
   const frequencies=kind==='bowl'?[920,1813,2421]:kind==='coin'?[3187,4761,6113]:[1180,2211];
   const decay=kind==='bowl'?10:kind==='coin'?19:36;
   buffer[i]=(frequencies.reduce((sum,f)=>sum+Math.sin(2*Math.PI*f*t),0)*.065+n*.016)*Math.exp(-t*decay)*Math.min(1,t/.002);
  }
 }
}
function wav(data){
 const result=Buffer.alloc(44+data.length*2);
 result.write('RIFF',0);result.writeUInt32LE(result.length-8,4);result.write('WAVEfmt ',8);
 result.writeUInt32LE(16,16);result.writeUInt16LE(1,20);result.writeUInt16LE(1,22);result.writeUInt32LE(rate,24);
 result.writeUInt32LE(rate*2,28);result.writeUInt16LE(2,32);result.writeUInt16LE(16,34);result.write('data',36);result.writeUInt32LE(data.length*2,40);
 for(let i=0;i<data.length;i++)result.writeInt16LE(Math.round(.9*Math.tanh(data[i])*32767),44+i*2);
 return result;
}
fs.mkdirSync(target,{recursive:true});
const files=[];
function save(name,seconds,source,synthesize){
 state=seed;const data=samples(seconds);synthesize(data);const bytes=wav(data);
 fs.writeFileSync(path.join(target,name+'.wav'),bytes);
 files.push({file:name+'.wav',script:'tools/audio/generate-vs01-audio.mjs',seed,sample_rate:rate,bits:16,channels:1,duration_seconds:seconds,sha256:crypto.createHash('sha256').update(bytes).digest('hex'),source});
}
save('guitar-theme',16,'Original algorithmic Karplus-Strong plucked-string motif; not a guitar recording',data=>{
 const chords=[[164.81,246.94,329.63,392],[130.81,196,329.63,246.94],[196,293.66,392,493.88],[146.83,220,293.66,369.99]];
 chords.forEach((chord,c)=>chord.forEach((note,n)=>pluck(data,c*3+n*.75,note,3.5,.8)));
});
save('footstep',.2,'Original filtered-noise envelope; no field recording',data=>texture(data,'footstep'));
save('phone-message',.55,'Original two-note soft pluck',data=>{pluck(data,0,659.25,.3,.8);pluck(data,.16,783.99,.35,.65);});
save('phone-ring',1.5,'Original short plucked ringtone; no commercial melody',data=>[440,554.37,659.25,554.37].forEach((f,i)=>pluck(data,i*.25,f,.55,.7)));
save('bowl',.65,'Original synthetic ceramic resonance',data=>texture(data,'bowl'));
save('chopsticks',.23,'Original synthetic wood tap',data=>texture(data,'chopsticks'));
save('coin',.38,'Original synthetic metallic resonance',data=>texture(data,'coin'));
save('candy',.55,'Original shaped-noise wrapper rustle',data=>texture(data,'candy'));
save('ambience-outdoor',8,'Original soft filtered-noise bed; not birds, voices or a field recording',data=>texture(data,'outdoor'));
save('ambience-indoor',8,'Original filtered-noise and low resonant bed; not a field recording',data=>texture(data,'indoor'));
fs.writeFileSync(path.join(target,'manifest.json'),JSON.stringify({version:1,seed,generated_on:'2026-10-05',provenance:'licenses/AUDIO_VS01.md',files},null,2)+'\n');
console.log('AUDIO_GENERATED '+files.length+' files: '+relative);
