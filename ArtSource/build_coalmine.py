"""
Generates the Coal Extraction model (pithead + ore cart) as a single flat-shaded
low-poly mesh, authored ~1 hex unit wide with its origin on the ground plane.

Run inside Blender (__file__ must be set so the script can find lowpoly.py):

    p = r"<repo>\\ArtSource\\build_coalmine.py"
    exec(compile(open(p).read(), p, "exec"), {"__file__": p})

Material slot order is a contract with Assets/Editor/CoalMineModelBuilder.cs,
which assigns the Unity materials by index. Do not reorder MATERIALS.
"""
import os, sys, importlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lowpoly
importlib.reload(lowpoly)          # pick up edits without restarting Blender
from lowpoly import *

TIMBER, STONE, SLATE, IRON, COAL, ROCK, GRASS = range(7)

MATERIALS = [
    ("Mine_Timber", (0.55, 0.42, 0.28)),
    ("Mine_Stone",  (0.90, 0.87, 0.79)),
    ("Mine_Slate",  (0.38, 0.42, 0.48)),
    ("Mine_Iron",   (0.30, 0.30, 0.33)),
    ("Mine_Coal",   (0.10, 0.10, 0.12)),
    ("Mine_Rock",   (0.46, 0.33, 0.28)),
    ("Mine_Grass",  (0.28, 0.45, 0.22)),
]

begin(MATERIALS)

# ---------------------------------------------------------------- the pit
# A raised rock rim with a dark floor, rather than a hole: the hex prism below
# is a shared mesh and cannot be cut per tile.
PX, PY = 0.08, -0.04
OHX, OHY = 0.19, 0.16
IHX, IHY = 0.12, 0.10
RIM = 0.055

box(PX + (IHX + OHX) / 2, PY, 0.0, (OHX - IHX) / 2, OHY, RIM, ROCK)
box(PX - (IHX + OHX) / 2, PY, 0.0, (OHX - IHX) / 2, OHY, RIM, ROCK)
box(PX, PY + (IHY + OHY) / 2, 0.0, IHX, (OHY - IHY) / 2, RIM, ROCK)
box(PX, PY - (IHY + OHY) / 2, 0.0, IHX, (OHY - IHY) / 2, RIM, ROCK)
box(PX, PY, 0.0, IHX, IHY, 0.014, COAL)

# ---------------------------------------------------------- the headframe
LEG_X, LEG_Y = 0.21, 0.185      # base half-spread, straddling the pit
TOP_X, TOP_Y = 0.075, 0.070     # battered inward toward the sheave
H = 0.58

legs = []
for sx in (-1, 1):
    for sy in (-1, 1):
        p0 = (PX + sx * LEG_X, PY + sy * LEG_Y, 0.0)
        p1 = (PX + sx * TOP_X, PY + sy * TOP_Y, H)
        beam(p0, p1, 0.014, 0.014, TIMBER)
        legs.append((p0, p1))


def leg_point(p0, p1, f):
    return tuple(p0[i] + (p1[i] - p0[i]) * f for i in range(3))


# One girt band plus a diagonal per face: enough bracing to read as a timber
# tower without paying for a full X on every bay.
GIRT = 0.42
order = [0, 1, 3, 2]            # walk the legs around the tower, not across it
for i in range(4):
    a, b = legs[order[i]], legs[order[(i + 1) % 4]]
    beam(leg_point(*a, GIRT), leg_point(*b, GIRT), 0.010, 0.010, TIMBER)
    beam(leg_point(*a, 0.06), leg_point(*b, GIRT), 0.008, 0.008, TIMBER)

box(PX, PY, H, 0.085, 0.080, 0.028, TIMBER)          # head platform

# Sheave wheel. Its plane holds the winding-house drum so the cable stays flat.
WC = V(PX, PY, 0.675)
DC = V(-0.22, 0.12, 0.095)
h2 = Vector((DC.x - WC.x, DC.y - WC.y, 0)).normalized()
axis = V(-h2.y, h2.x, 0)

for sx in (-1, 1):              # the pair of posts carrying the axle
    beam((PX + sx * 0.062 * axis.y, PY - sx * 0.062 * axis.x, H + 0.028),
         (PX + sx * 0.030 * axis.y, PY - sx * 0.030 * axis.x, 0.675),
         0.009, 0.009, TIMBER)

cylinder(WC, axis, 0.075, 0.011, IRON, seg=10)
cylinder(WC, axis, 0.022, 0.014, IRON, seg=6)        # hub
cylinder(DC, axis, 0.035, 0.050, IRON, seg=8)        # winding drum
for sx in (-1, 1):                                    # drum trestle
    box(DC.x + sx * 0.055 * axis.x, DC.y + sx * 0.055 * axis.y, 0.0,
        0.016, 0.016, DC.z, TIMBER)

