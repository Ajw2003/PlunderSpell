"""Writes manifest.csv: every sound in docs/plans/audio.md as one row.

The CSV is the file every other AudioForge script reads. This script exists because about 200 of
its rows are the same pattern repeated per Age or per guard voice, which is safer to expand in
code than to type out. Edit the lists here, then run:

    python3 Tools/AudioForge/manifest_source.py

Recipe syntax (read by forge/build.py):
    synth:<recipe> key=value ...          a function in forge/recipes.py
    kenney:<pack>/<glob> [gain=dB] [shift=ratio]   a CC0 file in library/kenney/<pack>/Audio/
    music:raid age=<age> layer=<layer>    forge/music.py
    music:theme variant=<name>
    music:sting kind=<kind> [age=<age>]
    Several layers are summed with " + ".

`final` is where the shipped version comes from (docs/plans/audio.md §7.1):
    G generated here · L library · A AI text-to-sound · R recorded · C composed · M AI music
"""

import csv
from pathlib import Path

ROOT = Path(__file__).resolve().parent
AGES = ("bronze", "high", "late", "powder")

rows = []


def add(name, variants, folder, bus, spatial, loop, final, noise, recipe, brief, ai_prompt=""):
    rows.append(dict(name=name, variants=variants, folder=folder, bus=bus, spatial=spatial,
                     loop=int(loop), final=final, noise=noise, recipe=recipe, brief=brief,
                     ai_prompt=ai_prompt))


def ui(name, n, recipe, brief, final="L"):
    add(name, n, "UI", "UI", "2d", 0, final, "none", recipe, brief)


def sfx(name, n, folder, recipe, brief, final="L", noise="low", bus="SFX/World", spatial="3d",
        loop=0, ai=""):
    add(name, n, folder, bus, spatial, loop, final, noise, recipe, brief, ai)


def amb(name, recipe, brief, final="L", spatial="2d", folder="Ambience", ai=""):
    add(name, 1, folder, "SFX/Ambience", spatial, 1, final, "none", recipe, brief, ai)


K = "kenney:"

# --- 3.1 UI ---------------------------------------------------------------------------------------
ui("ui_button_hover", 3, K + "ui-audio/rollover*", "a dry fingertip on vellum")
ui("ui_button_click", 3, K + "interface-sounds/click_*", "a quill tap on a wooden desk")
ui("ui_button_back", 1, K + "rpg-audio/bookFlip* shift=0.9", "a page flipped backwards")
ui("ui_button_denied", 1, "synth:thump dur=0.15 freq=140 click=0.6 decay=35", "a dull wooden knock", "G")
ui("ui_screen_open", 2, K + "rpg-audio/bookFlip*", "a page turning")
ui("ui_screen_close", 1, K + "rpg-audio/bookClose*", "a book closing softly")
ui("ui_slider_tick", 1, "synth:tick dur=0.04 freq=1800 noise=0.6", "a small ratchet click", "G")
ui("ui_toggle_on", 1, K + "rpg-audio/metalLatch*", "a latch opening")
ui("ui_toggle_off", 1, K + "rpg-audio/metalClick*", "a latch closing")
ui("ui_keybind_listen", 1, K + "rpg-audio/bookPlace* gain=-6", "a quill dipped in ink")
ui("ui_keybind_set", 1, "synth:thump dur=0.2 freq=110 click=0.4 decay=20", "a wax seal pressed", "L")
ui("ui_error", 1, K + "interface-sounds/error_00*", "torn parchment")
ui("ui_pause_open", 1, K + "rpg-audio/cloth* shift=0.8", "a heavy curtain drawn")
ui("ui_pause_close", 1, K + "rpg-audio/cloth* shift=0.7", "a heavy curtain drawn back")
ui("ui_lobby_join", 1, "synth:chime notes=880/1320 dur=0.6 gap=0.09", "a small handbell, rising", "G")
ui("ui_lobby_leave", 1, "synth:chime notes=1320/880 dur=0.6 gap=0.09", "a small handbell, falling", "G")
ui("ui_invite_received", 1, "synth:thump dur=0.2 freq=120 click=0.8 decay=25 + synth:thump dur=0.2 freq=118 click=0.8 decay=25",
   "a knock on a wooden door, twice")
ui("ui_ready", 1, K + "rpg-audio/metalPot* gain=-4", "a tankard set down")
ui("ui_hit_confirm", 3, "synth:tick dur=0.06 freq=700 decay=70 noise=0.5", "a very short thock under the world sound", "G")
ui("ui_hit_friendly", 2, "synth:boing dur=0.35 freq=260 + synth:thump dur=0.15 freq=200 click=0.8", "a comic wooden bonk", "G")
ui("ui_kill_confirm", 1, "synth:thump dur=0.4 freq=70 click=0.2 decay=10", "a low muted drum tap", "G")

# --- 3.2 Voice casting ----------------------------------------------------------------------------
S = "SFX/Spells"
sfx("sfx_voice_listen_open", 1, "SFX/Voice", "synth:breath dur=0.07 inhale=1 + synth:shimmer dur=0.07 root=880 attack=0.1",
    "under 80 ms: a soft intake of air plus a faint lapis shimmer", "G", "none", S, "2d")
sfx("sfx_voice_listen_close", 1, "SFX/Voice", "synth:shimmer dur=0.08 root=660 attack=0.02",
    "the shimmer cut off", "G", "none", S, "2d")
sfx("sfx_voice_teammate_tell", 3, "SFX/Voice", "synth:shimmer dur=0.35 root=330 attack=0.8 rise=1.5 + synth:breath dur=0.35 inhale=1",
    "a rising whispered lapis hum ~300 ms: the half-a-heartbeat warning", "A", "none", S,
    ai="a rising whispered magical hum, breathy voices swelling into a glassy shimmer, 0.4 seconds, dry, no reverb")
sfx("sfx_spell_fizzle", 3, "SFX/Spells", "synth:hiss dur=0.4 low=1500 high=6000 decay=8 + synth:whoosh dur=0.3 f0=800 f1=300",
    "a wet match; a puff of dust", "A", "low", S,
    ai="a damp match failing to light, weak fizzle and a small puff of dust, comic, 0.5 seconds")
sfx("sfx_spell_no_mana", 1, "SFX/Spells", "synth:hiss dur=0.25 low=2000 high=7000 decay=15 + synth:tick dur=0.05 freq=900",
    "a dry cough of sparks", "G", "none", S, "2d")
sfx("sfx_spell_mana_restored", 1, "SFX/Spells", "synth:chime notes=990/1480 dur=0.8 gap=0.1 decay=5",
    "a soft verdigris chime", "G", "none", S, "2d")

# --- 3.3 Spells -----------------------------------------------------------------------------------
def spell(name, n, recipe, brief, noise, ai, final="A", loop=0):
    sfx(name, n, "SFX/Spells", recipe, brief, final, noise, S, "3d", loop, ai)


LAPIS = " + synth:shimmer dur=0.3 root=520 attack=0.3 gain=-10"
spell("sfx_spell_ignis_cast", 3, "synth:whumph dur=0.25 size=2" + LAPIS, "a hard spit of ember, 180 ms", "low",
      "a short hard spit of fire, an ember flicked from a fingertip, magical, 0.3 seconds")
spell("sfx_spell_ignis_travel_loop", 1, "synth:crackle dur=3 density=60 body=0.3 + synth:hiss dur=3 low=1000 high=5000 flutter=9",
      "a small hiss and flutter", "low", "a small fireball flying, fluttering hiss and crackle, seamless loop", loop=1)
spell("sfx_spell_ignis_impact", 3, "synth:whumph dur=0.6 size=1.4 + synth:crackle dur=0.6 density=80 body=0",
      "a flame pop and crackle", "mid", "a fireball hitting stone, flame pop and crackle burst, 0.8 seconds")
