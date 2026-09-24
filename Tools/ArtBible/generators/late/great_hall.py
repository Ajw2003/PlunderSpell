from common_enemy import *

S = 55
sh = Sheet("great-hall")
sh.frame(f"{AGE_NAME} · STRUCTURE · KEEP", "The Great Hall",
         "12 × 12 m cell · walls 4.60 m · roof cap 7.52 m · ≤ 25k tris", "section 1 m = 55 px · plan 1 m = 30 px",
         glow_c="#3E2610", glow_xy=("22%", "68%"))
FL = 0.30
struct_under(sh, S, 8, marks=((FL, "fl"), (FL + 4.6, "+4.6"), (FL + 7.52, "+7.5")))
V = View(100, 690, S)
R = lambda x0, y0, x1, y1: f"M {x0} {y0} L {x1} {y0} L {x1} {y1} L {x0} {y1} Z"

sh.d(f'<pattern id="ashlar" width="{S}" height="{S/2}" patternUnits="userSpaceOnUse" x="100" y="690">'
     f'<rect width="{S}" height="{S/2}" fill="#A88B64"/>'
     f'<path d="M0 0 H{S} M0 {S/2} H{S} M{S*0.3} 0 V{S/2}" stroke="#6E5A40" stroke-width="1"/></pattern>')
sh.d('<pattern id="cut" width="8" height="8" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
     '<rect width="8" height="8" fill="#7A6A52"/><path d="M0 0 V8" stroke="#4A3E2E" stroke-width="2"/></pattern>')
sh.d('<pattern id="mille" width="22" height="22" patternUnits="userSpaceOnUse">'
     '<rect width="22" height="22" fill="#3A4A2C"/><path d="M3 18 C5 14 8 13 10 14 M14 6 C16 3 19 3 20 5" stroke="#6E7E4E" stroke-width="1.4" fill="none"/>'
     '<circle cx="5" cy="6" r="1.8" fill="#A8905E"/><circle cx="16" cy="15" r="1.6" fill="#9E2A2F"/><circle cx="11" cy="20" r="1.2" fill="#D6CDB6"/></pattern>')
sh.d(f'<pattern id="tiles" width="10" height="6" patternUnits="userSpaceOnUse"><rect width="10" height="6" fill="#8A4B32"/><path d="M0 6 H10 M5 0 V6" stroke="#4A2618" stroke-width=".8"/></pattern>')
sh.lin("oak", [(0, "#3A2A1A"), (.5, "#6B4F33"), (1, "#4A3622")])
sh.lin("oakV", [(0, "#6B4F33"), (1, "#2E2014")], 0, 0, 0, 1)
sh.lin("linen", [(0, "#B9B09B"), (.5, "#E4DCC6"), (1, "#A8A08C")])
sh.lin("red", [(0, "#5E171B"), (.5, "#9E2A2F"), (1, "#4E1216")])
sh.rad("fire", [(0, "#FFD9A0", .95), (.2, "#C4542E", .6), (1, "#C4542E", 0)])
sh.rad("warm", [(0, "#8A5A2A", .5), (1, "#14120E", 0)])
sh.lin("dark", [(0, "#0B0A08", .8), (1, "#0B0A08", .3)], 0, 0, 0, 1)

# far north wall (elevation)
sh.path(V, R(1.0, FL, 11.0, FL + 4.6), "url(#ashlar)")
sh.path(V, R(1.0, FL, 11.0, FL + 4.6), "url(#dark)")
# north archway (2.60 × 3.31) behind the dais, hidden by the dorsal tapestry
sh.path(V, f"M 4.7 {FL} L 4.7 {FL+2.5} C 4.7 {FL+3.0} 5.4 {FL+3.25} 6.0 {FL+3.31} C 6.6 {FL+3.25} 7.3 {FL+3.0} 7.3 {FL+2.5} L 7.3 {FL} Z", "#0B0A08", "#6E5A40", 1.2)
# firelight wash
sh.add(f'<circle cx="{V.x(2.2):.1f}" cy="{V.y(FL+0.8):.1f}" r="260" fill="url(#warm)"/>')
# dorsal tapestry on its rod: 8.0 × 2.7 m, millefleur with a hunting band
sh.path(V, R(2.0, FL + 1.35, 10.0, FL + 4.05), "url(#mille)", "#1A2012", 1)
sh.path(V, R(2.0, FL + 3.8, 10.0, FL + 4.05), "#6B5238")
sh.path(V, R(2.0, FL + 1.35, 10.0, FL + 1.55), "#6B5238")
for i in range(9):  # hunting figures: stag and hounds in silhouette, woven brown
    x = 2.6 + i * 0.85
    sh.path(V, f"M {x} {FL+2.3} L {x+0.35} {FL+2.3} L {x+0.42} {FL+2.55} L {x+0.3} {FL+2.45} L {x+0.05} {FL+2.45} Z", "#6B5238")
    sh.path(V, f"M {x+0.05} {FL+2.3} L {x+0.05} {FL+2.1} M {x+0.3} {FL+2.3} L {x+0.3} {FL+2.1}", stroke="#6B5238", sw=2)
