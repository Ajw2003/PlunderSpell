#!/usr/bin/env python3
"""Build the review page for the enemy engine + animation plans.

Reads every enemy's clip list from docs/art/data/*.json, sorts each clip into the
plan's layers (Base, Family, Signature), and writes docs/generated/enemy-animation-plan/
index.html with the two plans' decisions and phases and the full clip matrix.
Thumbnails come from docs/generated/art-bible-models/img (run gallery.py first).

    python3 Tools/ArtForge/anim_plan_page.py
"""

import html
import json
import os
import re
import shutil

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(REPO, "docs", "generated", "enemy-animation-plan")
THUMBS = os.path.join(REPO, "docs", "generated", "art-bible-models", "img")
AGES = ["bronze", "high", "late", "powder"]

# First word stems that make a clip one of the shared Base clips every human gets.
BASE = ("idle", "walk", "patrol", "procession", "alert", "hit", "stagger", "heavy_stagger",
        "death", "run", "retreat", "flee", "duck", "archway", "crypt_duck", "stand_up",
        "kneel_get_up", "shout", "challenge", "chant", "call_partner", "wake_slow",
        "cower", "search", "strafe", "sideways", "shrug", "yelp", "charge")
# Weapon-family stems: the motion is shared across enemies carrying the same kind of weapon.
FAMILY = {
    "polearm": ("thrust", "attack_thrust", "attack_glaive", "sweep", "attack_hook", "attack_chop",
                "cant_glaive", "slope", "attack_spike", "attack_hammer", "attack_axe"),
    "sword & shield": ("rapier", "shield", "attack_overhead", "overhead", "attack_shield",
                       "advance_shield", "draw_sword", "draw_falchion", "attack_falchion",
                       "guard_stance"),
    "crossbow": ("aim_hold", "shoot", "reload_span", "reload_interrupted", "melee_bow"),
    "sling": ("sling", "reload", "vault"),
    "long gun": ("brace_and_aim", "fire", "aim", "fire_volley", "plant_rest", "club"),
    "pistol": ("draw_pistol", "pistol"),
    "throw": ("unhook", "throw"),
}
FAMILY_OF_ENEMY = {
    "palace-levy": "polearm", "wall-slinger": "sling", "dendra-champion": "sword & shield",
    "flame-keeper": "throw", "lantern-warden": "polearm", "castle-crossbowman": "crossbow",
    "household-knight": "sword & shield", "alaunt-hound": "hound",
    "sallet-halberdier": "polearm", "handgunner": "long gun", "gothic-knight": "polearm",
    "pavisier": "sword & shield", "partisan-guard": "polearm", "musketeer": "long gun",
    "cuirassier": "pistol", "petardier": "throw",
}


def clip_name(text):
    return re.split(r"[ —:(]", text.strip())[0].rstrip(",.")


def layer(slug, clip):
    if slug == "alaunt-hound":
        return "hound"
    for stem in BASE:
        if clip == stem or clip.startswith(stem + "_") or clip.startswith(stem):
            if not any(clip.startswith(s) for fam in FAMILY.values() for s in fam if len(s) > len(stem)):
                return "base"
    for stems in FAMILY.values():
        if any(clip == s or clip.startswith(s) for s in stems):
            return "family"
    return "signature"


