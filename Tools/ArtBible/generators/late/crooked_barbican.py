from common_enemy import *

S = 55  # section: 1 m = 55 px
sh = Sheet("crooked-barbican")
sh.frame(f"{AGE_NAME} · STRUCTURE · CURTAINWALL", "The Crooked Barbican",
         "12 × 12 m cell · walls 5.20 m (open) · ≤ 25k tris", "section 1 m = 55 px · plan 1 m = 30 px",
         glow_c="#3A2410", glow_xy=("28%", "62%"))
FL = 0.30
struct_under(sh, S, 8, marks=((FL, "fl"), (FL + 3.0, "+3.0"), (FL + 5.2, "+5.2")))
V = View(100, 690, S)

sh.d(f'<pattern id="ashlar" width="{S}" height="{S/2}" patternUnits="userSpaceOnUse" x="100" y="690">'
     f'<rect width="{S}" height="{S/2}" fill="#A88B64"/>'
     f'<path d="M0 0 H{S} M0 {S/2} H{S} M{S*0.3} 0 V{S/2}" stroke="#6E5A40" stroke-width="1"/>'
     f'<path d="M1 1.5 H{S-1}" stroke="#C9AE84" stroke-width=".8" opacity=".6"/></pattern>')
sh.d(f'<pattern id="ashlar2" width="{S}" height="{S}" patternUnits="userSpaceOnUse" x="{100 + S*0.5}" y="{690 - S/2}">'
     f'<rect width="{S}" height="{S}" fill="none"/><path d="M{S*0.3} {S/2} V{S}" stroke="#6E5A40" stroke-width="1"/></pattern>')
sh.d('<pattern id="cut" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
     '<rect width="8" height="8" fill="#7A6A52"/><path d="M0 0 V8" stroke="#4A3E2E" stroke-width="2"/></pattern>')
sh.d(f'<pattern id="brick" width="{S*0.25}" height="{S*0.08}" patternUnits="userSpaceOnUse">'
     f'<rect width="{S*0.25}" height="{S*0.08}" fill="#8A4B32"/><path d="M0 0 H{S*0.25} M{S*0.12} 0 V{S*0.08}" stroke="#4A2618" stroke-width=".8"/></pattern>')
sh.lin("oakG", [(0, "#3A2A1A"), (.5, "#6B4F33"), (1, "#4A3622")])
sh.rad("fire", [(0, "#FFD9A0", .9), (.15, "#C4542E", .55), (1, "#C4542E", 0)])
sh.rad("warm", [(0, "#8A5A2A", .45), (1, "#14120E", 0)])
sh.lin("dark", [(0, "#0B0A08", .85), (1, "#0B0A08", .35)], 0, 0, 0, 1)

R = lambda x0, y0, x1, y1: f"M {x0} {y0} L {x1} {y0} L {x1} {y1} L {x0} {y1} Z"

# ───────── SECTION (cut along the first leg, looking east; south at left) ─────────
# far wall of the passage (elevation, ashlar), deck and parapet beyond
sh.path(V, R(1.5, FL, 9.3, FL + 4.0), "url(#ashlar)")
sh.path(V, R(1.5, FL, 9.3, FL + 4.0), "url(#ashlar2)")
sh.path(V, R(1.5, FL, 9.3, FL + 4.0), "url(#dark)")
# the dog-leg: the second leg opens east through this far wall (pointed arch into darkness)
sh.path(V, f"M 6.7 {FL} L 6.7 {FL+2.2} C 6.7 {FL+2.8} 7.4 {FL+3.2} 8.0 {FL+3.4} C 8.6 {FL+3.2} 9.3 {FL+2.8} 9.3 {FL+2.2} L 9.3 {FL} Z", "#0B0A08", "#6E5A40", 1.5)
sh.path(V, f"M 7.0 {FL} L 7.0 {FL+2.1} C 7.0 {FL+2.6} 7.5 {FL+2.95} 8.0 {FL+3.1} C 8.5 {FL+2.95} 9.0 {FL+2.6} 9.0 {FL+2.1} L 9.0 {FL} Z", "#14120E")
sh.add(f'<text x="{V.x(8.0):.1f}" y="{V.y(FL+1.4):.1f}" text-anchor="middle" font-family="{MONO}" font-size="9" fill="#635C4C">TURNS EAST</text>')
# keyhole gun-loops from the guardroom, brick-lined
for yy in (2.8, 5.2):
    sh.add(f'<rect x="{V.x(yy)-12:.1f}" y="{V.y(FL+2.05):.1f}" width="24" height="{1.3*S:.1f}" fill="url(#brick)" stroke="#4A2618"/>')
    sh.add(keyhole(V.x(yy), V.y(FL + 1.0), S))
