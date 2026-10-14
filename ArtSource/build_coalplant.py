"""
!!  SUPERSEDED -- DO NOT RUN WITHOUT ASKING FIRST  !!

This script generated the ORIGINAL coal plant. Murillo has since hand-edited the
model in Blender, and CoalPlant.blend is now the source of truth for what ships.
Running this would overwrite both CoalPlant.blend and CoalPlant.fbx and discard
those edits. It is kept because the building itself is still all its work, and
because it is the starting point if the model is ever re-derived in code.

Known differences between this script and the shipped model:
  - the coal store was moved clear of the building, out to the far side
  - the conveyor gallery between store and boiler house was deleted (-84 tris)
  - the grass tufts were spread out to the footprint corners, +/-0.63
  - the turbine hall's doors and buttresses were sunk 0.016 into the ground
  - the whole footprint was then centred on the tile origin

To fold the edits back in, measure them off the .blend and edit this file, then
verify the rebuild matches: 1010 tris and per-material counts of
252/264/24/36/204/146/12/72.

-----------------------------------------------------------------------------

Generates the Coal Thermal plant (Carvao) as a single flat-shaded low-poly mesh,
authored ~1 hex unit wide with its origin on the ground plane.

Battersea by way of the pithead: a brick boiler house ribbed with pilasters,
four pale chimneys on corner piers, a lower glazed turbine hall across the
front, and a coal store feeding it by conveyor. Stone, slate, iron and coal
reuse the mine's exact values so the two tiles read as the same world.

Run inside Blender (__file__ must be set so the script can find lowpoly.py):

    p = r"<repo>\\ArtSource\\build_coalplant.py"
    exec(compile(open(p).read(), p, "exec"), {"__file__": p})

Material slot order is a contract with Assets/Editor/CoalPlantModelBuilder.cs,
which assigns the Unity materials by index. Do not reorder MATERIALS.
"""
import os, sys, importlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import lowpoly
importlib.reload(lowpoly)          # pick up edits without restarting Blender
from lowpoly import *

BRICK, STONE, ROOF, IRON, GLASS, COAL, CONCRETE, GRASS = range(8)

MATERIALS = [
    ("Plant_Brick",    (0.52, 0.30, 0.24)),
    ("Plant_Stone",    (0.90, 0.87, 0.79)),
    ("Plant_Roof",     (0.31, 0.34, 0.40)),
    ("Plant_Iron",     (0.30, 0.30, 0.33)),
    ("Plant_Glass",    (0.34, 0.47, 0.60)),
    ("Plant_Coal",     (0.10, 0.10, 0.12)),
    ("Plant_Concrete", (0.60, 0.60, 0.58)),
    ("Plant_Grass",    (0.28, 0.45, 0.22)),
]

begin(MATERIALS)

# ------------------------------------------------------------ boiler house
# The tall brick box that carries the chimneys. Everything else hangs off it.
BX, BY = 0.08, 0.10
BHX, BHY = 0.25, 0.185
BH = 0.38

box(BX, BY, 0.0, BHX, BHY, BH, BRICK)

# Vertical pilasters, Battersea's one unmistakable feature. They run past the
# wall head so the parapet reads as toothed rather than as a flat lid.
PILASTERS = [-0.12, -0.02, 0.08, 0.18, 0.28]
for px in PILASTERS:
    box(px, BY - BHY, 0.0, 0.016, 0.012, BH + 0.022, BRICK)
    box(px, BY + BHY, 0.0, 0.016, 0.012, BH + 0.022, BRICK)

# Tall recessed glazing in the bays between the pilasters.
for i in range(len(PILASTERS) - 1):
    gx = (PILASTERS[i] + PILASTERS[i + 1]) / 2
    box(gx, BY - BHY - 0.002, 0.085, 0.032, 0.004, 0.235, GLASS)
    box(gx, BY + BHY + 0.002, 0.085, 0.032, 0.004, 0.235, GLASS)

# Shallow gabled roof, ridge running across the block.
taper(BX, BY, BH, (BHX + 0.008, BHY + 0.008), (BHX + 0.008, 0.016), 0.086, ROOF)
for vx in (-0.06, 0.08, 0.22):                      # ridge vents
    box(vx, BY, BH + 0.055, 0.022, 0.020, 0.022, IRON)

# --------------------------------------------------- corner piers + chimneys
# Piers stand slightly proud of the block, as at Battersea, so the chimneys read
# as carried by the building rather than stuck on the roof.
PIER_H = 0.42
CHIMNEY_TOP = 0.78
for cx in (BX - 0.225, BX + 0.225):
    for cy in (BY - 0.160, BY + 0.160):
        box(cx, cy, 0.0, 0.062, 0.062, PIER_H, BRICK)
        box(cx, cy, PIER_H, 0.068, 0.068, 0.020, STONE)          # pier coping
        # Stacks are deliberately heavy. Thin ones foreshorten into masts at the
        # board camera's 49-degree pitch; Battersea's whole silhouette is bulk.
        tube(cx, cy, PIER_H + 0.020, 0.047, 0.039,
             CHIMNEY_TOP - PIER_H - 0.020, STONE, seg=8, cap_bottom=False)
        tube(cx, cy, CHIMNEY_TOP, 0.043, 0.053, 0.032,
             STONE, seg=8, cap_bottom=False)                     # flared cap
        tube(cx, cy, CHIMNEY_TOP + 0.032, 0.044, 0.044, 0.008,
             COAL, seg=8, cap_bottom=False)                      # sooted mouth