def build():
    os.makedirs(os.path.join(OUT, "img"), exist_ok=True)
    rows, totals = [], {"base": 0, "family": 0, "signature": 0, "hound": 0}
    all_clips = 0
    for age in AGES:
        data = json.load(open(os.path.join(REPO, "docs", "art", "data", f"{age}.json")))
        for enemy in data["enemies"]:
            clips = [clip_name(c) for c in enemy["rig"]["animations"]]
            tagged = [(c, layer(enemy["slug"], c)) for c in clips]
            for _c, t in tagged:
                totals[t] += 1
            all_clips += len(clips)
            thumb = f"{age}-{enemy['slug']}.jpg"
            if os.path.exists(os.path.join(THUMBS, thumb)):
                shutil.copy(os.path.join(THUMBS, thumb), os.path.join(OUT, "img", thumb))
            else:
                thumb = None
                print(f"MISSING thumbnail for {age}/{enemy['slug']} (run Tools/ArtForge/gallery.py)")
            rows.append((data, enemy, tagged, thumb))

    matrix = []
    for data, enemy, tagged, thumb in rows:
        chips = "".join(f'<span class="chip {t}">{html.escape(c)}</span>' for c, t in tagged)
        img = (f'<img src="img/{thumb}" alt="{html.escape(enemy["name"])} review sheet" loading="lazy">'
               if thumb else "")
        matrix.append(
            f'<tr><td class="who">{img}<b>{html.escape(enemy["name"])}</b>'
            f'<span>{html.escape(data["name"])} · {html.escape(enemy["role"])} · '
            f'{html.escape(FAMILY_OF_ENEMY.get(enemy["slug"], ""))}</span></td>'
            f'<td class="clips">{chips}</td><td class="n">{len(tagged)}</td></tr>')

    page = (TEMPLATE.replace("{{MATRIX}}", "\n".join(matrix))
            .replace("{{ALL}}", str(all_clips))
            .replace("{{BASE}}", str(totals["base"])).replace("{{FAMILY}}", str(totals["family"]))
            .replace("{{SIG}}", str(totals["signature"])).replace("{{HOUND}}", str(totals["hound"])))
    with open(os.path.join(OUT, "index.html"), "w", encoding="utf-8") as handle:
        handle.write(page)
    print(f"wrote {os.path.relpath(OUT, REPO)}/index.html: {all_clips} clips "
          f"(base {totals['base']}, family {totals['family']}, signature {totals['signature']}, "
          f"hound {totals['hound']})")


