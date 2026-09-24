"""Small maths for AnimForge: rotations in world axes, easing, two-bone IK.

Rotation convention (the whole module uses it): a pose rotation is (rx, ry, rz)
degrees about the WORLD axes of the rest pose, applied Z * Y * X, exactly like
art_forge.rig.apply_pose. X tips a bone forward/back (+X tips an upright bone
forward to -Y and swings a hanging bone BACK to +Y), Y rolls it sideways, Z turns it
about the vertical (+Z turns the figure's front, -Y, toward +X, its own left).
"""

from __future__ import annotations

import math

from mathutils import Matrix, Quaternion, Vector

X = Vector((1.0, 0.0, 0.0))
Y = Vector((0.0, 1.0, 0.0))
Z = Vector((0.0, 0.0, 1.0))
IDENT = Quaternion()


def euler_q(r) -> Quaternion:
    """(rx, ry, rz) degrees, world axes, applied X then Y then Z."""
    rx, ry, rz = (math.radians(a) for a in r)
    return (Quaternion(Z, rz) @ Quaternion(Y, ry) @ Quaternion(X, rx))


def q_power(q: Quaternion, f: float) -> Quaternion:
    """q ** f along the short arc; f may be < 0 or > 1 (anticipation, overshoot)."""
    if q.w < 0.0:
        q = Quaternion((-q.w, -q.x, -q.y, -q.z))
    axis, angle = q.to_axis_angle()
    if angle < 1e-9:
        return Quaternion()
    return Quaternion(axis, angle * f)


def slerp(a: Quaternion, b: Quaternion, f: float) -> Quaternion:
    """Slerp that extrapolates for f outside 0..1 (mathutils' slerp clamps)."""
    delta = a.inverted() @ b
    return a @ q_power(delta, f)


def orthonormal(primary: Vector, secondary: Vector, fallback: Vector = Y) -> tuple[Vector, Vector, Vector]:
    p = primary.normalized()
    s = secondary - p * secondary.dot(p)
    if s.length < 1e-6:
        s = fallback - p * fallback.dot(p)
        if s.length < 1e-6:
            s = X - p * X.dot(p)
    s.normalize()
    return p, s, p.cross(s)


def frame_rot(dir0: Vector, pole0: Vector, dir1: Vector, pole1: Vector) -> Quaternion:
    """The rotation taking frame (dir0, pole0) onto (dir1, pole1); poles are
    orthogonalised against their directions."""
    a = orthonormal(dir0, pole0)
    b = orthonormal(dir1, pole1)
    ma = Matrix((a[0], a[1], a[2])).transposed()
    mb = Matrix((b[0], b[1], b[2])).transposed()
    return (mb @ ma.transposed()).to_quaternion()


def two_bone(root: Vector, target: Vector, l1: float, l2: float, pole: Vector,
             max_reach: float = 0.9995) -> tuple[Vector, Vector, float]:
    """Place the middle joint of a two-bone chain. The joint bends toward `pole`.

    Returns (mid, end, shortfall): `end` is the target, or the nearest reachable
    point on the line to it when the chain is too short (shortfall metres > 0),
    so a caller can count how often an IK target was out of reach.
    """
    to = target - root
    d = to.length
    reach = (l1 + l2) * max_reach
    shortfall = 0.0
    if d > reach:
        shortfall = d - reach
        d = reach
    d = max(d, abs(l1 - l2) + 1e-4)
    direction = to.normalized() if to.length > 1e-9 else Vector((0, 0, -1))
    end = root + direction * d
    a = (l1 * l1 - l2 * l2 + d * d) / (2.0 * d)
    h = math.sqrt(max(0.0, l1 * l1 - a * a))
    _p, bend, _n = orthonormal(direction, pole)
    return root + direction * a + bend * h, end, shortfall


# ---- easing ----------------------------------------------------------------------

def _smooth(u):
    return u * u * (3.0 - 2.0 * u)


def _minjerk(u):
    return u * u * u * (10.0 - 15.0 * u + 6.0 * u * u)


EASE = {
    "linear": lambda u: u,
    "in": lambda u: u * u * u,                       # slow start, fast arrival (a strike)
    "out": lambda u: 1.0 - (1.0 - u) ** 3,           # fast start, settles (a recovery)
    "inout": _smooth,
    "smooth": _minjerk,
    "hold": lambda u: 0.0,
    # Overshoot past the key and settle back (follow-through): easeOutBack.
    "overshoot": lambda u: 1.0 + 2.2 * (u - 1.0) ** 3 + 1.2 * (u - 1.0) ** 2,
    # Pull back before leaving (anticipation): easeInBack.
    "anticipate": lambda u: 2.2 * u ** 3 - 1.2 * u ** 2,
}


def ease(name: str, u: float) -> float:
    u = min(1.0, max(0.0, u))
    try:
        return EASE[name](u)
    except KeyError:
        raise KeyError(f"unknown ease {name!r}; known: {sorted(EASE)}") from None