# transverse vault ribs + the pointed barrel vault seen in section (intrados crown 3.80)
sh.path(V, R(1.5, FL + 3.5, 9.3, FL + 4.0), "url(#brick)", "#4A2618", 1)
for yy in (1.5, 3.4, 5.4, 7.4, 9.1):
    sh.path(V, R(yy, FL + 3.3, yy + 0.25, FL + 4.0), "#8C7454", "#4A3E2E", 1)
# cut: floor slab, south wall with outer gate, north block, deck, parapets
sh.path(V, R(0, 0, 12, FL), "url(#cut)", "#2A241C", 1)
sh.path(V, R(0, FL + 3.0, 1.5, FL + 5.2), "url(#cut)", "#2A241C", 1.2)  # over the gate
sh.path(V, f"M 0 {FL+3.0} L 1.5 {FL+3.0} L 1.5 {FL+3.1} C 1.1 {FL+3.0} 0.4 {FL+3.0} 0 {FL+3.1} Z", "#6E5A40")
sh.path(V, R(9.3, FL, 12, FL + 5.2), "url(#cut)", "#2A241C", 1.2)  # north block (stair behind)
sh.path(V, R(1.5, FL + 4.0, 9.3, FL + 4.3), "url(#cut)", "#2A241C", 1.2)  # deck over the vault
# parapet + merlons on the deck
sh.path(V, R(1.5, FL + 4.3, 1.9, FL + 5.2), "url(#ashlar)", "#6E5A40", 1)
sh.path(V, R(8.9, FL + 4.3, 9.3, FL + 5.2), "url(#ashlar)", "#6E5A40", 1)
# murder-holes: shafts through the vault to the deck
for yy in (2.4, 4.0, 5.6, 7.2, 8.4):
    sh.path(V, R(yy - 0.15, FL + 3.5, yy + 0.15, FL + 4.3), "#0B0A08", "#C4542E", 1.2)
# outer gate: two-leaf oak door hung open against the jamb, iron studs
sh.path(V, R(1.5, FL, 1.62, FL + 2.95), "url(#oakG)", "#1E150C", 1)
sh.path(V, R(1.62, FL, 2.9, FL + 2.95), "url(#oakG)", "#1E150C", 1)
for i in range(6):
    for j in range(3):
        sh.add(f'<circle cx="{V.x(1.8 + j*0.45):.1f}" cy="{V.y(FL + 0.3 + i*0.45):.1f}" r="1.8" fill="#2E2F31"/>')
