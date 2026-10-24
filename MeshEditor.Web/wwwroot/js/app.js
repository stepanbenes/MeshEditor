// Connection of the page to the background worker (worker/worker.js), which runs the .NET layer code.
// Blazor calls callJson; the 3D viewer calls call directly and gets the buffers without going through .NET.
import * as auth from './auth.js';

let worker = null;
let ready = null;
let nextId = 1;
let useAuth = false;
let progressTarget = null;
const pending = new Map();

function start() {
    if (!ready) {
        ready = new Promise((resolve, reject) => {
            worker = new Worker(new URL('worker/worker.js', document.baseURI), { type: 'module' });
            worker.onmessage = (e) => {
                const msg = e.data;
                if (msg.ready) {
                    resolve();
                } else if (msg.progress) {
                    progressTarget?.invokeMethodAsync('OnProgress', msg.progress, msg.message, msg.percent);
                } else if (msg.id && pending.has(msg.id)) {
                    const { resolve: done } = pending.get(msg.id);
                    pending.delete(msg.id);
                    done(msg);
                }
            };
            worker.onerror = (e) => {
                const error = new Error(e.message || 'The background worker failed to start.');
                reject(error);
                for (const { reject: fail } of pending.values()) fail(error);
                pending.clear();
            };
        });
    }
    return ready;
}

// { result, buffers } of a worker method. Files: [{ name, relativePath, file }].
export async function call(method, args, jobId, files) {
    await start();
    const message = { id: nextId++, method, jobId: jobId || '', args: args || {}, files: files || [] };
    if (useAuth) Object.assign(message, await auth.getToken());
    return new Promise((resolve, reject) => {
        pending.set(message.id, { resolve, reject });
        worker.postMessage(message);
    });
}

// For Blazor: the result as JSON ({"error": ...} when it failed).
export async function callJson(method, argsJson, jobId, fileInputIds) {
    try {
        const files = [];
        for (const inputId of fileInputIds || []) {
            const input = document.getElementById(inputId);
            for (const file of input?.files || []) {
                files.push({ name: file.name, relativePath: file.webkitRelativePath || file.name, file });
            }
        }
        const { result } = await call(method, JSON.parse(argsJson || '{}'), jobId, files);
        return JSON.stringify(result);
    } catch (err) {
        return JSON.stringify({ error: String(err && err.message || err) });
    }
}

export function cancel(jobId) {
    worker?.postMessage({ cancel: jobId });
}

export function setProgressTarget(dotnetRef) {
    progressTarget = dotnetRef;
}

export function setUseAuth(value) {
    useAuth = value;
}

// Files picked in a file input: [{ name, relativePath, size }].
export function pickedFiles(inputId) {
    const input = document.getElementById(inputId);
    return [...(input?.files || [])].map(f => ({ name: f.name, relativePath: f.webkitRelativePath || f.name, size: f.size }));
}

export function clearFiles(inputId) {
    const input = document.getElementById(inputId);
    if (input) input.value = '';
}

export const signIn = () => auth.signIn();
export const signOut = () => auth.signOut();
export async function initAuth(clientId, tenantId) {
    return JSON.stringify(await auth.init(clientId, tenantId));
}
