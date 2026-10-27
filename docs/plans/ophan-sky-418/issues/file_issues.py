# Files the #418 step issues and rewrites #418's body for the approved sky design ("Build plan approved: ophan in the
# sky 1C 2A 3A 4A 5A 6A 7A 8A 9A", 2026-10-27). Run once from the repo root: python docs/plans/ophan-sky-418/issues/file_issues.py
# Prints each issue's number. Bodies are kept beside this script as .md files for the record.
import json, os, subprocess

REPO = "Ajw2003/PlunderSpell"
HERE = os.path.dirname(os.path.abspath(__file__))
PAGE = "docs/plans/ophan-sky-418/ophan-sky-418.html"
SIGN = "\n\nFiled by AJ's agent\n"

STEPS = [
    ("sky", "The Ophan appears in the sky, with its hum and screen shake", "gameplay",
     "A prefab of the Ophan of Hours at 150 times its model (about 1.4 km tall, 1.65 km across), with its Animator on "
     "`TheOphanOfHours_Rig`. `HuntWindowSeconds` (60 s, `OphanDemandTuning`) before the portal closes it appears 900 m off "
     "the castle, high in the sky, always facing the castle with its eye on the players, and plays its 4-second arrival. "
     "Its warning (#434) plays on every machine: a loud, low drone made in code as a stand-in until real audio exists, and "
     "the existing camera shake. The player camera's far clip plane rises from 1000 m to 3000 m so it is drawn whole. "
     "A test switch forces a hunted raid until the demand (#419) exists. It is the same on every machine.",
     "#433 (the tuning asset, merged into staging).",
     "Every other step below.",
     "In a solo Editor raid it appears at 60 s left, whole, eye toward the castle, with the hum and the shake; a test "
     "checks the arrival time; a co-op check shows it on both machines; screenshots are looked at."),
    ("eye", "The Ophan's eye follows its target, and its rings spin by state", "gameplay",
     "It targets whoever carries the most worth (the sell value of the piece they carry, the number the portal's tally "
     "counts), then the next down the list. Its `Eye` bone turns every frame to follow that player; its three rings "
     "(`GreatWheel`, `InnerWheel`, `TiltWheel`) spin at a speed set by its state: fastest while charging the charged beam, "
     "half that during the lock-on, a slow turn while watching, spinning down to a stop as it leaves. The tells: for the "
     "lock-on the eye narrows and pulses, the pulse quickening over 3 s; for the charge the eye swells to half again its "
     "size and brightens steadily to full blast. Target, state and tells are the same on every machine.",
     "The sky step.",
     "The slow beam step and the charged beam step.",
     "Tests for target order and state; a solo raid with screenshots of the eye on its target and each tell; the same "
     "target and state on both machines in co-op."),
    ("slow", "The Ophan's slow beam: lock-on, drain, and the stone that stops it", "gameplay",
     "After a 3-second lock-on its slow beam drains 25 health a second while the straight line from its eye to the target "
     "passes through no stone. Getting behind a wall, or under a floor, escapes it. The beam is drawn tens of metres wide at "
     "the eye, narrowing to a pool of light on the target. A death goes through the same path as the Wick's catch, so every "
     "machine sees it. A beam on a player carrying a piece flagged `IsArtifact` lifts that piece into the sky instead: they "
     "live, and the Ophan leaves at once (`HuntSecondsAfterPaid` 0). After a kill it moves to the next target.",
     "The eye step.",
     "The charged beam step.",
     "Tests for the drain, cover, the take and leaving once paid; screenshots of the lock-on, the beam on a player in the "
     "open and blocked by stone, and the take; co-op shows the same outcome on both machines."),
    ("charged", "The Ophan's charged beam, and the rooms it scorches open", "gameplay",
     "Once its target has been behind cover for 3 s, it charges for 3 s and fires a beam that kills in one hit where it "
     "lands, then waits 10 s before it can charge again. The room it strikes is scorched for the rest of the raid on every "
     "machine: its walls are drawn burnt and the slow beam passes through them, though they stay solid to walk into. A "
     "charged hit on a player carrying an artifact takes the artifact instead, as the slow beam does.",
     "The slow beam step.",
     "The checks step.",
     "Tests for the charge timing, the one-hit kill and the scorched list; screenshots of the charge and a scorched room; "
     "co-op shows the same scorched room on both machines."),
    ("wings", "A pose for the Ophan with its wings spread wide", "art",
     "A new held pose on the Ophan's existing rig with the wings spread flat, so it reads about half again as wide as it is "
     "tall, made through the art pipeline and checked on a review sheet like its other clips. It holds after the arrival.",
     "Nothing: it can be made alongside the other steps.",
     "The checks step, which shows it in the sky.",
     "The pose imports cleanly, its review sheet is looked at, and a raid screenshot shows the Ophan wider than tall."),
    ("checks", "Prove the Ophan in the sky and land it on staging", "gameplay",
     "Both full suites, a co-op check where the Ophan locks on to and strikes the client with each machine seeing the same "
     "Ophan, beams and outcomes, and the raid-length, Wick and lair checks. `docs/4-systems/fear.md` and `raid.md` updated. "
     "A pull request into the staging branch says Refs #418 and \"Manual co-op test still owed\". Merged into staging "
     "locally and checked again there only when everything is green.",
     "Every step above.",
     "#419 and the demand loop's playtest.",
     "Every check above is green on the feature branch and again on staging, with screenshots looked at."),
]


