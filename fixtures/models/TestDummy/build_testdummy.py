"""Builds TestDummy, the CC0 humanoid used as an OpenPerch test fixture, and exports it as FBX.

Run with Blender 4.x from the repository root:

    blender --background --factory-startup \
        --python fixtures/models/TestDummy/build_testdummy.py -- [--output-dir DIR] [--no-blend]

Everything is generated from the numbers in this file; no external asset is read.
Coordinates are Blender's: Z up, the character faces -Y and its left side is +X.
The geometry is built from plain tuples rather than mathutils so that it can be checked without Blender.
"""

import argparse
import math
import os
import sys

import bpy

MODEL_NAME = "TestDummy"
ARMATURE_NAME = "Armature"

MATERIALS = {
    "TestDummy_Skin": (1.0, 0.86, 0.78),
    "TestDummy_Cloth": (0.32, 0.43, 0.76),
    "TestDummy_Hair": (0.42, 0.28, 0.21),
    "TestDummy_Eye": (0.97, 0.97, 0.97),
    "TestDummy_Detail": (0.12, 0.10, 0.12),
    "TestDummy_Accessory": (0.86, 0.25, 0.35),
}

HEAD_CENTER = (0.0, 0.0, 1.40)
HEAD_RADII = (0.10, 0.105, 0.12)
EYE_RADIUS = 0.020
EYE_DEPTH = 0.012
EYELID_RADIUS = 0.0225
EYELID_OPEN_ANGLE = math.radians(62.0)
EYE_HEIGHT = 1.39
EYE_X = 0.035
MOUTH_Z = 1.335
MOUTH_HALF_WIDTH = 0.018
BROW_Z = 1.425
BROW_INNER_X = 0.018
BROW_OUTER_X = 0.058


def v_add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def v_sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def v_scale(a, s):
    return (a[0] * s, a[1] * s, a[2] * s)


def v_dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def v_cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def v_length(a):
    return math.sqrt(v_dot(a, a))


def v_normalize(a):
    length = v_length(a)
    if length < 1e-12:
        raise ValueError("cannot normalize a zero-length vector")
    return v_scale(a, 1.0 / length)


def v_lerp(a, b, t):
    return v_add(a, v_scale(v_sub(b, a), t))


def mirror_x(co):
    return (-co[0], co[1], co[2])


def head_surface_y(x, z, inset=0.0):
    """Front (-Y) surface of the head ellipsoid at (x, z), moved outwards by -inset."""
    rx, ry, rz = HEAD_RADII
    k = 1.0 - (x / rx) ** 2 - ((z - HEAD_CENTER[2]) / rz) ** 2
    return HEAD_CENTER[1] - ry * math.sqrt(max(k, 0.0)) - inset


def rotate_about_x(co, center, angle):
    """Rotates co about the X axis through center. Positive angles move +Z towards -Y (the face)."""
    dy = co[1] - center[1]
    dz = co[2] - center[2]
    c, s = math.cos(angle), math.sin(angle)
    return (co[0], center[1] + dy * c - dz * s, center[2] + dy * s + dz * c)