spell("sfx_spell_ignis_misfire", 2, "synth:whumph dur=0.9 size=1.0 + synth:hiss dur=0.9 low=2000 high=8000 decay=3",
      "a whumph right in your face: your beard catches", "mid",
      "a beard suddenly catching fire right next to the ear, whoomph and sizzle, comic, 1 second")
spell("sfx_spell_frango_cast", 2, "synth:thump dur=0.3 freq=120 click=1.2 decay=20 + synth:shatter dur=0.3 pieces=10 low=2500" + LAPIS,
      "a plosive crack, a knuckle through stone", "mid", "a plosive magical crack like a fist punching through stone, 0.4 seconds")
spell("sfx_spell_frango_shatter_stone", 3, "synth:crumble dur=1.8 density=50", "stone splitting, then rubble", "high",
      "masonry wall splitting apart and collapsing into rubble, 2 seconds", "L")
spell("sfx_spell_frango_shatter_wood", 2, K + "impact-sounds/impactWood_heavy_* + synth:crumble dur=1.0 density=40 size=0.3 gain=-6",
      "timber splintering", "high", "", "L")
spell("sfx_spell_frango_shatter_metal", 2, "synth:metal_ring dur=1.5 freq=900 clank=1.0 + synth:thump dur=0.2 freq=200 click=1",
      "an iron snap and a ringing tail", "high", "an iron lock snapping apart, metallic crack with a ringing tail, 1.5 seconds", "L")
spell("sfx_spell_frango_misfire", 1, "synth:thump dur=0.3 freq=150 click=1.5 decay=20 + synth:shatter dur=0.8 pieces=50",
      "the crack right in your hands", "high", "a crack of magic in your hands and a precious object shattering, 1 second")
spell("sfx_spell_levo_cast", 2, "synth:shimmer dur=0.5 root=330 attack=0.6 rise=1.3 + synth:breath dur=0.5 inhale=1 gain=-6",
      "an open, rising breath-tone", "low", "an open rising airy magical tone, levitation spell, 0.5 seconds")
spell("sfx_spell_levo_hold_loop", 1, "synth:hum dur=4 freq=110 tremolo=3", "a hum; the game shifts its pitch by the held mass",
      "none", "", "G", 1)
spell("sfx_spell_levo_release", 1, "synth:whine dur=0.4 f0=220 f1=110 depth=0.01", "the hum drops away", "none", "", "G")
spell("sfx_spell_levo_misfire", 1, "synth:whine dur=1.5 f0=300 f1=900 wobble=6 depth=0.08", "a wobbling, rising whine",
      "low", "a wobbling rising whine of someone floating away out of control, comic, 1.5 seconds", "G")
spell("sfx_spell_aurumvoco_cast", 1, "synth:chime notes=1047/1568 dur=1.6 gap=0.34 decay=3 gold=1 + synth:shimmer dur=1.2 root=520 gain=-8",
      "a two-beat chord of distant bells: the gold timbre", "mid",
      "two beats of distant golden bells ringing, rich and warm, magical treasure reveal, 1.5 seconds")
spell("sfx_spell_aurumvoco_reveal_loop", 1, "synth:coins dur=4 count=14 spread=1.0 + synth:shimmer dur=4 root=1047 attack=0.5 gain=-10",
      "coins singing through the walls", "none", "", "G", 1)
spell("sfx_spell_aurumvoco_end", 1, "synth:chime notes=1568/1047 dur=1.2 gap=0.2 decay=4 gold=1", "the bells fading out", "none", "", "G")
spell("sfx_spell_aurumvoco_misfire", 2, "synth:coins dur=1.5 count=40 + synth:whine dur=1.2 f0=1800 f1=2600 wobble=11 depth=0.1",
      "a coin burst and a shrill metallic scream", "max",
      "a burst of gold coins exploding outwards with a shrill metallic screaming, comic, 1.5 seconds")
spell("sfx_spell_velox_cast", 1, "synth:whoosh dur=0.15 f0=2000 f1=6000 attack=0.1", "a snap of air", "low",
      "a sharp snap of air, 0.2 seconds")
spell("sfx_spell_velox_dash", 2, "synth:whoosh dur=0.35 f0=300 f1=4000 attack=0.3", "a whoosh past the ear", "low",
      "a fast whoosh past the ear, 0.4 seconds", "L")
spell("sfx_spell_velox_misfire", 1, "synth:whoosh dur=0.35 f0=300 f1=4000 + synth:thump dur=0.3 freq=80 click=0.8",
      "a whoosh then a thud", "mid", "a fast whoosh ending in a body thudding into a wall, comic, 0.8 seconds")
spell("sfx_spell_saltus_cast", 1, "synth:boing dur=0.3 freq=120", "a coiled spring", "low", "a coiled spring releasing, magical, 0.3 seconds")
spell("sfx_spell_saltus_launch", 1, "synth:whoosh dur=0.5 f0=200 f1=2500 attack=0.2", "a rising whoosh", "low", "", "L")
spell("sfx_spell_saltus_land_slam", 2, "synth:thump dur=0.6 freq=55 click=1.0 decay=7 + synth:crumble dur=0.8 density=30 size=0.5 gain=-8",
      "a flagstone boom and dust", "high", "a heavy body slamming onto flagstones, boom and dust, 1 second", "L")
spell("sfx_spell_saltus_misfire", 1, "synth:boing dur=0.5 freq=300 + synth:thump dur=0.2 freq=150 click=0.5",
      "a sad little boing, then a stumble", "low", "a feeble sad little boing spring sound and a stumble, comic, 0.8 seconds")
spell("sfx_spell_somnus_cast_soft", 2, "synth:breath dur=0.9 inhale=0 low=300 high=2500 + synth:shimmer dur=0.9 root=392 attack=0.2 tremolo=3 gain=-8",
      "a breathy lullaby exhale", "none", "a soft breathy lullaby exhale with a gentle magical shimmer, sleep spell, 1 second")
spell("sfx_spell_somnus_cast_loud", 1, "synth:horn freq=196 dur=0.8 brass=1.0 + synth:breath dur=0.9 inhale=0",
      "a harsh blare over the exhale", "high", "a sleep spell ruined by shouting: a harsh brassy blare over a breathy exhale, 1 second")
spell("sfx_spell_somnus_apply", 2, "synth:whine dur=0.7 f0=500 f1=250 depth=0.01 + synth:breath dur=0.7 inhale=0 gain=-6",
      "a soft down-pitched sigh", "none", "a soft falling sigh, someone drifting asleep, 0.7 seconds")
spell("sfx_spell_somnus_misfire", 1, "synth:breath dur=0.8 inhale=0 + synth:thump dur=0.3 freq=90 click=0.4",
      "a sigh, then a thump", "low", "a sleepy sigh then a body flopping onto the floor, comic, 1.2 seconds")
spell("sfx_spell_porta_cast", 1, "synth:shimmer dur=1.0 root=147 voices=7 attack=0.2 + synth:thump dur=0.6 freq=50 decay=6",
      "a final, heavy word-tone: a lapis tear", "high", "a deep final magical word-tone, cold and heavy, 1 second")
spell("sfx_spell_porta_open", 1, "synth:whoosh dur=2.0 f0=80 f1=3000 q=0.8 attack=0.3 + synth:shimmer dur=2 root=110 voices=7 attack=0.3",
      "fabric of time tearing, a low roar", "high", "the fabric of time tearing open into a portal, cold roar and glassy shimmer, 2.5 seconds")
spell("sfx_spell_porta_loop", 1, "synth:drone dur=8 freqs=110/164.8/220 tremolo=0.3 bright=2000", "a brighter lapis drone",
      "none", "", "G", 1)
