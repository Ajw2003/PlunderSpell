"""Frame -> world-delta rotations (`Qx`), with IK, on the reference or any human rig.

Two entry points:

- `solve(ref, frame)` on the reference skeleton (the clip's own rig): FK from the
  pose, then leg IK to the foot targets and arm IK to the weapon. This is what the
  FBX files carry.
- `retarget(rig, solved, ...)` plays a solved reference frame on another human
  rig (the Lantern Warden) the way Unity's Humanoid retarget will: each bone points
  where the reference bone points (Qx @ C^-1), hips offset scaled by hip height.
  `hand_ik=True` then pulls the left hand onto the rig's own weapon grip, standing
  in for the Humanoid IK pass the engine runs on two-handed clips.

Both return a `Solved` (Qx per bone, hips offset, free heads for detached props)
plus IK diagnostics (how far a target was out of reach).
"""

from __future__ import annotations

from dataclasses import dataclass, field

from mathutils import Quaternion, Vector

from . import mathx
from .clip import Frame
from .skeleton import HUMAN_BONES, LEFT_GRIP_OFFSET, WEAPON, Posed, Skeleton

ELBOW_HINT = {"L": Vector((0.8, 0.5, -0.4)), "R": Vector((-0.8, 0.5, -0.4))}


@dataclass
class Solved:
    Qx: dict[str, Quaternion]
    hips: Vector
    free: dict[str, Vector] = field(default_factory=dict)   # bone -> posed head override
    shortfall: dict[str, float] = field(default_factory=dict)


def _rest_pole(skel: Skeleton, upper: str, lower: str, hint: Vector) -> Vector:
    root, mid, end = skel.head[upper], skel.head[lower], skel.tail[lower]
    line = (end - root).normalized()
    off = (mid - root) - line * (mid - root).dot(line)
    return off.normalized() if off.length > 1e-4 else hint


def chain_ik(skel: Skeleton, posed: Posed, upper: str, lower: str, target: Vector,
             hint: Vector) -> tuple[Quaternion, Quaternion, float]:
    """World deltas for a two-bone chain whose end (lower's tail) reaches target."""
    root = posed.heads[upper]
    l1, l2 = skel.length(upper), skel.length(lower)
    mid, end, short = mathx.two_bone(root, target, l1, l2, hint)
    line = (end - root).normalized()
    pole = (mid - root) - line * (mid - root).dot(line)
    if pole.length < 1e-6:
        pole = hint
    pole0 = _rest_pole(skel, upper, lower, hint)
    q_up = mathx.frame_rot(skel.direction(upper), pole0, (mid - root).normalized(), pole)
    q_lo = mathx.frame_rot(skel.direction(lower), pole0, (end - mid).normalized(), pole)
    return q_up, q_lo, short


def _world_chain(skel: Skeleton, pose: dict[str, Quaternion]) -> dict[str, Quaternion]:
    """Parent-relative pose -> reference world deltas, human bones only."""
    world = {}
    for name in skel.order:
        if name not in HUMAN_BONES:
            continue
        par = skel.parent[name]
        pq = world.get(par, Quaternion()) if par else Quaternion()
        world[name] = pq @ pose.get(name, Quaternion())
    return world


def _extras(skel: Skeleton, Qx: dict, extras: dict[str, Quaternion], keep: set[str]) -> None:
    """Non-human bones follow their parent, turned by their own parent-relative key."""
    for name in skel.order:
        if name in HUMAN_BONES or name in keep:
            continue
        par = skel.parent[name]
        Qx[name] = Qx[par] @ extras.get(name, Quaternion())


def _mirror(v: Vector) -> Vector:
    return Vector((-v.x, v.y, v.z))


def _follow(skel: Skeleton, Qx: dict, bone: str) -> None:
    """Props under `bone` (weapon, grip target) turn rigidly with it again."""
    for name in skel.order:
        par = skel.parent[name]
        if name not in HUMAN_BONES and par is not None and (par == bone or par in _desc(skel, bone)):
            Qx[name] = Qx[par]


def _desc(skel: Skeleton, bone: str) -> set:
    out, todo = set(), [bone]
    while todo:
        b = todo.pop()
        for c in skel.children(b):
            out.add(c)
            todo.append(c)
    return out


def left_grip_axis(skel: Skeleton, weapon_bone: str) -> Vector:
    """The haft axis through the LEFT fist at rest: the right fist's rest grip
    (hand direction + weapon axis) mirrored across YZ, then swung onto the left
    hand's own rest direction (the warden's two arms rest differently)."""
    m_dir = _mirror(skel.direction("Hand.R"))
    m_axis = _mirror(skel.direction(weapon_bone))
    return m_dir.rotation_difference(skel.direction("Hand.L")) @ m_axis