def humanoid_bones():
    """(name, head, tail, parent) in T-pose. Names match Unity's HumanBodyBones so Humanoid mapping is automatic."""
    bones = [
        ("Hips", (0.0, 0.0, 0.80), (0.0, 0.0, 0.90), None),
        ("Spine", (0.0, 0.0, 0.90), (0.0, 0.0, 1.02), "Hips"),
        ("Chest", (0.0, 0.0, 1.02), (0.0, 0.0, 1.13), "Spine"),
        ("UpperChest", (0.0, 0.0, 1.13), (0.0, 0.0, 1.25), "Chest"),
        ("Neck", (0.0, 0.0, 1.25), (0.0, 0.0, 1.31), "UpperChest"),
        ("Head", (0.0, 0.0, 1.31), (0.0, 0.0, 1.50), "Neck"),
    ]
    eye_y = head_surface_y(EYE_X, EYE_HEIGHT) + EYE_DEPTH
    left = [
        ("LeftEye", (EYE_X, eye_y, EYE_HEIGHT), (EYE_X, eye_y - 0.03, EYE_HEIGHT), "Head"),
        ("LeftShoulder", (0.02, 0.0, 1.21), (0.10, 0.0, 1.21), "UpperChest"),
        ("LeftUpperArm", (0.10, 0.0, 1.21), (0.33, 0.0, 1.21), "LeftShoulder"),
        ("LeftLowerArm", (0.33, 0.0, 1.21), (0.555, 0.0, 1.21), "LeftUpperArm"),
        ("LeftHand", (0.555, 0.0, 1.21), (0.645, 0.0, 1.21), "LeftLowerArm"),
        ("LeftUpperLeg", (0.08, 0.0, 0.78), (0.08, 0.0, 0.44), "Hips"),
        ("LeftLowerLeg", (0.08, 0.0, 0.44), (0.08, 0.0, 0.085), "LeftUpperLeg"),
        ("LeftFoot", (0.08, 0.0, 0.085), (0.08, -0.10, 0.025), "LeftLowerLeg"),
        ("LeftToes", (0.08, -0.10, 0.025), (0.08, -0.15, 0.025), "LeftFoot"),
    ]
    for name, head, tail, parent in left:
        bones.append((name, head, tail, parent))
        bones.append((
            name.replace("Left", "Right", 1),
            mirror_x(head),
            mirror_x(tail),
            parent.replace("Left", "Right", 1) if parent.startswith("Left") else parent,
        ))
    return bones


def chain(prefix, joints, parent):
    """Bones prefix_0..prefix_(n-1) along joints, each the child of the previous one."""
    bones = []
    for k in range(len(joints) - 1):
        bones.append(("%s_%d" % (prefix, k), joints[k], joints[k + 1], parent if k == 0 else "%s_%d" % (prefix, k - 1)))
    return bones


HAIR_SIDE_L_JOINTS = [(0.105, -0.045, 1.43), (0.118, -0.050, 1.36), (0.122, -0.050, 1.30), (0.120, -0.045, 1.25)]
HAIR_SIDE_R_JOINTS = [mirror_x(p) for p in HAIR_SIDE_L_JOINTS]
HAIR_BACK_JOINTS = [(0.0, 0.11, 1.45), (0.0, 0.15, 1.34), (0.0, 0.165, 1.22), (0.0, 0.155, 1.10), (0.0, 0.135, 0.99)]
TAIL_JOINTS = [(0.0, 0.085, 0.80), (0.0, 0.17, 0.765), (0.0, 0.26, 0.75), (0.0, 0.34, 0.78), (0.0, 0.40, 0.85)]

HAT_BASE = (0.03, 0.0, 1.51)
HAT_HEIGHT = 0.085

SKIRT_TOP_Z = 0.80
SKIRT_BOTTOM_Z = 0.54
# Ordered by angle from +X towards +Y (0, 90, 180, 270 degrees); skirt_weights indexes this list by quadrant.
SKIRT_CHAINS = ["Skirt_Side_L", "Skirt_Back", "Skirt_Side_R", "Skirt_Front"]


def skirt_radii(h):
    """Elliptic skirt cross-section at h (0 = waist, 1 = hem)."""
    return 0.145 + 0.125 * h, 0.10 + 0.14 * h


def skirt_point(h, phi):
    rx, ry = skirt_radii(h)
    return (rx * math.cos(phi), ry * math.sin(phi), SKIRT_TOP_Z + (SKIRT_BOTTOM_Z - SKIRT_TOP_Z) * h)