spell("sfx_spell_porta_misfire", 1, "synth:thump dur=0.6 freq=90 click=0.9 decay=8 gain=-8 + synth:reed freq=300 dur=0.5 drop=1.2 gain=-10",
      "a distant door slams, then a questioning echo", "mid",
      "a heavy door slamming far away down a stone corridor, echoing, 1.5 seconds", "L")
sfx("sting_spell_misfire", 3, "Stingers", "music:sting kind=spell_misfire",
    "a crumhorn blat: the comic twin reveal", "C", "none", "Music", "2d",
    ai="a single comic crumhorn blat, a medieval buzzy reed instrument sagging downward in pitch, 0.7 seconds")

# --- 3.4 Status -----------------------------------------------------------------------------------
sfx("sfx_status_burning_loop", 1, "SFX/Status", "synth:crackle dur=4 density=50 body=0.6", "crackle close to cloth",
    "L", "low", "SFX/World", "3d", 1)
sfx("sfx_status_burning_out", 1, "SFX/Status", "synth:hiss dur=0.8 low=1500 high=6000 decay=4", "a hiss and smoulder", "L", "none")
sfx("sfx_status_asleep_player_loop", 2, "SFX/Status", "synth:snore dur=3.5 pitch=40", "soft breathing, never the player's voice",
    "R", "low", "SFX/Foley", "3d", 1)
sfx("sfx_status_stagger", 2, "SFX/Status", "synth:whine dur=1.2 f0=3200 f1=3000 depth=0.005", "a ringing-ear whine (local)",
    "G", "none", "SFX/Foley", "2d")
sfx("sfx_status_levitating_loop", 1, "SFX/Status", "synth:whine dur=3 f0=600 f1=600 wobble=5 depth=0.06",
    "the Levo misfire loop", "G", "none", "SFX/Spells", "3d", 1)

# --- 3.5 The wizard -------------------------------------------------------------------------------
F = "SFX/Foley"
for surface, recipe in (
        ("stone", K + "impact-sounds/footstep_concrete_*"),
        ("wood", K + "impact-sounds/footstep_wood_*"),
        ("earth", K + "impact-sounds/footstep_grass_* shift=0.85"),
        ("rushes", K + "impact-sounds/footstep_snow_* shift=1.15"),
        ("tile", K + "impact-sounds/footstep_concrete_* shift=1.2"),
        ("metal", K + "impact-sounds/impactMetal_light_* gain=-8"),
        ("water", "synth:drips dur=0.25 rate=12 + synth:whoosh dur=0.2 f0=400 f1=1500 gain=-6")):
    n = 4 if surface in ("metal", "water") else 6
    sfx(f"foley_step_{surface}", n, "Foley", recipe, f"a step on {surface}", "L", "mid" if surface == "metal" else "low", F)
sfx("foley_robe_move", 6, "Foley", K + "rpg-audio/cloth*", "wool robe swish", "L", "none", F)
sfx("foley_player_jump", 2, "Foley", K + "rpg-audio/cloth* + synth:thump dur=0.1 freq=100 click=0.3 gain=-10", "cloth whip and push-off", "L", "low", F)
sfx("foley_player_land", 3, "Foley", K + "impact-sounds/impactSoft_medium_*", "a light thump", "L", "low", F)
sfx("foley_player_land_heavy", 2, "Foley", K + "impact-sounds/impactSoft_heavy_*", "a thud and a rattle of gear", "L", "mid", F)
sfx("foley_player_dodge", 3, "Foley", "synth:whoosh dur=0.25 f0=500 f1=2500 + " + K + "rpg-audio/cloth* gain=-4", "a quick cloth whoosh and scuff", "L", "low", F)
sfx("sfx_player_hurt", 4, "SFX/Player", K + "impact-sounds/impactPunch_medium_*", "a body thump plus a cloth hit", "L", "low", F)
sfx("sfx_player_hurt_heavy", 2, "SFX/Player", K + "impact-sounds/impactPunch_heavy_*", "a heavier impact", "L", "mid", F)
sfx("sfx_player_heartbeat_loop", 1, "SFX/Player", "synth:heartbeat bpm=70 beats=4", "a slow heartbeat (local)", "G", "none", F, "2d", 1)
sfx("sfx_player_downed", 1, "SFX/Player", K + "impact-sounds/impactSoft_heavy_* + " + K + "rpg-audio/dropLeather*", "the body collapses", "L", "mid", F)
sfx("sfx_player_death", 1, "SFX/Player", K + "impact-sounds/impactSoft_heavy_* + synth:bell_toll freq=110 dur=4 gain=-6",
    "a collapse, then a single low bell", "L", "mid", F)
sfx("sfx_player_carried_loop", 1, "SFX/Player", K + "rpg-audio/cloth* + synth:creak dur=2 rate=8 pitch=300 gain=-12",
    "limp-body cloth drag and creak", "L", "low", F, loop=1)

# --- 3.6 Grab, carry, throw -----------------------------------------------------------------------
sfx("sfx_grab_beam_start", 1, "SFX/Grab", "synth:whine dur=0.2 f0=500 f1=1500 depth=0 + synth:shimmer dur=0.2 root=880 attack=0.1 gain=-8",
    "a verdigris zip", "G", "none", "SFX/Spells")
sfx("sfx_grab_beam_loop", 1, "SFX/Grab", "synth:hum dur=3 freq=220 tremolo=6 harm=0.4", "a quiet hum; strains with bulk", "G", "none",
    "SFX/Spells", loop=1)
sfx("sfx_grab_beam_release", 1, "SFX/Grab", "synth:whine dur=0.15 f0=1200 f1=400 depth=0", "a soft snap", "G", "none", "SFX/Spells")
sfx("sfx_grab_pickup_light", 3, "SFX/Grab", K + "rpg-audio/handleSmallLeather* + " + K + "rpg-audio/beltHandle* gain=-6", "a quick lift", "L", "none")
sfx("sfx_grab_pickup_heavy", 3, "SFX/Grab", K + "impact-sounds/impactPlank_medium_* gain=-6 pitch=0.8", "scrape and heave", "L", "low")
sfx("sfx_carry_strain_loop", 1, "SFX/Grab", "synth:creak dur=3 rate=12 pitch=250 rise=0.8", "wood-and-gilt creaks and groans", "L", "low", loop=1)
sfx("sfx_throw_whoosh_light", 3, "SFX/Grab", "synth:whoosh dur=0.25 f0=600 f1=3500", "a short whoosh", "L", "none")
sfx("sfx_throw_whoosh_heavy", 3, "SFX/Grab", "synth:whoosh dur=0.5 f0=150 f1=1500 q=1.0", "a slow deep whoosh", "L", "low")

# --- 3.7 Physics ----------------------------------------------------------------------------------
P = "Physics"
for mat, light, heavy, noise_l, noise_h, nl, nh in (
        ("stone", K + "impact-sounds/impactMining_*", K + "impact-sounds/impactMining_* shift=0.7", "low", "mid", 4, 3),
        ("wood", K + "impact-sounds/impactWood_light_*", K + "impact-sounds/impactWood_heavy_*", "low", "mid", 4, 3),
        ("metal", K + "impact-sounds/impactMetal_light_*", K + "impact-sounds/impactMetal_heavy_*", "mid", "high", 4, 3),
        ("bronze", K + "impact-sounds/impactBell_heavy_* shift=1.3", K + "impact-sounds/impactBell_heavy_*", "mid", "high", 3, 3),
        ("gold", "synth:metal_ring dur=0.8 freq=1600 decay=5 clank=0.3", "synth:metal_ring dur=1.2 freq=900 decay=3 clank=0.5",
         "mid", "high", 3, 2)):
    sfx(f"phys_impact_{mat}_light", nl, P, light, f"{mat} struck lightly", "L", noise_l)
    sfx(f"phys_impact_{mat}_heavy", nh, P, heavy, f"{mat} struck hard", "L", noise_h)