def arms_to_weapon(skel: Skeleton, Qx: dict, hips: Vector, grip: Vector, wq: Quaternion,
                   lhand: float, elbows: dict, weapon_bone: str, right: bool,
                   shortfall: dict) -> None:
    """Right hand carries the weapon frame (grip point, rotation wq from the
    canonical weapon frame); the left hand grips the haft LEFT_GRIP_OFFSET along
    it, blended by `lhand`."""
    axis_rest = skel.direction(weapon_bone)
    if right:
        # The weapon bone's delta: its rest frame -> the key's frame. Its rest frame
        # is the canonical one (haft +Z) swung onto this rig's rest haft axis.
        to_rest = mathx.Z.rotation_difference(axis_rest)
        q_weapon = wq @ to_rest.inverted()
        posed = skel.fk(Qx, hips)
        q_hand = q_weapon                       # the weapon is rigid in the right fist
        wrist = grip - q_hand @ (skel.grip("R") - skel.head["Hand.R"])
        hint = Vector(elbows.get("R", ELBOW_HINT["R"]))
        q_up, q_lo, short = chain_ik(skel, posed, "UpperArm.R", "LowerArm.R", wrist, hint)
        Qx["UpperArm.R"], Qx["LowerArm.R"], Qx["Hand.R"] = q_up, q_lo, q_hand
        _follow(skel, Qx, "Hand.R")
        shortfall["hand.R"] = max(shortfall.get("hand.R", 0.0), short)
    if lhand <= 1e-3:
        return
    posed = skel.fk(Qx, hips)
    # The haft as the rig actually holds it now (right hand's weapon bone).
    w_head = posed.heads[weapon_bone]
    w_axis = posed.world[weapon_bone] @ axis_rest
    target = w_head + w_axis * LEFT_GRIP_OFFSET
    hand_dir0 = skel.direction("Hand.L")
    shoulder = posed.heads["UpperArm.L"]
    want = target - shoulder
    want = want - w_axis * want.dot(w_axis)
    if want.length < 1e-4:
        want = Vector((0.0, 0.0, -1.0))
    q_hand = mathx.frame_rot(hand_dir0, left_grip_axis(skel, weapon_bone),
                             want.normalized(), w_axis)
    wrist = target - q_hand @ (skel.grip("L") - skel.head["Hand.L"])
    hint = Vector(elbows.get("L", ELBOW_HINT["L"]))
    q_up, q_lo, short = chain_ik(skel, posed, "UpperArm.L", "LowerArm.L", wrist, hint)
    shortfall["hand.L"] = max(shortfall.get("hand.L", 0.0), short)
    for bone, q in (("UpperArm.L", q_up), ("LowerArm.L", q_lo), ("Hand.L", q_hand)):
        Qx[bone] = mathx.slerp(Qx[bone], q, lhand) if lhand < 1.0 else q
    _follow(skel, Qx, "Hand.L")


def solve(ref: Skeleton, frame: Frame, weapon_bone: str | None = WEAPON) -> Solved:
    Qx = _world_chain(ref, frame.pose)
    hips = frame.hips.copy()
    shortfall: dict[str, float] = {}
    if frame.feet:
        posed = ref.fk(Qx, hips)
        fwd = Qx["Hips"] @ Vector((0.0, -1.0, 0.0))
        for side, (ankle, q_foot) in frame.feet.items():
            hint = (fwd + Vector((0.12 if side == "L" else -0.12, 0.0, 0.0))).normalized()
            q_up, q_lo, short = chain_ik(ref, posed, f"UpperLeg.{side}", f"LowerLeg.{side}",
                                         ankle, hint)
            Qx[f"UpperLeg.{side}"], Qx[f"LowerLeg.{side}"] = q_up, q_lo
            Qx[f"Foot.{side}"] = q_foot
            shortfall[f"foot.{side}"] = short
    if frame.weapon and weapon_bone and weapon_bone in ref.head:
        _extras(ref, Qx, frame.extras, set())
        grip, wq = frame.weapon
        arms_to_weapon(ref, Qx, hips, grip, wq, frame.lhand, frame.elbows, weapon_bone,
                       True, shortfall)
    _extras(ref, Qx, frame.extras, set())
    return Solved(Qx, hips, {}, shortfall)


def retarget(rig: Skeleton, ref_solved: Solved, extras: dict[str, Quaternion] | None = None,
             weapon_bone: str | None = None, lhand: float = 0.0, elbows: dict | None = None,
             keep: dict[str, Quaternion] | None = None) -> Solved:
    """Play a reference solution on `rig` (see module docstring). `keep` pins bones'
    world deltas (carry-layer overrides are applied by the caller before this)."""
    Qx = {}
    for name in rig.order:
        if name in HUMAN_BONES and name in ref_solved.Qx:
            Qx[name] = ref_solved.Qx[name] @ rig.correction[name].inverted()
    hips = ref_solved.hips * rig.hip_scale
    shortfall: dict[str, float] = {}
    _extras(rig, Qx, extras or {}, set())
    if weapon_bone and lhand > 1e-3:
        arms_to_weapon(rig, Qx, hips, None, None, lhand, elbows or {}, weapon_bone, False,
                       shortfall)
        _extras(rig, Qx, extras or {}, set())
    return Solved(Qx, hips, {}, shortfall)


def to_local(skel: Skeleton, solved: Solved) -> dict[str, Quaternion]:
    """World deltas -> parent-relative (world-axis) rotations, e.g. for a carry
    layer that replaces masked bones' local rotations over another clip."""
    out = {}
    for name in skel.order:
        par = skel.parent[name]
        pq = solved.Qx.get(par, Quaternion()) if par else Quaternion()
        out[name] = pq.inverted() @ solved.Qx[name]
    return out


def layer_override(skel: Skeleton, base: Solved, local: dict[str, Quaternion],
                   mask: set[str]) -> Solved:
    """An Animator override layer: masked bones take `local` (parent-relative)
    rotations over the base's parents; everything else keeps the base."""
    base_local = to_local(skel, base)
    Qx = {}
    for name in skel.order:
        par = skel.parent[name]
        pq = Qx.get(par, Quaternion()) if par else Quaternion()
        Qx[name] = pq @ (local[name] if name in mask and name in local else base_local[name])
    return Solved(Qx, base.hips.copy(), dict(base.free), dict(base.shortfall))
