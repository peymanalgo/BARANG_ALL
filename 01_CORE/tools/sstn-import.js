// SSTN categorized JPEG importer — imports only the 10 exact-match folders
// Usage: node sstn-import.js
const fs=require('fs');
const path=require('path');
const API='http://localhost:8080';
const COMPANY='42f0e459-6095-45bb-aa13-f38126d91d9c';
const ROOT='D:/Image/SSTN CATEGORISE JPEG';
const FOLDERS={
  'AMBULANCE':'SHUT-15-10',
  'BASKETBALL':'SHUT-31-02',
  'CRYPTOCURRENCY':'SHUT-09-10',
  'FASHION':'SHUT-10-09',
  'FOOTBALL':'SHUT-31-01',
  'GRADUATION':'SHUT-14-02',
  'LOVE':'SHUT-21-12',
  'NURSE':'SHUT-15-07',
  'PAINTING':'SHUT-05-02',
  'WINTER':'SHUT-29-04'
};
const sleep=ms=>new Promise(r=>setTimeout(r,ms));
async function login(){
  const r=await fetch(API+'/api/v1/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({username:'admin',password:'BarangDev@2026'})});
  const j=await r.json();
  if(!j.accessToken)throw new Error('login failed');
  return j.accessToken;
}
async function uploadFile(token,file){
  const buf=fs.readFileSync(file);
  const fd=new FormData();
  fd.append('file',new Blob([buf]),path.basename(file));
  const r=await fetch(API+'/api/v1/files/upload',{method:'POST',headers:{'Authorization':'Bearer '+token,'X-Company-ID':COMPANY},body:fd});
  const j=await r.json();
  return {ok:r.ok,status:r.status,j};
}
async function createArtwork(token,code,title,catCode){
  const r=await fetch(API+'/api/v1/artworks',{method:'POST',headers:{'Authorization':'Bearer '+token,'X-Company-ID':COMPANY,'Content-Type':'application/json'},body:JSON.stringify({code,title,description:null,categoryCode:catCode,designerUserId:null,lifecycleStatus:'Active'})});
  const j=await r.json();
  return {ok:r.ok,status:r.status,j};
}
async function createVersion(token,artworkId,fileVersionId){
  const r=await fetch(API+'/api/v1/artworks/'+artworkId+'/versions',{method:'POST',headers:{'Authorization':'Bearer '+token,'X-Company-ID':COMPANY,'Content-Type':'application/json'},body:JSON.stringify({fileVersionId,notes:'SSTN import'})});
  return {ok:r.ok,status:r.status};
}
function safeCode(s){return s.toUpperCase().replace(/[^A-Z0-9]/g,'-').replace(/-+/g,'-').replace(/^-|-$/g,'').substring(0,45);}
(async()=>{
  const token=await login();
  let done=0,failed=0,dup=0;
  const errors=[];
  for(const [folder,catCode] of Object.entries(FOLDERS)){
    const dir=path.join(ROOT,folder);
    if(!fs.existsSync(dir)){console.log('skip (no dir):',folder);continue;}
    const files=fs.readdirSync(dir).filter(f=>/\.(jpe?g|png|webp)$/i.test(f));
    console.log('=== '+folder+' ('+files.length+' files) -> '+catCode);
    let i=0;
    for(const f of files){
      i++;
      const full=path.join(dir,f);
      const base=path.basename(f,path.extname(f));
      const code=safeCode(folder)+'-'+safeCode(base);
      const title=folder.charAt(0)+folder.slice(1).toLowerCase().replace(/-/g,' ')+' — '+base;
      try{
        const up=await uploadFile(token,full);
        if(!up.ok){failed++;errors.push(folder+'/'+f+' upload '+up.status+' '+(up.j.code||''));continue;}
        const fileVersionId=up.j.fileVersionId;
        const aw=await createArtwork(token,code,title,catCode);
        if(!aw.ok){
          if(aw.j.code==='artwork.duplicate_code'){dup++;errors.push(folder+'/'+f+' duplicate code '+code);}
          else {failed++;errors.push(folder+'/'+f+' artwork '+aw.status+' '+(aw.j.code||''));}
          continue;
        }
        const artworkId=aw.j.id;
        const v=await createVersion(token,artworkId,fileVersionId);
        if(!v.ok){failed++;errors.push(folder+'/'+f+' version '+v.status);continue;}
        done++;
        if(done%25===0)console.log('  progress:',done,'imported');
      }catch(e){failed++;errors.push(folder+'/'+f+' EX '+e.message);}
      await sleep(30);
    }
  }
  console.log('=== DONE. imported:',done,'| failed:',failed,'| dup:',dup);
  if(errors.length){fs.writeFileSync('sstn-import-errors.txt',errors.join('\n'));console.log('errors file written');}
})().catch(e=>{console.error('FATAL',e);process.exit(1);});
