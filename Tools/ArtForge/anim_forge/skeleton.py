"""Skeletons: rest data, forward kinematics and retargeting, all in Python.

A pose is solved to one WORLD-SPACE DELTA rotation per bone, `Qx[b]`: how far the
bone has turned from its own rest orientation, about its posed head. That is
independent of bone roll, so the same numbers drive any rig; converting to
Blender's per-bone basis (for keyframes) happens only at bake time, in
`basis_rotation`.

Retargeting between two rigs whose rest poses differ (the reference human hangs its
right forearm; the warden's rest holds it level for the glaive) goes through a
per-bone correction `C[b]`, the swing from the reference rest direction to this
rig's rest direction. A reference delta Q plays on this rig as Q @ C^-1: the bone
ends up pointing where the reference bone points. That is what Unity's Humanoid
retarget does through its T-pose normalisation, so the review renders show what
the engine will show.
"""

from __future__ import annotations

from dataclasses import dataclass, field

import bpy
from mathutils import Quaternion, Vector

HUMAN_BONES = ["Root", "Hips", "Spine", "Chest", "Neck", "Head",
               "Shoulder.L", "UpperArm.L", "LowerArm.L", "Hand.L",
               "Shoulder.R", "UpperArm.R", "LowerArm.R", "Hand.R",
               "UpperLeg.L", "LowerLeg.L", "Foot.L",
               "UpperLeg.R", "LowerLeg.R", "Foot.R"]


@dataclass
class Skeleton:
    name: str
    order: list[str]                         # parents before children
    parent: dict[str, str | None]
    head: dict[str, Vector]
    tail: dict[str, Vector]
    rest_q: dict[str, Quaternion]            # bone matrix_local rotation (armature space)
    correction: dict[str, Quaternion] = field(default_factory=dict)
    hip_scale: float = 1.0                   # this rig's hip height / the reference's

    # ---- construction -----------------------------------------------------------

    @classmethod
    def from_armature(cls, obj: bpy.types.Object) -> "Skeleton":
        bones = obj.data.bones
        order = []

        def visit(b):
            order.append(b.name)
            for c in b.children:
                visit(c)
        for b in bones:
            if b.parent is None:
                visit(b)
        return cls(name=obj.name, order=order,
                   parent={b.name: (b.parent.name if b.parent else None) for b in bones},
                   head={b.name: b.head_local.copy() for b in bones},
                   tail={b.name: b.tail_local.copy() for b in bones},
                   rest_q={b.name: b.matrix_local.to_quaternion() for b in bones})

    def retarget_from(self, reference: "Skeleton") -> "Skeleton":
        """Fill `correction` (per shared bone) and `hip_scale` against the reference."""
        self.correction = {}
        for name in self.order:
            if name in reference.head:
                d_ref = reference.direction(name)
                d_me = self.direction(name)
                self.correction[name] = d_ref.rotation_difference(d_me)
        self.hip_scale = self.head["Hips"].z / reference.head["Hips"].z
        return self

    # ---- queries ----------------------------------------------------------------

    def direction(self, name: str) -> Vector:
        return (self.tail[name] - self.head[name]).normalized()

    def length(self, name: str) -> float:
        return (self.tail[name] - self.head[name]).length

    def children(self, name: str) -> list[str]:
        return [n for n in self.order if self.parent[n] == name]

    def is_human(self, name: str) -> bool:
        return name in HUMAN_BONES

    def grip(self, side: str) -> Vector:
        """Rest centre of the fist: the middle of the hand bone (figures.Human.grip)."""
        return (self.head[f"Hand.{side}"] + self.tail[f"Hand.{side}"]) * 0.5

    # ---- forward kinematics ------------------------------------------------------

    def fk(self, Qx: dict[str, Quaternion], hips: Vector,
           free: dict[str, Vector] | None = None) -> "Posed":
        """Posed heads and tails from world-delta rotations (missing bones follow
        their parent rigidly) and the Hips offset (armature space, metres). `free`
        pins a bone's posed head (a detached prop); its children follow it."""
        world: dict[str, Quaternion] = {}
        heads: dict[str, Vector] = {}
        for name in self.order:
            par = self.parent[name]
            q = Qx.get(name)
            if q is None:
                q = world[par] if par else Quaternion()
            world[name] = q
            if par is None:
                heads[name] = self.head[name].copy()
            else:
                heads[name] = heads[par] + world[par] @ (self.head[name] - self.head[par])
            if name == "Hips":
                heads[name] = heads[name] + hips
            if free and name in free:
                heads[name] = free[name].copy()
        return Posed(self, world, heads)

    def point(self, posed: "Posed", bone: str, rest_point: Vector) -> Vector:
        return posed.heads[bone] + posed.world[bone] @ (rest_point - self.head[bone])

    # ---- baking -----------------------------------------------------------------

    def basis_rotation(self, posed: "Posed", name: str) -> Quaternion:
        """Blender pose-bone rotation_quaternion for this bone."""
        par = self.parent[name]
        parent_q = posed.world[par] if par else Quaternion()
        local = parent_q.inverted() @ posed.world[name]
        r = self.rest_q[name]
        return r.inverted() @ local @ r

    def basis_location(self, name: str, offset: Vector) -> Vector:
        """Blender pose-bone location for an armature-space offset of a bone whose
        parent chain is unposed (only Hips is ever translated)."""
        return self.rest_q[name].inverted() @ offset