TEMPLATE = """<title>Household in Motion</title>
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Eczar:wght@600;700;800&family=Spectral:ital,wght@0,300;0,400;1,400&family=Overpass+Mono:wght@400;600&display=swap">
<style>
:root{color-scheme:dark;--bg:#14120E;--ash:#1E1A14;--ash-hi:#282318;--line:#332D22;--soft:#262119;--vellum:#DCD2BA;--dim:#9A9078;--faint:#635C4C;
--verdigris:#5FA288;--orpiment:#C9A227;--madder:#C4542E;--lapis:#7A6AA0;
--display:"Eczar",Georgia,serif;--body:"Spectral",Georgia,serif;--mono:"Overpass Mono",ui-monospace,Menlo,monospace}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--vellum);font-family:var(--body);font-size:17px;line-height:1.65}
.wrap{max-width:1180px;margin:0 auto;padding-inline:20px}
section{padding-block:64px;border-top:1px solid var(--soft)}
.hero{border-top:0;padding-block:72px 48px}
.eyebrow{font-family:var(--mono);font-size:11px;letter-spacing:.22em;text-transform:uppercase;color:var(--faint);display:flex;gap:12px;align-items:center;margin-bottom:18px}
.eyebrow::after{content:"";flex:1;height:1px;background:var(--line)}
h1{font-family:var(--display);font-size:clamp(2.8rem,9vw,5.6rem);font-weight:800;line-height:.9;letter-spacing:-.03em;margin:0 0 .2em}
h1 em{font-style:normal;color:var(--verdigris)}
h2{font-family:var(--display);font-size:clamp(1.7rem,4vw,2.4rem);margin:0 0 .45em;line-height:1.12;text-wrap:balance}
h3{font-family:var(--display);font-size:1.15rem;margin:0 0 .35em}
p{margin:0 0 1em;max-width:68ch}
.lede{font-size:1.2rem;font-weight:300}
.dim{color:var(--dim)}
.stamp{display:flex;flex-wrap:wrap;gap:8px;font-family:var(--mono);font-size:11px;letter-spacing:.14em;text-transform:uppercase;color:var(--faint)}
.stamp span{border:1px solid var(--line);padding:5px 11px}
.status{display:inline-block;font-family:var(--mono);font-size:11px;letter-spacing:.16em;text-transform:uppercase;color:var(--madder);border:1px solid var(--madder);padding:4px 10px;margin-bottom:22px}
.grid{display:grid;gap:1px;background:var(--soft);border:1px solid var(--soft);grid-template-columns:repeat(auto-fit,minmax(min(100%,270px),1fr))}
.grid>div{background:var(--ash);padding:22px}
.kicker{font-family:var(--mono);font-size:10.5px;letter-spacing:.2em;text-transform:uppercase;color:var(--faint);margin-bottom:8px}
.grid p{font-size:.92rem;color:var(--dim);margin:0}
.decide{display:grid;gap:1px;background:var(--soft);border:1px solid var(--soft)}
.decide>div{background:var(--ash);padding:20px 22px;display:grid;grid-template-columns:56px 1fr;gap:16px}
.decide .n{font-family:var(--mono);font-size:12px;color:var(--madder);letter-spacing:.1em}
.decide p{font-size:.92rem;color:var(--dim);margin:.2em 0 0}
.decide b{color:var(--vellum);font-weight:600}
.rec{color:var(--verdigris);font-family:var(--mono);font-size:10.5px;letter-spacing:.12em;text-transform:uppercase}
.phases{display:grid;gap:1px;background:var(--soft);border:1px solid var(--soft)}
.phase{background:var(--ash);padding:20px 22px;display:grid;grid-template-columns:70px 1fr;gap:18px}
.phase .id{font-family:var(--mono);font-size:12px;color:var(--verdigris);letter-spacing:.12em}
.phase p{font-size:.9rem;color:var(--dim);margin:.25em 0 0}
.phase .audit{font-family:var(--mono);font-size:11px;color:var(--faint);margin-top:8px;letter-spacing:.04em}
.phase .audit b{color:var(--dim);font-weight:400}
.flow{font-family:var(--mono);font-size:12px;color:var(--dim);border:1px solid var(--line);padding:14px 16px;overflow-x:auto;white-space:nowrap;background:var(--ash)}
.flow b{color:var(--verdigris);font-weight:400}
.legend{display:flex;flex-wrap:wrap;gap:8px;margin:14px 0 18px}
.tablewrap{overflow-x:auto;border:1px solid var(--soft)}
table{border-collapse:collapse;width:100%;min-width:760px}
th{font-family:var(--mono);font-size:10px;letter-spacing:.18em;text-transform:uppercase;color:var(--faint);text-align:left;font-weight:400;padding:12px 14px;background:var(--bg);border-bottom:1px solid var(--line)}
td{padding:12px 14px;border-bottom:1px solid var(--soft);vertical-align:top;background:var(--ash)}
td.who{width:250px}
td.who img{display:block;width:100%;max-width:230px;height:auto;margin-bottom:8px;border:1px solid var(--line)}
td.who b{font-family:var(--display);font-size:1.05rem;font-weight:600;display:block}
td.who span{font-family:var(--mono);font-size:10.5px;color:var(--faint);letter-spacing:.05em}
td.n{font-family:var(--mono);color:var(--dim);text-align:right;font-variant-numeric:tabular-nums}
.chip{display:inline-block;font-family:var(--mono);font-size:11px;padding:3px 8px;margin:0 5px 6px 0;border:1px solid var(--line);color:var(--dim)}
.chip.base{color:var(--dim);background:var(--soft)}
.chip.family{color:var(--verdigris);border-color:#2E4C41}
.chip.signature{color:var(--vellum);border-color:var(--faint);font-weight:600}
.chip.hound{color:var(--lapis);border-color:#3E3654}
.counts{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:1px;background:var(--soft);border:1px solid var(--soft);margin-top:18px}
.counts div{background:var(--ash);padding:16px 18px}
.counts b{display:block;font-family:var(--display);font-size:1.9rem;line-height:1}
.counts span{font-family:var(--mono);font-size:10px;letter-spacing:.16em;text-transform:uppercase;color:var(--faint)}
footer{padding-block:48px 70px;border-top:1px solid var(--soft);font-family:var(--mono);font-size:11px;letter-spacing:.1em;color:var(--faint)}
@media (max-width:640px){.decide>div,.phase{grid-template-columns:1fr;gap:6px}td.who{width:auto}}
</style>
<div class="wrap">
<section class="hero">
  <div class="status">Plan · awaiting approval · nothing built</div>
  <h1>The household, <em>in motion</em></h1>
  <p class="lede">Sixteen defenders stand modelled and rigged. This is the plan for walking them into a raid, and then teaching them to move: to patrol, turn at a noise, swing, reload, fall asleep under Somnus, and fall down.</p>
  <div class="stamp"><span>2 plans</span><span>{{ALL}} clips asked for</span><span>~80 to author</span><span>4 decisions for you</span></div>
</section>

<section>
  <div class="eyebrow">The loop, unchanged</div>
  <h2>Everything goes through the same audit as the art</h2>
  <p class="dim">The models were checked by setting each render beside its concept. The engine and the animations get the same treatment. Each clip gets a review sheet beside its enemy's concept, and each in-engine enemy gets a Unity screenshot sheet beside its concept.</p>
  <div class="flow">plan → <b>build</b> → audit (sheet beside the concept) → <b>fix</b> → audit → commit &nbsp;·&nbsp; <b>Tools/autosave.sh</b> guards every long run</div>
  <div class="grid" style="margin-top:24px">
    <div><div class="kicker">Where they stand</div><p>All 16 are imported: Unity has written their <code>.meta</code> files. They import as Generic rigs with no avatar, and no prefab, roster entry or spawn uses them yet.</p></div>
    <div><div class="kicker">What the engine does today</div><p>A guard is one networked <code>CastleGuard</code> with synced state and health on a NavMesh agent. The Lair's chosen Age does not reach the guard planner. Nothing animates any enemy.</p></div>
    <div><div class="kicker">The hidden gap</div><p>Attacks run only on the host. Other players see the projectile or the damage, but never the swing. That signal has to be replicated before any attack animation can play for everyone.</p></div>
  </div>
</section>

<section>
  <div class="eyebrow">Run by you first</div>
  <h2>Four decisions before any work starts</h2>
  <div class="decide">
    <div><span class="n">01</span><div><h3>The ten EnemyForge enemies</h3><p><span class="rec">Recommended</span> <b>The art-bible set replaces the household four</b> (Watchman, Man-at-Arms, Sergeant, War-hound). The supernatural five stay in the Crypt until the bestiary question is settled. Alternatives: keep all 26, or retire all ten now.</p></div></div>
    <div><span class="n">02</span><div><h3>Enemies only in their own Age</h3><p><span class="rec">Recommended</span> <b>Yes.</b> This needs the Age choice to reach the castle, and the era-rooms work needs the same wiring, so build it once for both. Alternative: mix all four Ages until that lands.</p></div></div>
    <div><span class="n">03</span><div><h3>Where the motion comes from</h3><p><span class="rec">Recommended</span> <b>Keyframed in Blender by code ("AnimForge")</b>: stylised to match the art, runs end to end here, reproducible and reviewable. Alternatives: a motion-capture library (natural locomotion, but downloaded by hand with an account, the licence needs checking, and there are almost no crossbow or pavise moves), or a hybrid of library locomotion and AnimForge weapon moves.</p></div></div>
    <div><span class="n">04</span><div><h3>Death, sleep and being thrown</h3><p><span class="rec">Recommended</span> <b>Animated clips</b> for sleep, stun and knock-down. <b>A ragdoll</b> for thrown or killed guards; the downed-player carry already has the switch to copy. Plus: <b>in-place</b> clips (the NavMesh moves the guard), 1024 textures for now, and LODs and spring bones after the first playtest.</p></div></div>
  </div>
</section>

<section>
  <div class="eyebrow">Plan one · docs/plans/artbible-enemies-in-engine.md</div>
  <h2>Into the engine</h2>
  <div class="phases">
    <div class="phase"><span class="id">E0</span><div><h3>Import settings owned by code</h3><p>An import script sets Humanoid avatars for the fifteen humans (their bones are already Unity-Humanoid names, mapped explicitly) and a Generic rig for the hound. URP Lit materials are rebuilt from the baked maps, with emission restored to its HDR strength. Nothing is set by hand in the Inspector.</p><div class="audit"><b>Audit:</b> a test checks every rig type, and that every avatar is valid and human.</div></div></div>
    <div class="phase"><span class="id">E1</span><div><h3>Prefabs from the art bible's own numbers</h3><p>A forge reads the JSON and the ArtForge manifest: height, zones and role. It builds prefab variants with a collider, a NavMesh agent sized to fit the archways, role tuning (patrol, ranged, heavy, special), prop sockets, and a small warm light on every lantern, match and fuse.</p><div class="audit"><b>Audit:</b> the scale tests are extended to all 16. In-engine review sheets put Unity screenshots beside each concept.</div></div></div>
    <div class="phase"><span class="id">E2</span><div><h3>Roster entries</h3><p>Zones and weights come from each enemy's spec. Role sets the weight: patrol 10, ranged 7, heavy 4, special 3.</p><div class="audit"><b>Audit:</b> every zone still has a posting, and a five-seed sweep tables who spawned where.</div></div></div>
    <div class="phase"><span class="id">E3</span><div><h3>The Age reaches the guards</h3><p>Roster entries get an Age. The planner filters by Age first; if that leaves nothing, it falls back to zone only and logs a warning, never silently. The pass-through is built once, shared with the era-rooms work.</p><div class="audit"><b>Audit:</b> a Bronze Age seed contains only Bronze Age defenders.</div></div></div>
    <div class="phase"><span class="id">E4</span><div><h3>In a raid, networked</h3><p>Guards spawn through the existing networked path. One new replicated attack signal lets every player see every swing.</p><div class="audit"><b>Audit:</b> CombatBench per enemy (patrol, chase, attack, Somnus, Levo), then a two-player co-op run.</div></div></div>
  </div>
</section>

<section>
  <div class="eyebrow">Plan two · docs/plans/artbible-enemy-animations.md</div>
  <h2>Teaching them to move</h2>
  <p class="dim">The specs ask for {{ALL}} clips. Most are the same motion on a different body, so they're authored once and retargeted through Unity's Humanoid system. That leaves about eighty to author. Most \"one-off\" clips, like <code>idle_lantern</code> or <code>walk_burdened</code>, turn out to be a base motion plus that enemy's way of holding its gear. That becomes one upper-body carry pose per enemy (16) instead of dozens of separate clips.</p>
  <div class="counts">
    <div><b>{{BASE}}</b><span>base clips asked</span></div>
    <div><b>{{FAMILY}}</b><span>weapon family</span></div>
    <div><b>{{SIG}}</b><span>signature</span></div>
    <div><b>{{HOUND}}</b><span>hound</span></div>
  </div>
  <div class="phases" style="margin-top:24px">
    <div class="phase"><span class="id">A0</span><div><h3>Clip table, on paper first</h3><p>Every clip's layer, loop flag, length and events. The events are footsteps (so guards make noise in the acoustics system), the attack-hit frame, projectile release, and a prop dropping. The matrix below is the first draft.</p><div class="audit"><b>Audit:</b> every clip the 16 specs ask for is mapped or dropped with a reason.</div></div></div>
    <div class="phase"><span class="id">A1</span><div><h3>AnimForge, in Blender</h3><p>A pose library on the shared human rig. Clips are timed pose keys with anticipation and overshoot; locomotion is a gait matched to the agent speed, so feet don't slide. Weapon families carry IK targets, so two-handed weapons stay in both hands after retargeting. The hound's clips are made on its own rig.</p><div class="audit"><b>Audit:</b> a clip review sheet per clip (8 frames beside the concept, plus a foot-contact trace and a short video), then fix and audit again.</div></div></div>
    <div class="phase"><span class="id">A2</span><div><h3>Import</h3><p>The same import script sets Humanoid or Generic, loop flags, in-place root motion and events, all from the clip table.</p><div class="audit"><b>Audit:</b> every clip exists, retargets cleanly, and carries its events.</div></div></div>
    <div class="phase"><span class="id">A3</span><div><h3>One animator, sixteen overrides</h3><p>One base controller with four parts: locomotion blend, an upper-body action layer, additive hits, and full-body states (sleep, levitate, stagger, duck, death). The forge generates one override controller per enemy for its weapon and signature moves. The hound gets its own.</p><div class="audit"><b>Audit:</b> no override leaves an empty slot, so no enemy falls back to a T-pose.</div></div></div>
    <div class="phase"><span class="id">A4</span><div><h3>Driven by the game</h3><p>A driver reads the synced guard state, speed from movement, and the replicated attack signal, and only sets animator values. Animation events feed footstep noise and projectile timing. Death and being thrown switch to the ragdoll.</p><div class="audit"><b>Audit:</b> CombatBench per enemy, a co-op check that both players see the same swing, and in-engine sheets with an attack strip.</div></div></div>
  </div>
  <p class="dim" style="margin-top:22px"><b style="color:var(--vellum)">Suggested order:</b> prove the whole chain on one enemy first, the Lantern Warden: base clips plus the polearm family, then import, animator and driver. Once he patrols and swings in a raid, fan out one Age at a time, in parallel, as with the models.</p>
</section>

<section>
  <div class="eyebrow">The first draft of the clip table</div>
  <h2>Every clip the specs ask for</h2>
  <p class="dim">Taken from each enemy's spec. The colour says how a clip gets made.</p>
  <div class="legend"><span class="chip base">base: shared by every human</span><span class="chip family">family: shared by a weapon type</span><span class="chip signature">signature: this enemy only</span><span class="chip hound">hound: its own rig</span></div>
  <div class="tablewrap"><table><thead><tr><th>Enemy</th><th>Clips</th><th>n</th></tr></thead><tbody>
{{MATRIX}}
  </tbody></table></div>
</section>

<footer>PLUNDERSPELL · ENEMY ENGINE &amp; ANIMATION PLAN · AWAITING APPROVAL · GENERATED BY Tools/ArtForge/anim_plan_page.py</footer>
</div>
"""

if __name__ == "__main__":
    build()