def all_bones():
    bones = humanoid_bones()
    bones += chain("Hair_Side_L", HAIR_SIDE_L_JOINTS, "Head")
    bones += chain("Hair_Side_R", HAIR_SIDE_R_JOINTS, "Head")
    bones += chain("Hair_Back", HAIR_BACK_JOINTS, "Head")
    bones.append(("Skirt_Root", (0.0, 0.0, SKIRT_TOP_Z), (0.0, 0.0, SKIRT_TOP_Z - 0.06), "Hips"))
    for index, name in enumerate(SKIRT_CHAINS):
        phi = index * math.pi / 2.0
        joints = [skirt_point(h, phi) for h in (0.0, 0.5, 1.0)]
        bones += chain(name, joints, "Skirt_Root")
    bones += chain("Tail", TAIL_JOINTS, "Hips")
    # Accessory_Hat becomes a SkinnedMeshRenderer, whose vertices follow its bones only, so scaling the hat needs a
    # bone of its own. Rooting it at the hat's base keeps a scaled hat sitting on the head.
    bones.append(("Hat", HAT_BASE, v_add(HAT_BASE, (0.0, 0.0, HAT_HEIGHT)), "Head"))
    # Not parented to the body on purpose: the charm is meant to follow a body bone through a Parent Constraint.
    bones.append(("Charm", (-0.17, -0.03, 0.84), (-0.17, -0.03, 0.80), None))
    return bones


class MeshBuilder:
    """Collects vertices, faces, per-corner UVs, materials, bone weights and shape keys for one mesh object."""

    def __init__(self, name):
        self.name = name
        self.verts = []
        self.weights = []
        self.faces = []
        self.face_uvs = []
        self.face_materials = []
        self.face_smooth = []
        self.groups = {}
        self.shape_keys = []

    def triangle_count(self):
        return sum(len(face) - 2 for face in self.faces)

    def material_names(self):
        names = []
        for name in self.face_materials:
            if name not in names:
                names.append(name)
        return names

    def add_vertex(self, co, weights, group=None):
        weights = weights(co) if callable(weights) else weights
        self.verts.append(tuple(co))
        self.weights.append(dict(weights))
        index = len(self.verts) - 1
        if group:
            self.groups.setdefault(group, []).append(index)
        return index

    def add_face(self, indices, uvs, material, smooth):
        self.faces.append(tuple(indices))
        self.face_uvs.append(tuple(uvs))
        self.face_materials.append(material)
        self.face_smooth.append(smooth)

    def add_ellipsoid(self, center, radii, segments, rings, material, weights,
                      theta_range=(0.0, math.pi), group=None, smooth=True):
        """Ellipsoid (or a polar band of it). Rings run from +Z downwards; theta is measured from +Z."""
        cx, cy, cz = center
        rx, ry, rz = radii
        t0, t1 = theta_range
        rows = []
        for i in range(rings + 1):
            theta = t0 + (t1 - t0) * i / rings
            st, ct = math.sin(theta), math.cos(theta)
            if st < 1e-9:
                rows.append([self.add_vertex((cx, cy, cz + rz * ct), weights, group)])
                continue
            row = []
            for j in range(segments):
                phi = 2.0 * math.pi * j / segments
                co = (cx + rx * st * math.cos(phi), cy + ry * st * math.sin(phi), cz + rz * ct)
                row.append(self.add_vertex(co, weights, group))
            rows.append(row)
        for i in range(rings):
            upper, lower = rows[i], rows[i + 1]
            v0 = 1.0 - i / rings
            v1 = 1.0 - (i + 1) / rings
            for j in range(segments):
                j1 = (j + 1) % segments
                u0 = j / segments
                u1 = (j + 1) / segments
                if len(upper) == 1:
                    self.add_face((upper[0], lower[j], lower[j1]),
                                  ((0.5 * (u0 + u1), v0), (u0, v1), (u1, v1)), material, smooth)
                elif len(lower) == 1:
                    self.add_face((upper[j], lower[0], upper[j1]),
                                  ((u0, v0), (0.5 * (u0 + u1), v1), (u1, v0)), material, smooth)
                else:
                    self.add_face((upper[j], lower[j], lower[j1], upper[j1]),
                                  ((u0, v0), (u0, v1), (u1, v1), (u1, v0)), material, smooth)
        return rows

    def add_tube(self, points, radii, segments, material, ring_weights, cap_start=True, cap_end=True,
                 section=(1.0, 1.0), normal_hint=(0.0, 0.0, 1.0), group=None, smooth=True):
        """Tube along a polyline. section scales the cross-section along the ring normal and binormal."""
        count = len(points)
        tangents = []
        for i in range(count):
            if i == 0:
                d = v_sub(points[1], points[0])
            elif i == count - 1:
                d = v_sub(points[-1], points[-2])
            else:
                d = v_add(v_normalize(v_sub(points[i], points[i - 1])), v_normalize(v_sub(points[i + 1], points[i])))
            tangents.append(v_normalize(d))

        normal = v_sub(normal_hint, v_scale(tangents[0], v_dot(normal_hint, tangents[0])))
        if v_length(normal) < 1e-6:
            fallback = (1.0, 0.0, 0.0)
            normal = v_sub(fallback, v_scale(tangents[0], v_dot(fallback, tangents[0])))
        normal = v_normalize(normal)

        rows = []
        for i in range(count):
            tangent = tangents[i]
            # Carry the previous normal along the curve so the rings do not twist.
            normal = v_normalize(v_sub(normal, v_scale(tangent, v_dot(normal, tangent))))
            binormal = v_cross(tangent, normal)
            row = []
            for j in range(segments):
                phi = 2.0 * math.pi * j / segments
                offset = v_add(v_scale(normal, radii[i] * section[0] * math.cos(phi)),
                               v_scale(binormal, radii[i] * section[1] * math.sin(phi)))
                row.append(self.add_vertex(v_add(points[i], offset), ring_weights[i], group))
            rows.append(row)

        for i in range(count - 1):
            lo, hi = rows[i], rows[i + 1]
            v0 = i / (count - 1)
            v1 = (i + 1) / (count - 1)
            for j in range(segments):
                j1 = (j + 1) % segments
                u0 = j / segments
                u1 = (j + 1) / segments
                self.add_face((lo[j], lo[j1], hi[j1], hi[j]),
                              ((u0, v0), (u1, v0), (u1, v1), (u0, v1)), material, smooth)

        def cap_uv(j):
            phi = 2.0 * math.pi * j / segments
            return (0.5 + 0.5 * math.cos(phi), 0.5 + 0.5 * math.sin(phi))

        if cap_start:
            center = self.add_vertex(points[0], ring_weights[0], group)
            for j in range(segments):
                j1 = (j + 1) % segments
                self.add_face((center, rows[0][j1], rows[0][j]),
                              ((0.5, 0.5), cap_uv(j1), cap_uv(j)), material, smooth)
        if cap_end:
            center = self.add_vertex(points[-1], ring_weights[-1], group)
            for j in range(segments):
                j1 = (j + 1) % segments
                self.add_face((center, rows[-1][j], rows[-1][j1]),
                              ((0.5, 0.5), cap_uv(j), cap_uv(j1)), material, smooth)
        return rows


