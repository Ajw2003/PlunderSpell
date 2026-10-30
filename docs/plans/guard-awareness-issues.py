"""Opens the Guard awareness parent issue and its step issues, then fills in the cross-references."""
import json, subprocess, sys
R = "Ajw2003/PlunderSpell"
L = "AjsAgent created this,in progress"
def gh(*a):
    return subprocess.run(["gh", *a], check=True, capture_output=True, text=True).stdout.strip()

steps = [
 ("base", "Record how guards behave today, before any change", "testing", [], 
  "Record a co-op run (Editor host plus a built client) in the Gym with the five things aj saw: a guard reaching a checked spot, a player across a floor hole, a chase behind a pillar, a fight with three melee guards, and a player standing beside a guard. Screen-recorded with ffmpeg ddagrab and read frame by frame.",
  "The recording and its frame write-up sit in docs/generated/guard-awareness-baseline/, and each of the five moments has a timestamp."),
 ("route", "A guard with no route takes the long way or holds below", "gameplay", ["base"],
  "When a route to the player or to a lead fails, the guard walks to the reachable floor nearest the target instead of giving up (Investigate) or standing still (Chase). A melee guard that still cannot get there holds below, throws and calls for help. The map answers through a new read-only question on IGuardNavigationMap and CastleGuardNavigationMap; the #478 nav files are not edited.",
  "Edit-mode tests cover Chase and Investigate against an unreachable target, and the recorded run shows a guard at a floor hole going round or throwing."),
 ("sight", "Only castle stone blocks a guard's sight", "gameplay", ["base"],
  "The line of sight ignores loot, other guards and anything under a room's Props part (RoomPartSplitter); walls, roofs, floors and doors still block.",
  "An edit-mode test shows a crate and another guard no longer hiding a player while a wall still does, and the recorded run shows a player half behind a prop being seen."),
 ("look", "Standing guards look around and turn toward sounds", "gameplay", ["base"],
  "At an investigated spot and at each patrol pause the guard turns 60 degrees left, then 60 degrees right. A heard noise turns the guard toward where the sound reached it before it walks.",
  "Edit-mode tests cover the sweep and the turn, and the recorded run shows a guard scanning a room."),
 ("cone", "A vision cone in three layers", "gameplay", ["look"],
  "Centre 110 degrees at full range, seen at once; sides out to 180 degrees at half range, noticed after 0.4 s in view; a 2.5 m circle all round. Pure rules in GuardBrain, used by GuardSight.",
  "Edit-mode tests cover each layer and the 0.4 s side delay, and the recorded run shows a player beside a guard being noticed."),
 ("hear", "Hearing gives where a sound came in and which way", "gameplay", ["look"],
  "On each noise a sphere cast (0.3 m thick) runs from the guard toward the source. Clear: the lead is the source. Blocked: the lead is the first doorway or hole on the route toward the source, with a direction back to the source; the guard walks there, faces the direction and pushes on a room at a time while the noise repeats.",
  "Edit-mode tests cover the clear and blocked cases, and the recorded run shows a guard coming through the doorway a footstep was heard behind."),
 ("range", "Sight and guard hearing at 1.5 times their range", "gameplay", ["base"],
  "GuardBrain.BaseSightScale 1.5 becomes 2.25 (21 m calm becomes 31.5 m). Guards hear noises from 1.5 times their radius; the alarm hears at the old radius, so it does not fill faster.",
  "Edit-mode tests cover both ranges and the unchanged alarm radius."),
 ("lose", "A guard loses you only when you are rooms away", "gameplay", ["route", "hear"],
  "Each guard keeps a last-known spot per player, updated by sight, noise and other guards' sightings, and a heading. On losing sight it runs to the spot plus the heading, then searches. It goes back to patrol only after 8 s with nothing new and with the last-known spot 3 or more rooms away (rooms counted on the castle map).",
  "Edit-mode tests cover hiding behind a pillar (keeps hunting) and breaking away three rooms (gives up), and the recorded run shows both."),
 ("push", "Faster guards and two melee swings at once", "gameplay", ["base"],
  "ChaseSpeed 4.5 becomes 5.5 m/s and GuardBrain.MoveSpeed (the alarm speed bonus, up to x1.4) is used for every move. MeleeTurnsPerTarget goes to 2. Leads from cries and the hue and cry are followed at chase speed.",
  "Edit-mode tests cover the speeds per alarm level and two melee turns, and the recorded run shows two guards striking one player."),
 ("qa", "Record the same run after the changes and hand over for QA", "testing", ["route", "sight", "look", "cone", "hear", "range", "lose", "push"],
  "Repeat the baseline co-op run on the finished build, read it frame by frame, and build the QA page with before and after frames and clips.",
  "The QA page is published and every step issue says needs review."),
]
parent_body = lambda nums: f"""aj's co-op playtest of 2026-10-28 found guards staring at objects, ignoring players near beam holes, not aggressive enough, with no real cone or awareness, and giving up too fast. Causes: docs/generated/guard-deep-dive-2026-10-28/findings.md. Approved design: docs/plans/guard-awareness.html (1A 2A 3A 4A 5A).

Steps:
""" + "\n".join(f"- #{nums[k]} {t}" for k, t, *_ in steps) + """

## Depends on
Nothing open; the castle map after a break (#478) is used as it is.

## Depended on by
aj's QA of the guards.

## Done when
Every step is done and aj passes the QA page."""

parent = gh("issue", "create", "-R", R, "-t", "Guard awareness after the co-op playtest", "-b", "Steps follow.", "-l", L + ",gameplay")
pnum = parent.rsplit("/", 1)[1]
nums = {}
for k, t, cat, deps, what, done in steps:
    url = gh("issue", "create", "-R", R, "-t", t, "-b", "Filled in next.", "-l", L + "," + cat)
    nums[k] = url.rsplit("/", 1)[1]
for k, t, cat, deps, what, done in steps:
    users = [s for s in steps if k in s[3]]
    dep = "\n".join(f"#{nums[d]} ({next(s[1] for s in steps if s[0]==d)})" for d in deps) or "Nothing."
    use = "\n".join(f"#{nums[s[0]]} ({s[1]})" for s in users) or f"#{pnum} (aj's QA of the guards)."
    body = f"Part of #{pnum}\n\n{what}\n\n## Depends on\n{dep}\n\n## Depended on by\n{use}\n\n## Done when\n{done}"
    gh("issue", "edit", nums[k], "-R", R, "-b", body)
    iid = gh("api", f"repos/{R}/issues/{nums[k]}", "--jq", ".id")
    gh("api", "-X", "POST", f"repos/{R}/issues/{pnum}/sub_issues", "-F", f"sub_issue_id={iid}")
gh("issue", "edit", pnum, "-R", R, "-b", parent_body(nums))
print(json.dumps({"parent": pnum, **nums}))
