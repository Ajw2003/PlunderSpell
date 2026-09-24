from common_enemy import *

S = 55
sh = Sheet("counting-house")
sh.frame(f"{AGE_NAME} · STRUCTURE · INNERWARD", "The Counting House",
         "12 × 12 m cell · walls 4.00 m · ≤ 25k tris", "section A 1 m = 55 px · plan 1 m = 30 px",
         glow_c="#3A2410", glow_xy=("35%", "66%"))
FL = 0.30
struct_under(sh, S, 7, marks=((FL, "fl"), (FL + 2.88, "+2.9"), (FL + 4.0, "+4.0")))
V = View(100, 690, S)
R = lambda x0, y0, x1, y1: f"M {x0} {y0} L {x1} {y0} L {x1} {y1} L {x0} {y1} Z"

sh.d(f'<pattern id="ashlar" width="{S}" height="{S/2}" patternUnits="userSpaceOnUse" x="100" y="690">'
     f'<rect width="{S}" height="{S/2}" fill="#A88B64"/>'
     f'<path d="M0 0 H{S} M0 {S/2} H{S} M{S*0.3} 0 V{S/2}" stroke="#6E5A40" stroke-width="1"/></pattern>')
sh.d('<pattern id="cut" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
     '<rect width="8" height="8" fill="#7A6A52"/><path d="M0 0 V8" stroke="#4A3E2E" stroke-width="2"/></pattern>')
sh.d(f'<pattern id="brick" width="{S*0.25}" height="{S*0.08}" patternUnits="userSpaceOnUse">'
     f'<rect width="{S*0.25}" height="{S*0.08}" fill="#8A4B32"/><path d="M0 0 H{S*0.25} M{S*0.12} 0 V{S*0.08}" stroke="#4A2618" stroke-width=".8"/></pattern>')
sh.d('<pattern id="cheq" width="16" height="16" patternUnits="userSpaceOnUse"><rect width="16" height="16" fill="#4F5E3A"/>'
     '<rect width="8" height="8" fill="#34422A"/><rect x="8" y="8" width="8" height="8" fill="#34422A"/></pattern>')
sh.lin("oak", [(0, "#3A2A1A"), (.5, "#6B4F33"), (1, "#4A3622")])
sh.lin("oakV", [(0, "#7A5A3A"), (1, "#3A2A1A")], 0, 0, 0, 1)
sh.lin("iron", [(0, "#5E5F60"), (.4, "#2E2F31"), (1, "#131416")], 0, 0, 0, 1)
sh.rad("fire", [(0, "#FFD9A0", .95), (.2, "#C4542E", .5), (1, "#C4542E", 0)])
sh.rad("warm", [(0, "#8A5A2A", .5), (1, "#14120E", 0)])
sh.lin("dark", [(0, "#0B0A08", .8), (1, "#0B0A08", .3)], 0, 0, 0, 1)
sh.lin("day", [(0, "#DCD2BA", .18), (1, "#DCD2BA", 0)], 1, 0, 0, 0)

# far wall (north) in elevation
sh.path(V, R(1.0, FL, 11.0, FL + 4.0), "url(#ashlar)")
sh.path(V, R(1.0, FL, 4.7, FL + 4.0), "url(#brick)")  # strong room face, brick-skinned
sh.path(V, R(1.0, FL, 11.0, FL + 4.0), "url(#dark)")
# north archway 2.60 × 2.88
sh.path(V, f"M 4.7 {FL} L 4.7 {FL+2.2} C 4.7 {FL+2.6} 5.4 {FL+2.84} 6.0 {FL+2.88} C 6.6 {FL+2.84} 7.3 {FL+2.6} 7.3 {FL+2.2} L 7.3 {FL} Z", "#0B0A08", "#6E5A40", 1.2)
# strong-room door: oak, iron lattice, studs, lock plate, hasp + padlock, judas grille
sh.path(V, R(1.8, FL, 3.1, FL + 2.35), "#2A241C", "#6E5A40", 2)
sh.path(V, R(1.9, FL, 3.0, FL + 2.25), "url(#oak)", "#1E150C", 1)
sh.add(f'<clipPath id="doorclip"><path d="{V.p(R(1.9, FL, 3.0, FL + 2.25))}"/></clipPath><g clip-path="url(#doorclip)">')
for k in range(-4, 6):
    sh.path(V, f"M {1.9 + k*0.3} {FL} L {1.9 + k*0.3 + 2.25} {FL+2.25}", stroke="#2E2F31", sw=4)
    sh.path(V, f"M {3.0 - k*0.3} {FL} L {3.0 - k*0.3 - 2.25} {FL+2.25}", stroke="#2E2F31", sw=4)
