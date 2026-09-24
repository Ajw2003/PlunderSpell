"""
In-Blender validation gate. Runs with full bmesh/mesh access right after a
prop is built (and again, re-imported, right after export) so every check
from the directive's "AUTOMATED 3D VALIDATION CHECKS" list has real geometry
to inspect rather than guessing from a re-parsed file.

Returns a list of human-readable issue strings; empty list == pass.
"""
import math

import bmesh
import palette as pal

QUAD_DOMINANT_MIN = 0.75
UV_EPS = 1e-4
BASE_PIVOT_TOLERANCE = 0.03  # metres of slack for "pivot sits at the base"
# room_kit.paint_box grows every stacked box by OVERLAP (0.02) about its
# centre, so a wall sitting exactly on the cell edge legitimately reaches
# HALF + 0.01. Anything past this is a module genuinely hanging into its
# neighbour's cell.
FOOTPRINT_TOLERANCE = 0.05


def validate_object(obj, tri_budget: int, max_footprint: float | None = None) -> list[str]:
    issues = []

    # ── transforms ──────────────────────────────────────────────
    loc, rot, scale = obj.location, obj.rotation_euler, obj.scale
    if any(abs(c) > 1e-6 for c in loc):
        issues.append(f"pivot not at origin: location={tuple(loc)}")
    if any(abs(c) > 1e-6 for c in rot):
        issues.append(f"rotation not reset: euler={tuple(rot)}")
    if any(abs(c - 1.0) > 1e-6 for c in scale):
        issues.append(f"scale not applied: scale={tuple(scale)}")

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    bm.edges.ensure_lookup_table()
    bm.verts.ensure_lookup_table()

    # ── transforms: pivot sits at the prop's base (every builder is
    #    authored bottom-flush at local Z=0, so this is a real invariant,
    #    not just "object.location == origin") ─────────────────────
    min_z = min((v.co.z for v in bm.verts), default=0.0)
    if abs(min_z) > BASE_PIVOT_TOLERANCE:
        issues.append(f"pivot not at base: lowest vertex Z={min_z:.4f} (want ~0)")

    # ── footprint: the module stays inside its grid cell ────────
    #    The generator spaces cells by cellSize and never inspects the
    #    mesh, so a module wider than its cell silently intersects its
    #    neighbour — issue 19. Checking each side separately rather than
    #    the span catches an off-centre module that is narrow enough to
    #    pass a width check yet still overhangs one edge.
    if max_footprint is not None:
        half = max_footprint / 2 + FOOTPRINT_TOLERANCE
        for axis, name in ((0, "X"), (1, "Y")):
            lo = min(v.co[axis] for v in bm.verts)
            hi = max(v.co[axis] for v in bm.verts)
            if lo < -half or hi > half:
                issues.append(
                    f"overhangs the {max_footprint:.1f}m cell on {name}: "
                    f"[{lo:.3f}, {hi:.3f}] (allowed +/-{half:.3f})")

    # ── geometry: manifold ──────────────────────────────────────
    non_manifold = [e for e in bm.edges if not e.is_manifold]
    if non_manifold:
        issues.append(f"{len(non_manifold)} non-manifold edges")

    # ── geometry: isolated / duplicate verts ───────────────────
    isolated = [v for v in bm.verts if not v.link_faces]
    if isolated:
        issues.append(f"{len(isolated)} isolated vertices")
    dupes = _duplicate_vert_count(bm)
    if dupes:
        issues.append(f"{dupes} duplicate-position vertices")

    # ── geometry: quad-dominant ─────────────────────────────────
    n_faces = len(bm.faces)
    n_quads = sum(1 for f in bm.faces if len(f.verts) == 4)
    quad_ratio = (n_quads / n_faces) if n_faces else 0.0
    if quad_ratio < QUAD_DOMINANT_MIN:
        issues.append(f"not quad-dominant: {quad_ratio:.0%} quads (need >={QUAD_DOMINANT_MIN:.0%})")

    # ── budget: triangle count ──────────────────────────────────
    tri_count = sum(len(f.verts) - 2 for f in bm.faces)
    if tri_count > tri_budget:
        issues.append(f"over budget: {tri_count} tris > {tri_budget}")

    # ── UV: unwrapped, in-bounds, one-pigment-cell-per-face ─────
    uv_layer = bm.loops.layers.uv.active
    if uv_layer is None:
        issues.append("no active UV layer")
    else:
        grid = pal.PALETTE_GRID
        for f in bm.faces:
            us = [loop[uv_layer].uv.x for loop in f.loops]
            vs = [loop[uv_layer].uv.y for loop in f.loops]
            lo_u, hi_u, lo_v, hi_v = min(us), max(us), min(vs), max(vs)
            if lo_u < -UV_EPS or hi_u > 1 + UV_EPS or lo_v < -UV_EPS or hi_v > 1 + UV_EPS:
                issues.append(f"face {f.index} UV outside 0-1: u[{lo_u:.3f},{hi_u:.3f}] v[{lo_v:.3f},{hi_v:.3f}]")
                continue
            cell_u0, cell_v0 = math.floor(lo_u * grid), math.floor(lo_v * grid)
            cell_u1, cell_v1 = math.floor(min(hi_u * grid, grid - UV_EPS)), math.floor(min(hi_v * grid, grid - UV_EPS))
            if (cell_u0, cell_v0) != (cell_u1, cell_v1):
                issues.append(f"face {f.index} UV straddles two palette cells (bleed)")

    bm.free()
    return issues