sh.path(V, f"M 1.9 {FL+4.1} L 10.1 {FL+4.1}", stroke="#2E2F31", sw=3)
for i in range(17):
    sh.add(f'<circle cx="{V.x(2.0 + i*0.5):.1f}" cy="{V.y(FL+4.08):.1f}" r="3" fill="none" stroke="#2E2F31" stroke-width="1.5"/>')
for x in (3.5, 6.0, 8.5):  # folds
    sh.path(V, f"M {x} {FL+4.0} C {x+0.1} {FL+3.2} {x-0.1} {FL+2.2} {x+0.05} {FL+1.4}", stroke="#1A2012", sw=3, extra='opacity=".5"')
# cloth of estate (livery red) over the lord's chair
sh.path(V, f"M 5.2 {FL+3.9} L 6.8 {FL+3.9} L 6.8 {FL+1.4} L 5.2 {FL+1.4} Z", "url(#red)", "#3A0E10", 1)
sh.path(V, f"M 5.0 {FL+3.9} L 7.0 {FL+3.9} L 7.0 {FL+3.6} L 6.8 {FL+3.5} L 6.6 {FL+3.6} L 6.4 {FL+3.5} L 6.2 {FL+3.6} L 6.0 {FL+3.5} L 5.8 {FL+3.6} L 5.6 {FL+3.5} L 5.4 {FL+3.6} L 5.2 {FL+3.5} L 5.0 {FL+3.6} Z", "url(#red)", "#3A0E10", 1)
# dais + high table + chair
sh.path(V, R(1.4, FL, 10.6, FL + 0.35), "url(#oak)", "#1E150C", 1)
sh.path(V, R(1.4, FL + 0.35, 10.6, FL + 0.40), "#8A6A45")
sh.path(V, f"M 5.6 {FL+0.35} L 6.4 {FL+0.35} L 6.4 {FL+2.0} C 6.3 {FL+2.15} 5.7 {FL+2.15} 5.6 {FL+2.0} Z", "url(#oakV)", "#1E150C", 1)  # chair back
sh.path(V, R(2.5, FL + 1.10, 9.5, FL + 1.18), "url(#oakV)", "#1E150C", .8)  # table top
sh.path(V, f"M 2.45 {FL+1.18} L 9.55 {FL+1.18} L 9.6 {FL+0.62} C 8.0 {FL+0.58} 4.0 {FL+0.58} 2.4 {FL+0.62} Z", "url(#linen)", "#6E6656", .8)  # cloth
for x in (3.2, 4.4, 5.6, 6.8, 8.0, 9.0):
    sh.path(V, f"M {x} {FL+1.15} C {x+0.05} {FL+0.9} {x-0.05} {FL+0.75} {x} {FL+0.6}", stroke="#9F9784", sw=1)
# plate on the table: gilt nef (loot), ewer, cups
sh.path(V, f"M 7.2 {FL+1.18} L 7.6 {FL+1.18} L 7.55 {FL+1.3} C 7.7 {FL+1.35} 7.7 {FL+1.45} 7.55 {FL+1.5} L 7.25 {FL+1.5} C 7.1 {FL+1.45} 7.1 {FL+1.35} 7.25 {FL+1.3} Z", "#C9A227", "#5A440E", .8)
sh.path(V, f"M 7.4 {FL+1.5} L 7.41 {FL+1.75}", stroke="#B8B6AE", sw=1.2)
for x in (3.3, 4.2, 8.6):
    sh.path(V, f"M {x} {FL+1.18} L {x+0.12} {FL+1.18} L {x+0.14} {FL+1.32} L {x-0.02} {FL+1.32} Z", "#B8B6AE", "#4A4A46", .6)

