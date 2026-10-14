"""
Shared primitive toolkit for the generated tile models.

Every model script begins with `begin(MATERIALS)`, calls the primitives below to
pile up faces, and ends with `finish(name)` / `export(...)`. Geometry is built as
loose quads and triangles with duplicated vertices: the models are flat shaded,
so there is nothing to gain from welding, and duplication keeps every face's
winding independent.

Imported by build_coalmine.py and build_coalplant.py. Run those with __file__
defined so they can find this module:

    p = r"...\\ArtSource\\build_coalplant.py"
    exec(compile(open(p).read(), p, "exec"), {"__file__": p})
"""
import bpy, math
from mathutils import Vector

_verts, _faces, _mats = [], [], []
_materials = []


def begin(materials):
    """Starts a new model. `materials` is an ordered [(name, (r,g,b)), ...] --
    the order is a contract with the matching Unity *ModelBuilder's palette."""
    global _verts, _faces, _mats, _materials
    _verts, _faces, _mats = [], [], []
    _materials = list(materials)


def V(x, y, z):
    return Vector((x, y, z))


def quad(a, b, c, d, m):
    i = len(_verts)
    _verts.extend([a, b, c, d])
    _faces.append((i, i + 1, i + 2, i + 3))
    _mats.append(m)


def tri(a, b, c, m):
    i = len(_verts)
    _verts.extend([a, b, c])
    _faces.append((i, i + 1, i + 2))
    _mats.append(m)


def box_axes(base, X, Y, Z, hb, ht, height, m, cap_bottom=True, cap_top=True):
    """Frustum: rectangular section hb at `base`, ht at base + Z*height."""
    b, t = base, base + Z * height
    b00 = b - X * hb[0] - Y * hb[1]; b10 = b + X * hb[0] - Y * hb[1]
    b11 = b + X * hb[0] + Y * hb[1]; b01 = b - X * hb[0] + Y * hb[1]
    t00 = t - X * ht[0] - Y * ht[1]; t10 = t + X * ht[0] - Y * ht[1]
    t11 = t + X * ht[0] + Y * ht[1]; t01 = t - X * ht[0] + Y * ht[1]
    quad(b00, b10, t10, t00, m)
    quad(b10, b11, t11, t10, m)
    quad(b11, b01, t01, t11, m)
    quad(b01, b00, t00, t01, m)
    if cap_top:    quad(t00, t10, t11, t01, m)
    if cap_bottom: quad(b01, b11, b10, b00, m)


def box(cx, cy, z0, hx, hy, height, m, rot=0.0):
    c, s = math.cos(rot), math.sin(rot)
    box_axes(V(cx, cy, z0), V(c, s, 0), V(-s, c, 0), V(0, 0, 1),
             (hx, hy), (hx, hy), height, m)


def taper(cx, cy, z0, hb, ht, height, m, rot=0.0):
    c, s = math.cos(rot), math.sin(rot)
    box_axes(V(cx, cy, z0), V(c, s, 0), V(-s, c, 0), V(0, 0, 1),
             hb, ht, height, m)


def beam(p0, p1, w, h, m):
    """Square-section timber/steel running between two arbitrary points.

    Picks its own perpendicular, so the cross-section's roll is arbitrary. Fine
    for struts and cables; for anything whose orientation reads (ladder rungs,
    handrails) drive box_axes directly with an explicit frame.
    """
    p0, p1 = V(*p0), V(*p1)
    d = p1 - p0
    length = d.length
    Z = d / length
    ref = V(0, 0, 1) if abs(Z.z) < 0.9 else V(1, 0, 0)
    X = ref.cross(Z).normalized()
    Y = Z.cross(X)
    box_axes(p0, X, Y, Z, (w, h), (w, h), length, m)


def cylinder(center, axis, radius, half_len, m, seg=8):
    c = V(*center)
    A = V(*axis).normalized()
    ref = V(0, 0, 1) if abs(A.z) < 0.9 else V(1, 0, 0)
    U = ref.cross(A).normalized()
    W = A.cross(U)
    bc, tc = c - A * half_len, c + A * half_len
    ring = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        ring.append((U * math.cos(a) + W * math.sin(a)) * radius)
    for i in range(seg):
        r0, r1 = ring[i], ring[(i + 1) % seg]
        quad(bc + r0, bc + r1, tc + r1, tc + r0, m)
        tri(tc, tc + r0, tc + r1, m)
        tri(bc, bc + r1, bc + r0, m)


