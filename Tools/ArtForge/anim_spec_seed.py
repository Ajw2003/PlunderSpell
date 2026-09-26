#!/usr/bin/env python3
"""One-off seed that wrote the first Tools/ArtForge/anim_spec.json (plan A0, 2026-09-24).

Kept as the paper trail of how every JSON clip was mapped. The JSON is the source of
truth now: edit anim_spec.json, not this, and check it with anim_spec_check.py.
Rerunning overwrites the JSON (including anything AnimForge wrote back into it), so it
refuses unless given --force.
"""
import json, re, os, sys
if "--force" not in sys.argv:
    raise SystemExit("anim_spec_seed.py would overwrite Tools/ArtForge/anim_spec.json; "
                     "edit the JSON instead, or pass --force to regenerate it from scratch")

REPO = "/home/user/PlunderSpell"
AGES = ["bronze", "high", "late", "powder"]
FS = "Footstep"; HIT = "AttackHit"; REL = "ProjectileRelease"; DET = "PropDetach"

def ev(kind, t, **param):
    d = {"type": kind, "t": round(t, 3)}
    if param:
        d["param"] = param
    return d

# ----------------------------------------------------------------------------------
# Authored clips: what AnimForge builds. status a1 = built in phase A1 (this pass).
# length in seconds; locomotion lengths are the gait cycle at authored speed.
# ----------------------------------------------------------------------------------
C = {}
def clip(cid, layer, fbx, length, loop, events=(), family=None, status="planned",
         speed=None, additive=False, notes="", enemy=None):
    C[cid] = {"layer": layer, "family": family, "fbx": fbx, "length_s": length, "loop": loop,
              "additive": additive, "speed_mps": speed, "events": list(events),
              "status": status, "enemy": enemy, "notes": notes}

B = "Humanoid_Base.fbx"
# Base (A1): lengths for gait clips are rewritten by the forge from the gait solver
# (anim_forge writes the built value back into the spec's "built" block).
clip("idle", "base", B, 3.0, True, status="a1", notes="weight shift L->R->L, breathing, head glance")
clip("walk_slow", "base", B, 1.2, True, [ev(FS, 0.0, foot="L"), ev(FS, 0.5, foot="R")],
     status="a1", speed=1.1, notes="the JSON patrol pace (walk_round 1.1 m/s); heavies' 0.9-1.5 m/s blends between walk_slow and walk")
clip("walk", "base", B, 1.0, True, [ev(FS, 0.0, foot="L"), ev(FS, 0.5, foot="R")],
     status="a1", speed=2.0, notes="engine patrol agent speed (role table: patrol 2.0 m/s)")
clip("run", "base", B, 0.7, True, [ev(FS, 0.0, foot="L"), ev(FS, 0.5, foot="R")],
     status="a1", speed=4.2, notes="engine chase agent speed (role table: patrol chase 4.2 m/s)")
clip("alert_turn", "base", B, 0.6, False, [ev(FS, 0.55, foot="L")], status="a1",
     notes="head leads, chest follows, hips last; authored turning LEFT, mirror for right. The agent does the actual yaw over the same 0.6 s")
clip("shout", "base", B, 1.2, False, status="a1", notes="cups the left hand to the mouth, chest out, bellows. The noise is gameplay (driver), not a clip event")
clip("hit_react", "base", B, 0.4, False, status="a1", additive=True, notes="additive flinch over any state")
clip("stagger", "base", B, 0.8, False, [ev(FS, 0.45, foot="R"), ev(FS, 0.7, foot="L")], status="a1",
     notes="full-body recoil, two catch steps back")
clip("knock_down", "base", B, 1.2, False, status="a1", notes="non-lethal fall onto the back (stun, knock-down); killed/thrown guards use the ragdoll")
clip("get_up", "base", B, 2.4, False, [ev(FS, 0.8, foot="R")], status="a1", notes="from knock_down's last pose to standing")
clip("archway_duck", "base", B, 0.5, False, status="a1", additive=True, notes="dips head and shoulders ~0.12 m; additive over walk")
clip("sleep", "base", B, 4.0, True, status="a1", notes="Somnus: dozes on his feet, knees soft, chin on chest, slow breathing")
clip("levitate_struggle", "base", B, 1.2, True, status="a1", notes="Levo: held off the ground, legs kick, arms flail")
# Base, planned for later phases
clip("alert_turn_body", "base", B, 1.0, False, [ev(FS, 0.3, foot="L"), ev(FS, 0.7, foot="R")],
     notes="whole-body turn for helmed heavies who cannot turn the head")
