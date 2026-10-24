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
const undefinedColor = [0.4, 0.4, 0.4];
const rampSize = 256;

// The scalar value travels to the fragment shader as a vertex attribute and the color is looked up per pixel,
// so iso bands have crisp boundaries instead of colors smeared across triangles, and changing the range,
// colormap or band count only updates uniforms. Lighting stays Lambert's.
const scalarVertexShader = {
    head: `attribute float scalar;
attribute float scalarDefined;
varying float vScalar;
varying float vScalarDefined;
`,
    body: `vScalar = scalar;
vScalarDefined = scalarDefined;
`
};
const scalarFragmentShader = {
    head: `uniform bool hasScalars;
uniform float scalarMin;
uniform float scalarInvSpan;
uniform float bandCount;
uniform sampler2D colorRamp;
uniform vec3 plainColor;
uniform vec3 undefinedColor;
varying float vScalar;
varying float vScalarDefined;

vec3 rampColor(float t) {
    return texture2D(colorRamp, vec2((t * float(${rampSize - 1}) + 0.5) / float(${rampSize}), 0.5)).rgb;
}

vec3 bandColor(float band) {
    return rampColor((clamp(band, 0.0, bandCount - 1.0) + 0.5) / bandCount);
}

vec3 scalarColor() {
    if (!hasScalars) return plainColor;
    // undefined corners carry 0 in both attributes: the half of a triangle nearer to them takes the undefined
    // color and the rest interpolates the defined corners only
    if (vScalarDefined < 0.5) return undefinedColor;
    float value = vScalar / vScalarDefined;
    float t = scalarInvSpan > 0.0 ? clamp((value - scalarMin) * scalarInvSpan, 0.0, 1.0) : 0.5;
    if (bandCount < 1.0) return rampColor(t);
    // blend the two bands over about one pixel around their boundary to avoid jagged edges
    float x = t * bandCount;
    float boundary = floor(x + 0.5);
    float blend = clamp((x - boundary) / max(fwidth(x), 1e-6) + 0.5, 0.0, 1.0);
    return mix(bandColor(boundary - 1.0), bandColor(boundary), blend);
}
`,
    body: `diffuseColor.rgb *= scalarColor();
`
};

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

        this.ramps = new Map();
        this.uniforms = {
            hasScalars: { value: false },
            scalarMin: { value: 0 },
            scalarInvSpan: { value: 0 },
            bandCount: { value: 0 },
            colorRamp: { value: this.ramp('spectrum') },
            plainColor: { value: new THREE.Color(...plainColor) },
            undefinedColor: { value: new THREE.Color(...undefinedColor) }
        };
        this.material = new THREE.MeshLambertMaterial({ side: THREE.DoubleSide, polygonOffset: true, polygonOffsetFactor: 1, polygonOffsetUnits: 1 });
        this.material.onBeforeCompile = shader => {
            Object.assign(shader.uniforms, this.uniforms);
            shader.vertexShader = scalarVertexShader.head + shader.vertexShader.replace('#include <color_vertex>', '#include <color_vertex>\n' + scalarVertexShader.body);
            shader.fragmentShader = scalarFragmentShader.head + shader.fragmentShader.replace('#include <color_fragment>', '#include <color_fragment>\n' + scalarFragmentShader.body);
        };
        this.edgeMaterial = new THREE.LineBasicMaterial({ color: 0x0b1016, transparent: true, opacity: 0.35 });
        this.mesh = null;
        this.edges = null;
        this.key = null;         // solution/layer/mesh of the shown geometry
        this.layerKey = null;
        this.scalars = null;     // per corner
        this.uploadedScalars = null;
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
        const corners = positions.length / 3;
        geometry.setAttribute('scalar', new THREE.BufferAttribute(new Float32Array(corners), 1));
        geometry.setAttribute('scalarDefined', new THREE.BufferAttribute(new Float32Array(corners), 1));
        this.uploadedScalars = null;
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

    // A small texture with the colormap sampled like the legend's gradient. Its texels are sRGB, as the legend's are.
    ramp(colormap) {
        let texture = this.ramps.get(colormap);
        if (!texture) {
            const stops = colormaps[colormap] || colormaps.spectrum;
            const data = new Uint8Array(rampSize * 4);
            for (let i = 0; i < rampSize; i++) {
                const rgb = colorAt(stops, i / (rampSize - 1));
                data.set([Math.round(rgb[0] * 255), Math.round(rgb[1] * 255), Math.round(rgb[2] * 255), 255], i * 4);
            }
            texture = new THREE.DataTexture(data, rampSize, 1, THREE.RGBAFormat, THREE.UnsignedByteType);
            texture.colorSpace = THREE.SRGBColorSpace;
            texture.magFilter = texture.minFilter = THREE.LinearFilter;
            texture.wrapS = texture.wrapT = THREE.ClampToEdgeWrapping;
            texture.generateMipmaps = false;
            texture.needsUpdate = true;
            this.ramps.set(colormap, texture);
        }
        return texture;
    }

    colorize(colormap, bands, min, max) {
        const values = this.scalars?.values;
        if (values && values !== this.uploadedScalars) {
            const scalar = this.mesh.geometry.attributes.scalar, defined = this.mesh.geometry.attributes.scalarDefined;
            for (let i = 0; i < values.length; i++) {
                const v = values[i];
                const isDefined = !isNaN(v);
                scalar.array[i] = isDefined ? v : 0;
                defined.array[i] = isDefined ? 1 : 0;
            }
            scalar.needsUpdate = defined.needsUpdate = true;
            this.uploadedScalars = values;
        }
        const span = max - min;
        const u = this.uniforms;
        u.hasScalars.value = !!values;
        u.scalarMin.value = min ?? 0;
        u.scalarInvSpan.value = span > 0 ? 1 / span : 0;
        u.bandCount.value = bands;
        u.colorRamp.value = this.ramp(colormap);
        this.colormap = { stops: colormaps[colormap] || colormaps.spectrum, bands, min, max };
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
        this.clear();
        this.material.dispose();
        this.edgeMaterial.dispose();
        for (const texture of this.ramps.values()) texture.dispose();
        this.resizeObserver.disconnect();
        this.renderer.dispose();
        this.renderer.domElement.remove();
    }
}

export function create(host) {
    return new Viewer(host);
}
