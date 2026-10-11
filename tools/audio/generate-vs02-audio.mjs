import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const index=process.argv.indexOf('--output');
if(index<0||!process.argv[index+1])throw new Error('Use --output assets/audio/vs02 or test-output/subdirectory');
const target=path.resolve(root,process.argv[index+1]),relative=path.relative(root,target).replaceAll('\\','/');
if(relative!=='assets/audio/vs02'&&!relative.startsWith('test-output/'))throw new Error('Output must stay in this project audio/test-output directory');
const rate=44100,seed=20261011;let state=seed;
const random=()=>{state=(Math.imul(state,1664525)+1013904223)>>>0;return state/2147483648-1;};
const frequency=midi=>440*2**((midi-69)/12);

// Soft hammer envelope with slightly inharmonic, detuned decaying strings.
// This is an original synthetic piano-like timbre, not a sampled or recorded instrument.
function piano(data,at,midi,velocity){
 const start=Math.round(at*rate),length=Math.min(Math.round(5*rate),data.length-start),f=frequency(midi);
 const partials=[1,.24,.085,.027,.008],attack=.010,decay=1.15+(64-midi)*.016;
 for(let i=0;i<length;i++){
  const t=i/rate,hammer=1-Math.exp(-t/attack),release=Math.min(1,(length-i)/(rate*.6));let tone=0;
  for(let p=1;p<=partials.length;p++){
   const angular=2*Math.PI*f*p*Math.sqrt(1+.00008*p*p);
   const strings=(Math.sin(angular*t)+.45*Math.sin(angular*1.0006*t))/1.45;
   tone+=partials[p-1]*strings*Math.exp(-t*(1+.24*(p-1))/decay);
  }
  data[start+i]+=velocity*tone*hammer*release;
 }
}
function music(data){
 // Twelve eight-second phrases. Few bass notes, no arpeggio ostinato, and silence at the cadence.
 const phrases=[
  [[.8,62,.18],[2.8,65,.14],[5.2,69,.12]],
  [[.6,53,.075],[2.1,67,.14],[4.6,65,.115]],
  [[1.2,64,.15],[3.6,62,.12]],
  [],
  [[1.3,62,.155],[3.7,69,.12],[5.5,67,.10]],
  [[.7,55,.075],[2.8,65,.135]],
  [[1.5,64,.12],[4.2,62,.105]],
  [],
  [[1.0,65,.145],[3.6,69,.10]],
  [[.7,53,.065],[2.6,67,.115],[5.1,65,.09]],
  [[1.0,64,.115],[3.8,62,.10]],
  []
 ];
 for(let bar=0;bar<phrases.length;bar++)for(const [offset,note,velocity]of phrases[bar])piano(data,bar*8+offset,note,velocity);
 // Short, low-level room reflections keep the direct timbre clear and preserve rests.
 const direct=Float64Array.from(data);
 for(const [delay,gain]of [[.057,.11],[.101,.055],[.173,.025]]){
  const shift=Math.round(delay*rate);let smooth=0;
  for(let i=shift;i<data.length;i++){smooth=.65*smooth+.35*direct[i-shift];data[i]+=smooth*gain;}
 }
 for(let i=0;i<data.length;i++){const tail=(data.length-1-i)/(rate*1.5);data[i]*=Math.min(1,Math.max(0,tail));}
}
function ambience(data,indoor){
 let first=0,second=0;
 for(let i=0;i<data.length;i++){
  const t=i/rate;first=.996*first+.004*random();second=.996*second+.004*first;
  const breathe=.65+.2*Math.sin(2*Math.PI*t/12)+.15*Math.sin(2*Math.PI*t/8+.5);
  const room=indoor?.00028*Math.sin(2*Math.PI*53*t)+.00011*Math.sin(2*Math.PI*79*t):.00010*Math.sin(2*Math.PI*41*t);
  data[i]=8*(second*(indoor?.021:.033)*breathe+room)*Math.min(1,t/1.5,(data.length/rate-t)/1.5);
 }
}
function wav(data){
 const bytes=Buffer.alloc(44+data.length*2);bytes.write('RIFF',0);bytes.writeUInt32LE(bytes.length-8,4);bytes.write('WAVEfmt ',8);
 bytes.writeUInt32LE(16,16);bytes.writeUInt16LE(1,20);bytes.writeUInt16LE(1,22);bytes.writeUInt32LE(rate,24);bytes.writeUInt32LE(rate*2,28);
 bytes.writeUInt16LE(2,32);bytes.writeUInt16LE(16,34);bytes.write('data',36);bytes.writeUInt32LE(data.length*2,40);
 for(let i=0;i<data.length;i++)bytes.writeInt16LE(Math.round(Math.max(-1,Math.min(1,data[i]))*32767),44+i*2);
 return bytes;
}
fs.mkdirSync(target,{recursive:true});const files=[];
function save(file,seconds,source,render){
 state=seed;const data=new Float64Array(seconds*rate);render(data);const bytes=wav(data);fs.writeFileSync(path.join(target,file),bytes);
 files.push({file,script:'tools/audio/generate-vs02-audio.mjs',seed,sample_rate:rate,bits:16,channels:1,duration_seconds:seconds,sha256:crypto.createHash('sha256').update(bytes).digest('hex'),source});
}
save('piano-quiet-theme.wav',96,'Original sparse twelve-phrase piano-like composition; additive decaying string synthesis with soft hammer envelope, not a live piano recording',music);
save('ambience-outdoor-soft.wav',24,'Original very quiet double-low-pass breeze-like bed; no field recordings, voices, birds or high-frequency noise',data=>ambience(data,false));
save('ambience-indoor-soft.wav',24,'Original very quiet room-like filtered bed with faint low resonances; no field recordings or voices',data=>ambience(data,true));
fs.writeFileSync(path.join(target,'manifest.json'),JSON.stringify({version:2,seed,generated_on:'2026-10-11',provenance:'licenses/AUDIO_VS02.md',files},null,2)+'\n');
console.log('AUDIO_VS02_GENERATED 3 original sources: '+relative);