def gh(*args):
    return subprocess.run(["gh", *args], check=True, capture_output=True, text=True).stdout.strip()


numbers = {}
for key, title, category, what, depends, dependants, done in STEPS:
    body = (f"Part of #418\n\n{what}\n\n**Depends on:** {depends}\n**Depended on by:** {dependants}\n**Done when:** {done}\n\n"
            f"Plan page: `{PAGE}` (approved 2026-10-27)." + SIGN)
    with open(os.path.join(HERE, f"{key}.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write(f"# {title}\n\n{body}")
    url = gh("issue", "create", "--repo", REPO, "--title", title, "--body", body,
             "--label", "AjsAgent created this", "--label", category, "--label", "in progress")
    numbers[key] = url.rsplit("/", 1)[1]
    print(key, url)

parent = (
    "Part of #431\n\n"
    "**Redesigned 2026-10-27 by the owner** (\"Build plan approved: ophan in the sky 1C 2A 3A 4A 5A 6A 7A 8A 9A\"; plan page "
    f"`{PAGE}`; Decisions, 2026-10-27). The Ophan no longer drifts through walls or catches anyone. It hangs far off in the "
    "sky, 150 times its model and about 900 m from the castle, its eye on the players. It appears `HuntWindowSeconds` before "
    "the portal closes with a loud hum and screen shake. Its eye follows whoever carries the most worth, then down the list; "
    "its rings spin by state; its eye pulses for the lock-on and swells for the charge. A slow beam, after a 3-second lock-on, "
    "drains 25 health a second through anything but stone. A charged beam fires after 3 s in cover, kills in one hit and "
    "scorches the room open. A beam on someone carrying an artifact lifts it into the sky; they live and it leaves. Until "
    "the demand (#419) exists, any piece flagged `IsArtifact` is wanted and a test switch forces a hunted raid.\n\n"
    "**Steps:** " + ", ".join(f"#{numbers[k]}" for k, *_ in STEPS) + ".\n\n"
    "**Depends on:** #433 and the art already imported (#414, #436), both on staging.\n"
    "**Depended on by:** #419 and the demand loop's playtest.\n"
    "**Done when:** every step's done-criteria are met, the suites and the co-op checks are green on the feature branch and "
    "on staging, and a pull request into the staging branch says Refs this issue." + SIGN)
with open(os.path.join(HERE, "418.md"), "w", encoding="utf-8", newline="\n") as f:
    f.write(parent)
gh("issue", "edit", "418", "--repo", REPO, "--body", parent)
print("418 body updated")
json.dump(numbers, open(os.path.join(HERE, "numbers.json"), "w"), indent=1)