clip("shrug", "base", B, 0.5, False, additive=True, notes="small additive rock for hits that do not stagger")
clip("walk_back", "base", B, 1.2, True, [ev(FS, 0.0, foot="L"), ev(FS, 0.5, foot="R")], speed=1.0,
     notes="backs away facing the threat")
clip("strafe", "base", B, 0.8, True, [ev(FS, 0.0, foot="L"), ev(FS, 0.5, foot="R")], speed=1.4,
     notes="sidestep left; mirror for right")
clip("crouch_cover", "base", B, 2.0, True, notes="crouched to 1.25 m behind cover, peeks")
clip("cower", "base", B, 2.0, True, notes="crouched, arms over the head")
clip("kneel", "base", B, 1.5, False, notes="drops to one knee (heavy stagger, stagger_back)")

# Polearm family (A1)
P = "Humanoid_Polearm.fbx"
clip("polearm_guard", "family", P, 2.0, True, family="polearm", status="a1",
     notes="two-handed ready stance, haft levelled at the hip, point at chest height; the family's rest pose")
clip("polearm_thrust", "family", P, 0.9, False, [ev(HIT, 0.5), ev(FS, 0.42, foot="L")], family="polearm", status="a1",
     notes="draw back (anticipation) 0-0.38, lunge-thrust peak 0.5, overshoot, recover")
clip("polearm_sweep", "family", P, 1.1, False, [ev(HIT, 0.5), ev(FS, 0.4, foot="L")], family="polearm", status="a1",
     notes="wind-up to the right, wide horizontal cut at knee height across to the left")
clip("polearm_hook", "family", P, 1.2, False, [ev(HIT, 0.42), ev(FS, 0.35, foot="L"), ev(FS, 0.78, foot="L")],
     family="polearm", status="a1", notes="reach past, hook (hit) at 0.42, drag back 1 m by 0.8")
clip("polearm_chop", "family", P, 1.1, False, [ev(HIT, 0.55), ev(FS, 0.48, foot="L")], family="polearm", status="a1",
     notes="raise overhead (0.4 s tell), chop down")
clip("polearm_cant", "family", P, 0.4, False, family="polearm", status="a1", additive=True,
     notes="additive: tilts the pole 35 deg forward from the carry (doors, vault ribs, archways)")
clip("polearm_cant_back", "family", P, 0.4, False, family="polearm", additive=True,
     notes="additive: tilts a sloped pole back so its tip drops (halberd under arches)")
clip("polearm_thrust_overhand", "family", P, 0.7, False, [ev(HIT, 0.5)], family="polearm",
     notes="overhand spear jab over a shield")
# Sword & shield
S = "Humanoid_SwordShield.fbx"
for cid, L, evs, n in [
    ("shield_guard", 3.0, [], "shield forward, weapon over the rim (loop)"),
    ("sword_cut", 0.8, [ev(HIT, 0.5)], "forehand cut"),
    ("sword_overhead", 1.2, [ev(HIT, 0.6)], "heavy downward cut, 0.4 s wind-up tell"),
    ("sword_lunge", 1.1, [ev(HIT, 0.55), ev(FS, 0.45, foot="L")], "long thrust with full-body lean"),
    ("shield_bash", 0.8, [ev(HIT, 0.45), ev(FS, 0.35, foot="L")], "steps in and shoves with the shield"),
    ("shield_brace", 2.0, [], "braced behind the shield (loop)"),
    ("draw_sword", 0.6, [], "draws from the hip")]:
    clip(cid, "family", S, L, cid in ("shield_guard", "shield_brace"), evs, family="sword_shield", notes=n)
X = "Humanoid_Crossbow.fbx"
for cid, L, evs, n in [
    ("crossbow_aim", 1.2, [], "raise to the shoulder, then hold (loop the last 0.2 s)"),
    ("crossbow_shoot", 0.3, [ev(REL, 0.1)], "release and recoil"),
    ("crossbow_span", 6.0, [ev(FS, 0.1, foot="R")], "tip down, foot in stirrup, belt-hook span, seat bolt"),
    ("crossbow_abort", 0.8, [], "abandons the span, draws a knife"),
    ("crossbow_butt", 0.8, [ev(HIT, 0.5)], "clubs with the tiller")]:
    clip(cid, "family", X, L, False, evs, family="crossbow", notes=n)