sh.path(V, R(1.62, FL + 0.6, 2.9, FL + 0.7), "#2E2F31")
sh.path(V, R(1.62, FL + 2.2, 2.9, FL + 2.3), "#2E2F31")
# the gate slot (slot-machicolation in the gate arch)
sh.path(V, R(0.6, FL + 3.0, 0.85, FL + 5.2), "#0B0A08", "#C4542E", 1.2)
# deck: hot-sand cauldron at a murder-hole, brazier, handgunner silhouette on the deck
sh.path(V, f"M 3.6 {FL+4.3} L 4.4 {FL+4.3} L 4.5 {FL+4.9} L 3.5 {FL+4.9} Z", "#2E2F31", "#0B0A08", 1)
sh.path(V, f"M 3.45 {FL+4.9} L 4.55 {FL+4.9} L 4.55 {FL+4.97} L 3.45 {FL+4.97} Z", "#4A4B4D")
sh.add(f'<circle cx="{V.x(6.3):.1f}" cy="{V.y(FL+4.9):.1f}" r="70" fill="url(#fire)"/>')
sh.path(V, f"M 6.0 {FL+4.3} L 6.6 {FL+4.3} L 6.5 {FL+4.6} L 6.7 {FL+4.8} L 5.9 {FL+4.8} L 6.1 {FL+4.6} Z", "#2E2F31")
sh.path(V, f"M 6.0 {FL+4.8} C 6.1 {FL+5.2} 6.25 {FL+5.0} 6.3 {FL+5.35} C 6.38 {FL+5.05} 6.5 {FL+5.2} 6.6 {FL+4.8} Z", "#C4542E")
sh.add(human(V.x(7.6), V.y(FL + 4.3), S, "#4A3E2E", .95))
# torch in the passage + firelight
sh.add(f'<circle cx="{V.x(4.8):.1f}" cy="{V.y(FL+2.3):.1f}" r="120" fill="url(#warm)"/>')
sh.add(f'<circle cx="{V.x(4.8):.1f}" cy="{V.y(FL+2.3):.1f}" r="40" fill="url(#fire)"/>')
sh.path(V, f"M 4.75 {FL+1.9} L 4.85 {FL+1.9} L 4.87 {FL+2.25} L 4.73 {FL+2.25} Z", "#2E2F31")
sh.path(V, f"M 4.72 {FL+2.25} C 4.72 {FL+2.45} 4.8 {FL+2.4} 4.8 {FL+2.6} C 4.85 {FL+2.42} 4.9 {FL+2.45} 4.88 {FL+2.25} Z", "#C4542E")
# the standard human in the passage
sh.add(human(V.x(3.6), V.y(FL), S, "#DCD2BA", .5))
sh.flecks(V, (1.5, FL + 2.8, 9.3, FL + 3.5), 40, "#1E1B17", 7, .03, .08, .55)  # soot under the vault
# dimension: vault crown
sh.text(V.x(6.0), V.y(FL + 3.05), "vault crown +3.80 · deck +4.30", 9.5, "#DCD2BA")
sh.text(V.x(0.1), V.y(-0.2) + 14, "S · OUTER GATE", 9.5, "#9A9078")
sh.text(V.x(9.35), V.y(-0.2) + 14, "N · STAIR BLOCK", 9.5, "#9A9078")

