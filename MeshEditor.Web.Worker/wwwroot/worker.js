// Background worker of the web app: hosts the .NET runtime with the layer code (WorkerApi), so that loading,
// generating and uploading layers never blocks the page. Messages from the page:
//   { id, method, jobId, args, token, expiresOn, files }  ->  { id, result, buffers }  (buffers are transferred)
//   { cancel: jobId }
// Progress of a job is posted as { progress: jobId, message, percent }.
import { dotnet } from './_framework/dotnet.js';

const runtime = await dotnet.create();
runtime.setModuleImports('worker', {
    progress: (jobId, message, percent) => self.postMessage({ progress: jobId, message, percent })
});
const api = (await runtime.getAssemblyExports('MeshEditor.Web.Worker.dll')).MeshEditor.Web.Worker.WorkerApi;

// calls run one after another: the layer generator is synchronous, and the solution state is shared
let queue = Promise.resolve();

self.onmessage = (e) => {
    const msg = e.data;
    if (msg.cancel) {
        api.Cancel(msg.cancel);
        return;
    }
    queue = queue.then(() => handle(msg));
};

async function handle(msg) {
    try {
        if (msg.token) api.SetToken(msg.token, msg.expiresOn || Date.now() + 30 * 60 * 1000);
        for (const file of msg.files || []) {
            const bytes = new Uint8Array(await file.file.arrayBuffer());
            api.StageFile(msg.jobId, file.name, file.relativePath || file.name, bytes);
        }
        const result = JSON.parse(await api.Call(msg.method, msg.jobId || '', JSON.stringify(msg.args || {})));
        const buffers = {};
        const transfer = [];
        if (result && Array.isArray(result.buffers)) {
            for (const name of result.buffers) {
                const bytes = api.TakeBuffer(name);
                buffers[name] = bytes.buffer;
                transfer.push(bytes.buffer);
            }
        }
        self.postMessage({ id: msg.id, result, buffers }, transfer);
    } catch (err) {
        self.postMessage({ id: msg.id, result: { error: String(err && err.message || err) } });
    }
}

self.postMessage({ ready: true });
