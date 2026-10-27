# Writes round 4 of the #418 build plan page's content (page.json): the Ophan far off in the sky, after the owner said six
# times was far too small. Run from the repo root, then build with make_page.py.
import json

p = 'docs/plans/ophan-sky-418/page.json'
d = json.load(open(p, encoding='utf-8'))
sky = 'docs/plans/ophan-sky-418/scale-preview/sky/'
d['eyebrow'] = "Build plan for approval, round 4 · issue #418, the Ophan of Hours · your answers through 2026-10-27 · nothing built yet"
d['short']['lead'] = (
    "The Ophan hangs far off in the sky, a kilometre or more from the castle, so large it dwarfs it, always facing the castle with its eye on the players. "
    "It appears 60 seconds before the portal closes with a loud hum and screen shake, and targets whoever carries the most. Its slow beam locks on over "
    "3 seconds and drains health fast; cover stops it. Its charged beam kills in one hit and can break your cover. A beam on someone carrying the wanted "
    "piece lifts the piece into the sky, spares them, and sends the Ophan away. Six things are still open under Choices, each with my suggestion marked.")
d['short']['items'][0][2] = "Settled: arrival 1A 2B 3* 4* 5B 6A 7*, beam 1* 2A 3A, your scale photos, and far off in the sky"


def img(src, alt, caption):
    return {"src": src, "alt": alt, "caption": caption}