# cut: slab, west wall with fireplace + hood, east wall with window
sh.path(V, R(0, 0, 12, FL), "url(#cut)", "#2A241C", 1)
sh.path(V, f"M 0 {FL} L 0.35 {FL} L 0.35 {FL+1.8} L 1.0 {FL+1.8} L 1.0 {FL+4.6} L 0 {FL+4.6} Z", "url(#cut)", "#2A241C", 1.2)
sh.path(V, R(0.35, FL, 1.0, FL + 1.8), "#8A4B32", "#4A2618", 1)  # brick fireback
sh.path(V, R(0.35, FL, 1.0, FL + 1.8), "url(#dark)")
# hood projecting into the room
sh.path(V, f"M 1.0 {FL+1.8} L 2.35 {FL+1.8} L 2.35 {FL+2.05} L 1.9 {FL+2.1} C 1.5 {FL+2.9} 1.25 {FL+3.8} 1.2 {FL+4.6} L 1.0 {FL+4.6} Z", "url(#cut)", "#2A241C", 1.2)
sh.path(V, R(1.0, FL + 1.72, 2.4, FL + 1.8), "#A88B64", "#6E5A40", .8)  # mantel shelf
sh.path(V, f"M 1.9 {FL} L 2.25 {FL} L 2.2 {FL+1.72} L 1.95 {FL+1.72} Z", "url(#ashlar)", "#6E5A40", 1)  # jamb
sh.path(V, f"M 0.35 {FL+1.8} L 0.35 {FL+4.6} L 0.6 {FL+4.6} L 0.6 {FL+1.9} Z", "#1E1B17")  # flue
# fire on the hearth
sh.add(f'<circle cx="{V.x(1.2):.1f}" cy="{V.y(FL+0.35):.1f}" r="90" fill="url(#fire)"/>')
sh.path(V, f"M 0.5 {FL} L 1.8 {FL} L 1.8 {FL+0.08} L 0.5 {FL+0.08} Z", "#2E2F31")
sh.path(V, f"M 0.6 {FL+0.08} C 0.7 {FL+0.5} 0.9 {FL+0.35} 1.0 {FL+0.75} C 1.1 {FL+0.4} 1.3 {FL+0.55} 1.35 {FL+0.9} C 1.45 {FL+0.5} 1.6 {FL+0.45} 1.7 {FL+0.08} Z", "#C4542E")
sh.path(V, f"M 0.85 {FL+0.08} C 0.9 {FL+0.3} 1.05 {FL+0.25} 1.1 {FL+0.5} C 1.2 {FL+0.3} 1.35 {FL+0.3} 1.4 {FL+0.08} Z", "#FFD9A0")
sh.path(V, f"M 0.6 {FL+0.05} L 1.7 {FL+0.12} L 1.68 {FL+0.18} L 0.62 {FL+0.1} Z", "#3A2A1A")
sh.flecks(V, (1.0, FL + 1.9, 2.3, FL + 4.5), 30, "#1E1B17", 4, .03, .09, .7)  # soot up the hood
sh.path(V, f"M 11.0 {FL} L 12 {FL} L 12 {FL+4.6} L 11.0 {FL+4.6} L 11.0 {FL+3.6} L 11.4 {FL+3.45} L 11.4 {FL+1.25} L 11.0 {FL+1.1} Z", "url(#cut)", "#2A241C", 1.2)
sh.path(V, f"M 11.4 {FL+1.25} L 12 {FL+1.25} L 12 {FL+3.45} L 11.4 {FL+3.45} Z", "#2A2E30", "#2E2F31", 1)  # glazed light
sh.path(V, f"M 11.7 {FL+1.25} L 11.7 {FL+3.45}", stroke="#2E2F31", sw=2)
sh.path(V, f"M 11.0 {FL+1.1} L 11.4 {FL+1.25} L 11.4 {FL+1.18} Z", "#A88B64")

# roof: wall posts on corbels, arch braces to the collar, principals, purlins, tiles
sh.path(V, R(0, FL + 4.6, 12, FL + 4.75), "url(#oak)", "#1E150C", .8)  # wall plates + tie
for (x0, x1, m) in ((1.0, 1.25, 1), (10.75, 11.0, -1)):
    sh.path(V, R(x0, FL + 3.1, x1, FL + 4.6), "url(#oak)", "#1E150C", 1)
    cx = x0 if m == 1 else x1
    sh.path(V, f"M {cx} {FL+3.1} L {cx + m*0.35} {FL+3.1} L {cx} {FL+2.8} Z", "#A88B64", "#6E5A40", 1)  # stone corbel
