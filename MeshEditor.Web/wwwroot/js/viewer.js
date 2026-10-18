// three.js view of one layer: triangles colored by a data component, cell edges, deformation by a vector field.
// The geometry and data come straight from the background worker as typed arrays.
import * as THREE from '../lib/three/three.module.min.js';
import { OrbitControls } from '../lib/three/OrbitControls.js';
import { call } from './app.js';

const colormaps = {
    spectrum: [[0x2b, 0x2f, 0xbf], [0x1f, 0x8f, 0xe0], [0x22, 0xc7, 0xa9], [0x9b, 0xe0, 0x4a], [0xf2, 0xd3, 0x3a], [0xf0, 0x8a, 0x2a], [0xd6, 0x32, 0x2b]],
    heat: [[0x1d, 0x3f, 0xbf], [0xf2, 0xf2, 0xf2], [0xc8, 0x23, 0x23]],
    grayscale: [[0x10, 0x10, 0x10], [0xf0, 0xf0, 0xf0]]
};
const plainColor = [0.62, 0.7, 0.78];

function colorAt(stops, t) {
    const x = Math.min(1, Math.max(0, t)) * (stops.length - 1);
    const i = Math.min(stops.length - 2, Math.floor(x));
    const f = x - i;
    const a = stops[i], b = stops[i + 1];
    return [(a[0] + (b[0] - a[0]) * f) / 255, (a[1] + (b[1] - a[1]) * f) / 255, (a[2] + (b[2] - a[2]) * f) / 255];
}

function css(rgb) {
    return `rgb(${rgb.map(v => Math.round(v * 255)).join(',')})`;
}

function formatValue(v) {
    if (!isFinite(v)) return '–';
    const a = Math.abs(v);
    if (a !== 0 && (a < 1e-3 || a >= 1e5)) return v.toExponential(2);
    return Number(v.toPrecision(4)).toString();
}

class Viewer {
    constructor(host) {
        this.host = host;
        this.renderer = new THREE.WebGLRenderer({ antialias: true });
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
        this.renderer.setClearColor(0x161e27, 1);
        host.prepend(this.renderer.domElement);
        this.scene = new THREE.Scene();
        this.camera = new THREE.PerspectiveCamera(35, 1, 0.01, 1000);
        this.camera.up.set(0, 0, 1);
        this.controls = new OrbitControls(this.camera, this.renderer.domElement);
        this.controls.enableDamping = true;
        this.scene.add(new THREE.HemisphereLight(0xffffff, 0x334455, 1.6));
        const light = new THREE.DirectionalLight(0xffffff, 1.2);
        this.camera.add(light);
        light.position.set(1, 1, 2);
        this.scene.add(this.camera);

        this.material = new THREE.MeshLambertMaterial({ vertexColors: true, side: THREE.DoubleSide, polygonOffset: true, polygonOffsetFactor: 1, polygonOffsetUnits: 1 });
        this.edgeMaterial = new THREE.LineBasicMaterial({ color: 0x0b1016, transparent: true, opacity: 0.35 });
        this.mesh = null;
        this.edges = null;
        this.key = null;         // solution/layer/mesh of the shown geometry
        this.layerKey = null;
        this.scalars = null;     // per corner
        this.range = null;
        this.request = 0;
        this.vectorCache = new Map();

        this.legend = document.createElement('div');
        this.legend.className = 'vp-legend';
        this.legend.hidden = true;
        host.appendChild(this.legend);
        this.pick = document.createElement('div');
        this.pick.className = 'vp-pick';
        this.pick.hidden = true;
        host.appendChild(this.pick);
        this.status = document.createElement('div');
        this.status.className = 'vp-status';
        this.status.hidden = true;
        host.appendChild(this.status);

        this.raycaster = new THREE.Raycaster();
        let down = null;
        this.renderer.domElement.addEventListener('pointerdown', e => down = [e.clientX, e.clientY]);
        this.renderer.domElement.addEventListener('pointerup', e => {
            if (down && Math.hypot(e.clientX - down[0], e.clientY - down[1]) < 4) this.pickAt(e);
            down = null;
        });

        this.resizeObserver = new ResizeObserver(() => this.resize());
        this.resizeObserver.observe(host);
        this.resize();
        const loop = () => {
            if (this.disposed) return;
            requestAnimationFrame(loop);
            this.controls.update();
            this.renderer.render(this.scene, this.camera);
        };
        loop();
    }

    resize() {
        const w = this.host.clientWidth, h = this.host.clientHeight;
        if (!w || !h) return;
        this.renderer.setSize(w, h, false);
        this.camera.aspect = w / h;
        this.camera.updateProjectionMatrix();
    }

    setStatus(text) {
        this.status.textContent = text || '';
        this.status.hidden = !text;
    }