sh.add('</g>')
for k in range(-3, 5):
    for j in range(8):
        x = 1.9 + 0.15 + j * 0.3 * 0.5 + (k % 2) * 0.075
        y = FL + 0.15 + k * 0.3
        if 1.95 < x < 2.95 and FL + 0.05 < y < FL + 2.2:
            pass
for i in range(7):
    for j in range(4):
        sh.add(f'<circle cx="{V.x(2.02 + j*0.28):.1f}" cy="{V.y(FL + 0.15 + i*0.32):.1f}" r="2" fill="#5E5F60"/>')
sh.path(V, R(2.65, FL + 0.95, 2.92, FL + 1.35), "url(#iron)", "#0E0F10", 1)  # lock plate
sh.path(V, R(2.76, FL + 1.1, 2.8, FL + 1.22), "#0B0A08")
sh.path(V, f"M 2.3 {FL+1.18} L 2.62 {FL+1.18}", stroke="#2E2F31", sw=3)  # hasp
sh.path(V, f"M 2.2 {FL+1.2} L 2.36 {FL+1.2} L 2.36 {FL+1.04} L 2.2 {FL+1.04} Z", "url(#iron)", "#0E0F10", 1)  # padlock
sh.path(V, R(2.25, FL + 1.7, 2.65, FL + 1.95), "#0B0A08", "#2E2F31", 1)
for x in (2.33, 2.45, 2.57):
    sh.path(V, f"M {x} {FL+1.7} L {x} {FL+1.95}", stroke="#2E2F31", sw=2)
# ledger shelves on the north wall beside the archway
sh.path(V, R(7.6, FL, 10.8, FL + 3.3), "url(#oak)", "#1E150C", 1)
sh.path(V, R(7.7, FL + 0.1, 10.7, FL + 3.2), "#1E1810")
books = random.Random(3)
for level in range(4):
    y0 = FL + 0.45 + level * 0.72
    sh.path(V, R(7.6, y0 - 0.06, 10.8, y0), "url(#oakV)", "#1E150C", .6)
    x = 7.75
    while x < 10.6:
        w = books.uniform(0.07, 0.14)
        h = books.uniform(0.35, 0.55)
        if books.random() < 0.18:  # a rolled bill in a pigeonhole
            sh.add(f'<circle cx="{V.x(x+0.06):.1f}" cy="{V.y(y0+0.08):.1f}" r="2.6" fill="#A89C7E" stroke="#6E6656"/>')
            x += 0.14
            continue
        c = books.choice(["#5A3A26", "#6E4A30", "#4A3020", "#7A5236", "#3A2618"])
        sh.path(V, R(x, y0, x + w, y0 + h), c, "#1E150C", .5)
        sh.path(V, R(x, y0 + h * 0.7, x + w, y0 + h * 0.76), "#8A6446")
        x += w + 0.01
# coin sacks at the foot of the shelves
for x in (7.9, 8.4):
    sh.path(V, f"M {x} {FL} C {x-0.05} {FL+0.25} {x+0.05} {FL+0.35} {x+0.15} {FL+0.38} C {x+0.25} {FL+0.35} {x+0.35} {FL+0.25} {x+0.3} {FL} Z", "#8A7456", "#3A2A1A", .8)