# principals 28° pitch from the wall centre-line to the ridge at +7.22
pitch = math.tan(math.radians(28))
ridge = 4.6 + 5.5 * pitch
for m in (1, -1):
    x0 = 0.5 if m == 1 else 11.5
    sh.path(V, f"M {x0 - m*0.3} {FL+4.6} L 6.0 {FL+ridge+0.35} L 6.0 {FL+ridge+0.1} L {x0 - m*0.3 + m*0.2} {FL+4.55} Z", "url(#tiles)", "#4A2618", .8)
    sh.path(V, f"M {x0} {FL+4.6} L 6.0 {FL+ridge} L 6.0 {FL+ridge-0.25} L {x0 + m*0.3} {FL+4.6} Z", "url(#oak)", "#1E150C", 1)
    for t in (0.3, 0.6):
        px = x0 + (6.0 - x0) * t
        py = 4.6 + (ridge - 4.6) * t
        sh.path(V, R(px - 0.1, FL + py, px + 0.1, FL + py + 0.2), "#4A3622", "#1E150C", .8)  # purlins
collar_h = 4.6 + (3.51 - 0.5) * pitch
sh.path(V, R(3.45, FL + collar_h - 0.2, 8.55, FL + collar_h), "url(#oakV)", "#1E150C", 1)
for m in (1, -1):  # arch braces: a two-centred arch springing from the wall posts to the collar
    xa = 1.25 if m == 1 else 10.75
    sh.path(V, f"M {xa} {FL+3.1} C {xa + m*0.4} {FL+4.9} {6 - m*2.2} {FL+collar_h-0.2} {6.0} {FL+collar_h-0.2} L {6.0} {FL+collar_h-0.42} C {6 - m*2.4} {FL+collar_h-0.45} {xa + m*0.25} {FL+4.7} {xa} {FL+3.5} Z", "url(#oak)", "#1E150C", 1)
sh.path(V, R(5.93, FL + collar_h - 0.2, 6.07, FL + ridge - 0.25), "url(#oak)", "#1E150C", .8)  # king strut
# iron candle wheel hung from the collar
sh.path(V, f"M 6.0 {FL+collar_h-0.42} L 6.0 {FL+3.4}", stroke="#2E2F31", sw=1.2)
sh.path(V, R(5.3, FL + 3.35, 6.7, FL + 3.42), "#2E2F31")
for x in (5.35, 5.7, 6.0, 6.3, 6.65):
    sh.add(f'<circle cx="{V.x(x):.1f}" cy="{V.y(FL+3.55):.1f}" r="18" fill="url(#fire)"/>')
    sh.path(V, R(x - 0.025, FL + 3.42, x + 0.025, FL + 3.55), "#DCD2BA")
# the 4.60 lid line: everything above is the roof cap that breaks through
sh.top.append(f'<line x1="{V.x(0):.1f}" y1="{V.y(FL+4.6):.1f}" x2="{V.x(12):.1f}" y2="{V.y(FL+4.6):.1f}" stroke="#C4542E" stroke-width="1.2" stroke-dasharray="6 4"/>')
sh.text(V.x(8.0), V.y(FL + 4.6) - 8, "4.60 module lid — roof cap breaks through (feature)", 9.5, "#C4542E", "middle")
sh.add(human(V.x(3.2), V.y(FL), S, "#DCD2BA", .5))