# ───────── PLAN ─────────
PS, PX0, PY0 = 30, 800, 150
P = lambda x, y: (PX0 + x * PS, PY0 + (12 - y) * PS)
def prect(x0, y0, x1, y1, fill, stroke="#2A241C", sw=1):
    a, b = P(x0, y1); c, d = P(x1, y0)
    sh.add(f'<rect x="{a:.1f}" y="{b:.1f}" width="{c-a:.1f}" height="{d-b:.1f}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
prect(0, 0, 12, 12, "url(#cut)", "#635C4C", 1.5)
# open floor areas
prect(1.7, 1.5, 4.3, 9.3, "#2A241C")   # leg 1
prect(1.7, 6.7, 10.5, 9.3, "#2A241C")  # leg 2 (to the inner gate)
prect(10.5, 6.7, 12, 9.3, "#2A241C")   # inner gate through the east wall
prect(1.7, 0, 4.3, 1.5, "#2A241C")     # outer gate through the south wall
prect(5.8, 1.5, 10.5, 5.2, "#322A20")  # guardroom
prect(5.0, 9.8, 11.0, 11.0, "#322A20") # stair to the deck
for i in range(12):
    a, b = P(5.0 + i * 0.5, 9.8); c, d = P(5.0 + i * 0.5, 11.0)
    sh.add(f'<line x1="{a:.1f}" y1="{b:.1f}" x2="{c:.1f}" y2="{d:.1f}" stroke="#635C4C" stroke-width=".8"/>')
# guardroom door onto leg 2
prect(8.0, 5.2, 9.0, 6.7, "#322A20", "none")
# dog-leg arrow
pts = [P(3.0, 0.6), P(3.0, 8.0), P(11.4, 8.0)]
sh.add('<polyline points="' + " ".join(f"{a:.1f},{b:.1f}" for a, b in pts) + '" fill="none" stroke="#9A9078" stroke-width="1.2" stroke-dasharray="4 3"/>')
a, b = P(11.4, 8.0)
sh.add(f'<path d="M {a:.1f} {b:.1f} L {a-7:.1f} {b-4:.1f} L {a-7:.1f} {b+4:.1f} Z" fill="#9A9078"/>')
# murder-holes (plan)
for (x, y) in ((3.0, 2.4), (3.0, 4.0), (3.0, 5.6), (3.0, 7.2), (5.5, 8.0), (7.5, 8.0), (9.5, 8.0)):
    a, b = P(x, y)
    sh.add(f'<rect x="{a-4:.1f}" y="{b-4:.1f}" width="8" height="8" fill="#0B0A08" stroke="#C4542E" stroke-width="1.2"/>')
# gun-loops (plan) as small keyhole ticks
loops = [((1.0, 0.0), "v"), ((5.2, 0.0), "v"), ((5.8, 2.8), "h"), ((5.8, 4.4), "h"), ((12.0, 3.0), "h"), ((0.0, 3.5), "h"), ((0.0, 7.5), "h")]
for (x, y), o in loops:
    a, b = P(x, y)
    if o == "v":
        sh.add(f'<rect x="{a-2:.1f}" y="{b-22:.1f}" width="4" height="22" fill="#0B0A08" stroke="#8A4B32"/><circle cx="{a:.1f}" cy="{b-22:.1f}" r="3.5" fill="#0B0A08" stroke="#8A4B32"/>')
    else:
        dx = 22 if x < 6 and x > 0 else (-22 if x >= 12 else 22)
        if 5 < x < 6:
            dx = -22
        sh.add(f'<rect x="{min(a, a+dx):.1f}" y="{b-2:.1f}" width="22" height="4" fill="#0B0A08" stroke="#8A4B32"/><circle cx="{a+dx:.1f}" cy="{b:.1f}" r="3.5" fill="#0B0A08" stroke="#8A4B32"/>')
# gates (plan): leaves
for (x0, y0, x1, y1) in ((1.7, 1.45, 4.3, 1.55), (10.45, 6.7, 10.55, 9.3)):
    prect(x0, y0, x1, y1, "#6B4F33", "#1E150C")
# 1 m ticks
for i in range(13):
    a, b = P(i, 0); sh.add(f'<line x1="{a:.1f}" y1="{b:.1f}" x2="{a:.1f}" y2="{b+5:.1f}" stroke="#9A9078"/>')
    a, b = P(0, i); sh.add(f'<line x1="{a-5:.1f}" y1="{b:.1f}" x2="{a:.1f}" y2="{b:.1f}" stroke="#9A9078"/>')
a, b = P(0, 0)
sh.text(a, b + 18, "0", 9, "#9A9078", "middle"); sh.text(P(12, 0)[0], b + 18, "12 m", 9, "#9A9078", "middle")
sh.text(P(12, 12)[0] - 4, P(12, 12)[1] - 6, "N ↑", 10, "#9A9078", "end")
# plan labels
lab = lambda x, y, t, c="#DCD2BA", anc="middle": sh.text(P(x, y)[0], P(x, y)[1], t, 9, c, anc)
lab(3.0, -0.9, "DOOR · OUTER GATE 2.60 × 3.00")
lab(3.0, 3.2, "LEG 1", "#9A9078")
lab(7.2, 8.35, "LEG 2", "#9A9078")
lab(8.15, 3.5, "GUARDROOM", "#9A9078")
lab(8.15, 2.9, "4.7 × 3.7 m", "#635C4C")
lab(8.0, 10.25, "STAIR → DECK +4.30", "#DCD2BA")
lab(11.9, 7.0, "DOOR · INNER GATE", "#DCD2BA", "end")
lab(1.8, 6.3, "MURDER-HOLE ×7", "#C4542E", "start")
lab(-0.2, 5.3, "ARROW-LOOP", "#8A4B32", "end")
lab(-0.2, 4.9, "(KEYHOLE GUN) ×7", "#8A4B32", "end")

sh.callout(V.x(4.0), V.y(FL + 3.9), 520, 150, "MURDER-HOLES", "0.30 m shafts, hot sand")
sh.callout(V.x(5.2), V.y(FL + 1.2), 520, 205, "KEYHOLE GUN-LOOP", "0.20 m round + 0.90 slit")
sh.callout(V.x(2.2), V.y(FL + 1.6), 250, 205, "OUTER GATE", "oak leaves, iron studs", "end")

sh.palette([("#A88B64", "dressed sandstone"), ("#7A6A52", "rubble core"), ("#8A4B32", "brick lining"),
            ("#6B4F33", "oak gates"), ("#2E2F31", "iron"), ("#1E1B17", "soot")])
sh.write()