def single(bone):
    return {bone: 1.0}


def chain_rings(joints, radii, prefix, steps):
    """Subdivides a bone chain into rings. Rings on an inner joint are shared half and half by the two bones."""
    bone_count = len(joints) - 1
    points, ring_radii, weights = [], [], []
    for k in range(bone_count):
        for s in range(steps):
            f = s / steps
            points.append(v_lerp(joints[k], joints[k + 1], f))
            ring_radii.append(radii[k] + (radii[k + 1] - radii[k]) * f)
            if s == 0 and k > 0:
                weights.append({"%s_%d" % (prefix, k - 1): 0.5, "%s_%d" % (prefix, k): 0.5})
            else:
                weights.append(single("%s_%d" % (prefix, k)))
    points.append(joints[-1])
    ring_radii.append(radii[-1])
    weights.append(single("%s_%d" % (prefix, bone_count - 1)))
    return points, ring_radii, weights


TORSO_JOINTS = [("Hips", 0.90), ("Spine", 1.02), ("Chest", 1.13), ("UpperChest", 1.25), ("Neck", None)]


def torso_weights(co, blend=0.03):
    z = co[2]
    for index, (bone, top) in enumerate(TORSO_JOINTS):
        if top is None or z < top - blend:
            return single(bone)
        if z <= top + blend:
            t = (z - (top - blend)) / (2.0 * blend)
            upper = TORSO_JOINTS[index + 1][0]
            return {bone: 1.0 - t, upper: t}
    return single("Neck")


