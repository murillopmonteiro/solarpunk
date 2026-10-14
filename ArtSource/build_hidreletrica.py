"""
Generates the Hidreletrica (hydroelectric) plant as a single flat-shaded
low-poly mesh, authored ~1 hex unit wide with its origin on the ground plane.

A small river station: a concrete dam holding back the head, a spillway sheeting
down its face into foam, and a red brick powerhouse on the far bank with its
tailrace arch. The timber landing stage and stone piers come from the watermill
reference; brick, slate, iron and glass are the power station's exact values.

This tile only ever sits on a Waterfall hex, which is already water-coloured, so
the model does NOT lay down a downstream water plane -- the hex is the river.
What it adds is the water the hex cannot show: the head standing higher than the
bank behind the dam, the sheet falling down the spillway, and foam streaks
running downstream so the river reads as moving rather than as a blue floor.

Run inside Blender (__file__ must be set so the script can find lowpoly.py):

    p = r"<repo>\\ArtSource\\build_hidreletrica.py"
    exec(compile(open(p).read(), p, "exec"), {"__file__": p})

Material slot order is a contract with Assets/Editor/HydroModelBuilder.cs, which
assigns the Unity materials by index. Do not reorder MATERIALS.
"""
import os, sys, importlib, math

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lowpoly
importlib.reload(lowpoly)          # pick up edits without restarting Blender
from lowpoly import *

BRICK, CONCRETE, ROOF, TIMBER, IRON, GLASS, WATER, FOAM, ROCK, GRASS, LAND = range(11)

MATERIALS = [
    ("Hydro_Brick",    (0.52, 0.30, 0.24)),
    ("Hydro_Concrete", (0.72, 0.71, 0.67)),
    ("Hydro_Roof",     (0.31, 0.34, 0.40)),
    ("Hydro_Timber",   (0.55, 0.42, 0.28)),
    ("Hydro_Iron",     (0.30, 0.30, 0.33)),
    ("Hydro_Glass",    (0.34, 0.47, 0.60)),
    ("Hydro_Water",    (0.26, 0.46, 0.58)),
    ("Hydro_Foam",     (0.86, 0.90, 0.91)),
    ("Hydro_Rock",     (0.46, 0.44, 0.42)),
    ("Hydro_Grass",    (0.28, 0.45, 0.22)),
    # Exactly HexCell's open-ground green, so the banks read as the same land as
    # the neighbouring tiles rather than as green props sitting on a blue floor.
    # Kept separate from Hydro_Grass, which stays darker so planting reads
    # against the bank it grows on.
    ("Hydro_Land",     (0.26, 0.55, 0.23)),
]

begin(MATERIALS)

DAM_Y = 0.10              # the dam runs across X at this line
CREST = 0.26              # top of the concrete
DAM_CX, DAM_HX = -0.215, 0.285

# ----------------------------------------------------------------- the banks
# Built first, because everything else stands on them. Without these the dam and
# the powerhouse sat straight on the water and read as floating: a dam is a thing
# wedged between two banks, and losing the banks loses the reason it is there.
#
# Each bank is one mass plus a couple of rotated chunks overlapping its edge. A
# single rectangle reads as a green card laid on the river; the overlaps break
# the straight edge into something shoreline-shaped. The slight inward taper
# gives the edge a batter, so it slopes into the water instead of standing as a
# cliff.
for bx, by, hx, hy, h, rot in (
    (-0.435, 0.060, 0.088, 0.345, 0.045, 0.0),      # left bank, the dam abuts it
    (-0.352, 0.300, 0.078, 0.098, 0.042, 0.30),
    (-0.366, -0.180, 0.070, 0.088, 0.040, -0.25),
    (0.280, -0.010, 0.222, 0.300, 0.045, 0.0),      # right bank, under the powerhouse
    (0.150, 0.262, 0.088, 0.082, 0.042, 0.40),
    (0.168, -0.312, 0.082, 0.076, 0.040, -0.35),
):
    taper(bx, by, 0.0, (hx, hy), (hx - 0.016, hy - 0.016), h, LAND, rot)

# ------------------------------------------------------------------- the dam
box(DAM_CX, DAM_Y, 0.0, DAM_HX, 0.055, CREST, CONCRETE)
box(DAM_CX, DAM_Y, CREST, DAM_HX + 0.012, 0.070, 0.024, CONCRETE)   # crest walkway

# Handrail along the downstream edge, where a walkway would actually need one.
RAIL_Y = DAM_Y - 0.062
for rx in (-0.47, -0.355, -0.24, -0.125, -0.01):
    box(rx, RAIL_Y, CREST + 0.024, 0.007, 0.007, 0.052, IRON)