sfx("phys_impact_ceramic_light", 4, P, K + "impact-sounds/impactPlate_light_*", "faience or terracotta knocked", "L")
sfx("phys_impact_glass_light", 3, P, K + "impact-sounds/impactGlass_light_*", "glass knocked", "L")
sfx("phys_impact_cloth", 3, P, K + "impact-sounds/impactSoft_medium_* gain=-4", "cloth landing", "L", "none")
sfx("phys_impact_book", 3, P, K + "rpg-audio/bookPlace*", "a book landing", "L")
sfx("phys_impact_body", 4, P, K + "impact-sounds/impactPunch_heavy_* + " + K + "impact-sounds/impactSoft_heavy_*", "a thrown body", "L", "mid")
sfx("phys_impact_coins", 3, P, K + "casino-audio/chips-collide-* + synth:coins dur=0.5 count=6 gain=-4", "coins landing", "L", "mid")
sfx("phys_scrape_stone_loop", 1, P, "synth:hiss dur=3 low=300 high=3000 flutter=13 + synth:rattle dur=3 density=30 freq=900", "dragging over stone", "L", "low", loop=1)
sfx("phys_scrape_wood_loop", 1, P, "synth:creak dur=3 rate=30 pitch=500 rise=1.0", "dragging over wood", "L", "low", loop=1)
sfx("phys_scrape_metal_loop", 1, P, "synth:hiss dur=3 low=2000 high=8000 flutter=17 + synth:rattle dur=3 density=40 freq=2500", "dragging metal", "L", "mid", loop=1)
sfx("phys_roll_loop", 1, P, "synth:rattle dur=2 density=20 freq=400 + synth:hiss dur=2 low=100 high=600 flutter=4", "round things rolling", "L", "low", loop=1)
sfx("phys_break_ceramic", 3, P, K + "impact-sounds/impactPlate_heavy_* + synth:shatter dur=0.8 pieces=25 low=1200 brightness=0.6", "pottery shattering", "L", "high")
sfx("phys_break_glass", 3, P, K + "impact-sounds/impactGlass_heavy_* + synth:shatter dur=1.0 pieces=40", "small glass shattering", "L", "high")
sfx("phys_break_glass_large", 2, P, "synth:shatter dur=2.5 pieces=160 + " + K + "impact-sounds/impactGlass_heavy_*", "the Venetian Mirror: a big long shatter",
    "L", "max", ai="a large antique mirror shattering on a stone floor, long cascade of glass, 3 seconds")
sfx("phys_break_wood", 3, P, K + "impact-sounds/impactWood_heavy_* + synth:crumble dur=0.9 density=40 size=0.3 gain=-8", "wood splintering", "L", "high")
sfx("phys_break_book", 1, P, K + "rpg-audio/bookFlip* + synth:hiss dur=0.4 low=1000 high=6000 decay=6", "a psalter torn apart", "L", "low")
sfx("phys_break_liquid", 2, P, "synth:whoosh dur=0.6 f0=300 f1=1200 q=0.8 + synth:drips dur=0.6 rate=20", "contents spilling", "L", "low")
sfx("phys_coin_spill", 3, P, "synth:coins dur=1.2 count=30 + " + K + "casino-audio/chips-stack-*", "coins scattering", "L", "high")
sfx("sfx_loot_value_lost", 2, "SFX/Loot", "synth:coin_drain steps=6", "a coin-drain tink-tink-tink dropping away", "G", "none", "UI", "2d")
sfx("sfx_loot_highlight", 1, "SFX/Loot", "synth:chime notes=2637 dur=0.5 gold=1 decay=8", "a very faint gold glint", "G", "none", "UI", "2d")
sfx("sfx_loot_pickup_gold", 2, "SFX/Loot", "synth:coins dur=0.5 count=4", "a brief coin chime on top of the pickup", "G", "none")

# --- 3.8 Weapons ----------------------------------------------------------------------------------
W = "SFX/Weapons"


def wpn(name, n, recipe, brief, noise, final="L", loop=0, ai=""):
    sfx(name, n, "SFX/Weapons", recipe, brief, final, noise, W, "3d", loop, ai)


for fam, pitch in (("blade", 1.0), ("bronze", 0.8)):
    wpn(f"sfx_wpn_{fam}_swing", 4, f"synth:whoosh dur=0.3 f0={900 * pitch:.0f} f1={4500 * pitch:.0f} q=2.5", f"{fam} swing", "low")
    wpn(f"sfx_wpn_{fam}_hit_flesh", 3, K + f"rpg-audio/knifeSlice* shift={pitch} + " + K + "impact-sounds/impactPunch_medium_* gain=-6", "blade into body", "low")
    wpn(f"sfx_wpn_{fam}_hit_armour", 3, K + f"impact-sounds/impactMetal_medium_* shift={pitch}", "blade on armour", "mid")
    wpn(f"sfx_wpn_{fam}_hit_wood", 2, K + f"rpg-audio/chop* shift={pitch}", "blade into wood", "low")
    wpn(f"sfx_wpn_{fam}_hit_stone", 2, K + f"impact-sounds/impactMining_* shift={pitch * 1.2:.2f}", "blade on stone", "low")
    wpn(f"sfx_wpn_{fam}_clash", 3, f"synth:metal_ring dur=0.9 freq={1300 * pitch:.0f} clank=1.0", "blades meeting", "mid")
    wpn(f"sfx_wpn_{fam}_draw", 1, K + f"rpg-audio/drawKnife* shift={pitch}", "drawn from the scabbard", "low")
wpn("sfx_wpn_bronze_bend", 1, "synth:creak dur=0.5 rate=60 pitch=900 rise=0.6 + synth:metal_ring dur=0.6 freq=500 clank=0.2", "the khopesh bends", "low", "G")
wpn("sfx_wpn_rapier_swing", 3, "synth:whoosh dur=0.18 f0=2500 f1=8000 q=4", "a thin whip", "none")
wpn("sfx_wpn_rapier_hit", 2, K + "rpg-audio/knifeSlice* shift=1.3", "a light hit", "none")
wpn("sfx_wpn_rapier_snap", 1, "synth:metal_ring dur=0.5 freq=2400 clank=1.5", "it snaps clean", "low")
wpn("sfx_wpn_blunt_swing", 3, "synth:whoosh dur=0.5 f0=150 f1=1200 q=1.2", "a heavy swing", "low")
wpn("sfx_wpn_blunt_hit_armour", 3, K + "impact-sounds/impactMetal_heavy_* + synth:metal_ring dur=1.5 freq=450 clank=0.8 gain=-4", "a huge clang", "high")
wpn("sfx_wpn_blunt_hit_flesh", 2, K + "impact-sounds/impactPunch_heavy_* shift=0.8", "a thud", "mid")
wpn("sfx_wpn_blunt_hit_door", 2, K + "impact-sounds/impactWood_heavy_* shift=0.8", "a door battered", "high")
wpn("sfx_wpn_shield_bash", 2, K + "impact-sounds/impactPlank_medium_* shift=0.8", "a shield bash", "mid")
wpn("sfx_wpn_shield_block", 3, K + "impact-sounds/impactWood_medium_*", "a blow blocked", "mid")
wpn("sfx_wpn_shield_arrow_thunk", 3, K + "impact-sounds/impactWood_light_* shift=0.7", "an arrow thunks into the shield", "low")
wpn("sfx_wpn_shield_pavise_plant", 1, K + "impact-sounds/impactWood_heavy_* shift=0.6 + synth:thump dur=0.3 freq=60", "a pavise slammed into the floor", "mid")
wpn("sfx_wpn_xbow_span_loop", 1, "synth:creak dur=2.5 rate=14 pitch=700 rise=1.0 + synth:rattle dur=2.5 density=10 freq=1800", "the long terrible pause", "none", loop=1)
wpn("sfx_wpn_xbow_loose", 2, "synth:thump dur=0.25 freq=180 click=1.0 decay=25 + synth:metal_ring dur=0.4 freq=300 clank=0.2 gain=-8", "the string loosed", "none")
wpn("sfx_wpn_xbow_bolt_fly", 1, "synth:whoosh dur=0.3 f0=3000 f1=1500 q=3", "a bolt in flight", "none")
for tgt, rec in (("wood", "impactWood_light_*"), ("stone", "impactMining_* shift=1.3"), ("flesh", "impactPunch_medium_*")):
    wpn(f"sfx_wpn_xbow_bolt_hit_{tgt}", 2, K + "impact-sounds/" + rec, f"a bolt hits {tgt}", "low")