def build_body():
    body = MeshBuilder("Body")
    skin, cloth = "TestDummy_Skin", "TestDummy_Cloth"

    body.add_ellipsoid(HEAD_CENTER, HEAD_RADII, 48, 32, skin, single("Head"))
    body.add_tube([(0.0, 0.0, 1.24), (0.0, 0.0, 1.275), (0.0, 0.0, 1.31)], [0.036, 0.034, 0.034], 20, skin,
                  [single("Neck")] * 3, cap_start=False, cap_end=False)

    body.add_ellipsoid((0.0, 0.0, 1.14), (0.13, 0.085, 0.13), 40, 20, cloth, torso_weights)
    body.add_ellipsoid((0.0, 0.0, 0.97), (0.11, 0.075, 0.11), 40, 20, cloth, torso_weights)
    body.add_ellipsoid((0.0, 0.0, 0.83), (0.13, 0.085, 0.09), 40, 20, cloth, torso_weights)

    for side, mirror in (("Left", False), ("Right", True)):
        def place(co, mirror=mirror):
            return mirror_x(co) if mirror else co

        upper_arm, lower_arm, hand = side + "UpperArm", side + "LowerArm", side + "Hand"
        body.add_ellipsoid(place((0.105, 0.0, 1.21)), (0.044, 0.044, 0.044), 20, 10, skin, single(upper_arm))
        body.add_tube([place((0.11, 0.0, 1.21)), place((0.22, 0.0, 1.21)), place((0.33, 0.0, 1.21))],
                      [0.038, 0.035, 0.032], 20, skin, [single(upper_arm)] * 3,
                      cap_start=False, cap_end=False, normal_hint=(0.0, 0.0, 1.0))
        body.add_ellipsoid(place((0.33, 0.0, 1.21)), (0.033, 0.033, 0.033), 20, 10, skin, single(lower_arm))
        body.add_tube([place((0.33, 0.0, 1.21)), place((0.44, 0.0, 1.21)), place((0.555, 0.0, 1.21))],
                      [0.031, 0.028, 0.025], 20, skin, [single(lower_arm)] * 3,
                      cap_start=False, cap_end=False, normal_hint=(0.0, 0.0, 1.0))
        body.add_ellipsoid(place((0.60, 0.0, 1.21)), (0.05, 0.02, 0.036), 20, 10, skin, single(hand))

        upper_leg, lower_leg, foot, toes = side + "UpperLeg", side + "LowerLeg", side + "Foot", side + "Toes"
        body.add_ellipsoid(place((0.08, 0.0, 0.78)), (0.066, 0.066, 0.066), 20, 10, cloth, single(upper_leg))
        body.add_tube([place((0.08, 0.0, 0.78)), place((0.08, 0.0, 0.61)), place((0.08, 0.0, 0.44))],
                      [0.064, 0.056, 0.048], 20, skin, [single(upper_leg)] * 3,
                      cap_start=False, cap_end=False, normal_hint=(1.0, 0.0, 0.0))
        body.add_ellipsoid(place((0.08, 0.0, 0.44)), (0.049, 0.049, 0.049), 20, 10, skin, single(lower_leg))
        body.add_tube([place((0.08, 0.0, 0.44)), place((0.08, 0.0, 0.26)), place((0.08, 0.0, 0.085))],
                      [0.046, 0.042, 0.035], 20, skin, [single(lower_leg)] * 3,
                      cap_start=False, cap_end=False, normal_hint=(1.0, 0.0, 0.0))
        body.add_ellipsoid(place((0.08, 0.0, 0.085)), (0.036, 0.036, 0.036), 20, 10, skin, single(foot))

        def foot_weights(co, foot=foot, toes=toes):
            return single(toes) if co[1] < -0.10 else single(foot)

        body.add_ellipsoid(place((0.08, -0.04, 0.035)), (0.04, 0.095, 0.035), 24, 12, cloth, foot_weights)

    add_face_features(body)
    return body