d['changes'] = [
    ["new", "Your far reference", "A third to half of the screen's height, towering over the treeline.", None,
     "Measured from your photo: its body takes about 26% of the screen's height above the trees, and its raised arm reaches about 39%.",
     ["", "", ""], img("docs/plans/ophan-sky-418/scale-refs/ophan-scale-far.jpg", "Your far scale reference",
                       "Far: a giant towering over the treeline."), {"visual": True}],
    ["new", "Your close reference", "It fills almost the whole screen.", None,
     "Measured from your photo: about 95% of the screen's height.",
     ["", "", ""], img("docs/plans/ophan-sky-418/scale-refs/ophan-scale-close.jpg", "Your close scale reference",
                       "Close: it fills the screen."), {"visual": True}],
    ["new", "Option C from a courtyard", "150 times its model: 1.4 km tall, 1.65 km across, 900 m out.", None,
     "A preview in a real raid in the Editor, through the player's 60-degree view, from an open courtyard 1.1 km away. It is 68 degrees tall, more than "
     "the whole view, so its wings run off the top of the screen; it is 75 degrees across. The castle's towers stand under it like toys.",
     ["", "", ""], img(sky + "courtyard-150x-900m.png", "The Ophan at 150 times its size over the castle, from a courtyard",
                       "Option C, from a courtyard: it overflows the view."), {"visual": True}],
    ["new", "Option B from inside a room", "117 times its model: 1.1 km tall, 1.3 km across, 1 km out.", None,
     "From inside a room, which is open to the sky: 51 degrees tall (84% of the view's height) and 57 degrees across. Its eye looks straight down at you "
     "over the room's walls.",
     ["", "", ""], img(sky + "room-117x-1000m.png", "The Ophan at 117 times its size, seen from inside a room",
                       "Option B, from inside a room."), {"visual": True}],
    ["new", "Option A from the wall-walk", "52 times its model: 500 m tall, 570 m across, 600 m out.", None,
     "From the curtain wall's walk: 38 degrees tall (63% of the view's height) and 43 degrees across. Large, but it sits inside the sky rather than filling it.",
     ["", "", ""], img(sky + "wallwalk-52x-600m.png", "The Ophan at 52 times its size, seen from the wall-walk",
                       "Option A, from the wall-walk."), {"visual": True}],
    ["new", "The player's camera cuts it off today", "Its far clip plane is 1000 m; the Ophan sits 700 m to 1.2 km away.", None,
     "This is the same courtyard view as option B, drawn with the player's own camera setting: everything beyond 1000 m is not drawn, so only a sliver "
     "of the Ophan shows. The build raises the player camera's far clip plane to 3000 m. That is one setting, and the castle is small enough that it "
     "costs nothing measurable. Fog is off in raids today; the castle's light haze only softens the Ophan's lower edge.",
     ["", "", ""], img(sky + "courtyard-playerclip-117x-1000m.png", "The Ophan cut off by the player's 1000 m far clip plane",
                       "Option B with today's 1000 m far clip: most of it is not drawn."), {"visual": True}],
    ["new", "What the castle can break today", "Walls and floors are one piece, and rooms have no roofs.", None,
     "Each room's floor and four walls are a single mesh with a single collider, so no wall can be removed without the floor going too. No room has a "
     "roof: the sky is open above every room, and only the crypt and the keep's lower storey have anything overhead. Door plugs are the only separate "
     "pieces. The castle is never sent over the network: every machine builds it from one shared seed, so any broken wall needs its own sync.",
     ["", "", ""], None, {"visual": False}],
    ["new", "What a beam looks like from a kilometre away", "A thin line would vanish; it has to be drawn huge.", None,
     "At a kilometre, a beam two metres wide is a tenth of a degree across, too thin to see. So the beam is drawn tens of metres wide where it leaves the "
     "eye, narrowing to a pool of light on the target, and it glows. Because the source is so far away, the warning has to show at the eye itself: the eye "
     "brightens through the 3-second lock and the charge, which every player can see wherever they stand.",
     ["", "", ""], None, {"visual": False}],
]
d['choices'] = [
    ["size", "How big, and how far off? (It always faces the castle, eye first, and never comes near it.)",
     [["A", "52 times its model, 600 m out: 63% of the view's height", 0, "Large, but it sits in the sky rather than filling it."],
      ["B", "117 times its model, 1 km out: 84% of the view's height", 0, "Fills most of the view; you can take it in at a glance."],
      ["C", "150 times its model, 900 m out: more than the whole view", 1,
       "You cannot take it all in at once; you look up and around to see it, which is what taking up the sky means."]],
     "Farther off also means it looks the same from everywhere in the castle: it never seems to come closer."],
    ["wings", "You asked for it wider. Its wings rise in a V, so on screen it is taller than it is wide. Spread them?",
     [["A", "Yes: a new held pose with the wings spread flat, so it is about half again as wide as it is tall", 1,
       "About a day in the art pipeline: one new pose on the existing rig, checked on a review sheet like the other clips."],
      ["B", "No: keep the V; the size alone is enough", 0, ""]],
     "The model is 11 m across and 9.6 m tall; only a pose change makes it read wider."],
    ["hum", "The hum: you wrote \"a loud menstrual hum\". Which did you mean?",
     [["A", "A monstrous hum: a low, loud drone for the 4-second arrival, made in code until real audio exists, with the existing camera shake", 1,
       "There is no Ophan sound in the project yet; a generated drone is a stand-in you can replace. The game already has a camera-shake system."],
      ["B", "Another word: tell me", 0, ""]],
     "A stand-in hum now, real audio later."],
    ["numbers", "The beams' numbers",
     [["A", "Slow beam: 25 health a second (4 seconds from full). Charged beam: it charges for 3 seconds once its target has been behind cover for 3 seconds, then waits 10 seconds before it can charge again", 1,
       "Fast enough to frighten, slow enough to reach cover; hiding works for a moment, then it blasts the cover."],
      ["B", "Slow beam 50 a second (2 seconds); charged beam on a fixed timer every 15 seconds", 0, ""],
      ["C", "Slow beam 15 a second (about 7 seconds); charged beam every 20 seconds", 0, ""]],
     "The 3-second lock-on is already settled by your answer."],
    ["cover", "What counts as cover from the slow beam?",
     [["A", "Real stone: cover is whenever the straight line from the Ophan's eye to you passes through a wall, or a floor above you", 1,
       "It comes in low from one side, about 30 degrees up, so the wall on its side of a room shields you; the crypt and the keep's lower storey shield you everywhere."],
      ["B", "Being inside any room counts as cover", 0,
       "Easier to read, but a room open to the sky would hide you from something looking straight into it."]],
     "The castle has no roofs today, so this decides how much shelter there is."],
    ["break", "How much does the charged beam break?",
     [["A", "It scorches the stone open: for the rest of the raid the slow beam passes through that room's walls, drawn burnt, on every machine", 1,
       "The smallest build, about two days: one synced list of scorched rooms and a burnt material, no new art. The walls stay solid to walk into."],
      ["B", "It knocks the hit wall down", 0,
       "About two weeks: every room in every Age has its walls split from its floor in the art pipeline, plus a sync of fallen walls and updated guard paths."],
      ["C", "It punches a hole where it hits", 0,
       "The largest and riskiest, several weeks: cutting meshes and rebuilding colliders while the game runs, kept the same on every machine."]],
     "Rooms are one piece today, so how much it breaks sets the size of the build."],
]
d['wrapup']['summary'] = ("A plan for the Ophan of Hours as a celestial threat far off in the sky, large enough to dwarf the castle, eye always on the "
                          "players: a hum and screen shake, a slow beam that cover stops, and a one-hit charged beam that breaks cover.")
d['wrapup']['blocking'] = "Your six picks under Choices. Nothing is built until you approve."
json.dump(d, open(p, 'w', encoding='utf-8', newline='\n'), indent=2, ensure_ascii=False)
print("page.json written")
