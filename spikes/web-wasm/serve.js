const http=require('http'),fs=require('fs'),path=require('path');const root=process.argv[2];
const types={'.wasm':'application/wasm','.js':'text/javascript','.json':'application/json','.html':'text/html','.css':'text/css'};
http.createServer((q,r)=>{let p=path.join(root,decodeURIComponent(q.url.split('?')[0]));if(!fs.existsSync(p)||fs.statSync(p).isDirectory())p=path.join(root,'index.html');r.writeHead(200,{'Content-Type':types[path.extname(p)]||'application/octet-stream'});fs.createReadStream(p).pipe(r);}).listen(8765);