beam((-0.478, RAIL_Y, CREST + 0.070), (-0.002, RAIL_Y, CREST + 0.070),
     0.005, 0.005, IRON)

# Abutment pier between dam and powerhouse, as in the reference.
box(0.055, DAM_Y, 0.0, 0.040, 0.075, CREST + 0.050, CONCRETE)

# --------------------------------------------------------------- the spillway
# A sloped chute cut into the dam face with a sheet of water on it. Built on an
# explicit frame rather than with beam(), because the roll of an inclined slab
# is exactly the thing beam() picks arbitrarily.
CHUTE_TOP = V(0.0, DAM_Y - 0.055, CREST)
CHUTE_BOT = V(0.0, DAM_Y - 0.225, 0.020)
_d = CHUTE_BOT - CHUTE_TOP
CHUTE_LEN = _d.length
CZ = _d / CHUTE_LEN
CX = V(1, 0, 0)
CY = CZ.cross(CX)

SPILL_X, SPILL_HW = -0.215, 0.115
CHUTE_T = 0.014
box_axes(V(SPILL_X, CHUTE_TOP.y, CHUTE_TOP.z), CX, CY, CZ,
         (SPILL_HW + 0.018, CHUTE_T), (SPILL_HW + 0.018, CHUTE_T),
         CHUTE_LEN, CONCRETE)

# The sheet has to ride on the OUTWARD face of the chute, which means offsetting
# along the chute's own normal. Offsetting in world y/z instead buries a 0.007
# slab inside a 0.014 one and the spillway renders as bare concrete.
_water_base = V(SPILL_X, CHUTE_TOP.y, CHUTE_TOP.z) + CY * (CHUTE_T + 0.006)
box_axes(_water_base, CX, CY, CZ,
         (SPILL_HW, 0.007), (SPILL_HW, 0.007), CHUTE_LEN * 0.98, WATER)

# Plunge pool: the white water where the sheet hits. This is most of what sells
# "flowing" at tile scale -- the sheet alone reads as a ramp.
for fx, fy, r, h in ((-0.215, -0.150, 0.075, 0.042),
                     (-0.135, -0.170, 0.045, 0.028),
                     (-0.295, -0.165, 0.048, 0.030)):
    cone((fx, fy, 0.004), r, h, FOAM, seg=6)
box(-0.215, -0.118, 0.0, 0.118, 0.032, 0.020, FOAM)

# Foam streaks carried downstream. Flat quads just clear of the hex surface --
# two triangles each, and they do more for the sense of current than geometry.
# Tapered wedges fanning out of the plunge pool, not flat bars. Long even
# rectangles of foam read as white planks floating on the river; a wedge that
# narrows downstream catches the light on three faces and reads as churn.
for sx, sy, hw, hl, rot in ((-0.215, -0.260, 0.078, 0.026, 0.00),
                            (-0.325, -0.330, 0.062, 0.022, 0.30),
                            (-0.105, -0.320, 0.060, 0.021, -0.28),
                            (-0.250, -0.415, 0.052, 0.018, 0.10),
                            (-0.045, -0.440, 0.046, 0.016, -0.15),
                            (-0.380, -0.450, 0.042, 0.015, 0.25)):
    taper(sx, sy, 0.0, (hw, hl), (hw * 0.35, hl * 0.45), 0.013, FOAM, rot)

# --------------------------------------------------------------- the head
# There is no slab of impounded water here, and that is deliberate. A raised
# WATER box behind the dam read as a dull flat panel in the build -- the hex is
# already bright blue, so a second, darker blue rectangle looks like a wall, not
# like water. The hex IS the reservoir. What it cannot show is movement, so the
# only thing added upstream is drift: ripples pulled toward the spillway mouth.
for sx, sy, hw, hl, rot in ((-0.215, 0.235, 0.080, 0.012, 0.0),
                            (-0.345, 0.300, 0.062, 0.010, 0.22),
                            (-0.085, 0.305, 0.058, 0.010, -0.20),
                            (-0.230, 0.390, 0.070, 0.011, 0.05)):
    c, s = math.cos(rot), math.sin(rot)
    panel((sx, sy, 0.006), V(c, s, 0), V(-s, c, 0), hw, hl, FOAM)

# --------------------------------------------------------------- powerhouse
PX, PY = 0.265, -0.020
PHX, PHY = 0.195, 0.155
PH = 0.380