Sl = "Humanoid_Sling.fbx"
for cid, L, evs, n in [
    ("sling_reload", 0.9, [], "dips into the pouch, fits a stone"),
    ("sling_windup", 1.2, [], "two overhead whirls"),
    ("sling_release", 0.4, [ev(REL, 0.45)], "underarm-to-overarm release")]:
    clip(cid, "family", Sl, L, False, evs, family="sling", notes=n)
G = "Humanoid_LongGun.fbx"
for cid, L, evs, n in [
    ("gun_brace_aim", 1.2, [], "rests the gun, sights along the barrel (loop the hold)"),
    ("gun_fire", 0.8, [ev(REL, 0.5)], "match to the pan, hang-fire, bang, recoil 0.2 m"),
    ("gun_reload", 9.0, [], "swab, powder, ram, prime (interruptible; the handgunner loops it to 40 s)"),
    ("gun_club", 1.0, [ev(HIT, 0.55)], "swings the gun by the stock"),
    ("gun_plant_rest", 0.8, [], "spikes the musket rest into the floor")]:
    clip(cid, "family", G, L, cid == "gun_reload", evs, family="long_gun", notes=n)
Pi = "Humanoid_Pistol.fbx"
clip("pistol_draw", "family", Pi, 0.5, False, family="pistol", notes="butt-first from the holster")
clip("pistol_fire", "family", Pi, 0.3, False, [ev(REL, 0.15)], family="pistol", notes="no warning glow, flash")
T = "Humanoid_Throw.fbx"
clip("throw_unhook", "family", T, 0.5, False, family="throw", notes="frees the missile from the yoke/belt")
clip("throw_lob", "family", T, 0.8, False, [ev(REL, 0.6)], family="throw", notes="overarm/underarm lob; 10 m, 2 m arc")

# ----------------------------------------------------------------------------------
# Carry poses (one per human enemy): upper-body mask over base idle/walk/run.
# ----------------------------------------------------------------------------------
CARRY = {
    "palace-levy": "spear upright at the right shoulder, shield slung across the back",
    "wall-slinger": "sling dangling from the right hand, left hand free",
    "dendra-champion": "rapier low in the right hand, arms held out by the cuirass",
    "flame-keeper": "censer in the right hand, yoke of fire-pots across the shoulders",
    "lantern-warden": "glaive upright in the right fist (butt just off the floor), lantern hanging from the left fist",
    "castle-crossbowman": "crossbow held low at the port, thumb on the nut",
    "household-knight": "shield up on the left arm, sword sheathed/low",
    "sallet-halberdier": "halberd at the slope on the right shoulder",
    "handgunner": "handgonne on the right shoulder, match hand held away",
    "gothic-knight": "poleaxe grounded/upright in both hands",
    "pavisier": "pavise carried upright at the left side, hand on the rim",
    "partisan-guard": "partisan sloped on the right shoulder, left hand on the sword hilt",
    "musketeer": "musket shouldered, rest trailing from the left wrist",
    "cuirassier": "arms held out by the plate, hands free near the holsters",
    "petardier": "madrier plank on the back, stooped, hands on the straps",
}
# The warden's carry poses are built in A1: a 1 s held pose each, in its own FBX with
# the signature clips (they key its prop bones: Glaive, LanternRing, LanternBody).
clip("warden_carry", "carry", "LanternWarden_Signature.fbx", 1.0, True, status="a1",
     enemy="lantern-warden", notes="upper-body mask pose: glaive upright in the right fist, lantern hanging from the left")
clip("warden_carry_run", "carry", "LanternWarden_Signature.fbx", 1.0, True, status="a1",
     enemy="lantern-warden", notes="upper-body mask pose for run_to_noise: lantern held out in front, glaive angled forward")

# ----------------------------------------------------------------------------------
# Per-enemy mapping: clip -> (source, layer, extra)
#   source: authored clip id, "carry:<base>" (base clip + that enemy's carry pose),
#           "ragdoll" (dropped as a clip), "sig" (the enemy's own signature clip)
# ----------------------------------------------------------------------------------
RAGDOLL = ("ragdoll (decision 3): a killed guard switches to the ragdoll; the driver fires the "
           "death consequences (props drop, sparks) at the switch, so no clip is authored")
