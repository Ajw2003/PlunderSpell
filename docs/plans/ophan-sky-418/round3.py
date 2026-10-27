# Writes round 3 of the #418 build plan page's content (page.json). Run from the repo root, then build with make_page.py.
import json

p = 'docs/plans/ophan-sky-418/page.json'
d = json.load(open(p, encoding='utf-8'))
d['eyebrow'] = "Build plan for approval, round 3 · issue #418, the Ophan of Hours · your answers through 2026-10-27 · nothing built yet"
d['short']['lead'] = (
    "The Ophan appears in the sky 60 seconds before the portal closes, about 63 m tall, announced by a loud hum and screen shake. "
    "It targets whoever carries the most. Its slow beam locks on over 3 seconds and drains health fast; cover stops it. Its charged beam kills in one hit "
    "and can break your cover. A beam on someone carrying the wanted piece lifts the piece into the sky, spares them, and sends the Ophan away. "
    "Your answers are settled below; five things are still open under Choices, each with my suggestion marked.")
it = d['short']['items']
it[0] = ["check", "Your answers", "Settled: arrival 1A 2B 3* 4* 5B 6A 7*, beam 1* 2A 3A, and your scale photos", "done",
         "Wanted means any piece flagged IsArtifact until the demand (#419) exists. It targets whoever carries the most and keeps hunting after a kill. "
         "It stays in the sky and never chases or catches. The slow beam locks on over 3 seconds; getting under cover escapes it, and walls block it. "
         "The charged beam kills in one hit and can take cover down. A beam on the carrier lifts the wanted piece into the sky; they live and it leaves. "
         "Its warning is a loud hum and screen shake. Its size comes from your two photos. Recorded in docs/6-decisions/Decisions.md, 2026-10-27.",
         "Nothing from you."]
it[3] = ["eye", "Step 2", "The two beams", "",
         "The slow beam: a 3-second lock, then health drains while nothing blocks the line from the Ophan to you. The charged beam: one hit, and it breaks "
         "the cover it passes through, as set under Choices. A hit on a carrier lifts the wanted piece away instead. Deaths go through the same path as the "
         "Wick's catch, so every machine sees them. The beam numbers sit on the Ophan's own component, the way the Wick's numbers sit on the guard; the #433 "
         "asset keeps exactly its approved fields.",
         "Nothing until the build is shown."]


def img(src, alt, caption):
    return {"src": src, "alt": alt, "caption": caption}


d['changes'] = [
    ["new", "Your far reference", "A third to half of the screen's height, towering over the treeline.", None,
     "Measured from your photo: its body takes about 26% of the screen's height above the trees, and its raised arm reaches about 39%. "
     "The arm spans about a third of the width.",
     ["", "", ""], img("docs/plans/ophan-sky-418/scale-refs/ophan-scale-far.jpg", "Your far scale reference",
                       "Far: a giant towering over the treeline."), {"visual": True}],
    ["new", "The Ophan from across the castle", "Six times its model: 63 m tall, 72 m across.", None,
     "A preview in a real raid in the Editor, through a camera with the player's 60-degree view, on the curtain wall 84 m away. It takes about 55% of "
     "the screen's height. Hovering 20 m outside the wall instead, it takes about 41% from 110 m. The battlements in front are what a player on the wall "
     "sees. At ground level inside the castle the rooms block the view, so it has to sit high to be seen at all.",
     ["", "", ""], img("docs/plans/ophan-sky-418/scale-preview/over-wall/far-walltop-6x.png",
                       "The Ophan at six times its size, seen from across the castle", "From across the castle, at six times its size."), {"visual": True}],
    ["new", "Your close reference", "Looking up from its feet, it fills almost the whole screen.", None,
     "Measured from your photo: about 95% of the screen's height.",
     ["", "", ""], img("docs/plans/ophan-sky-418/scale-refs/ophan-scale-close.jpg", "Your close scale reference",
                       "Close: looking up from its feet."), {"visual": True}],
    ["new", "The Ophan from beneath", "Standing under it, looking up.", None,
     "The same preview, standing beneath it and looking straight up: it fills the screen, as in your photo. Hovering 20 m outside the wall instead, "
     "it fills only about two-thirds of the screen from the closest point a player can reach.",
     ["", "", ""], img("docs/plans/ophan-sky-418/scale-preview/over-wall/close-ground-6x.png",
                       "The Ophan at six times its size, seen from beneath", "From beneath, at six times its size."), {"visual": True}],
    ["new", "What the castle can break today", "Walls and floors are one piece, and rooms have no roofs.", None,
     "Each room's floor and four walls are a single mesh with a single collider, so no wall can be removed without the floor going too. No room has a "
     "roof: the sky is open above every room, and only the crypt and the keep's lower storey have anything overhead. Door plugs are the only separate "
     "pieces. The castle is never sent over the network: every machine builds it from one shared seed, so any broken wall needs its own sync.",
     ["", "", ""], None, {"visual": False}],
]
d['choices'] = [
    ["place", "Where does it hover? (Its size, six times its model, is set by your photos.)",
     [["A", "Over the curtain wall, on the side farthest from the arrival portal, its lowest point 12 m up", 1,
       "From across the castle it takes about 55% of the screen, and standing beneath it fills the screen, as in both your photos."],
      ["B", "20 m outside the wall, its lowest point 20 m up", 0,
       "From across the castle it takes about 41% of the screen, but from the closest point it fills only about two-thirds."]],
     "Both of your photos are matched only when it hovers over the wall."],
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
     [["A", "Real stone: cover is whenever the straight line from the Ophan to you passes through a wall, or a floor above you", 1,
       "Honest and simple. With no roofs, a room's walls shield you only near the wall facing the Ophan; the crypt and the keep's lower storey shield you everywhere."],
      ["B", "Being inside any room counts as cover", 0,
       "Easier to read, but a room open to the sky would hide you from something hovering above it."]],
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
d['wrapup']['summary'] = ("A plan for the Ophan of Hours as a celestial threat at six times its size: it appears in the sky with a hum and screen shake, "
                          "locks on to whoever carries the most, drains health with a slow beam that cover stops, and breaks cover with a one-hit charged beam.")
d['wrapup']['blocking'] = "Your five picks under Choices. Nothing is built until you approve."
json.dump(d, open(p, 'w', encoding='utf-8', newline='\n'), indent=2, ensure_ascii=False)
print("page.json written")