    // options: { solutionId, layerId, meshIndex, field, component, timeStep, vectorField, scale (null: automatic),
    //            colormap, bands, showEdges, rangeMin, rangeMax }
    // Returns a summary for the page: { points, cells, triangles, min, max, scale, error }.
    async show(o) {
        const request = ++this.request;
        const stale = () => request !== this.request;
        try {
            const layerKey = `${o.solutionId}/${o.layerId}`;
            let scalars = null;
            let meshIndex = o.meshIndex;
            if (o.field) {
                this.setStatus('Loading data…');
                const r = await call('getScalars', { solutionId: o.solutionId, layerId: o.layerId, field: o.field, component: o.component || 'magnitude', timeStep: o.timeStep });
                if (r.result.error) throw new Error(r.result.error);
                if (stale()) return null;
                scalars = { values: new Float32Array(r.buffers.scalars), min: r.result.meta.min, max: r.result.meta.max };
                meshIndex = r.result.meta.meshIndex;
            }
            const key = `${layerKey}/${meshIndex}`;
            if (key !== this.key) {
                this.setStatus('Loading mesh…');
                const r = await call('buildView', { solutionId: o.solutionId, layerId: o.layerId, meshIndex });
                if (r.result.error) throw new Error(r.result.error);
                if (stale()) return null;
                this.setGeometry(new Float32Array(r.buffers.positions), new Float32Array(r.buffers.edges), r.result.meta, layerKey !== this.layerKey);
                this.key = key;
                this.layerKey = layerKey;
                if (layerKey !== this.vectorLayer) {
                    this.vectorCache.clear();
                    this.vectorLayer = layerKey;
                }
            }
            this.scalars = scalars;

            let scale = 0;
            let vectors = null;
            if (o.vectorField) {
                const vkey = `${key}/${o.vectorField}/${o.timeStep}`;
                vectors = this.vectorCache.get(vkey);
                if (!vectors) {
                    this.setStatus('Loading deformation…');
                    const r = await call('getVectors', { solutionId: o.solutionId, layerId: o.layerId, field: o.vectorField, timeStep: o.timeStep });
                    if (r.result.error) throw new Error(r.result.error);
                    if (stale()) return null;
                    vectors = { corners: new Float32Array(r.buffers.cornerVectors), edges: new Float32Array(r.buffers.edgeVectors), max: r.result.meta.maxMagnitude };
                    this.vectorCache.set(vkey, vectors);
                }
                scale = o.scale ?? (vectors.max > 0 ? Number((0.1 * this.diagonal / vectors.max).toPrecision(2)) : 1);
            }
            this.deform(vectors, scale);

            const min = o.rangeMin ?? scalars?.min, max = o.rangeMax ?? scalars?.max;
            this.colorize(o.colormap || 'spectrum', o.bands ? 12 : 0, min, max);
            this.edges.visible = o.showEdges !== false;
            this.updateLegend(o, min, max);
            this.pick.hidden = true;
            this.setStatus('');
            return { points: this.meta.points, cells: this.meta.cells, triangles: this.meta.triangles, min: scalars?.min ?? null, max: scalars?.max ?? null, scale, bounds: [...this.meta.boundsMin, ...this.meta.boundsMax], error: null };
        } catch (err) {
            if (stale()) return null;
            this.setStatus('');
            return { error: String(err && err.message || err) };
        }
    }

    setGeometry(positions, edgePositions, meta, fit) {
        if (this.mesh) {
            this.scene.remove(this.mesh, this.edges);
            this.mesh.geometry.dispose();
            this.edges.geometry.dispose();
        }
        this.meta = meta;
        this.basePositions = positions;
        this.baseEdges = edgePositions;
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.BufferAttribute(positions.slice(), 3));
        geometry.setAttribute('color', new THREE.BufferAttribute(new Float32Array(positions.length), 3));
        geometry.computeVertexNormals();
        this.mesh = new THREE.Mesh(geometry, this.material);
        const edgeGeometry = new THREE.BufferGeometry();
        edgeGeometry.setAttribute('position', new THREE.BufferAttribute(edgePositions.slice(), 3));
        this.edges = new THREE.LineSegments(edgeGeometry, this.edgeMaterial);
        this.scene.add(this.mesh, this.edges);