wpn("sfx_wpn_sling_whirl_loop", 1, "synth:whoosh dur=0.35 f0=300 f1=900 q=1.5 attack=0.5", "a cord whirled overhead", "none", loop=1)
wpn("sfx_wpn_sling_release", 2, "synth:whoosh dur=0.2 f0=1500 f1=4000 + synth:tick dur=0.03 freq=600", "the stone let fly", "none")
wpn("sfx_wpn_sling_stone_hit", 2, K + "impact-sounds/impactMining_*", "a stone hits", "low")
wpn("sfx_wpn_match_hiss_loop", 1, "synth:hiss dur=3 low=2500 high=9000 flutter=7 + synth:crackle dur=3 density=10 body=0", "a burning match-cord", "low", loop=1)
wpn("sfx_wpn_match_fire", 3, "synth:gunshot dur=1.6 size=1.3", "a matchlock or hand-cannon shot", "max", "A",
    ai="a medieval hand cannon firing, black powder boom with a deep rolling tail, 2 seconds")
wpn("sfx_wpn_match_tail_small", 1, "synth:crumble dur=1.5 density=5 size=0.4 gain=-10", "echo tail, small room", "none")
wpn("sfx_wpn_match_tail_large", 1, "synth:explosion dur=3.5 size=0.6 gain=-14", "echo tail, big hall", "none")
wpn("sfx_wpn_match_reload", 1, K + "rpg-audio/metalClick* + " + K + "rpg-audio/metalLatch* + synth:hiss dur=0.5 low=800 high=4000 decay=5 gain=-10",
    "ramrod and powder", "low")
wpn("sfx_wpn_match_flash_in_pan", 1, "synth:whumph dur=0.4 size=1.5 + synth:hiss dur=0.5 low=2000 high=8000 decay=6", "it didn't fire", "mid")
wpn("sfx_wpn_wheellock_spin", 1, "synth:rattle dur=0.3 density=120 freq=4000 spread=0.2", "the wheel spins", "low")
wpn("sfx_wpn_flint_click", 1, K + "rpg-audio/metalClick*", "flint strikes", "low")
wpn("sfx_wpn_pistol_fire", 3, "synth:gunshot dur=1.2 size=0.9", "a pistol shot", "high", "A",
    ai="a 17th century flintlock pistol shot, sharp crack and smoke, 1.2 seconds")
wpn("sfx_wpn_pistol_reload", 1, K + "rpg-audio/metalLatch* + " + K + "rpg-audio/metalClick*", "reloading a pistol", "low")
wpn("sfx_wpn_ball_ricochet", 3, "synth:whine dur=0.4 f0=3000 f1=1200 wobble=0 depth=0 + synth:tick dur=0.04 freq=3000", "a ricochet", "low")
wpn("sfx_wpn_ball_hit_flesh", 2, K + "impact-sounds/impactPunch_medium_* shift=1.2", "a ball hits a body", "low")
wpn("sfx_wpn_ball_hit_stone", 2, K + "impact-sounds/impactMining_* shift=1.4", "a ball hits stone", "low")
wpn("sfx_wpn_fuse_light", 1, "synth:whumph dur=0.3 size=2 + synth:hiss dur=0.3 low=3000 high=9000", "a fuse catches", "low")
wpn("sfx_wpn_fuse_loop", 1, "synth:hiss dur=2 low=3000 high=10000 flutter=12 + synth:crackle dur=2 density=40 body=0", "a fizzing fuse", "low", loop=1)
wpn("sfx_wpn_explosion_small", 2, "synth:explosion dur=2.5 size=0.8", "a grenado bursts", "max", "A",
    ai="a small black powder grenade exploding in a stone room, 2.5 seconds")
wpn("sfx_wpn_explosion_large", 2, "synth:explosion dur=4 size=1.6", "a petard blows a door", "max", "A",
    ai="a large black powder petard blasting a heavy door apart, huge boom and debris, 4 seconds")
wpn("sfx_wpn_debris", 2, "synth:crumble dur=2 density=40 size=0.6", "debris raining down", "mid")
wpn("sfx_wpn_caltrops_scatter", 2, K + "casino-audio/chips-collide-* shift=0.8 + synth:rattle dur=0.5 density=40 freq=1500", "caltrops flung", "low")
wpn("sfx_wpn_caltrops_step_on", 2, "synth:tick dur=0.05 freq=1400 noise=0.8 + " + K + "impact-sounds/impactPunch_medium_* gain=-8", "a caltrop stepped on", "low")
wpn("sfx_wpn_equip", 2, K + "rpg-audio/beltHandle*", "a weapon taken up", "low")
wpn("sfx_wpn_break", 2, "synth:metal_ring dur=0.6 freq=1100 clank=1.2 + " + K + "impact-sounds/impactWood_medium_*", "a weapon breaks", "low")

# --- 3.9 Enemies ----------------------------------------------------------------------------------
C = "SFX/Creatures"
for gear, rec in (("linen", K + "rpg-audio/cloth*"),
                  ("leather", K + "rpg-audio/clothBelt* + " + K + "rpg-audio/handleSmallLeather* gain=-6"),
                  ("mail", "synth:rattle dur=0.4 density=60 freq=3500 + " + K + "rpg-audio/cloth* gain=-6"),
                  ("plate", K + "impact-sounds/impactPlate_light_* gain=-6 + synth:rattle dur=0.3 density=25 freq=1800"),
                  ("bronze_plate", K + "impact-sounds/impactBell_heavy_* shift=1.6 gain=-12 + synth:rattle dur=0.3 density=15 freq=900")):
    sfx(f"foley_gear_{gear}_move", 4, "Foley", rec, f"{gear.replace('_', ' ')} moving with the body", "L", "low", F)
sfx("foley_step_hound", 6, "Foley", "synth:tick dur=0.05 freq=2500 noise=0.8 + synth:tick dur=0.05 freq=2300 noise=0.8", "claws on stone", "L", "low", F)
sfx("sfx_enemy_dendra_boar_tusk_rattle", 2, "SFX/Enemies", "synth:rattle dur=0.4 density=30 freq=1200 spread=0.3", "a helmet of boar tusks clacking", "L", "low", C)
sfx("sfx_enemy_keeper_firepot_throw", 2, "SFX/Enemies", "synth:whoosh dur=0.4 f0=300 f1=1500 + synth:crackle dur=0.4 density=40 body=0", "a pot of fire thrown", "L", "low", C)
sfx("sfx_enemy_keeper_firepot_shatter", 2, "SFX/Enemies", "synth:whumph dur=0.9 size=1.2 + " + K + "impact-sounds/impactPlate_heavy_*", "a clay pot of fire bursting",
    "A", "high", C, ai="a clay pot full of burning oil shattering and bursting into flame, 1.2 seconds")