box(PX, PY, 0.0, PHX + 0.018, PHY + 0.018, 0.062, CONCRETE)         # plinth
box(PX, PY, 0.062, PHX, PHY, PH, BRICK)
taper(PX, PY, 0.062 + PH, (PHX + 0.020, PHY + 0.020), (0.052, 0.020), 0.105, ROOF)
box(PX, PY, 0.062 + PH + 0.078, 0.060, 0.048, 0.034, ROOF)          # roof monitor
wall_panel(PX, PY - 0.048, 0.062 + PH + 0.095, 0.050, 0.012, GLASS)

# The tall narrow windows are the building's whole character in the reference.
for dx in (-0.125, -0.042, 0.042, 0.125):
    wall_panel(PX + dx, PY - PHY, 0.230, 0.022, 0.088, GLASS)
for dx in (-0.125, 0.125):
    wall_panel(PX + dx, PY - PHY, 0.115, 0.020, 0.030, GLASS)

# Tailrace arch: the water leaves the building here, so it is dark, not glazed.
box(PX - 0.020, PY - PHY - 0.010, 0.0, 0.070, 0.012, 0.072, CONCRETE)
box(PX - 0.020, PY - PHY - 0.016, 0.0, 0.052, 0.008, 0.058, WATER)
panel((PX - 0.020, PY - PHY - 0.070, 0.008), V(1, 0, 0), V(0, 1, 0),
      0.055, 0.048, FOAM)

# ----------------------------------------------------- timber landing stage
# From the watermill reference, and it gives the concrete somewhere warm to sit.
box(-0.395, -0.235, 0.0, 0.115, 0.065, 0.014, TIMBER)
for px in (-0.485, -0.305):
    taper(px, -0.235, 0.0, (0.030, 0.034), (0.022, 0.026), 0.046, ROCK)
for px in (-0.470, -0.320):
    box(px, -0.292, 0.014, 0.008, 0.008, 0.062, TIMBER)
beam((-0.478, -0.292, 0.076), (-0.312, -0.292, 0.076), 0.005, 0.005, TIMBER)

# ----------------------------------------------------------- rock and reed
# This tile has no bank: the whole hex is river. Loose grass tufts scattered over
# it floated, and bare rocks read as grey crates dropped in the water. So the
# planting sits on islets -- a boulder with something growing out of it, which is
# what actually survives in a river and gives the nature somewhere to stand.
for rx, ry, hx, hy, hz, rot, reed in (
    (-0.415, -0.355, 0.062, 0.050, 0.038, 0.4, 0.052),
    (0.155, -0.330, 0.050, 0.040, 0.030, -0.6, 0.042),
    (0.445, 0.205, 0.056, 0.045, 0.034, 1.1, 0.046),
    (-0.470, 0.215, 0.046, 0.037, 0.026, -0.8, 0.036),
    (0.075, -0.475, 0.042, 0.034, 0.024, 0.9, 0.0),
    (-0.115, -0.250, 0.038, 0.030, 0.021, -0.3, 0.0),
):
    taper(rx, ry, 0.0, (hx, hy), (hx * 0.66, hy * 0.66), hz, ROCK, rot)
    if reed:
        taper(rx, ry, hz * 0.85, (hx * 0.55, hy * 0.45),
              (hx * 0.12, hy * 0.10), reed, GRASS, rot + 0.5)

# Two scrub bushes rooted on the larger islets, for height that is not concrete.
for bx, by, r, h in ((0.445, 0.205, 0.058, 0.082), (-0.415, -0.355, 0.062, 0.090)):
    cone((bx, by, 0.028), r, h, GRASS, seg=6)

# Tufts on the banks. They sit at bank height rather than z=0 -- tuft() plants at
# ground level, which here is the river bed, and they would stand in the water.
for cx, cy, hgt, rot in (
    (-0.440, 0.300, 0.030, 0.3), (-0.452, -0.050, 0.026, -0.5),
    (-0.428, -0.278, 0.024, 1.0), (0.205, 0.243, 0.028, -0.2),
    (0.402, 0.232, 0.025, 0.7), (0.222, -0.285, 0.027, -1.1),
    (0.418, -0.272, 0.023, 0.5),
):
    taper(cx, cy, 0.041, (0.022, 0.013), (0.005, 0.003), hgt, GRASS, rot)

# A pair of bank trees, to give the far side some height against the sky.
for bx, by, r, h in ((-0.445, 0.155, 0.072, 0.115), (0.395, -0.048, 0.066, 0.100)):
    cone((bx, by, 0.040), r, h, GRASS, seg=6)

# ------------------------------------------------------------------ assemble
REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
obj = finish("Hidreletrica")
export(obj,
       os.path.join(REPO, "ArtSource", "Hidreletrica.blend"),
       os.path.join(REPO, "Assets", "_Game", "Models", "Hidreletrica.fbx"))
