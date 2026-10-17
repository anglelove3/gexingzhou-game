import assert from 'node:assert/strict';
import fs from 'node:fs';
import crypto from 'node:crypto';
import {execFileSync} from 'node:child_process';
const manifest=JSON.parse(fs.readFileSync('assets/audio/vs03/manifest.json','utf8'));
assert.equal(manifest.files.length,1);const entry=manifest.files[0];const bytes=fs.readFileSync('assets/audio/vs03/'+entry.file);
assert.equal(bytes.toString('ascii',0,4),'RIFF');assert.equal(bytes.toString('ascii',8,12),'WAVE');
assert.equal(bytes.readUInt16LE(20),1);assert.equal(bytes.readUInt16LE(22),1);assert.equal(bytes.readUInt32LE(24),44100);assert.equal(bytes.readUInt16LE(34),16);
assert.equal(bytes.readUInt32LE(40),bytes.length-44);assert.equal(bytes.readUInt32LE(4),bytes.length-8);
const count=(bytes.length-44)/2;assert.equal(count/44100,32);let power=0,peak=0,maxStep=0,last=0;
for(let i=0;i<count;i++){const s=bytes.readInt16LE(44+i*2)/32768;power+=s*s;peak=Math.max(peak,Math.abs(s));maxStep=Math.max(maxStep,Math.abs(s-last));last=s;}
const rms=Math.sqrt(power/count);assert.ok(peak>0&&peak<.02);assert.ok(rms<.004);assert.ok(maxStep<.004);
assert.ok(Math.abs(bytes.readInt16LE(44)-bytes.readInt16LE(bytes.length-2))<=2);
const hash=data=>crypto.createHash('sha256').update(data).digest('hex');assert.equal(hash(bytes),entry.sha256);
for(const variant of ['a','b']){const output='test-output/bc-audio/repro-'+variant;execFileSync(process.execPath,['tools/audio/generate-vs03-audio.mjs','--output',output],{stdio:'pipe'});assert.equal(hash(fs.readFileSync(output+'/'+entry.file)),entry.sha256);}
fs.mkdirSync('test-output/bc-audio',{recursive:true});fs.writeFileSync('test-output/bc-audio/metrics.json',JSON.stringify({seconds:32,peak,rms,maxStep,sha256:entry.sha256},null,2)+'\n');
console.log('AUDIO_VS03_SOURCE_PASS PCM mono44100/16bit 32seconds quiet seamless hash reproducible; human listening pending');
