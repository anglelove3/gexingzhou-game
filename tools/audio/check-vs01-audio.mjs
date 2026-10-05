import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {execFileSync} from 'node:child_process';
const root=process.cwd();
const manifestPath=path.join(root,'assets/audio/vs01/manifest.json');
assert.ok(fs.existsSync(manifestPath),'Missing formal audio manifest');
const manifest=JSON.parse(fs.readFileSync(manifestPath,'utf8'));
assert.equal(manifest.seed,20261005);
assert.equal(manifest.files.length,10);
for(const variant of ['a','b'])execFileSync(process.execPath,['tools/audio/generate-vs01-audio.mjs','--output','test-output/audio-repro-'+variant],{cwd:root});
for(const item of manifest.files){
 const bytes=fs.readFileSync(path.join(root,'assets/audio/vs01',item.file));
 assert.equal(bytes.toString('ascii',0,4),'RIFF');assert.equal(bytes.toString('ascii',8,12),'WAVE');
 assert.equal(bytes.readUInt16LE(20),1);assert.equal(bytes.readUInt16LE(22),1);assert.equal(bytes.readUInt32LE(24),44100);assert.equal(bytes.readUInt16LE(34),16);
 assert.ok(bytes.length>1000);let peak=0;
 for(let p=44;p<bytes.length;p+=2)peak=Math.max(peak,Math.abs(bytes.readInt16LE(p)/32768));
 assert.ok(peak>0&&peak<=.95,'Empty or clipping '+item.file);
 const hash=crypto.createHash('sha256').update(bytes).digest('hex');
 assert.equal(hash,item.sha256);
 for(const variant of ['a','b'])assert.equal(crypto.createHash('sha256').update(fs.readFileSync(path.join(root,'test-output/audio-repro-'+variant,item.file))).digest('hex'),hash);
}
console.log('AUDIO_SOURCE_PASS 10/10 deterministic mono 44100Hz 16-bit PCM; peaks <=0.95; listening pending');