# ── castle layout (castle revamp, docs/plans/castle-revamp.md) ────────────
# A module is built by stacking separate primitives, so every prop, wall
# segment and merlon is its own connected mesh island. Two rules the old
# pieces broke, checked per island:
#   * nothing floats: every island must rest on the ground or touch
#     something that does (the battlements used to ring cells whose wall was
#     on one side only, so three sides of merlons hung in the air);
#   * an enclosed room keeps a clear cross-shaped walkway between its four
#     archways (set-pieces through the middle of rooms sealed off most of the
#     castle on the NavMesh — see the navigation audit).

TOUCH = 0.06            # metres: boxes this close count as touching
WALKWAY_HALF = 1.6      # half-width of each arm of the clear cross
WALKWAY_HEAD = 2.0      # headroom above the floor that must stay clear
FLAT_DECOR = 0.12       # a rug or runner this thin is walkable
ROOM_INNER = 5.4        # anything reaching past this is the room's wall, not a prop


def _islands(bm):
    """Connected face islands as (min_xyz, max_xyz) boxes."""
    seen = set()
    boxes = []
    for f in bm.faces:
        if f.index in seen:
            continue
        stack = [f]
        seen.add(f.index)
        lo = [1e9, 1e9, 1e9]
        hi = [-1e9, -1e9, -1e9]
        while stack:
            face = stack.pop()
            for v in face.verts:
                for a in range(3):
                    lo[a] = min(lo[a], v.co[a])
                    hi[a] = max(hi[a], v.co[a])
                for linked in v.link_faces:
                    if linked.index not in seen:
                        seen.add(linked.index)
                        stack.append(linked)
        boxes.append((tuple(lo), tuple(hi)))
    return boxes


def _touch(a, b, tol=TOUCH):
    return all(a[0][i] <= b[1][i] + tol and b[0][i] <= a[1][i] + tol for i in range(3))


def validate_castle_layout(obj, enclosed: bool, floor_top: float = 0.3) -> list[str]:
    issues = []
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    boxes = _islands(bm)
    bm.free()

    # Floating: flood from everything standing on the ground.
    grounded = {i for i, b in enumerate(boxes) if b[0][2] < 0.05}
    frontier = list(grounded)
    while frontier:
        i = frontier.pop()
        for j, other in enumerate(boxes):
            if j not in grounded and _touch(boxes[i], other):
                grounded.add(j)
                frontier.append(j)
    floating = [b for i, b in enumerate(boxes) if i not in grounded]
    if floating:
        lo, hi = floating[0]
        issues.append(f"{len(floating)} floating piece(s), e.g. one spanning "
                      f"x[{lo[0]:.1f},{hi[0]:.1f}] y[{lo[1]:.1f},{hi[1]:.1f}] z[{lo[2]:.1f},{hi[2]:.1f}]")

    if enclosed:
        blocking = []
        for lo, hi in boxes:
            is_shell = (max(abs(lo[0]), abs(hi[0]), abs(lo[1]), abs(hi[1])) > ROOM_INNER)
            if is_shell or hi[2] <= floor_top + FLAT_DECOR or lo[2] >= floor_top + WALKWAY_HEAD:
                continue
            in_x_arm = lo[0] < WALKWAY_HALF and hi[0] > -WALKWAY_HALF
            in_y_arm = lo[1] < WALKWAY_HALF and hi[1] > -WALKWAY_HALF
            if in_x_arm or in_y_arm:
                blocking.append((lo, hi))
        if blocking:
            lo, hi = blocking[0]
            issues.append(f"{len(blocking)} prop(s) block the walkway between archways, e.g. "
                          f"x[{lo[0]:.1f},{hi[0]:.1f}] y[{lo[1]:.1f},{hi[1]:.1f}] "
                          f"(keep |x| and |y| >= {WALKWAY_HALF} below {WALKWAY_HEAD}m)")
    return issues


def _duplicate_vert_count(bm, dist=1e-5) -> int:
    seen = {}
    dupes = 0
    for v in bm.verts:
        key = (round(v.co.x / dist), round(v.co.y / dist), round(v.co.z / dist))
        # only counts as a true duplicate if verts are coincident AND
        # unconnected (a shared-position vert with an edge between them is
        # legitimate topology, not a duplicate)
        bucket = seen.setdefault(key, [])
        for other in bucket:
            if other not in {e.other_vert(v) for e in v.link_edges}:
                dupes += 1
                break
        bucket.append(v)
    return dupes