# ------------------------------------------------------------ turbine hall
# The long low wing across the front, glazed almost end to end.
TX, TY = 0.06, -0.235
THX, THY = 0.30, 0.150
TH = 0.185

box(TX, TY, 0.0, THX, THY, TH, BRICK)
taper(TX, TY, TH, (THX + 0.006, THY + 0.006), (THX + 0.006, 0.014), 0.072, ROOF)

# Roof lights, so the long hall roof is not one blank plane from above -- which
# is most of what the board camera sees of it.
for lx in (-0.175, 0.055, 0.235):
    box(lx, TY - 0.048, TH + 0.030, 0.042, 0.024, 0.016, GLASS)

FRONT = TY - THY - 0.002
for wx in (-0.25, -0.17, 0.16, 0.24):                            # window bays
    box(wx, FRONT, 0.048, 0.028, 0.004, 0.105, GLASS)
for dx in (-0.055, 0.055):                                       # the big doors
    box(dx, FRONT, 0.0, 0.048, 0.005, 0.140, GLASS)
    box(dx, FRONT - 0.004, 0.140, 0.054, 0.004, 0.014, STONE)    # door lintel
for bx2 in (-0.295, -0.125, 0.005, 0.125, 0.295):                # buttresses
    box(bx2, FRONT + 0.002, 0.0, 0.014, 0.010, TH + 0.016, BRICK)

# ------------------------------------------------------------- the coal store
# Same shape language as the mine's spoil heap: a dark cone with a few chunks
# broken off it, sitting straight on the ground. A paved pad under it read as a
# grey rectangle dropped on the hex, so the heap meets the grass directly.
SX, SY = -0.33, -0.04
cone((SX, SY, 0.0), 0.125, 0.095, COAL, seg=7)
box(SX + 0.105, SY - 0.085, 0.0, 0.024, 0.020, 0.024, COAL, rot=0.7)
box(SX - 0.080, SY + 0.105, 0.0, 0.020, 0.017, 0.019, COAL, rot=-0.3)
box(SX + 0.060, SY + 0.115, 0.0, 0.017, 0.015, 0.016, COAL, rot=1.1)

# Conveyor gallery lifting coal from the store into the boiler house.
CONV_A = (SX + 0.090, SY + 0.020, 0.095)
CONV_B = (BX - BHX + 0.010, BY - 0.090, 0.290)
d = V(CONV_B[0] - CONV_A[0], CONV_B[1] - CONV_A[1], 0).normalized()
side = V(-d.y, d.x, 0) * 0.030
for s in (-1, 1):
    beam((CONV_A[0] + side.x * s, CONV_A[1] + side.y * s, CONV_A[2]),
         (CONV_B[0] + side.x * s, CONV_B[1] + side.y * s, CONV_B[2]),
         0.011, 0.011, IRON)
for f in (0.22, 0.52, 0.82):                                     # cross ties
    p = [CONV_A[i] + (CONV_B[i] - CONV_A[i]) * f for i in range(3)]
    beam((p[0] - side.x, p[1] - side.y, p[2] - 0.014),
         (p[0] + side.x, p[1] + side.y, p[2] - 0.014), 0.006, 0.006, IRON)
for s in (-1, 1):                                                # trestle legs
    box(CONV_A[0] + 0.020 + side.x * s, CONV_A[1] + side.y * s, 0.0,
        0.011, 0.011, CONV_A[2] + 0.010, IRON)

# --------------------------------------------------------- yard and planting
box(0.06, -0.412, 0.0, 0.262, 0.034, 0.009, CONCRETE)            # apron strip

for cx, cy, hgt, rot in [
    (-0.46, 0.24, 0.026, 0.3), (-0.30, 0.36, 0.021, -0.8),
    (0.44, 0.30, 0.024, -0.5), (0.46, 0.06, 0.022, 1.1),
    (-0.47, -0.30, 0.023, -0.2), (0.24, 0.42, 0.020, 0.7),
]:
    tuft(cx, cy, hgt, rot, GRASS)

# ------------------------------------------------------------------ assemble
REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
obj = finish("CoalPlant")
export(obj,
       os.path.join(REPO, "ArtSource", "CoalPlant.blend"),
       os.path.join(REPO, "Assets", "_Game", "Models", "CoalPlant.fbx"))
