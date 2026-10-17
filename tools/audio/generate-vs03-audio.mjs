import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const at=process.argv.indexOf('--output');
if(at<0||!process.argv[at+1])throw new Error('Use --output assets/audio/vs03 or test-output/subdirectory');
const target=path.resolve(root,process.argv[at+1]);const relative=path.relative(root,target).replaceAll('\\','/');
if(relative!=='assets/audio/vs03'&&!relative.startsWith('test-output/'))throw new Error('Output must remain in project audio/test-output');
const rate=44100,seconds=32,seed=20261016;let state=seed;
const random=()=>{state=(Math.imul(state,1664525)+1013904223)>>>0;return state/2147483648-1;};
const samples=new Float64Array(rate*seconds);let smooth=0;
for(let i=0;i<samples.length;i++){
 const t=i/rate;smooth=.997*smooth+.003*random();
 samples[i]=smooth*.007+.00065*Math.sin(2*Math.PI*50*t)+.00018*Math.sin(2*Math.PI*100*t);
}
// Two distant bell gestures, not a recording or an invitation/quest cue.
for(const start of [7.5,23.3])for(let i=0;i<rate*2;i++){
 const t=i/rate;const envelope=(1-Math.exp(-t/0.008))*Math.exp(-t/0.32);
 samples[Math.round(start*rate)+i]+=.0035*envelope*(Math.sin(2*Math.PI*1180*t)+.25*Math.sin(2*Math.PI*1731*t));
}
// Four very soft pairs of low-passed foot contacts. No speech or collected field audio.
for(const start of [3.2,3.7,16.1,16.6]){let contact=0;for(let i=0;i<rate*.32;i++){
 const t=i/rate;contact=.97*contact+.03*random();samples[Math.round(start*rate)+i]+=.006*contact*Math.sin(Math.PI*t/.32)**2;
}}
const bytes=Buffer.alloc(44+samples.length*2);bytes.write('RIFF',0);bytes.writeUInt32LE(bytes.length-8,4);bytes.write('WAVEfmt ',8);
bytes.writeUInt32LE(16,16);bytes.writeUInt16LE(1,20);bytes.writeUInt16LE(1,22);bytes.writeUInt32LE(rate,24);bytes.writeUInt32LE(rate*2,28);
bytes.writeUInt16LE(2,32);bytes.writeUInt16LE(16,34);bytes.write('data',36);bytes.writeUInt32LE(samples.length*2,40);
for(let i=0;i<samples.length;i++){const t=i/rate;const fade=Math.min(1,t/1.5,(samples.length-1-i)/(rate*1.5));bytes.writeInt16LE(Math.round(samples[i]*fade*32767),44+i*2);}
fs.mkdirSync(target,{recursive:true});fs.writeFileSync(path.join(target,'ambience-street-soft.wav'),bytes);
const files=[{file:'ambience-street-soft.wav',script:'tools/audio/generate-vs03-audio.mjs',seed,sample_rate:rate,bits:16,channels:1,duration_seconds:seconds,sha256:crypto.createHash('sha256').update(bytes).digest('hex'),source:'Original synthetic freezer hum, two soft distant bicycle-bell gestures and four filtered foot contacts. No voices, recordings or third-party samples.'}];
fs.writeFileSync(path.join(target,'manifest.json'),JSON.stringify({version:3,seed,generated_on:'2026-10-16',provenance:'licenses/AUDIO_VS03.md',files},null,2)+'\n');
console.log('AUDIO_VS03_GENERATED '+relative);
