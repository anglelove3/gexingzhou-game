import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {execFileSync} from 'node:child_process';

const legacy=process.argv.includes('--legacy');
const source=legacy?'assets/audio/vs01':'assets/audio/vs02';
const manifest=JSON.parse(fs.readFileSync(path.join(source,'manifest.json'),'utf8'));
const music=manifest.files.find(f=>f.file.includes(legacy?'guitar':'piano'));
assert.ok(music.duration_seconds>=64,'Music loop is too short for long quiet phrases: '+music.duration_seconds+' seconds');
assert.equal(manifest.files.length,3,'Only music and two quiet beds belong to this revision');
const report=[];
for(const item of manifest.files){
 const bytes=fs.readFileSync(path.join(source,item.file));
 assert.equal(bytes.toString('ascii',0,4),'RIFF');assert.equal(bytes.toString('ascii',8,12),'WAVE');
 assert.equal(bytes.readUInt16LE(20),1);assert.equal(bytes.readUInt16LE(22),1);assert.equal(bytes.readUInt32LE(24),44100);assert.equal(bytes.readUInt16LE(34),16);
 const count=(bytes.length-44)/2;let peak=0,power=0,differencePower=0,maxStep=0,previous=0,quietWindows=0;
 for(let i=0;i<count;i++){
  const sample=bytes.readInt16LE(44+i*2)/32768;peak=Math.max(peak,Math.abs(sample));power+=sample*sample;
  const step=sample-previous;differencePower+=step*step;maxStep=Math.max(maxStep,Math.abs(step));previous=sample;
 }
 for(let start=0;start<count;start+=44100){let sum=0;const end=Math.min(start+44100,count);for(let i=start;i<end;i++){const s=bytes.readInt16LE(44+i*2)/32768;sum+=s*s;}if(Math.sqrt(sum/(end-start))<.002)quietWindows++;}
 const rms=Math.sqrt(power/count),roughness=Math.sqrt(differencePower/count);
 assert.ok(peak>0&&peak<.5,'Clipping or empty source '+item.file);
 assert.ok(maxStep<.04,'Abrupt waveform steps '+item.file);
 assert.ok(Math.abs(bytes.readInt16LE(44)-bytes.readInt16LE(bytes.length-2))<=2,'Discontinuous loop boundary '+item.file);
 assert.equal(crypto.createHash('sha256').update(bytes).digest('hex'),item.sha256,'Hash mismatch '+item.file);
 if(item===music){assert.ok(quietWindows>=12,'Music has insufficient breathing space');assert.ok(rms<.05,'Music dynamics are too loud');}
 else{assert.ok(item.duration_seconds>=16,'Ambience loop too short');assert.ok(rms<.004&&roughness<.0001,'Ambient bed is too loud or hissy');}
 report.push({file:item.file,seconds:count/44100,peak,rms,roughness,maxStep,quietSeconds:quietWindows});
}
for(const variant of ['a','b']){
 const output='test-output/polish-2026-10-11/audio/repro-'+variant;
 execFileSync(process.execPath,['tools/audio/generate-vs02-audio.mjs','--output',output],{stdio:'pipe'});
 for(const item of manifest.files)assert.equal(crypto.createHash('sha256').update(fs.readFileSync(path.join(output,item.file))).digest('hex'),item.sha256,'Reproduction differs '+item.file);
}
fs.mkdirSync('test-output/polish-2026-10-11/audio',{recursive:true});
fs.writeFileSync('test-output/polish-2026-10-11/audio/source-metrics.json',JSON.stringify(report,null,2)+'\n');
console.log('AUDIO_VS02_SOURCE_PASS 3/3 reproducible; PCM; quiet phrases; low bed roughness; seamless file boundaries; listening remains subjective');