def add_face_features(body):
    eye_mat, detail, skin = "TestDummy_Eye", "TestDummy_Detail", "TestDummy_Skin"
    eye_y = head_surface_y(EYE_X, EYE_HEIGHT) + EYE_DEPTH
    for side, x in (("Left", EYE_X), ("Right", -EYE_X)):
        center = (x, eye_y, EYE_HEIGHT)
        bone = side + "Eye"
        body.add_ellipsoid(center, (EYE_RADIUS,) * 3, 20, 12, eye_mat, single(bone))
        pupil = (x, eye_y - 0.0185, EYE_HEIGHT)
        body.add_ellipsoid(pupil, (0.008, 0.003, 0.008), 16, 8, detail, single(bone))
        body.add_ellipsoid(center, (EYELID_RADIUS,) * 3, 20, 8, skin, single("Head"),
                           theta_range=(0.0, EYELID_OPEN_ANGLE), group="eyelid_" + side)

        sign = 1.0 if x > 0 else -1.0
        brow_points = []
        for k in range(6):
            bx = sign * (BROW_INNER_X + (BROW_OUTER_X - BROW_INNER_X) * k / 5.0)
            bz = BROW_Z + 0.004 * k / 5.0
            brow_points.append((bx, head_surface_y(bx, bz, inset=0.002), bz))
        body.add_tube(brow_points, [0.0028] * 6, 6, detail, [single("Head")] * 6,
                      section=(1.0, 0.6), normal_hint=(0.0, 0.0, 1.0), group="brow_" + side)

    mouth_points = []
    for k in range(9):
        mx = -MOUTH_HALF_WIDTH + 2.0 * MOUTH_HALF_WIDTH * k / 8.0
        mouth_points.append((mx, head_surface_y(mx, MOUTH_Z, inset=0.001), MOUTH_Z))
    body.add_tube(mouth_points, [0.0025] * 9, 6, detail, [single("Head")] * 9,
                  section=(1.0, 0.6), normal_hint=(0.0, 0.0, 1.0), group="mouth")

    add_shape_keys(body, eye_y)


def add_shape_keys(body, eye_y):
    eye_centers = {"Left": (EYE_X, eye_y, EYE_HEIGHT), "Right": (-EYE_X, eye_y, EYE_HEIGHT)}

    def eyelids(angle_deg):
        moved = {}
        for side, center in eye_centers.items():
            for index in body.groups["eyelid_" + side]:
                moved[index] = rotate_about_x(body.verts[index], center, math.radians(angle_deg))
        return moved

    def mouth(offset_fn):
        moved = {}
        for index in body.groups["mouth"]:
            x, y, z = body.verts[index]
            moved[index] = (x, y, z + offset_fn(x / MOUTH_HALF_WIDTH, z - MOUTH_Z))
        return moved

    def brows(offset_fn):
        moved = {}
        for side in ("Left", "Right"):
            for index in body.groups["brow_" + side]:
                x, y, z = body.verts[index]
                t = (abs(x) - BROW_INNER_X) / (BROW_OUTER_X - BROW_INNER_X)
                moved[index] = (x, y, z + offset_fn(min(max(t, 0.0), 1.0)))
        return moved

    def merged(*parts):
        result = {}
        for part in parts:
            result.update(part)
        return result

    body.shape_keys = [
        ("Blink", eyelids(90.0)),
        ("LookUp", eyelids(-12.0)),
        ("LookDown", eyelids(25.0)),
        ("Smile", mouth(lambda u, dz: 0.005 * u * u - 0.0015)),
        ("Angry", merged(brows(lambda t: -0.007 * (1.0 - t) + 0.002 * t),
                         mouth(lambda u, dz: -0.004 * u * u))),
        ("Surprised", merged(brows(lambda t: 0.007),
                             mouth(lambda u, dz: (0.003 if dz > 0 else -0.006) * (1.0 - u * u)),
                             eyelids(-12.0))),
    ]