M = {
 "palace-levy": {
    "idle_leaning": ("carry:idle", {"carry": "levy_leaning", "note": "idle + a leaning carry variant (spear as a prop, shield rim grounded)"}),
    "patrol_walk": ("carry:walk", {}),
    "alert_turn": ("alert_turn", {}),
    "shout_alarm": ("shout", {}),
    "guard_stance": ("shield_guard", {}),
    "thrust": ("polearm_thrust_overhand", {}),
    "shield_bash": ("shield_bash", {}),
    "archway_duck": ("polearm_cant", {"note": "dips the spear point: the polearm cant, not a body duck (1.70 m man, 2.59 m arch)"}),
    "stagger_hit": ("stagger", {}),
    "death_fall": ("ragdoll", {}),
    "flee_panic": ("carry:run", {"carry": "levy_panic", "events_extra": [ev(DET, 0.0, prop="Shield")],
                    "note": "run + a panic carry (spear trailing); shield dropped at entry"}),
 },
 "wall-slinger": {
    "idle_scan": ("carry:idle", {"note": "shading the eyes is the carry variant's left arm"}),
    "patrol_walk": ("carry:walk", {}),
    "alert_turn": ("alert_turn", {"note": "ends crouched: blend to crouch_cover"}),
    "reload": ("sling_reload", {}),
    "sling_windup": ("sling_windup", {}),
    "sling_release": ("sling_release", {}),
    "strafe_left": ("strafe", {}),
    "strafe_right": ("strafe", {"mirror": True}),
    "retreat_run": ("carry:run", {}),
    "vault_parapet": ("sig", {"length": 0.9, "events": [ev(FS, 0.1, foot="R"), ev(FS, 0.85, foot="L")],
                      "note": "one-handed hop over 1.10 m; needs root motion over the parapet (the agent's off-mesh link moves him)"}),
    "hit_flinch": ("hit_react", {}),
    "death_fall": ("ragdoll", {"note": "the pouch spills stones as physics props at the switch"}),
 },
 "dendra-champion": {
    "idle_heavy": ("carry:idle", {}),
    "walk_slow": ("carry:walk_slow", {"note": "1.1 m/s max: walk_slow at 1.0x"}),
    "alert_turn": ("alert_turn_body", {}),
    "rapier_lunge": ("sword_lunge", {}),
    "rapier_cut": ("sword_cut", {}),
    "shield_brace": ("shield_brace", {}),
    "shrug_impact": ("shrug", {}),
    "sideways_aisle": ("sig", {"length": 1.8, "loop": True, "events": [ev(FS, 0.0, foot="L"), ev(FS, 0.5, foot="R")],
                       "note": "side-on shuffle through aisles < 1.2 m; strafe gait with the cuirass square"}),
    "heavy_stagger": ("stagger", {}),
    "kneel_get_up": ("get_up", {"note": "3 s: get_up at 0.8x from the kneel"}),
    "death_topple": ("ragdoll", {}),
 },
 "flame-keeper": {
    "idle_tend": ("carry:idle", {"note": "slow censer arcs are the carry's right arm (a swing prop, later a damped transform)"}),
    "procession_walk": ("carry:walk_slow", {}),
    "alert_turn": ("alert_turn", {}),
    "chant_alarm": ("shout", {"note": "censer raised overhead: the carry variant; 2 s = shout at 0.6x"}),
    "unhook_pot": ("throw_unhook", {"events_extra": [ev(DET, 0.7, prop="FirePot")]}),
    "throw_pot": ("throw_lob", {}),
    "censer_swing": ("sig", {"length": 0.9, "events": [ev(HIT, 0.5)], "note": "1.5 m ember arc with the censer"}),
    "crypt_duck": ("archway_duck", {}),
    "retreat_walk": ("walk_back", {}),
    "hit_flinch": ("hit_react", {}),
    "death_fall": ("ragdoll", {"note": "hung pots break and ignite at the switch"}),
 },
 "lantern-warden": {
    "idle_lantern": ("carry:idle", {"note": "the concept holds the glaive upright; the JSON's 'rests on the shoulder' is not used (see the carry pose)"}),
    "walk_round": ("carry:walk", {"note": "JSON 1.1 m/s vs engine patrol 2.0 m/s: walk at the agent speed; walk_slow covers 1.1"}),
    "alert_turn": ("alert_turn", {}),
    "lantern_raise_search": ("sig", {"length": 2.5, "status": "a1"}),
    "cant_glaive": ("polearm_cant", {}),
    "shout_alarm": ("shout", {}),
    "run_to_noise": ("carry:run", {"carry": "warden_carry_run", "note": "JSON 3.4 m/s vs engine chase 4.2 m/s: run at the agent speed; lantern held in front"}),
    "attack_glaive_thrust": ("polearm_thrust", {"events_extra": [ev(DET, 0.08, prop="LanternRing")],
                             "note": "the lantern is let go as the left hand goes to the haft; the driver drops the prop once per engagement"}),
    "attack_glaive_sweep": ("polearm_sweep", {}),
    "hit_react": ("stagger", {"note": "0.5 s stagger: stagger at 1.6x"}),
    "death_drop_lantern": ("sig", {"length": 1.6, "status": "a1",
                           "events": [ev(DET, 0.16, prop="LanternRing"), ev(FS, 0.3, foot="R")],
                           "note": "the one death authored as a clip: it lets go of the lantern (PropDetach) and crumples; the ragdoll may take over from 0.5"}),
 },
 "castle-crossbowman": {
    "idle_ready": ("carry:idle", {}),
    "walk_patrol": ("carry:walk", {}),
    "alert_turn": ("alert_turn", {}),
    "aim_hold": ("crossbow_aim", {}),
    "shoot": ("crossbow_shoot", {}),
    "reload_span": ("crossbow_span", {}),
    "reload_interrupted": ("crossbow_abort", {}),
    "strafe_to_loop": ("strafe", {}),
    "melee_bow_butt": ("crossbow_butt", {}),
    "hit_react": ("stagger", {}),
    "death_forward": ("ragdoll", {"note": "the bow skitters away at the switch"}),
 },
 "household-knight": {
    "idle_guard": ("carry:idle", {}),
    "wake_slow": ("get_up", {"note": "wakes by degrees: get_up's second half + draw_sword; 2 s"}),
    "walk_heavy": ("carry:walk_slow", {}),
    "alert_turn": ("alert_turn_body", {}),
    "advance_shield": ("carry:walk", {"carry": "knight_shield_forward"}),
    "attack_overhead": ("sword_overhead", {}),
    "attack_shield_bash": ("shield_bash", {}),
    "duck_door": ("archway_duck", {}),
    "stagger_heavy": ("stagger", {}),
    "helm_knocked_off": ("sig", {"length": 0.9, "events": [ev(DET, 0.1, prop="Helm")], "note": "clutches his head"}),
    "death_kneel": ("ragdoll", {"note": "kneel-then-topple could become a signature clip later; ragdoll for now"}),
 },
 "alaunt-hound": {
    "sleep_curl": ("hound", {"length": 4.0, "loop": True}),
    "wake_to_noise": ("hound", {"length": 0.8}),
    "sniff_track": ("hound", {"length": 0.9, "loop": True, "speed": 1.4,
                    "events": [ev(FS, 0.0), ev(FS, 0.25), ev(FS, 0.5), ev(FS, 0.75)]}),
    "alert_point": ("hound", {"length": 0.6}),
    "bark_alarm": ("hound", {"length": 1.5}),
    "gallop": ("hound", {"length": 0.45, "loop": True, "speed": 7.5, "events": [ev(FS, 0.0), ev(FS, 0.5)]}),
    "stairs_run": ("hound", {"length": 0.5, "loop": True, "speed": 5.0, "events": [ev(FS, 0.0), ev(FS, 0.5)]}),
    "lunge_bite": ("hound", {"length": 0.5, "events": [ev(HIT, 0.7)]}),
    "worry_shake": ("hound", {"length": 1.2, "events": [ev(HIT, 0.5)]}),
    "yelp_hit": ("hound", {"length": 0.4}),
    "death_fall": ("hound", {"length": 1.3, "note": "the hound has no ragdoll in the engine plan yet: a clip"}),
 },
 "sallet-halberdier": {
    "idle_slope": ("carry:idle", {"carry": "halberdier_grounded"}),
    "walk_round": ("carry:walk", {}),
    "slope_halberd_arch": ("polearm_cant_back", {}),
    "alert_turn": ("alert_turn", {}),
    "call_partner": ("shout", {}),
    "search_pair": ("sig", {"length": 3.0, "note": "sweeps the halberd tip low through shadows"}),
    "run_to_noise": ("carry:run", {"carry": "halberdier_levelled"}),
    "attack_thrust": ("polearm_thrust", {}),
    "attack_hook": ("polearm_hook", {}),
    "attack_chop": ("polearm_chop", {}),
    "hit_react": ("stagger", {}),
    "death_fall": ("ragdoll", {"note": "the halberd falls clear at the switch"}),
 },
 "handgunner": {
    "idle_shoulder": ("carry:idle", {}),
    "walk_post": ("carry:walk", {}),
    "alert_turn": ("alert_turn", {}),
    "brace_and_aim": ("gun_brace_aim", {}),
    "fire": ("gun_fire", {}),
    "reload": ("gun_reload", {}),
    "call_pavisier": ("shout", {}),
    "retreat_behind_pavise": ("crouch_cover", {}),
    "club": ("gun_club", {}),
    "hit_react": ("stagger", {"events_extra": [ev(DET, 0.3, prop="Match")], "note": "the match may drop (breakables)"}),
    "death_fall": ("ragdoll", {"note": "the lit match drops at the switch"}),
 },
 "gothic-knight": {
    "idle_stand": ("carry:idle", {}),
    "walk_heavy": ("carry:walk_slow", {}),
    "alert_turn_slow": ("alert_turn_body", {}),
    "visor_raise": ("sig", {"length": 0.8, "note": "pushes the visor up (Visor bone)"}),
    "charge": ("carry:run", {"carry": "gothic_levelled", "note": "3.0 m/s: run at 0.71x playback"}),
    "attack_spike_thrust": ("polearm_thrust", {}),
    "attack_hammer": ("polearm_chop", {}),
    "attack_axe_sweep": ("polearm_sweep", {}),
    "shrug_spell": ("shrug", {}),
    "hit_react_heavy": ("stagger", {}),
    "stand_up": ("get_up", {}),
    "death_topple": ("ragdoll", {}),
 },
 "pavisier": {
    "idle_shield": ("carry:idle", {}),
    "walk_carry": ("carry:walk", {"carry": "pavisier_front"}),
    "alert_turn": ("alert_turn", {}),
    "plant_pavise": ("sig", {"length": 1.0, "events": [ev(DET, 0.6, prop="Pavise"), ev(FS, 0.55, foot="R")],
                     "note": "stamps the spikes in; the pavise becomes world cover"}),
    "crouch_behind": ("crouch_cover", {}),
    "advance_pavise": ("sig", {"length": 1.6, "loop": True, "events": [ev(FS, 0.3, foot="L"), ev(FS, 0.8, foot="R")],
                       "note": "lift, step 1 m, re-plant"}),
    "shove": ("shield_bash", {}),
    "draw_falchion": ("draw_sword", {}),
    "attack_falchion": ("sword_cut", {}),
    "hit_react": ("hit_react", {}),
    "death_fall": ("ragdoll", {"note": "a planted pavise stays standing"}),
 },
 "partisan-guard": {
    "idle_shoulder": ("carry:idle", {}),
    "patrol_walk": ("carry:walk", {}),
    "raise_lantern": ("sig", {"length": 0.8, "note": "unhooks the lantern to eye height: shares the warden's lantern_raise pose"}),
    "alert_turn": ("alert_turn", {}),
    "challenge_shout": ("shout", {}),
    "charge_run": ("carry:run", {"carry": "partisan_levelled"}),
    "thrust": ("polearm_thrust", {}),
    "sweep": ("polearm_sweep", {}),
    "archway_tilt": ("polearm_cant", {}),
    "hit_react": ("hit_react", {"events_extra": [ev(DET, 0.2, prop="Morion")], "note": "the morion can fly off (driver decides)"}),
    "death_fall": ("ragdoll", {}),
 },
 "musketeer": {
    "idle_carry": ("carry:idle", {}),
    "patrol_walk": ("carry:walk", {}),
    "alert_turn": ("alert_turn", {}),
    "plant_rest": ("gun_plant_rest", {}),
    "aim": ("gun_brace_aim", {}),
    "fire_volley": ("gun_fire", {}),
    "reload": ("gun_reload", {}),
    "club_melee": ("gun_club", {}),
    "draw_sword": ("draw_sword", {"events_extra": [ev(DET, 0.1, prop="Musket")]}),
    "hit_react": ("hit_react", {"events_extra": [ev(DET, 0.2, prop="Hat")]}),
    "death_fall": ("ragdoll", {"note": "the match drops lit at the switch"}),
 },
 "cuirassier": {
    "idle_heavy": ("carry:idle", {}),
    "patrol_walk": ("carry:walk_slow", {}),
    "alert_turn": ("alert_turn_body", {}),
    "draw_pistol": ("pistol_draw", {}),
    "pistol_fire": ("pistol_fire", {}),
    "draw_sword": ("draw_sword", {}),
    "overhead_cut": ("sword_overhead", {}),
    "shoulder_barge": ("sig", {"length": 0.8, "events": [ev(HIT, 0.5), ev(FS, 0.2, foot="L"), ev(FS, 0.45, foot="R")],
                       "note": "2 m lunge; the agent moves him"}),
    "archway_duck": ("archway_duck", {}),
    "stagger_back": ("kneel", {}),
    "death_fall": ("ragdoll", {}),
 },
 "petardier": {
    "idle_burdened": ("carry:idle", {}),
    "walk_burdened": ("carry:walk", {}),
    "alert_turn": ("alert_turn", {}),
    "plant_petard": ("sig", {"length": 3.0, "events": [ev(DET, 0.6, prop="Petard")], "note": "unslings, hooks, lights"}),
    "flee_run": ("carry:run", {"carry": "petardier_flee"}),
    "throw_grenado": ("throw_lob", {}),
    "cower": ("cower", {}),
    "hit_react": ("hit_react", {}),
    "self_detonate": ("sig", {"length": 1.5, "note": "ends in the ragdoll/gib; no event (the blast is gameplay)"}),
    "death_fall": ("ragdoll", {}),
 },
}

