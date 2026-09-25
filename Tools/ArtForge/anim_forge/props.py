"""The Lantern Warden's props in motion: the lantern hangs and swings, and drops.

No spring bones (decision: after the first playtest). The lantern's swing is
simulated here and BAKED into keys: a point mass on a 0.2 m rod from the ring in
the left fist, driven by the fist's motion (Verlet, 8 substeps per frame), so the
swing is phase-locked to the stride because the stride is what drives it.
Looping clips are pre-rolled three cycles so the baked swing loops. A PropDetach
lets the lantern fall under gravity, land upright and tip onto its side.

Used for the warden's own FBX (carry and signature clips) and for every review
render on the warden. Humanoid clips carry no lantern bones; in game the lantern
follows the fist until the engine's damped transform exists (plan decision 4).
"""

from __future__ import annotations

import math

from mathutils import Quaternion, Vector

from .skeleton import Skeleton
from .solve import Solved

RING, BODY = "LanternRing", "LanternBody"
ROD = 0.20          # ring to the lantern's centre of mass (m)
DOWN = Vector((0.0, 0.0, -1.0))
G = 9.81


def _ring_heads(skel: Skeleton, frames: list[Solved]) -> list[Vector]:
    return [skel.fk(s.Qx, s.hips).heads[RING] for s in frames]


def _set(skel: Skeleton, solved: Solved, direction: Vector) -> None:
    rest = skel.direction(BODY)
    q = rest.rotation_difference(direction.normalized())
    solved.Qx[RING] = q
    solved.Qx[BODY] = q


def hang(skel: Skeleton, frames: list[Solved], fps: int, loop: bool,
         damping: float = 0.03, until: int | None = None, limit: float = 55.0) -> list[float]:
    """Swing the lantern under the fist. Returns the swing angle (deg) per frame."""
    if RING not in skel.head:
        return []
    n = len(frames) if until is None else until
    pivots = _ring_heads(skel, frames[:n])
    sub = 8
    dt = 1.0 / (fps * sub)
    period = n - 1 if loop and n > 1 else 0
    seq = []
    if loop and period:
        seq = [pivots[i % period] for i in range(3 * period)] + pivots[:n]
        pre = 3 * period
    else:
        pre = fps
        seq = [pivots[0]] * pre + pivots
    bob = seq[0] + DOWN * ROD
    prev = bob.copy()
    out_dirs = []
    for k in range(1, len(seq)):
        a, b = seq[k - 1], seq[k]
        for j in range(1, sub + 1):
            p = a.lerp(b, j / sub)
            vel = (bob - prev) * (1.0 - damping)
            prev = bob.copy()
            bob = bob + vel + Vector((0.0, 0.0, -G)) * dt * dt
            d = (bob - p).normalized()
            # The bail stops the lantern swinging past `limit` from hanging.
            if d.angle(DOWN) > math.radians(limit):
                side = (d - DOWN * d.dot(DOWN)).normalized()
                a = math.radians(limit)
                d = DOWN * math.cos(a) + side * math.sin(a)
                prev = p + d * ROD            # the stop kills the swing's speed
            bob = p + d * ROD
        out_dirs.append((bob - b).normalized())
    dirs = [(seq[0] - seq[0] + DOWN)] + out_dirs
    dirs = dirs[pre:pre + n]
    angles = []
    for s, d in zip(frames[:n], dirs):
        _set(skel, s, d)
        angles.append(math.degrees(DOWN.angle(d)))
    return angles


def drop(skel: Skeleton, frames: list[Solved], fps: int, at: float) -> None:
    """PropDetach at time `at` (s): free fall, land upright, tip onto its side."""
    if RING not in skel.head:
        return
    i0 = min(len(frames) - 1, max(0, round(at * fps)))
    hang(skel, frames, fps, loop=False, until=i0 + 1)
    start = skel.fk(frames[i0].Qx, frames[i0].hips).heads[RING]
    start_dir = frames[i0].Qx.get(BODY, Quaternion()) @ skel.direction(BODY)
    length = skel.length(RING) + skel.length(BODY)          # ring head to lantern base
    land_z = length + 0.005
    fall_t = math.sqrt(max(0.0, 2.0 * (start.z - land_z) / G))
    tip_t = 0.35
    side = Vector((0.55, -0.83, 0.0)).normalized()           # tips out and forward
    for i in range(i0, len(frames)):
        t = (i - i0) / fps
        if t <= fall_t:
            head = start.copy()
            head.z = start.z - 0.5 * G * t * t
            k = t / max(fall_t, 1e-6)
            direction = start_dir.lerp(DOWN, k).normalized()
        else:
            u = min(1.0, (t - fall_t) / tip_t)
            u = u * u                                        # accelerates as it tips
            base = Vector((start.x, start.y, 0.0))
            ang = math.radians(90.0 * u)
            direction = (DOWN * math.cos(ang) + side * math.sin(ang)).normalized()
            # pivot about the base edge: the head sits `length` above the base, tipping
            up = -direction
            head = base + up * length + side * 0.07 * math.sin(ang)
            head.z = max(head.z, 0.075)
        _set(skel, frames[i], direction)
        frames[i].free[RING] = head