@dataclass
class Posed:
    skel: Skeleton
    world: dict[str, Quaternion]
    heads: dict[str, Vector]

    def tail(self, name: str) -> Vector:
        s = self.skel
        return self.heads[name] + self.world[name] @ (s.tail[name] - s.head[name])

    def point(self, bone: str, rest_point: Vector) -> Vector:
        return self.skel.point(self, bone, rest_point)


# ---- armatures -----------------------------------------------------------------

def build_armature(bones: list[dict], name: str) -> bpy.types.Object:
    """An armature from figure bone dicts (figures.Figure.rig()["bones"]), no mesh.
    Same construction as EnemyForge's build_armature (roll 0, not connected), so
    the rest matrices match the models' rigs."""
    data = bpy.data.armatures.new(name)
    obj = bpy.data.objects.new(name, data)
    bpy.context.scene.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    made = {}
    for spec in bones:
        b = data.edit_bones.new(spec["name"])
        b.head = Vector(spec["head"])
        b.tail = Vector(spec["tail"])
        made[spec["name"]] = b
    for spec in bones:
        if spec.get("parent"):
            made[spec["name"]].parent = made[spec["parent"]]
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in obj.pose.bones:
        pb.rotation_mode = "QUATERNION"
    return obj


def reference_bones(with_weapon: bool = False) -> list[dict]:
    """The reference human: figures.Human() at its defaults (1.76 m, EnemyForge's
    watchman proportions, arms in the default A-pose). With `with_weapon`, adds the
    polearm family's weapon bones (see weapon_bones)."""
    from art_forge.figures import Human
    fig = Human()
    bones = fig.rig()["bones"]
    if with_weapon:
        bones += weapon_bones(fig)
    return bones


# The polearm family's weapon, placed in the right fist exactly as the Lantern
# Warden holds its glaive: the haft vertical through the fist in the warden's rest
# pose. Measured from the warden rig (hand-local), so a clip authored on the
# reference grips the haft the way every polearm enemy's model grips it.
WEAPON = "Weapon"          # prop bone on Hand.R: head = right grip, tail = up the haft
WEAPON_GRIP_L = "Weapon_GripL"   # child of Weapon at the left hand's grip: the IK target
WEAPON_UP = 0.95           # grip to blade tip (the warden: 2.05 m glaive gripped at 1.10 m)
LEFT_GRIP_OFFSET = -0.55   # metres along the haft from the right grip to the left (toward the butt)


def weapon_bones(fig) -> list[dict]:
    """Weapon + Weapon_GripL on a figures.Human in its rest pose.

    The haft axis in the warden's rest is world +Z while its right forearm is held
    level; the direction is carried into the reference hand's frame by the swing
    between the two hands' rest directions, so relative to the fist it is the same
    grip."""
    from mathutils import Vector as V
    from art_forge.figures import ArmPose, Human
    head, tail = fig.bone("Hand.R")
    # The warden's right arm, as enemies_high.lantern_warden builds it.
    wh, wt = Human(height=1.763, bulk=1.10, shoulders=0.48, stoop=4.0,
                   arm_r=ArmPose(spread=16.0, swing=4.0, elbow=78.0)).bone("Hand.R")
    warden_hand = (wt - wh).normalized()
    swing = warden_hand.rotation_difference((tail - head).normalized())
    axis = swing @ V((0.0, 0.0, 1.0))
    grip = (head + tail) * 0.5
    tip = grip + axis * WEAPON_UP
    left = grip + axis * LEFT_GRIP_OFFSET
    return [
        {"name": WEAPON, "head": tuple(grip), "tail": tuple(tip), "parent": "Hand.R"},
        {"name": WEAPON_GRIP_L, "head": tuple(left), "tail": tuple(left + axis * 0.10),
         "parent": WEAPON},
    ]