        const min = new THREE.Vector3(...meta.boundsMin), max = new THREE.Vector3(...meta.boundsMax);
        this.diagonal = Math.max(1e-9, min.distanceTo(max));
        if (fit) this.fitCamera();
    }

    fitCamera() {
        if (!this.meta) return;
        const min = new THREE.Vector3(...this.meta.boundsMin), max = new THREE.Vector3(...this.meta.boundsMax);
        const center = min.clone().add(max).multiplyScalar(0.5);
        {
            this.controls.target.copy(center);
            // the bounding sphere fits the narrower side of the view
            const halfFov = THREE.MathUtils.degToRad(this.camera.fov) / 2;
            const halfView = Math.min(halfFov, Math.atan(Math.tan(halfFov) * this.camera.aspect));
            const distance = (this.diagonal / 2) / Math.sin(halfView) * 1.1;
            this.camera.position.copy(center).add(new THREE.Vector3(0.55, -0.75, 0.45).normalize().multiplyScalar(distance));
            this.camera.near = this.diagonal / 1000;
            this.camera.far = this.diagonal * 100;
            this.camera.updateProjectionMatrix();
            this.controls.update();
        }
    }

    deform(vectors, scale) {
        const position = this.mesh.geometry.attributes.position;
        const edgePosition = this.edges.geometry.attributes.position;
        const apply = (base, target, v) => {
            if (v && scale) {
                for (let i = 0; i < base.length; i++) target[i] = base[i] + scale * v[i];
            } else {
                target.set(base);
            }
        };
        apply(this.basePositions, position.array, vectors?.corners);
        apply(this.baseEdges, edgePosition.array, vectors?.edges);
        position.needsUpdate = true;
        edgePosition.needsUpdate = true;
        this.mesh.geometry.computeVertexNormals();
        this.mesh.geometry.computeBoundingSphere();
        this.edges.geometry.computeBoundingSphere();
    }

    colorize(colormap, bands, min, max) {
        const colors = this.mesh.geometry.attributes.color.array;
        const stops = colormaps[colormap] || colormaps.spectrum;
        const values = this.scalars?.values;
        const span = max - min;
        for (let i = 0, n = colors.length / 3; i < n; i++) {
            let rgb = plainColor;
            if (values) {
                let t = span > 0 ? (values[i] - min) / span : 0.5;
                if (bands) t = (Math.min(bands - 1, Math.floor(Math.min(1, Math.max(0, t)) * bands)) + 0.5) / bands;
                rgb = isNaN(values[i]) ? [0.4, 0.4, 0.4] : colorAt(stops, t);
            }
            colors[3 * i] = rgb[0];
            colors[3 * i + 1] = rgb[1];
            colors[3 * i + 2] = rgb[2];
        }
        this.mesh.geometry.attributes.color.needsUpdate = true;
        this.colormap = { stops, bands, min, max };
    }

    updateLegend(o, min, max) {
        if (!this.scalars) {
            this.legend.hidden = true;
            return;
        }
        const { stops, bands } = this.colormap;
        let gradient;
        if (bands) {
            const parts = [];
            for (let i = 0; i < bands; i++) parts.push(`${css(colorAt(stops, (i + 0.5) / bands))} ${i / bands * 100}% ${(i + 1) / bands * 100}%`);
            gradient = `linear-gradient(90deg, ${parts.join(', ')})`;
        } else {
            gradient = `linear-gradient(90deg, ${stops.map((_, i) => css(colorAt(stops, i / (stops.length - 1)))).join(', ')})`;
        }
        const ticks = [0, 0.25, 0.5, 0.75, 1].map(t => `<span>${formatValue(min + (max - min) * t)}</span>`).join('');
        const component = o.component && o.component !== 'magnitude' ? o.component : '|magnitude|';
        this.legend.innerHTML = `<div class="vp-legend-title"></div><div class="vp-ramp" style="background:${gradient}"></div><div class="vp-ticks">${ticks}</div>`;
        this.legend.firstChild.textContent = `${o.field} · ${component} · step ${o.timeStep}`;
        this.legend.hidden = false;
    }

    pickAt(e) {
        if (!this.mesh) return;
        const rect = this.renderer.domElement.getBoundingClientRect();
        const pointer = new THREE.Vector2(((e.clientX - rect.left) / rect.width) * 2 - 1, -((e.clientY - rect.top) / rect.height) * 2 + 1);
        this.raycaster.setFromCamera(pointer, this.camera);
        const hit = this.raycaster.intersectObject(this.mesh, false)[0];
        if (!hit) {
            this.pick.hidden = true;
            return;
        }
        // nearest corner of the hit triangle
        const position = this.mesh.geometry.attributes.position;
        let best = hit.face.a, bestDistance = Infinity;
        for (const corner of [hit.face.a, hit.face.b, hit.face.c]) {
            const d = new THREE.Vector3().fromBufferAttribute(position, corner).distanceTo(hit.point);
            if (d < bestDistance) { bestDistance = d; best = corner; }
        }
        const p = hit.point;
        let text = `x ${formatValue(p.x)}  y ${formatValue(p.y)}  z ${formatValue(p.z)}`;
        if (this.scalars) text += `\nvalue ${formatValue(this.scalars.values[best])}`;
        this.pick.textContent = text;
        this.pick.hidden = false;
    }

    resetCamera() {
        this.fitCamera();
    }

    clear() {
        if (this.mesh) {
            this.scene.remove(this.mesh, this.edges);
            this.mesh.geometry.dispose();
            this.edges.geometry.dispose();
            this.mesh = this.edges = null;
        }
        this.key = this.layerKey = null;
        this.scalars = null;
        this.legend.hidden = true;
        this.pick.hidden = true;
        this.request++;
    }

    dispose() {
        this.disposed = true;
        this.resizeObserver.disconnect();
        this.renderer.dispose();
        this.renderer.domElement.remove();
    }
}

export function create(host) {
    return new Viewer(host);
}