ROLE_SPEED = {"patrol": (2.0, 4.2), "ranged": (1.9, 3.6), "heavy": (1.5, 3.2)}
# Specials have a per-enemy row in the engine plan; until it exists, the JSON's own
# speeds (walk 1.0 m/s, the petardier's flee 3.6) or a 1.0 m/s stately walk.
SPECIAL_SPEED = {"flame-keeper": (1.0, 3.0), "pavisier": (1.0, 3.2), "petardier": (1.0, 3.6)}
FAMILY_OF = {"palace-levy": "polearm", "wall-slinger": "sling", "dendra-champion": "sword_shield",
             "flame-keeper": "throw", "lantern-warden": "polearm", "castle-crossbowman": "crossbow",
             "household-knight": "sword_shield", "alaunt-hound": None, "sallet-halberdier": "polearm",
             "handgunner": "long_gun", "gothic-knight": "polearm", "pavisier": "sword_shield",
             "partisan-guard": "polearm", "musketeer": "long_gun", "cuirassier": "pistol",
             "petardier": "throw"}

def names_of(text):
    head = re.split(r" — ", text, maxsplit=1)[0]
    return [n.strip() for n in head.split("/")]

def json_length(text):
    m = re.search(r"— ([0-9.]+) s", text)
    return float(m.group(1)) if m else None