# Cables: one down the shaft, one back to the drum.
beam((WC.x, WC.y, WC.z - 0.075), (PX, PY, 0.352), 0.005, 0.005, IRON)
beam((WC.x + h2.x * 0.075, WC.y + h2.y * 0.075, WC.z),
     (DC.x + h2.x * 0.035, DC.y + h2.y * 0.035, DC.z), 0.005, 0.005, IRON)

# Ore bucket on the end of the hoist rope.
taper(PX, PY, 0.258, (0.036, 0.031), (0.050, 0.044), 0.094, TIMBER)
box(PX, PY, 0.348, 0.054, 0.048, 0.012, IRON)
box(PX, PY, 0.330, 0.040, 0.035, 0.024, COAL)

# ------------------------------------------------------- the winding house
HX, HY = -0.30, 0.26
box(HX, HY, 0.0, 0.115, 0.095, 0.17, STONE)
taper(HX, HY, 0.17, (0.132, 0.112), (0.132, 0.010), 0.072, SLATE)   # gable roof
box(HX - 0.075, HY + 0.052, 0.17, 0.020, 0.020, 0.115, STONE)       # chimney
box(HX - 0.075, HY + 0.052, 0.285, 0.026, 0.026, 0.014, SLATE)
box(HX + 0.030, HY - 0.097, 0.0, 0.024, 0.004, 0.080, TIMBER)       # door
for dx in (-0.062, -0.010):                                         # windows
    box(HX + dx, HY - 0.097, 0.095, 0.018, 0.004, 0.030, IRON)

# No ladder to the head platform: at tile scale it crossed the headframe's
# bracing and read as clutter rather than as access.

# ------------------------------------------------------- the cart, on rails
RAIL_Y, CART_X = -0.34, -0.28
for dy in (-0.038, 0.038):                                          # rails
    beam((-0.47, RAIL_Y + dy, 0.020), (-0.02, RAIL_Y + dy, 0.020), 0.006, 0.010, IRON)
for x in (-0.44, -0.33, -0.22, -0.11):                              # sleepers
    box(x, RAIL_Y, 0.0, 0.013, 0.052, 0.018, TIMBER)

box(CART_X, RAIL_Y, 0.030, 0.052, 0.040, 0.022, IRON)               # chassis
taper(CART_X, RAIL_Y, 0.052, (0.052, 0.040), (0.070, 0.055), 0.078, TIMBER)
box(CART_X, RAIL_Y, 0.118, 0.073, 0.058, 0.010, IRON)               # rim band
cone((CART_X, RAIL_Y, 0.124), 0.056, 0.042, COAL, seg=6)            # load
box(CART_X + 0.030, RAIL_Y - 0.022, 0.126, 0.016, 0.014, 0.020, COAL, rot=0.5)
box(CART_X - 0.034, RAIL_Y + 0.016, 0.126, 0.014, 0.012, 0.017, COAL, rot=-0.4)
for dx in (-0.040, 0.040):                                          # wheels
    for dy in (-0.045, 0.045):
        cylinder((CART_X + dx, RAIL_Y + dy, 0.030), (0, 1, 0), 0.026, 0.006, IRON, seg=6)

# ------------------------------------------------------- spoil, rock, grass
cone((0.33, -0.32, 0.0), 0.125, 0.095, COAL, seg=7)
box(0.20, -0.36, 0.0, 0.024, 0.020, 0.026, COAL, rot=0.7)
box(0.43, -0.20, 0.0, 0.020, 0.017, 0.020, COAL, rot=-0.3)

for cx, cy, hx, hy, hz, rot in [
    (-0.44, 0.06, 0.030, 0.024, 0.022, 0.4),
    (0.40, 0.22, 0.026, 0.022, 0.019, -0.6),
    (-0.06, 0.34, 0.022, 0.018, 0.016, 0.9),
]:
    taper(cx, cy, 0.0, (hx, hy), (hx * 0.65, hy * 0.65), hz, ROCK, rot)

for cx, cy, hgt, rot in [
    (-0.45, -0.14, 0.046, 0.3), (-0.12, 0.42, 0.040, -0.8),
    (0.46, 0.02, 0.044, 1.1), (0.20, 0.34, 0.038, -0.2),
    (-0.38, -0.06, 0.036, 0.7), (0.02, -0.46, 0.042, -1.3),
]:
    tuft(cx, cy, hgt, rot, GRASS)

# ------------------------------------------------------------------ assemble
REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
obj = finish("CoalMine")
export(obj,
       os.path.join(REPO, "ArtSource", "CoalMine.blend"),
       os.path.join(REPO, "Assets", "_Game", "Models", "CoalMine.fbx"))