# daylight falling from the barred window
sh.path(V, f"M 11.0 {FL+2.6} L 11.0 {FL+1.3} L 7.2 {FL} L 5.5 {FL} Z", "url(#day)")
# counting table: chequered counting cloth, balance, coin stacks, open ledger, candles
sh.path(V, R(3.6, FL + 0.74, 7.6, FL + 0.80), "url(#oakV)", "#1E150C", 1)
sh.path(V, R(3.8, FL, 3.9, FL + 0.74), "url(#oak)"); sh.path(V, R(7.3, FL, 7.4, FL + 0.74), "url(#oak)")
sh.path(V, f"M 3.55 {FL+0.8} L 7.65 {FL+0.8} L 7.68 {FL+0.45} L 3.52 {FL+0.45} Z", "url(#cheq)", "#1A2012", .8)
sh.path(V, f"M 3.52 {FL+0.45} L 7.68 {FL+0.45} L 7.68 {FL+0.42} L 3.52 {FL+0.42} Z", "#A8905E")
# balance scale
sh.path(V, R(5.18, FL + 0.8, 5.22, FL + 1.35), "#2E2F31")
sh.path(V, f"M 4.9 {FL+1.33} L 5.5 {FL+1.33}", stroke="#2E2F31", sw=2.5)
for x in (4.9, 5.5):
    sh.path(V, f"M {x} {FL+1.33} L {x-0.08} {FL+1.02} M {x} {FL+1.33} L {x+0.08} {FL+1.02}", stroke="#5E5F60", sw=.8)
    sh.path(V, f"M {x-0.1} {FL+1.02} L {x+0.1} {FL+1.02} C {x+0.08} {FL+0.97} {x-0.08} {FL+0.97} {x-0.1} {FL+1.02} Z", "#5E5F60", "#1E1B17", .6)
# coin stacks (gold is loot)
for i, (x, n) in enumerate(((4.1, 6), (4.25, 4), (4.4, 8), (6.0, 5), (6.15, 7))):
    for k in range(n):
        sh.path(V, R(x, FL + 0.8 + k * 0.02, x + 0.1, FL + 0.8 + (k + 1) * 0.02), "#C9A227", "#5A440E", .4)
# open ledger
sh.path(V, f"M 6.4 {FL+0.8} L 7.2 {FL+0.8} L 7.15 {FL+0.87} L 6.8 {FL+0.84} L 6.45 {FL+0.87} Z", "#D8CCAE", "#8F8468", .8)
# candlestick + glow
sh.add(f'<circle cx="{V.x(3.95):.1f}" cy="{V.y(FL+1.25):.1f}" r="150" fill="url(#warm)"/>')
sh.add(f'<circle cx="{V.x(3.95):.1f}" cy="{V.y(FL+1.25):.1f}" r="30" fill="url(#fire)"/>')
sh.path(V, R(3.92, FL + 0.8, 3.98, FL + 1.18), "#2E2F31")
sh.path(V, R(3.93, FL + 1.18, 3.97, FL + 1.23), "#DCD2BA")
sh.path(V, f"M 3.95 {FL+1.23} C 3.93 {FL+1.27} 3.95 {FL+1.3} 3.95 {FL+1.33} C 3.97 {FL+1.3} 3.97 {FL+1.27} 3.95 {FL+1.23} Z", "#FFD9A0")
# bench
sh.path(V, R(3.9, FL + 0.42, 7.3, FL + 0.47), "url(#oakV)", "#1E150C", .6)

# cut: slab (with the strongbox pit), walls, window, ceiling joists
sh.path(V, f"M 0 0 L 8.3 0 L 8.3 -0.55 L 9.5 -0.55 L 9.5 0 L 12 0 L 12 {FL} L 9.5 {FL} L 9.5 0.0 L 9.42 0.0 L 9.42 -0.47 L 8.38 -0.47 L 8.38 {FL} L 0 {FL} Z", "url(#cut)", "#2A241C", 1)
sh.path(V, R(8.38, -0.47, 9.42, FL), "#0B0A08")
# strongbox in its pit
sh.path(V, R(8.45, -0.45, 9.35, 0.18), "url(#oak)", "#1E150C", 1)
for x in (8.5, 8.9, 9.3):
    sh.path(V, R(x - 0.03, -0.45, x + 0.03, 0.18), "url(#iron)")