def json_speed(text):
    m = re.search(r"([0-9.]+) m/s", text)
    return float(m.group(1)) if m else None

enemies = {}
for age in AGES:
    data = json.load(open(f"{REPO}/docs/art/data/{age}.json"))
    for e in data["enemies"]:
        slug = e["slug"]
        table = M[slug]
        rows = []
        seen = set()
        for text in e["rig"]["animations"]:
            for name in names_of(text):
                if name not in table:
                    raise SystemExit(f"unmapped {slug}/{name}")
                seen.add(name)
                src, x = table[name]
                row = {"clip": name, "json": text}
                loop = "loop" in text.split("—", 1)[-1][:25] or bool(re.search(r"cycle|m/s", text.split("—",1)[-1][:20]))
                jl, js = json_length(text), json_speed(text)
                if src == "ragdoll":
                    row.update(layer="base", family=None, source=None, status="dropped",
                               reason=RAGDOLL + ("; " + x["note"] if "note" in x else ""), loop=False,
                               length_s=jl, events=[])
                elif src == "hound":
                    row.update(layer="hound", family=None, source=f"hound_{name}", status="planned",
                               loop=x.get("loop", False), length_s=x["length"], speed_mps=x.get("speed"),
                               events=x.get("events", []), fbx="AlauntHound_Generic.fbx")
                    if "note" in x: row["note"] = x["note"]
                    clip(f"hound_{name}", "hound", "AlauntHound_Generic.fbx", x["length"], x.get("loop", False),
                         x.get("events", []), speed=x.get("speed"), enemy=slug, notes=x.get("note", ""))
                elif src == "sig":
                    cid = f"{slug.split('-')[0]}_{name}" if not slug.startswith("lantern") else name
                    status = x.get("status", "planned")
                    clip(cid, "signature", f"{e['name'].replace(' ', '').replace('-', '')}_Signature.fbx",
                         x["length"], x.get("loop", False), x.get("events", []), status=status,
                         enemy=slug, notes=x.get("note", ""))
                    row.update(layer="signature", family=None, source=cid, status=status,
                               loop=x.get("loop", False), length_s=x["length"],
                               events=x.get("events", []))
                    if "note" in x: row["note"] = x["note"]
                else:
                    carry = None
                    if src.startswith("carry:"):
                        src = src.split(":", 1)[1]
                        carry = x.get("carry", f"{slug.split('-')[0]}_carry")
                        if slug == "lantern-warden" and carry == "lantern_carry":
                            carry = "warden_carry"
                    if slug == "lantern-warden" and carry == "lantern_carry":
                        carry = "warden_carry"
                    s = C[src]
                    layer = "carry" if carry else s["layer"]
                    length = jl if jl else s["length_s"]
                    loop = s["loop"]
                    row.update(layer=layer, family=s["family"], source=src, status=s["status"], loop=loop)
                    if carry:
                        row["carry"] = carry
                        if not carry.startswith("warden"):
                            row["status"] = "planned"
                            row["status_why"] = f"source {src} is {s['status']}; carry pose {carry} is planned"
                    if s["speed_mps"]:
                        pw, ch = ROLE_SPEED.get(e["role"], SPECIAL_SPEED.get(slug, (None, None)))
                        agent = (ch if src == "run" else pw) if pw else js
                        if slug == "gothic-knight" and name == "charge": agent = 3.0
                        if slug == "dendra-champion": agent = 1.1
                        row["json_speed_mps"] = js
                        row["agent_speed_mps"] = agent
                        row["playback_rate"] = round(agent / s["speed_mps"], 3) if agent else None
                        row["length_s"] = None   # the built gait cycle / playback_rate
                    else:
                        row["length_s"] = length
                        row["playback_rate"] = round(s["length_s"] / length, 3)
                    if x.get("mirror"):
                        row["mirror"] = True
                    evs = [dict(v) for v in s["events"]] + x.get("events_extra", [])
                    row["events"] = sorted(evs, key=lambda v: v["t"])
                    if "note" in x: row["note"] = x["note"]
                rows.append(row)
        if set(table) - seen:
            raise SystemExit(f"{slug}: mapping names clips the JSON lacks: {set(table)-seen}")
        enemies[slug] = {"age": age, "name": e["name"], "role": e["role"], "height_m": e["height_m"],
                         "family": FAMILY_OF[slug],
                         "carry": ({"pose": ("warden_carry" if slug == "lantern-warden" else f"{slug.split('-')[0]}_carry"),
                                    "describes": CARRY[slug],
                                    "status": "a1" if slug == "lantern-warden" else "planned"}
                                   if slug in CARRY else None),
                         "clips": rows}