sfx("amb_enemy_lantern_creak_loop", 1, "SFX/Enemies", "synth:creak dur=2.5 rate=6 pitch=900 rise=1.1", "a creaking lantern bail", "L", "none", C, loop=1)
sfx("sfx_enemy_musket_rest_plant", 1, "SFX/Enemies", K + "impact-sounds/impactPlank_medium_*", "a musket fork planted", "L", "low", C)
sfx("sfx_enemy_petard_plant", 1, "SFX/Enemies", K + "impact-sounds/impactMetal_heavy_* shift=0.8", "a bell of powder hammered onto a door", "L", "mid", C)
for line, n, kind, dur, loop, noise in (("pant_loop", 1, "pant", 2.0, 1, "none"), ("growl", 3, "growl", 1.0, 0, "low"),
                                         ("bark", 4, "bark", 0.35, 0, "high"), ("bite", 3, "growl", 0.3, 0, "low"),
                                         ("yelp", 3, "yelp", 0.3, 0, "mid"), ("howl", 1, "howl", 3.0, 0, "high"),
                                         ("whimper_grabbed", 2, "yelp", 0.8, 0, "low")):
    sfx(f"vo_hound_{line}", n, "VO/hound", f"synth:hound dur={dur} kind={kind}", f"Alaunt war-hound: {line.replace('_', ' ')}",
        "L", noise, "SFX/Creatures", "3d", loop, ai=f"a large mastiff war dog {line.replace('_loop', '').replace('_', ' ')}")

VOICES = {
    "bronze": (("levy", 130), ("slinger", 150), ("champion", 95), ("keeper", 115)),
    "high": (("warden", 120), ("crossbowman", 140), ("knight", 100)),
    "late": (("halberdier", 125), ("handgunner", 145), ("manatarms", 98), ("pavisier", 135)),
    "powder": (("guard", 128), ("musketeer", 150), ("cuirassier", 92), ("petardier", 140)),
}
LINES = (("murmur", 4, "calm", 2.0, "none"), ("alert", 3, "alert", 1.0, "mid"), ("chase", 3, "shout", 1.2, "high"),
         ("search", 2, "calm", 1.4, "low"), ("lost", 2, "calm", 0.9, "low"), ("attack", 3, "grunt", 0.4, "mid"),
         ("hurt", 3, "pain", 0.5, "mid"), ("death", 2, "pain", 1.0, "mid"), ("asleep", 2, "sleep", 3.0, "low"),
         ("grabbed", 3, "alert", 1.0, "mid"), ("thrown", 2, "shout", 0.7, "mid"))
for age, voices in VOICES.items():
    for enemy, pitch in voices:
        for line, n, mood, dur, noise in LINES:
            recipe = f"synth:snore dur={dur} pitch={pitch / 2:.0f}" if line == "asleep" else \
                f"synth:babble dur={dur} pitch={pitch} mood={mood}"
            sfx(f"vo_{age}_{enemy}_{line}", n, f"VO/{age}", recipe,
                f"{enemy} ({age}): {line} (see Tools/AudioForge/vo/line-sheets/{age}.md)", "R", noise, "SFX/Creatures",
                "3d", 1 if line == "asleep" else 0)

# --- 3.10 Alarm -----------------------------------------------------------------------------------
ALARM = {
    "bronze": ("synth:horn freq=98 dur=3.5 brass=0.3 bend=0.85",
               "synth:gong freq=90 dur=5 + synth:horn freq=147 dur=3 brass=0.5 gain=-4",
               "a bull-horn call, then war-conch and bronze gong"),
    "high": ("synth:bell_toll freq=196 dur=6 count=1",
             "synth:bell_toll freq=196 dur=3 count=4 interval=0.9",
             "a single chapel bell, then fast tolling"),
    "late": ("synth:horn freq=147 dur=3 brass=0.7",
             "synth:bell_toll freq=262 dur=2 count=6 interval=0.45 big=0",
             "a watch horn, then a tocsin bell and drum"),
    "powder": ("synth:thump dur=0.5 freq=70 click=1.0 decay=8 + synth:rattle dur=1.5 density=20 freq=500 spread=0.2",
               "synth:rattle dur=4 density=40 freq=400 spread=0.2 + synth:gunshot dur=2.5 size=1.5 gain=-6",
               "a drum beat to quarters, then a drum roll and a signal gun"),
}
for age, (roused, huecry, brief) in ALARM.items():
    add(f"amb_alarm_{age}_roused", 1, "Ambience/Alarm", "SFX/World", "3d", 0, "L", "none", roused, brief)
    add(f"amb_alarm_{age}_huecry_loop", 1, "Ambience/Alarm", "SFX/World", "3d", 1, "L", "none", huecry, brief)
amb("amb_household_stir_loop", "synth:murmur_crowd dur=20 voices=10 + synth:drips dur=20 rate=0.2", "distant doors, muffled voices, dogs",
    "R", "3d")
sfx("sfx_castle_lockdown_portcullis_drop", 2, "SFX/Castle", "synth:rattle dur=1.0 density=60 freq=700 + synth:thump dur=0.8 freq=45 click=1.2 decay=6",
    "a chain let go, iron teeth hitting stone", "L", "high")
sfx("sfx_castle_lockdown_bolt", 3, "SFX/Castle", K + "rpg-audio/metalLatch* shift=0.6 + " + K + "impact-sounds/impactMetal_heavy_* gain=-6",
    "a heavy iron bolt thrown", "L", "mid")

# --- 3.11 Castle ----------------------------------------------------------------------------------
Cs = "SFX/Castle"
sfx("sfx_door_wood_open", 3, Cs, K + "rpg-audio/doorOpen_* + " + K + "rpg-audio/creak* gain=-4", "an iron hinge creak", "L")
sfx("sfx_door_wood_close", 3, Cs, K + "rpg-audio/doorClose_*", "a thud", "L")
sfx("sfx_door_heavy_open", 2, Cs, "synth:creak dur=2.0 rate=18 pitch=250 rise=0.7 + " + K + "rpg-audio/doorOpen_* shift=0.7", "a long groan", "L", "mid")
sfx("sfx_door_heavy_close", 2, Cs, K + "rpg-audio/doorClose_* shift=0.6 + synth:thump dur=0.8 freq=50 decay=6", "a boom", "L", "mid")
sfx("sfx_door_grate_open", 2, Cs, "synth:rattle dur=1.0 density=40 freq=1200 + synth:creak dur=1.0 rate=30 pitch=1500", "a rattle and squeal", "L", "mid")
sfx("sfx_door_grate_close", 2, Cs, K + "impact-sounds/impactMetal_heavy_* + synth:rattle dur=0.5 density=40 freq=1000", "an iron clang", "L", "mid")
sfx("sfx_door_locked_rattle", 3, Cs, K + "rpg-audio/metalLatch* + " + K + "rpg-audio/metalClick* + " + K + "rpg-audio/metalLatch* shift=0.9",
    "the handle jiggles, the bolt holds", "L")
sfx("sfx_door_unlock", 2, Cs, K + "rpg-audio/metalClick* shift=0.8 + " + K + "rpg-audio/metalLatch*", "a key turning in the ward", "L")
sfx("sfx_door_slam", 2, Cs, K + "impact-sounds/impactWood_heavy_* shift=0.7", "a crack", "L", "high")
sfx("sfx_portcullis_raise_loop", 1, Cs, "synth:rattle dur=3 density=40 freq=600 spread=0.3 + synth:creak dur=3 rate=10 pitch=300 rise=1.0",
    "chain and ratchet", "L", "mid", loop=1)
