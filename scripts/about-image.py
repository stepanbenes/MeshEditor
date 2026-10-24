#!/usr/bin/env python3
# Renders the picture of the About dialog (MeshEditor.Avalonia/Assets/about.png): a plate with a hole under vertical tension,
# meshed by quadrilaterals graded towards the hole, deformed (exaggerated) and colored by the von Mises stress of the Kirsch
# solution, with the rainbow color scale and the mesh edges as the postprocessor draws them.
# Needs numpy and matplotlib: python3 scripts/about-image.py [output.png]
import sys
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib import cm
from matplotlib.colors import LightSource, Normalize
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

output = sys.argv[1] if len(sys.argv) > 1 else "MeshEditor.Avalonia/Assets/about.png"

a = 0.38 # hole radius
half_width, half_height = 1.0, 1.7
thickness = 0.16
n_angle, n_radial, n_end = 64, 14, 6 # elements around the hole, from the hole to the square, in each end block

# nodes: rays from the hole to the boundary of the square [-1, 1]^2, graded towards the hole
theta = np.linspace(0, 2 * np.pi, n_angle + 1)[:-1] # n_angle is a multiple of 8: rays to the corners of the square
t = (np.linspace(0, 1, n_radial + 1)) ** 1.6
inner = np.stack([a * np.cos(theta), a * np.sin(theta)], axis=1)
outer = np.stack([np.cos(theta), np.sin(theta)], axis=1)
outer = outer / np.max(np.abs(outer), axis=1, keepdims=True) # onto the square
xy = inner[None, :, :] * (1 - t[:, None, None]) + outer[None, :, :] * t[:, None, None] # (radial, angle, 2)

quads = [] # 2D polygons
for i in range(n_radial):
	for j in range(n_angle):
		k = (j + 1) % n_angle
		quads.append(np.array([xy[i, j], xy[i, k], xy[i + 1, k], xy[i + 1, j]]))
# end blocks above and below the square, matching the nodes of its top and bottom edges
for sign in (1, -1):
	edge = xy[-1][np.isclose(xy[-1][:, 1], sign * 1.0)]
	edge = np.vstack([edge, [[-1.0, sign], [1.0, sign]]])
	xs = np.unique(np.round(edge[:, 0], 9))
	ys = sign * (1 + (half_height - 1) * np.linspace(0, 1, n_end + 1))
	for i in range(n_end):
		for j in range(len(xs) - 1):
			quads.append(np.array([[xs[j], ys[i]], [xs[j + 1], ys[i]], [xs[j + 1], ys[i + 1]], [xs[j], ys[i + 1]]]))

def kirsch(p):
	"""Stresses of an infinite plate with a hole under the unit tension in the y direction (Kirsch), as (sxx, syy, sxy)."""
	x, y = p[..., 0], p[..., 1]
	r = np.maximum(np.hypot(x, y), a)
	th = np.arctan2(x, y) # the angle from the load direction
	q = (a / r) ** 2
	srr = 0.5 * (1 - q) + 0.5 * (1 - 4 * q + 3 * q * q) * np.cos(2 * th)
	stt = 0.5 * (1 + q) - 0.5 * (1 + 3 * q * q) * np.cos(2 * th)
	srt = -0.5 * (1 + 2 * q - 3 * q * q) * np.sin(2 * th)
	return srr, stt, srt

def von_mises(p):
	srr, stt, srt = kirsch(p)
	return np.sqrt(srr ** 2 - srr * stt + stt ** 2 + 3 * srt ** 2)

def deform(p):
	# exaggerated: stretching along the load, the necking of the ligaments, a gentle bend out of the plane
	x, y = p[..., 0], p[..., 1]
	r = np.maximum(np.hypot(x, y), a)
	dx = -0.06 * x * (1 + (a / r) ** 2)
	dy = 0.10 * y + 0.08 * y * (a / r) ** 2
	dz = 0.22 * (y / half_height) ** 2
	return np.stack([x + dx, y + dy, dz], axis=-1)

norm = Normalize(vmin=0.0, vmax=3.0)
cmap = cm.jet
light = LightSource(azdeg=315, altdeg=35)

faces, colors = [], []
def add(poly3, value, shade_normal):
	n = shade_normal / np.linalg.norm(shade_normal)
	l = np.array([-0.5, 0.6, 0.65]); l /= np.linalg.norm(l)
	intensity = 0.55 + 0.45 * max(np.dot(n, l), 0.0)
	rgb = np.array(cmap(norm(value))[:3]) * intensity
	faces.append(poly3)
	colors.append((*np.clip(rgb, 0, 1), 1.0))

for q in quads:
	top = deform(q)
	top[:, 2] += thickness / 2
	n = np.cross(top[1] - top[0], top[3] - top[0])
	if n[2] < 0:
		n = -n
	add(top, von_mises(q.mean(axis=0)), n)

# the wall of the hole (where the stress is highest) and the side faces of the plate
def side(p0, p1, outward):
	b0, b1 = deform(p0), deform(p1)
	poly = np.array([b0 - [0, 0, thickness / 2], b1 - [0, 0, thickness / 2], b1 + [0, 0, thickness / 2], b0 + [0, 0, thickness / 2]])
	n = np.cross(poly[1] - poly[0], poly[3] - poly[0])
	if np.dot(n[:2], outward) < 0:
		n = -n
	add(poly, von_mises((p0 + p1) / 2), n)

for j in range(n_angle):
	k = (j + 1) % n_angle
	side(xy[0, j], xy[0, k], -(xy[0, j] + xy[0, k]) / 2)
for q in quads:
	for e in range(4):
		p0, p1 = q[e], q[(e + 1) % 4]
		mid = (p0 + p1) / 2
		if np.isclose(abs(mid[0]), half_width) and np.isclose(p0[0], p1[0]):
			side(p0, p1, np.array([np.sign(mid[0]), 0]))

fig = plt.figure(figsize=(4.2, 6.4), dpi=100)
# dark blue-grey gradient background, as the viewport of the postprocessor
bg = fig.add_axes([0, 0, 1, 1])
gradient = np.linspace(0, 1, 256)[:, None]
bg.imshow(gradient, aspect="auto", cmap=matplotlib.colors.LinearSegmentedColormap.from_list("bg", ["#3a4a66", "#0d1220"]), extent=[0, 1, 0, 1])
bg.axis("off")

ax = fig.add_axes([-0.47, -0.10, 1.76, 1.33], projection="3d", computed_zorder=False)
ax.set_facecolor((0, 0, 0, 0))
collection = Poly3DCollection(faces, facecolors=colors, edgecolors=(0.05, 0.05, 0.08, 0.55), linewidths=0.35)
ax.add_collection3d(collection)
ax.set_xlim(-1.6, 1.6)
ax.set_ylim(-1.6, 1.6)
ax.set_zlim(-1.6, 1.6)
ax.set_box_aspect((1, 1, 1))
ax.view_init(elev=50, azim=-100, roll=0)
ax.set_proj_type("persp", focal_length=0.45)
ax.axis("off")

# color scale
cax = fig.add_axes([0.875, 0.035, 0.03, 0.2])
bar = fig.colorbar(cm.ScalarMappable(norm=norm, cmap=cmap), cax=cax)
bar.outline.set_edgecolor("white")
bar.outline.set_linewidth(0.6)
bar.set_ticks([0, 1, 2, 3])
bar.ax.tick_params(colors="white", labelsize=8, length=2, width=0.6)
cax.set_title(r"$\sigma_{vM}$", color="white", fontsize=9, pad=4)

fig.savefig(output, dpi=100)
print(f"written {output}")