# Carry pose variants referenced above
variants = sorted({r["carry"] for en in enemies.values() for r in en["clips"] if r.get("carry")})

spec = {
  "about": ("AnimForge clip taxonomy (plan docs/plans/artbible-enemy-animations.md, phase A0). "
            "'clips' = what AnimForge authors; 'enemies' = every clip each art-bible JSON "
            "(docs/art/data/*.json, enemies[].rig.animations) asks for, mapped to a source clip or "
            "dropped with a reason. Event t is normalized (0..1) clip time. Check coverage with "
            "python3 Tools/ArtForge/anim_spec_check.py."),
  "fps": 30,
  "root_motion": "none: every clip is in place; the NavMeshAgent moves the guard (decision 'Motion')",
  "event_types": {
      "Footstep": "a foot plants; drives FootstepNoiseEmitter (param foot L/R)",
      "AttackHit": "the frame damage applies",
      "ProjectileRelease": "the missile leaves the hand/weapon",
      "PropDetach": "a prop leaves the rig (param prop = bone name): lantern, helm, shield, match"},
  "layers": {
      "base": "shared humanoid motion on the reference skeleton (Humanoid_Base.fbx)",
      "carry": "a base clip with the enemy's carry pose on the upper-body mask (arms, shoulders, prop bones)",
      "family": "one weapon-family clip shared by every enemy with that weapon, on the reference skeleton",
      "signature": "a motion only one enemy has, in that enemy's own FBX (moves its prop bones)",
      "hound": "the hound's Generic rig"},
  "playback": ("An enemy clip plays its source at playback_rate. Locomotion: rate = agent speed / authored "
               "speed, which keeps the feet planted; the Animator speed parameter should drive it."),
  "clips": C,
  "carry_variants": variants,
  "enemies": enemies,
}
out = f"{REPO}/Tools/ArtForge/anim_spec.json"
with open(out, "w", encoding="utf-8") as f:
    json.dump(spec, f, indent=1, ensure_ascii=False)
    f.write("\n")
n = sum(len(e["clips"]) for e in enemies.values())
print("wrote", out, n, "enemy clips,", len(C), "authored clips")
