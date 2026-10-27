# Writes round 5 of the #418 build plan page's content (page.json): the owner's eye, ring and tell direction from
# 2026-10-27, and what the rig can do. Run from the repo root after round4.py, then build with make_page.py.
import json

p = 'docs/plans/ophan-sky-418/page.json'
d = json.load(open(p, encoding='utf-8'))
d['eyebrow'] = "Build plan for approval, round 5 · issue #418, the Ophan of Hours · your answers through 2026-10-27 · nothing built yet"
d['short']['lead'] = (
    "The Ophan hangs far off in the sky, a kilometre or more from the castle, so large it dwarfs it. Its eye follows its target as they move: whoever "
    "carries the most worth, then the next down the list. Its rings spin faster or slower with what it is doing, and its eye glows and grows as it charges "
    "to full blast, with a similar tell for the lock-on. It appears 60 seconds before the portal closes with a loud hum and screen shake. Its slow beam "
    "locks on over 3 seconds and drains health fast; cover stops it. Its charged beam kills in one hit and can break cover. A beam on the carrier of the "
    "wanted piece lifts the piece into the sky, spares them, and sends the Ophan away. Nine things are still open under Choices, each with my suggestion marked.")
items = d['short']['items']
items[0][2] = "Settled: arrival 1A 2B 3* 4* 5B 6A 7*, beam 1* 2A 3A, your scale photos, far off in the sky, and the eye, rings and tells"
items[0][4] += (" Its eye follows its target as they move, choosing whoever carries the most worth, then down the list. Its rings spin faster or slower "
                "by state. Its eye glows and grows while charging, up to full blast, with a similar tell for the lock-on.")
items[2][4] += " Its eye turns to follow its target every frame, and its three rings spin at a speed set by what it is doing."

rig = ["new", "The rig can do all of it", "The eye and the three rings are their own bones, and the pupil already glows.", None,
       "Checked in the model file: the GreatWheel, InnerWheel and TiltWheel bones each carry their own ring (3,530, 1,600 and 1,634 vertices), and the "
       "Eye bone carries the eye (1,049 vertices). Code can turn and scale those four bones every frame after the animation plays, so the eye can follow "
       "a player and the rings can speed up and slow down without new clips. Its one material has an emission map, so the glow can be raised from code. "
       "The pupil is the part that glows, at nine times brightness (docs/4-systems/raid-scene-assembly.md).",
       ["", "", ""], None, {"visual": False}]
d['changes'].insert(6, rig)

d['choices'][2:2] = [
    ["worth", "What does \"most worth\" mean when it picks its target?",
     [["A", "The sell value of the piece a player is carrying, the same worth the portal's tally counts", 1,
       "The same number the haul is judged by, so carrying the best loot is what makes you the target."],
      ["B", "Artifacts first, then sell value", 0, "Whoever holds an artifact is always first."],
      ["C", "Weight carried", 0, ""]],
     "Your rule: the most worth first, then down the list."],
    ["rings", "Which state spins the rings fastest?",
     [["A", "Charging the charged beam is fastest, winding up to full blast; the lock-on is half that; hanging and watching is a slow turn; leaving, they spin down to a stop", 1,
       "The faster the rings, the closer the blow, so you can read its state from anywhere."],
      ["B", "The lock-on is fastest, and the charge slows them to a grinding stop before the blast", 0, ""]],
     "Speed tells you what is coming."],
    ["tells", "How does the lock-on's tell differ from the charge's?",
     [["A", "Lock-on: the eye narrows and pulses, the pulse quickening over the 3 seconds. Charge: the eye swells to half again its size and brightens steadily to full blast", 1,
       "A quickening pulse means move now; a swelling glow means get out of the way of what comes next. They read differently at a glance."],
      ["B", "Both glow and grow, in different colours: gold for the lock-on, white for the charge", 0, ""],
      ["C", "The lock-on shows only by the eye turning on you; only the charge glows", 0, ""]],
     "Both tells sit at the eye, which every player can see from anywhere."],
]
json.dump(d, open(p, 'w', encoding='utf-8', newline='\n'), indent=2, ensure_ascii=False)
print("page.json written:", len(d['choices']), "choices")