# ───────── PLAN ─────────
PS, PX0, PY0 = 30, 800, 150
P = lambda x, y: (PX0 + x * PS, PY0 + (12 - y) * PS)
def prect(x0, y0, x1, y1, fill, stroke="#2A241C", sw=1):
    a, b = P(x0, y1); c, d = P(x1, y0)
    sh.add(f'<rect x="{a:.1f}" y="{b:.1f}" width="{c-a:.1f}" height="{d-b:.1f}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
prect(0, 0, 12, 12, "url(#cut)", "#635C4C", 1.5)
prect(1, 1, 11, 11, "#2A241C")
for (x0, y0, x1, y1) in ((4.7, 0, 7.3, 1), (4.7, 11, 7.3, 12), (0, 4.7, 1, 7.3), (11, 4.7, 12, 7.3)):
    prect(x0, y0, x1, y1, "#2A241C", "none")
prect(1.4, 8.6, 10.6, 11.0, "#4A3622")          # dais
prect(2.5, 9.2, 9.5, 9.9, "#D6CDB6", "#6E6656")  # high table + cloth
prect(2.0, 10.85, 10.0, 11.0, "#4F5E3A", "none")  # dorsal tapestry
prect(1.0, 1.9, 2.35, 4.1, "#1E1B17", "#6E5A40")  # hearth hood
prect(0.35, 2.2, 1.0, 3.8, "#8A4B32", "#4A2618")  # fireback
prect(11.0, 2.3, 11.4, 3.7, "#2A2E30", "#2E2F31") # window embrasure
prect(11.0, 8.3, 11.4, 9.7, "#2A2E30", "#2E2F31")
for (x, y) in ((1.1, 6.0), (10.9, 6.0)):
    pass
for y in (2.0, 5.0, 8.0):
    for x in (1.0, 10.75):
        prect(x, y - 0.1, x + 0.25, y + 0.1, "#6B4F33", "none")  # wall posts
    a, b = P(1.1, y); c, d = P(10.9, y)
    sh.add(f'<line x1="{a:.1f}" y1="{b:.1f}" x2="{c:.1f}" y2="{d:.1f}" stroke="#6B4F33" stroke-width="1.2" stroke-dasharray="6 3"/>')
# trestle tables and benches in the body of the hall
for x in (3.0, 8.0):
    prect(x - 0.4, 2.0, x + 0.4, 7.4, "#4A3622", "#1E150C")
# section line
a, b = P(-0.4, 3.0); c, d = P(12.4, 3.0)
sh.add(f'<line x1="{a:.1f}" y1="{b:.1f}" x2="{c:.1f}" y2="{d:.1f}" stroke="#C4542E" stroke-width="1" stroke-dasharray="8 3 2 3"/>')
sh.text(c + 2, d + 4, "A", 10, "#C4542E")
for i in range(13):
    a, b = P(i, 0); sh.add(f'<line x1="{a:.1f}" y1="{b:.1f}" x2="{a:.1f}" y2="{b+5:.1f}" stroke="#9A9078"/>')
    a, b = P(0, i); sh.add(f'<line x1="{a-5:.1f}" y1="{b:.1f}" x2="{a:.1f}" y2="{b:.1f}" stroke="#9A9078"/>')
sh.text(P(0, 0)[0], P(0, 0)[1] + 18, "0", 9, "#9A9078", "middle"); sh.text(P(12, 0)[0], P(0, 0)[1] + 18, "12 m", 9, "#9A9078", "middle")
sh.text(P(12, 12)[0] - 4, P(12, 12)[1] - 6, "N ↑", 10, "#9A9078", "end")
lab = lambda x, y, t, c="#DCD2BA", anc="middle": sh.text(P(x, y)[0], P(x, y)[1], t, 9, c, anc)
lab(6.0, -0.9, "DOOR S · ARCH 2.60 × 3.31")
lab(6.0, 12.25, "DOOR N (behind tapestry)")
lab(6.0, 10.2, "DAIS +0.35 · HIGH TABLE", "#DCD2BA")
lab(6.0, 4.2, "HALL FLOOR 10 × 10", "#9A9078")
lab(2.6, 3.1, "HEARTH", "#C4542E", "start")
lab(10.9, 3.1, "WINDOW", "#DCD2BA", "end")
lab(10.9, 8.9, "WINDOW", "#DCD2BA", "end")
lab(1.2, 6.1, "DOOR W", "#DCD2BA", "start")
lab(10.8, 6.1, "DOOR E", "#DCD2BA", "end")
lab(6.0, 6.55, "TRUSSES @ 3 m", "#9A9078")

sh.callout(V.x(4.0), V.y(FL + 3.3), 520, 150, "DORSAL TAPESTRY", "8.0 × 2.7 m, burns, stealable")
sh.callout(V.x(2.9), V.y(FL + 5.2), 250, 205, "ARCH BRACE", "oak 0.22 m, to collar", "end")
sh.callout(V.x(1.7), V.y(FL + 3.0), 250, 280, "HEARTH HOOD", "sandstone, sooted", "end")
sh.callout(V.x(6.0), V.y(FL + 3.39), 520, 205, "CANDLE WHEEL", "iron, 5 lights")

sh.palette([("#A88B64", "dressed sandstone"), ("#6B4F33", "oak roof"), ("#4F5E3A", "tapestry wool"),
            ("#D6CDB6", "linen cloth"), ("#9E2A2F", "cloth of estate"), ("#8A4B32", "brick + tile"), ("#1E1B17", "soot")])
sh.write()