def build_hair():
    hair = MeshBuilder("Hair")
    material = "TestDummy_Hair"
    hx, hy, hz = HEAD_RADII
    hair.add_ellipsoid(HEAD_CENTER, (hx + 0.01, hy + 0.01, hz + 0.01), 48, 16, material, single("Head"),
                       theta_range=(0.0, 0.40 * math.pi))
    for prefix, joints in (("Hair_Side_L", HAIR_SIDE_L_JOINTS), ("Hair_Side_R", HAIR_SIDE_R_JOINTS)):
        points, radii, weights = chain_rings(joints, [0.022, 0.018, 0.013, 0.006], prefix, 3)
        hair.add_tube(points, radii, 12, material, weights, normal_hint=(0.0, -1.0, 0.0))
    points, radii, weights = chain_rings(HAIR_BACK_JOINTS, [0.032, 0.028, 0.022, 0.015, 0.006], "Hair_Back", 3)
    hair.add_tube(points, radii, 12, material, weights, normal_hint=(1.0, 0.0, 0.0))
    return hair


def build_skirt():
    skirt = MeshBuilder("Skirt")
    material = "TestDummy_Cloth"
    segments, rings = 48, 8
    rows = []
    for i in range(rings + 1):
        row = []
        for j in range(segments):
            phi = 2.0 * math.pi * j / segments
            row.append(skirt.add_vertex(skirt_point(i / rings, phi), skirt_weights(i, rings, phi)))
        rows.append(row)
    # Same winding as add_ellipsoid (rings run downwards, phi turns towards +Y), which keeps the faces outward.
    for i in range(rings):
        upper, lower = rows[i], rows[i + 1]
        v0 = 1.0 - i / rings
        v1 = 1.0 - (i + 1) / rings
        for j in range(segments):
            j1 = (j + 1) % segments
            u0 = j / segments
            u1 = (j + 1) / segments
            skirt.add_face((upper[j], lower[j], lower[j1], upper[j1]),
                           ((u0, v0), (u0, v1), (u1, v1), (u1, v0)), material, True)
    return skirt


def skirt_weights(ring, rings, phi):
    if ring == 0:
        return single("Skirt_Root")
    sector = phi / (math.pi / 2.0)
    first = int(math.floor(sector)) % 4
    f = sector - math.floor(sector)
    joint = rings // 2
    if ring < joint:
        along = {0: 1.0}
    elif ring == joint:
        along = {0: 0.5, 1: 0.5}
    else:
        along = {1: 1.0}
    weights = {}
    for chain_index, around in ((first, 1.0 - f), ((first + 1) % 4, f)):
        for segment, share in along.items():
            w = around * share
            if w > 1e-6:
                name = "%s_%d" % (SKIRT_CHAINS[chain_index], segment)
                weights[name] = weights.get(name, 0.0) + w
    return weights


def build_tail():
    tail = MeshBuilder("Tail")
    points, radii, weights = chain_rings(TAIL_JOINTS, [0.036, 0.032, 0.026, 0.018, 0.008], "Tail", 4)
    tail.add_tube(points, radii, 16, "TestDummy_Hair", weights, normal_hint=(1.0, 0.0, 0.0))
    return tail


def build_hat():
    hat = MeshBuilder("Accessory_Hat")
    material = "TestDummy_Accessory"
    base = HAT_BASE
    hat.add_tube([base, v_add(base, (0.0, 0.0, 0.008))], [0.075, 0.075], 32, material, [single("Hat")] * 2,
                 normal_hint=(1.0, 0.0, 0.0))
    hat.add_tube([v_add(base, (0.0, 0.0, 0.006)), v_add(base, (0.0, 0.0, 0.05)), v_add(base, (0.0, 0.0, HAT_HEIGHT))],
                 [0.046, 0.044, 0.042], 32, material, [single("Hat")] * 3, normal_hint=(1.0, 0.0, 0.0))
    return hat


def build_charm():
    charm = MeshBuilder("Accessory_Charm")
    material = "TestDummy_Accessory"
    charm.add_tube([(-0.17, -0.03, 0.84), (-0.17, -0.03, 0.815)], [0.003, 0.003], 6, material, [single("Charm")] * 2)
    charm.add_ellipsoid((-0.17, -0.03, 0.795), (0.02, 0.012, 0.024), 16, 10, material, single("Charm"))
    return charm