sfx("sfx_portcullis_stop", 1, Cs, K + "impact-sounds/impactMetal_heavy_* shift=0.7", "the winch stops", "L", "mid")
sfx("sfx_masonry_crumble", 3, Cs, "synth:crumble dur=3 density=70 size=1.5", "a stone collapse", "L", "max")
sfx("sfx_debris_settle", 3, Cs, "synth:crumble dur=2 density=25 size=0.2", "dust and pebbles", "L")
sfx("sfx_fire_ignite", 3, Cs, "synth:whumph dur=0.8", "a whumph", "L", "mid")
sfx("sfx_fire_tapestry_catch", 2, Cs, "synth:whumph dur=1.5 size=1.5 + synth:crackle dur=1.5 density=80 body=0.5", "a fast rising crackle roar",
    "A", "mid", ai="a hanging tapestry catching fire, fast rising crackling roar, 2 seconds")
for name, dens, body, dur in (("candle", 3, 0.05, 6), ("torch", 20, 0.4, 8), ("brazier", 30, 0.8, 8),
                              ("hearth", 25, 1.0, 10), ("blaze", 80, 2.0, 10)):
    sfx(f"amb_fire_{name}_loop", 1, "Ambience/Fire", f"synth:crackle dur={dur} density={dens} body={body}", f"{name} fire",
        "L", "low" if name == "blaze" else "none", "SFX/Ambience", "3d", 1)
sfx("sfx_fire_extinguish", 2, Cs, "synth:hiss dur=1.2 low=1500 high=8000 decay=2.5", "a hiss", "L", "none")
sfx("sfx_torch_drop", 2, Cs, K + "impact-sounds/impactWood_light_* + synth:hiss dur=0.5 low=2000 high=7000 decay=5", "a clatter and sputter", "L")
sfx("sfx_hazard_fire_spread_fast", 2, "SFX/Hazards", "synth:crackle dur=1.5 density=100 body=1.0 + synth:whoosh dur=1.5 f0=200 f1=900",
    "Bronze: fire runs, hungrier crackle", "L", "mid")
sfx("sfx_hazard_oil_pour", 1, "SFX/Hazards", "synth:whoosh dur=1.5 f0=200 f1=600 q=0.7 + synth:drips dur=1.5 rate=15", "High: boiling oil poured",
    "L", "mid", ai="boiling oil poured from above onto stone, thick splashing, 2 seconds")
sfx("sfx_hazard_oil_sizzle", 2, "SFX/Hazards", "synth:hiss dur=2 low=3000 high=11000 flutter=20 decay=1", "oil sizzling", "L", "mid")
sfx("sfx_hazard_murderhole_drop", 2, "SFX/Hazards", "synth:rattle dur=0.4 density=30 freq=800 + synth:crumble dur=0.8 density=30 size=0.4",
    "Late: stones from above", "L", "mid")
sfx("sfx_hazard_arrowloop_loose", 2, "SFX/Hazards", "synth:thump dur=0.2 freq=200 click=1.0 decay=30 + synth:whoosh dur=0.3 f0=3000 f1=1500 q=3",
    "Late: arrows through the loops", "L")
sfx("sfx_hazard_magazine_explosion", 1, "SFX/Hazards", "synth:explosion dur=6 size=2.5 + synth:crumble dur=6 density=80 size=2", "Powder: the magazine goes up",
    "A", "max", ai="a gunpowder magazine exploding inside a stone palace, enormous boom, collapse and debris, 6 seconds")
sfx("sfx_hazard_explosion_tail_distant", 2, "SFX/Hazards", "synth:explosion dur=4 size=1.5 gain=-6", "an explosion heard from elsewhere", "A", "none",
    spatial="2d", ai="a distant muffled explosion heard through thick castle walls, 4 seconds")
sfx("sfx_drawbridge_raise_loop", 1, Cs, "synth:rattle dur=3 density=50 freq=500 + synth:creak dur=3 rate=6 pitch=200", "(if #44 builds it) winching",
    "L", "high", loop=1)
sfx("sfx_drawbridge_slam", 1, Cs, "synth:thump dur=1.5 freq=40 click=1.5 decay=3 + synth:crumble dur=1.5 density=20", "(if #44 builds it) the bridge slams",
    "L", "high")

# --- 3.12 Portal and extraction -------------------------------------------------------------------
Po = "SFX/Portal"
amb("amb_portal_loop", "synth:drone dur=10 freqs=55/82.4/110/164.8 tremolo=0.25 bright=1400 + synth:shimmer dur=10 root=440 attack=0.5 gain=-14",
    "a cold lapis drone", "G", "3d", "Ambience")
sfx("sfx_portal_open", 1, Po, "synth:whoosh dur=2.5 f0=60 f1=2500 q=0.8 attack=0.4 + synth:shimmer dur=2.5 root=220 voices=7",
    "a tear and inrush", "A", "high", S, ai="a magical portal tearing open, cold inrushing air and glassy choir, 3 seconds")
sfx("sfx_portal_enter", 1, Po, "synth:whoosh dur=1.5 f0=100 f1=5000 q=0.8 attack=0.6 + synth:shimmer dur=1.5 root=330 rise=2",
    "a whoosh through time", "A", "none", S, "2d", ai="stepping through a portal in time, whoosh and muffled distant world, 1.5 seconds")
sfx("sfx_portal_arrive", 1, Po, "synth:whoosh dur=1.2 f0=4000 f1=300 q=0.8 attack=0.1 + synth:wind dur=1.2 gain=-6",
    "let out into fresh air", "A", "none", S, "2d", ai="arriving through a portal into a cold night castle courtyard, whoosh fading to wind, 1.5 seconds")
sfx("sfx_portal_narrowing_loop", 1, Po, "synth:drone dur=8 freqs=110/116.5 tremolo=2 bright=2500 + synth:hum dur=8 freq=440 tremolo=8",
    "the drone tightening", "G", "none", S, "2d", 1)
sfx("sting_portal_warning", 3, "Stingers", "music:sting kind=portal_warning", "a tolling lapis bell", "G", "none", "Music", "2d")
sfx("sfx_portal_collapse", 1, Po, "synth:whoosh dur=1.8 f0=3000 f1=60 q=0.8 attack=0.1 + synth:thump dur=1.0 freq=40 decay=4",
    "an implosion", "A", "high", S, ai="a magical portal collapsing inward, implosion with a deep thud, 2 seconds")
sfx("sfx_extract_item_cross", 3, Po, "synth:coin_count steps=4", "a coin-count chime; the game raises pitch with value", "G", "none", "UI", "2d")
sfx("sfx_extract_player_cross", 1, Po, "synth:whoosh dur=1.0 f0=150 f1=3000 q=0.8 + synth:shimmer dur=1 root=330 gain=-10", "the portal whoosh, softened",
    "A", "none", S)
ui("sfx_result_tally_tick", 1, K + "casino-audio/chip-lay-*", "a coin drop", "L")
ui("sfx_result_tally_total", 1, K + "rpg-audio/handleCoins* + synth:thump dur=0.3 freq=90 click=0.5", "a heavy coin purse set down", "L")

# --- 3.13 Lair ------------------------------------------------------------------------------------
amb("amb_lair_bed_loop", "synth:drips dur=30 rate=0.5 + synth:wind dur=30 low=80 high=300 gust=0.05 gain=-10", "drips, damp stone, a far creak")
amb("amb_lair_hearth_loop", "synth:crackle dur=12 density=22 body=1.0", "a warm crackle, the only safe fire", spatial="3d")
sfx("sfx_lair_debt_pay", 2, "SFX/Lair", "synth:coins dur=1.2 count=25 + " + K + "rpg-audio/handleCoins* + synth:hiss dur=0.6 low=3000 high=8000 flutter=25 gain=-12",
    "coins into a coffer, then a quill scratch", "L", "none", "UI", "2d")
sfx("sfx_lair_debt_due", 1, "SFX/Lair", "synth:thump dur=0.4 freq=80 click=0.9 decay=15 + synth:thump dur=0.4 freq=78 click=0.9 decay=15",
    "three slow knocks on the door", "R", "none", "UI", "2d")