sh.path(V, R(8.45, 0.12, 9.35, 0.18), "#2E2F31")
sh.path(V, R(8.85, -0.1, 8.97, 0.02), "url(#iron)", "#0E0F10", .8)
# flagstone trap lid propped open
sh.path(V, f"M 9.42 {FL} L 9.5 {FL} L 10.1 {FL+0.92} L 10.02 {FL+0.95} Z", "#7A6A52", "#2A241C", 1)
sh.path(V, R(9.38, FL - 0.03, 9.48, FL + 0.03), "#2E2F31")
sh.path(V, R(0, FL, 1.0, FL + 4.0), "url(#cut)", "#2A241C", 1.2)
sh.path(V, f"M 11.0 {FL} L 12 {FL} L 12 {FL+4.0} L 11.0 {FL+4.0} L 11.0 {FL+2.75} L 11.45 {FL+2.6} L 11.45 {FL+1.3} L 11.0 {FL+1.15} Z", "url(#cut)", "#2A241C", 1.2)
sh.path(V, R(11.45, FL + 1.3, 12, FL + 2.6), "#2A2E30")
for y in (FL + 1.55, FL + 1.95, FL + 2.35):
    sh.path(V, f"M 11.45 {y} L 12 {y}", stroke="#2E2F31", sw=2.5)
for x in (11.6, 11.8):
    sh.path(V, f"M {x} {FL+1.3} L {x} {FL+2.6}", stroke="#2E2F31", sw=2.5)
sh.path(V, R(0, FL + 4.0, 12, FL + 4.25), "url(#oak)", "#1E150C", 1)  # ceiling boards + joists
for x in (1.5, 3.0, 4.5, 6.0, 7.5, 9.0, 10.5):
    sh.path(V, R(x - 0.1, FL + 3.75, x + 0.1, FL + 4.0), "url(#oak)", "#1E150C", .8)
sh.flecks(V, (1.0, FL + 3.3, 11.0, FL + 3.75), 30, "#1E1B17", 11, .03, .07, .5)
sh.add(human(V.x(9.9), V.y(FL), S, "#DCD2BA", .5))
sh.text(V.x(8.9), V.y(-0.55) + 14, "pit −0.55", 9, "#9A9078", "end")