def build_meshes():
    return [build_body(), build_hair(), build_skirt(), build_tail(), build_hat(), build_charm()]


def create_materials():
    materials = {}
    for name, (r, g, b) in MATERIALS.items():
        material = bpy.data.materials.new(name)
        material.diffuse_color = (r, g, b, 1.0)
        material.use_nodes = True
        bsdf = material.node_tree.nodes.get("Principled BSDF")
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = (r, g, b, 1.0)
        materials[name] = material
    return materials


def create_armature(bones):
    data = bpy.data.armatures.new(ARMATURE_NAME)
    armature = bpy.data.objects.new(ARMATURE_NAME, data)
    bpy.context.scene.collection.objects.link(armature)
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    edit_bones = {}
    for name, head, tail, parent in bones:
        bone = data.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.roll = 0.0
        if parent is not None:
            bone.parent = edit_bones[parent]
            bone.use_connect = False
        edit_bones[name] = bone
    bpy.ops.object.mode_set(mode="OBJECT")
    return armature


def create_mesh_object(builder, materials, armature):
    mesh = bpy.data.meshes.new(builder.name)
    mesh.from_pydata(builder.verts, [], builder.faces)

    slots = builder.material_names()
    for name in slots:
        mesh.materials.append(materials[name])
    mesh.polygons.foreach_set("material_index", [slots.index(name) for name in builder.face_materials])
    mesh.polygons.foreach_set("use_smooth", builder.face_smooth)

    uv_layer = mesh.uv_layers.new(name="UVMap")
    for polygon in mesh.polygons:
        for corner, loop_index in enumerate(polygon.loop_indices):
            uv_layer.data[loop_index].uv = builder.face_uvs[polygon.index][corner]

    mesh.validate()
    mesh.update()

    obj = bpy.data.objects.new(builder.name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.parent = armature
    modifier = obj.modifiers.new("Armature", "ARMATURE")
    modifier.object = armature

    by_weight = {}
    for index, weights in enumerate(builder.weights):
        for bone, weight in weights.items():
            by_weight.setdefault((bone, round(weight, 6)), []).append(index)
    groups = {}
    for (bone, weight), indices in sorted(by_weight.items()):
        if bone not in groups:
            groups[bone] = obj.vertex_groups.new(name=bone)
        groups[bone].add(indices, weight, "REPLACE")

    if builder.shape_keys:
        obj.shape_key_add(name="Basis", from_mix=False)
        for name, moved in builder.shape_keys:
            key = obj.shape_key_add(name=name, from_mix=False)
            for index, co in moved.items():
                key.data[index].co = co
    return obj


def export_fbx(path):
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=False,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        use_space_transform=True,
        bake_space_transform=False,
        # Modifiers are not applied so that the shape keys are exported; the armature becomes skinning anyway.
        use_mesh_modifiers=False,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        use_armature_deform_only=True,
        armature_nodetype="NULL",
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )


def parse_args(argv):
    args = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser(prog="build_testdummy.py")
    parser.add_argument("--output-dir", default=os.path.dirname(os.path.abspath(__file__)),
                        help="directory for TestDummy.fbx and TestDummy.blend (default: next to this script)")
    parser.add_argument("--no-blend", action="store_true", help="do not save TestDummy.blend")
    return parser.parse_args(args)


def main():
    options = parse_args(sys.argv)
    os.makedirs(options.output_dir, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    materials = create_materials()
    armature = create_armature(all_bones())
    builders = build_meshes()
    for builder in builders:
        create_mesh_object(builder, materials, armature)

    fbx_path = os.path.join(options.output_dir, MODEL_NAME + ".fbx")
    export_fbx(fbx_path)
    if not options.no_blend:
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(options.output_dir, MODEL_NAME + ".blend"))

    total = sum(builder.triangle_count() for builder in builders)
    for builder in builders:
        print("%-16s %6d triangles" % (builder.name, builder.triangle_count()))
    print("%-16s %6d triangles, %d bones -> %s" % ("TestDummy", total, len(all_bones()), fbx_path))


if __name__ == "__main__":
    main()