for age in AGES:
    sfx(f"sfx_lair_era_select_{age}", 1, "SFX/Lair", f"music:sting kind=alarm_stirred age={age} + synth:hiss dur=1.2 low=3000 high=9000 decay=2 gain=-14",
        f"an hourglass turned, plus the {age} Age's instrument", "G", "none", "UI", "2d")
sfx("sfx_lair_weapon_rack_take", 2, "SFX/Lair", K + "rpg-audio/drawKnife* + " + K + "impact-sounds/impactWood_light_* gain=-6", "a scrape off a peg", "L", "none")
sfx("sfx_lair_revive", 1, "SFX/Lair", "synth:shimmer dur=2.5 root=262 voices=5 attack=0.6 rise=1.5", "a warm rising chord", "C", "none", "UI", "2d")

# --- 4. Music and stingers ------------------------------------------------------------------------
M = "Music"
for name, variant, brief in (("mus_title_loop", "title", "The Herald's Overture"),
                             ("mus_lair_loop", "lair", "one candle, one fire"),
                             ("mus_lair_after_loss_loop", "lair_after_loss", "solo hurdy-gurdy, sadder"),
                             ("mus_results_success_loop", "results_success", "a jaunty tavern reel"),
                             ("mus_results_failure_loop", "results_failure", "a slow tipsy reel"),
                             ("mus_credits", "credits", "a full theme arrangement")):
    add(name, 1, M, "Music", "2d", 1, "M", "none", f"music:theme variant={variant}", brief)
for age in AGES:
    for layer in ("calm", "stirred", "roused", "huecry"):
        add(f"mus_raid_{age}_{layer}", 1, M, "Music", "2d", 1, "C", "none", f"music:raid age={age} layer={layer}",
            f"{age} raid layer: {layer}; all four share bars and length")
add("mus_raid_portal_narrowing", 1, M, "Music", "2d", 1, "C", "none",
    "synth:shimmer dur=16 root=220 voices=7 attack=0.9 tremolo=2 rise=2 + synth:clock dur=16 bpm=120 gain=-6",
    "a rising lapis choir and a ticking figure")
for age in AGES:
    for kind in ("stirred", "roused", "huecry"):
        add(f"sting_alarm_{kind}_{age}", 1, "Stingers", "Music", "2d", 0, "C", "none", f"music:sting kind=alarm_{kind} age={age}",
            f"alarm → {kind}, {age} instruments")
STINGS = (
    ("sting_gold_found", "gold_found", "an orpiment shimmer: bells and a harp gliss",
     "a short magical treasure discovery sting: warm golden bells and a rising harp glissando, 3 seconds"),
    ("sting_player_down", "player_down", "a muted drum and a low bell",
     "a short sombre medieval sting: one muted frame drum hit and a single low church bell, 4 seconds"),
    ("sting_extract_success", "extract_success", "a fanfare",
     "a short triumphant but shabby medieval fanfare on natural trumpets and tabor, slightly out of tune, 3 seconds"),
    ("sting_raid_lost", "raid_lost", "a sackbut sighs downwards",
     "a short comic failure sting: a renaissance sackbut sliding sadly downwards, 3 seconds"),
    ("sting_portal_opened", "portal_opened", "the lapis choir swells",
     "an otherworldly choir of reversed voices and glass harmonica swelling up, cold and magical, 4 seconds"),
)
for name, kind, brief, prompt in STINGS:
    add(name, 2 if kind == "gold_found" else 1, "Stingers", "Music", "2d", 0, "M" if kind != "portal_opened" else "A", "none",
        f"music:sting kind={kind}", brief, prompt)

# --- 5. Ambience beds -----------------------------------------------------------------------------
AMB = (
    ("amb_bronze_exterior_loop", "synth:wind dur=30 low=200 high=1200 gain=-6 + synth:crickets dur=30 voices=8 + synth:wind dur=30 low=60 high=200 gust=0.05 gain=-8",
     "warm night wind, crickets, a distant sea"),
    ("amb_high_exterior_loop", "synth:wind dur=30 low=250 high=1800 gust=0.2 + synth:crickets dur=30 voices=2 gain=-10", "cold wind on stone, an owl, a far dog"),
    ("amb_late_exterior_loop", "synth:wind dur=30 low=300 high=1500 + synth:drips dur=30 rate=3", "wet stone, gutters dripping, flags snapping"),
    ("amb_powder_exterior_loop", "synth:wind dur=30 low=200 high=900 gust=0.08 gain=-6 + synth:bell_toll freq=392 dur=6 count=1 gain=-22 + synth:hiss dur=30 low=800 high=4000 gain=-18",
     "light breeze, distant town bells, a fountain"),
    ("amb_room_corridor_loop", "synth:wind dur=30 low=80 high=400 gust=0.1 gain=-6", "dead air, a faint draught"),
    ("amb_room_small_loop", "synth:wind dur=30 low=60 high=250 gust=0.05 gain=-10", "room tone"),
    ("amb_room_hall_loop", "synth:wind dur=30 low=50 high=300 gust=0.04 + synth:drone dur=30 freqs=55 bright=200 gain=-20", "a large, faintly humming space"),
    ("amb_room_stair_loop", "synth:wind dur=30 low=400 high=2200 gust=0.3", "a draught whistling round the newel"),
    ("amb_room_crypt_loop", "synth:drips dur=30 rate=1.2 + synth:wind dur=30 low=30 high=120 gust=0.03", "drips and deep sub-rumble"),
    ("amb_room_chapel_loop", "synth:drone dur=30 freqs=73.4/110 bright=500 tremolo=0.05 gain=-12 + synth:hiss dur=30 low=2000 high=6000 gain=-26",
     "a huge reverberant hush, a faint organ drone"),
    ("amb_room_magazine_loop", "synth:creak dur=30 rate=0.3 pitch=300 gain=-10 + synth:wind dur=30 low=40 high=150 gain=-12", "dry, muffled, creaking kegs"),
    ("amb_room_counting_loop", "synth:wind dur=30 low=60 high=250 gain=-10 + synth:clock dur=30 bpm=4 gain=-10", "quiet, an occasional abacus click"),
    ("amb_room_kunstkammer_loop", "synth:clock dur=30 bpm=60 + synth:wind dur=30 low=60 high=250 gain=-12", "a clock ticking, a clockwork curiosity"),
    ("amb_room_gallery_loop", "synth:wind dur=30 low=200 high=1000 gust=0.2 + synth:rattle dur=30 density=1.5 freq=2500 gain=-8", "wind rattling big glass panes"),
    ("amb_room_bronze_interior_loop", "synth:crackle dur=30 density=6 body=0.6 gain=-8 + synth:wind dur=30 low=60 high=250 gain=-10", "smoke, dust, a low fire"),
)
for name, recipe, brief in AMB:
    amb(name, recipe, brief)
for spot, recipe, brief in (("dripping", "synth:drips dur=12 rate=1.5", "a drip"),
                            ("rats", "synth:rattle dur=12 density=4 freq=5000 spread=0.3 + synth:babble dur=0.3 pitch=1400 mood=alert gain=-12", "rats in the stone"),
                            ("wind_gap", "synth:wind dur=12 low=600 high=2500 gust=0.3", "wind through a gap")):
    amb(f"amb_spot_{spot}", recipe, brief, spatial="3d")


def main():
    names = [r["name"] for r in rows]
    dupes = {n for n in names if names.count(n) > 1}
    if dupes:
        raise SystemExit(f"duplicate names: {sorted(dupes)}")
    out = ROOT / "manifest.csv"
    with out.open("w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        writer.writeheader()
        writer.writerows(rows)
    files = sum(int(r["variants"]) for r in rows)
    print(f"wrote {out.relative_to(ROOT.parent.parent)}: {len(rows)} sounds, {files} files")


if __name__ == "__main__":
    main()