# ───────── PLAN ─────────
PS, PX0, PY0 = 30, 800, 150
P = lambda x, y: (PX0 + x * PS, PY0 + (12 - y) * PS)
def prect(x0, y0, x1, y1, fill, stroke="#2A241C", sw=1):
    a, b = P(x0, y1); c, d = P(x1, y0)
    sh.add(f'<rect x="{a:.1f}" y="{b:.1f}" width="{c-a:.1f}" height="{d-b:.1f}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
prect(0, 0, 12, 12, "url(#cut)", "#635C4C", 1.5)
prect(1, 1, 11, 7.0, "#2A241C")       # counting room
prect(4.7, 7.0, 11, 11, "#2A241C")    # north passage + ledger alcove
prect(1, 7.8, 3.9, 11, "#3A2218", "#8A4B32")  # strong room (brick-lined)
for (x0, y0, x1, y1) in ((4.7, 0, 7.3, 1), (4.7, 11, 7.3, 12), (0, 4.7, 1, 7.3), (11, 4.7, 12, 7.3)):
    prect(x0, y0, x1, y1, "#2A241C", "none")
prect(1.9, 7.0, 3.0, 7.8, "#2A241C", "none")
prect(1.9, 7.35, 3.0, 7.45, "#6B4F33", "#2E2F31")  # iron-bound door leaf
prect(7.6, 10.6, 10.8, 11.0, "#6B4F33")            # shelves
prect(3.6, 4.2, 7.6, 5.4, "#4F5E3A", "#1A2012")    # counting table
prect(3.9, 3.7, 7.3, 3.95, "#6B4F33", "none")      # bench
prect(8.4, 2.7, 9.4, 3.3, "#2E2F31", "#C9A227")    # floor strongbox
prect(11.0, 2.4, 11.45, 3.6, "#2A2E30", "#2E2F31") # barred window
prect(1.4, 9.8, 2.4, 10.4, "#2E2F31", "#6E5A40")   # second chest in the strong room
prect(2.8, 9.8, 3.6, 10.4, "#2E2F31", "#6E5A40")
a, b = P(-0.4, 3.0); c, d = P(12.4, 3.0)
sh.add(f'<line x1="{a:.1f}" y1="{b:.1f}" x2="{c:.1f}" y2="{d:.1f}" stroke="#C4542E" stroke-width="1" stroke-dasharray="8 3 2 3"/>')
sh.text(c + 2, d + 4, "A", 10, "#C4542E")
for i in range(13):
    a, b = P(i, 0); sh.add(f'<line x1="{a:.1f}" y1="{b:.1f}" x2="{a:.1f}" y2="{b+5:.1f}" stroke="#9A9078"/>')
    a, b = P(0, i); sh.add(f'<line x1="{a-5:.1f}" y1="{b:.1f}" x2="{a:.1f}" y2="{b:.1f}" stroke="#9A9078"/>')
sh.text(P(0, 0)[0], P(0, 0)[1] + 18, "0", 9, "#9A9078", "middle"); sh.text(P(12, 0)[0], P(0, 0)[1] + 18, "12 m", 9, "#9A9078", "middle")
sh.text(P(12, 12)[0] - 4, P(12, 12)[1] - 6, "N ↑", 10, "#9A9078", "end")
lab = lambda x, y, t, c="#DCD2BA", anc="middle": sh.text(P(x, y)[0], P(x, y)[1], t, 9, c, anc)
lab(6.0, -0.9, "DOOR S · ARCH 2.60 × 2.88")
lab(6.0, 12.25, "DOOR N · ARCH")
lab(2.45, 9.3, "STRONG ROOM", "#DCD2BA")
lab(2.45, 8.85, "brick vault", "#9A9078")
lab(2.45, 6.4, "IRON DOOR", "#DCD2BA")
lab(9.2, 9.9, "LEDGER SHELVES", "#DCD2BA")
lab(5.6, 5.8, "COUNTING TABLE", "#DCD2BA")
lab(8.9, 1.9, "FLOOR STRONGBOX", "#C9A227")
lab(10.9, 4.0, "BARRED WINDOW", "#DCD2BA", "end")
lab(1.2, 5.3, "DOOR W", "#DCD2BA", "start")
lab(10.8, 6.1, "DOOR E", "#DCD2BA", "end")

sh.callout(V.x(2.45), V.y(FL + 1.6), 250, 205, "IRON-BOUND DOOR", "oak, lattice straps, 3 locks", "end")
sh.callout(V.x(5.2), V.y(FL + 1.3), 520, 150, "BALANCE + COUNTING CLOTH", "chequered wool, 0.25 m squares")
sh.callout(V.x(9.0), V.y(FL + 2.6), 520, 205, "LEDGER SHELVES", "oak, 4 tiers, pigeonholes")
sh.callout(V.x(11.7), V.y(FL + 2.1), 600, 262, "BARRED WINDOW", "iron grille, 0.55 × 1.30")
sh.callout(V.x(9.4), V.y(-0.2), 660, 712, "FLOOR STRONGBOX", "sunk 0.55, flagstone lid")

sh.palette([("#A88B64", "dressed sandstone"), ("#8A4B32", "brick lining"), ("#6B4F33", "oak"),
            ("#2E2F31", "blackened iron"), ("#4F5E3A", "counting cloth"), ("#C9A227", "coin (loot)"), ("#1E1B17", "soot")])
sh.write()