def tube(cx, cy, z0, r_bottom, r_top, height, m, seg=8, cap_bottom=True, cap_top=True):
    """Upright round shaft, optionally tapered. Chimneys and flues."""
    c = V(cx, cy, z0)
    top = c + V(0, 0, height)
    ring_b, ring_t = [], []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        ca, sa = math.cos(a), math.sin(a)
        ring_b.append(V(ca * r_bottom, sa * r_bottom, 0))
        ring_t.append(V(ca * r_top, sa * r_top, 0))
    for i in range(seg):
        j = (i + 1) % seg
        quad(c + ring_b[i], c + ring_b[j], top + ring_t[j], top + ring_t[i], m)
        if cap_top:    tri(top, top + ring_t[i], top + ring_t[j], m)
        if cap_bottom: tri(c, c + ring_b[j], c + ring_b[i], m)


def cone(center_base, radius, height, m, seg=7):
    c = V(*center_base)
    apex = c + V(0, 0, height)
    ring = [V(math.cos(2 * math.pi * i / seg) * radius,
              math.sin(2 * math.pi * i / seg) * radius, 0) for i in range(seg)]
    for i in range(seg):
        r0, r1 = ring[i], ring[(i + 1) % seg]
        tri(c + r0, c + r1, apex, m)
        tri(c, c + r1, c + r0, m)


def panel(center, X, Y, hx, hy, m):
    """A single flat quad, normal along X cross Y.

    For anything that only ever reads as a face on a wall -- windows, doors,
    signage -- this is 2 triangles where a thin box is 12, and at tile scale the
    two are indistinguishable. Use a box when the detail needs to sit proud
    enough to catch its own shading, like a pilaster.
    """
    c = V(*center)
    quad(c - X * hx - Y * hy, c + X * hx - Y * hy,
         c + X * hx + Y * hy, c - X * hx + Y * hy, m)


def wall_panel(cx, y_wall, cz, hw, hh, m, face=-1):
    """Window or door on a wall whose outward normal runs along Y.

    face=-1 for a wall facing -Y, +1 for one facing +Y. Sits a hair proud of the
    wall so it never z-fights with it.
    """
    panel((cx, y_wall + 0.004 * face, cz), V(-face, 0, 0), V(0, 0, 1), hw, hh, m)


def tuft(cx, cy, height, rot, m):
    taper(cx, cy, 0.0, (0.022, 0.013), (0.005, 0.003), height, m, rot)


def finish(name):
    """Builds the Blender object. Replaces everything already in the file."""
    # Emit the faces grouped by material index. The FBX round-trip orders
    # submeshes by the material's FIRST USE in the face list, not by slot index,
    # so an unsorted mesh reaches Unity with its slots permuted -- silently, and
    # with the slot count still correct. Sorting makes the two orders the same.
    order = sorted(range(len(_faces)), key=lambda i: _mats[i])
    faces = [_faces[i] for i in order]
    mats = [_mats[i] for i in order]

    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)

    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in _verts], [], faces)

    for mat_name, rgb in _materials:
        mat = bpy.data.materials.get(mat_name) or bpy.data.materials.new(mat_name)
        mat.diffuse_color = (rgb[0], rgb[1], rgb[2], 1.0)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
            bsdf.inputs["Roughness"].default_value = 0.85
        mesh.materials.append(mat)

    for poly, m in zip(mesh.polygons, mats):
        poly.material_index = m
        poly.use_smooth = False

    mesh.update()
    mesh.validate()

    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)

    tris = sum(len(p.vertices) - 2 for p in mesh.polygons)
    print("[%s] %d tris, %d faces, %d material slots, dims %s" % (
        name, tris, len(mesh.polygons), len(mesh.materials),
        tuple(round(d, 3) for d in obj.dimensions)))

    seen = []
    for p in mesh.polygons:
        if p.material_index not in seen:
            seen.append(p.material_index)
    assert seen == list(range(len(_materials))), \
        "material first-use order %s is not slot order -- Unity will permute the palette" % seen

    return obj


def export(obj, blend_path, fbx_path):
    """Saves the .blend and writes the FBX with the axis settings Unity needs."""
    import os
    os.makedirs(os.path.dirname(fbx_path), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    bpy.ops.wm.save_as_mainfile(filepath=blend_path)
    bpy.ops.export_scene.fbx(
        filepath=fbx_path,
        use_selection=True,
        object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL',
        bake_space_transform=True,
        axis_forward='-Z',
        axis_up='Y',
        mesh_smooth_type='FACE',   # preserves the flat-shaded low-poly look
        add_leaf_bones=False,
    )
    print("exported ->", fbx_path)
