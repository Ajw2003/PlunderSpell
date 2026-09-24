from common_enemy import *

HS, OS = 1000, 560
sh = Sheet("bankers-ledger")
sh.frame(f"{AGE_NAME} · PLUNDER · 140 COIN · 1.5 ST", "Banker's Ledger",
         "0.30 × 0.22 × 0.09 m (+ 0.60 m chain) · ≤ 1.5k tris · 1024²", f"hero 1 m = {HS} px · ortho 1 m = {OS} px", glow_xy=("30%", "70%"))
item_under(sh, 0.1, 0.1 * HS, f"0.1 m = {0.1*HS:.0f} px (hero) · {0.1*OS:.0f} px (ortho)")

sh.lin("calf", [(0, "#3A2618"), (.4, "#6E4A30"), (.7, "#5A3A26"), (1, "#2E1E12")])
sh.lin("calfT", [(0, "#7A5236"), (1, "#4A3020")], 0, 0, 1, 1)
sh.lin("pages", [(0, "#B9AD8E"), (.5, "#D8CCAE"), (1, "#A89C7E")], 0, 0, 0, 1)
sh.lin("ironV", [(0, "#6A6B6B"), (.4, "#2E2F31"), (1, "#131416")], 0, 0, 0, 1)
sh.rad("boss", [(0, "#7C7D7C"), (.4, "#2E2F31"), (1, "#0E0F10")], .35, .3, .7)
sh.lin("wax", [(0, "#5A2418"), (.5, "#8A3A2A"), (1, "#4A1C12")])

cx0, gy = 360, 610
def proj(x, y, z):
    return (cx0 + (x * 0.86 + z * 0.62) * HS, gy - (y + z * 0.40) * HS)

def poly(pts, fill, stroke="#14120E", sw=.9, extra=""):
    sh.add('<path d="M ' + " L ".join(f"{a:.1f} {b:.1f}" for a, b in pts) + f' Z" fill="{fill}" stroke="{stroke}" stroke-width="{sw}" {extra}/>')

W, D, T, B = 0.30, 0.22, 0.09, 0.012   # width, depth, height, board thickness
x0, x1, z0, z1 = -W / 2, W / 2, 0.0, D
# shadow
sh.add(f'<ellipse cx="{proj(0.02, 0, D/2)[0]:.1f}" cy="{proj(0, 0, D/2)[1]+6:.1f}" rx="{0.25*HS:.0f}" ry="{0.05*HS:.0f}" fill="#0B0A08" opacity=".65"/>')

# chain lying on the floor, from the back-left staple out to the left, ending in a wrenched staple
links = []
px, py = proj(x0 + 0.01, T - 0.004, D - 0.02)
pts = [(px, py)]
path = [(-0.17, 0.004, 0.25), (-0.23, 0.004, 0.28), (-0.29, 0.004, 0.24), (-0.32, 0.004, 0.16), (-0.31, 0.004, 0.08), (-0.27, 0.004, 0.02)]
for p in path:
    pts.append(proj(*p))
# interpolate links along the polyline
chain = []
for (a, b), (c, d) in zip(pts, pts[1:]):
    n = max(2, int(math.hypot(c - a, d - b) / 17))
    for i in range(n):
        t = i / n
        chain.append((a + (c - a) * t, b + (d - b) * t, math.degrees(math.atan2(d - b, c - a))))
for i, (a, b, ang) in enumerate(chain):
    if i % 2:
        sh.add(f'<rect x="{a-2:.1f}" y="{b-9:.1f}" width="4" height="18" rx="2" fill="#2E2F31" stroke="#0E0F10" transform="rotate({ang:.1f} {a:.1f} {b:.1f})"/>')
    else:
        sh.add(f'<ellipse cx="{a:.1f}" cy="{b:.1f}" rx="10" ry="5.5" fill="none" stroke="#4A4B4D" stroke-width="3.2" transform="rotate({ang:.1f} {a:.1f} {b:.1f})"/>'
               f'<ellipse cx="{a:.1f}" cy="{b:.1f}" rx="10" ry="5.5" fill="none" stroke="#8C8D8C" stroke-width=".8" transform="rotate({ang:.1f} {a:.1f} {b:.1f})"/>')
ex, ey = pts[-1]
sh.add(f'<path d="M {ex-4:.1f} {ey:.1f} L {ex+22:.1f} {ey+4:.1f} L {ex+26:.1f} {ey-6:.1f} L {ex+30:.1f} {ey+10:.1f} L {ex+4:.1f} {ey+8:.1f} Z" fill="url(#ironV)" stroke="#0E0F10"/>')  # torn staple
sh.add(f'<path d="M {ex+22:.1f} {ey+4:.1f} L {ex+34:.1f} {ey-2:.1f} L {ex+36:.1f} {ey+6:.1f}" fill="#6B4F33" stroke="#3A2A1A"/>')  # splinter of desk still on it

# bottom board, page block, top board
poly([proj(x0, 0, z0), proj(x1, 0, z0), proj(x1, B, z0), proj(x0, B, z0)], "url(#calf)")
poly([proj(x1, 0, z0), proj(x1, 0, z1), proj(x1, B, z1), proj(x1, B, z0)], "#2E1E12")
inset = 0.006
poly([proj(x0 + inset, B, z0 + inset), proj(x1 - inset, B, z0 + inset), proj(x1 - inset, T - B, z0 + inset), proj(x0 + inset, T - B, z0 + inset)], "url(#pages)")
poly([proj(x1 - inset, B, z0 + inset), proj(x1 - inset, B, z1 - inset), proj(x1 - inset, T - B, z1 - inset), proj(x1 - inset, T - B, z0 + inset)], "#B9AD8E")
for i in range(1, 14):  # page lines on the fore-edge and tail
    y = B + (T - 2 * B) * i / 14
    a, b = proj(x0 + inset, y, z0 + inset); c, d = proj(x1 - inset, y, z0 + inset)
    sh.add(f'<path d="M {a:.1f} {b:.1f} L {c:.1f} {d:.1f}" stroke="#8F8468" stroke-width=".5"/>')
    c2, d2 = proj(x1 - inset, y, z1 - inset)
    sh.add(f'<path d="M {c:.1f} {d:.1f} L {c2:.1f} {d2:.1f}" stroke="#8F8468" stroke-width=".5"/>')
poly([proj(x0, T - B, z0), proj(x1, T - B, z0), proj(x1, T, z0), proj(x0, T, z0)], "url(#calf)")
poly([proj(x1, T - B, z0), proj(x1, T - B, z1), proj(x1, T, z1), proj(x1, T, z0)], "#2E1E12")
poly([proj(x0, T, z0), proj(x1, T, z0), proj(x1, T, z1), proj(x0, T, z1)], "url(#calfT)", sw=1.1)
# blind-tooled frame on the cover
for m in (0.02, 0.035):
    poly([proj(x0 + m, T, z0 + m), proj(x1 - m, T, z0 + m), proj(x1 - m, T, z1 - m), proj(x0 + m, T, z1 - m)], "none", "#2E1E12", .9)
    poly([proj(x0 + m + .002, T, z0 + m + .002), proj(x1 - m - .002, T, z0 + m + .002), proj(x1 - m - .002, T, z1 - m - .002), proj(x0 + m + .002, T, z1 - m - .002)], "none", "#8A6446", .5)
a, b = proj(x0 + 0.035, T, z0 + 0.035); c, d = proj(x1 - 0.035, T, z1 - 0.035)
sh.add(f'<path d="M {a:.1f} {b:.1f} L {c:.1f} {d:.1f}" stroke="#2E1E12" stroke-width=".8"/>')
a, b = proj(x1 - 0.035, T, z0 + 0.035); c, d = proj(x0 + 0.035, T, z1 - 0.035)
sh.add(f'<path d="M {a:.1f} {b:.1f} L {c:.1f} {d:.1f}" stroke="#2E1E12" stroke-width=".8"/>')
# five iron bosses
for (x, z) in ((x0 + .03, z0 + .03), (x1 - .03, z0 + .03), (x1 - .03, z1 - .03), (x0 + .03, z1 - .03), (0, D / 2)):
    a, b = proj(x, T, z)
    sh.add(f'<ellipse cx="{a:.1f}" cy="{b:.1f}" rx="11" ry="7" fill="url(#boss)" stroke="#0E0F10"/>')
# paper title label
poly([proj(-0.05, T, 0.14), proj(0.05, T, 0.14), proj(0.05, T, 0.17), proj(-0.05, T, 0.17)], "#D8CCAE", "#6E6656", .6)
for k in range(3):
    a, b = proj(-0.04, T, 0.148 + k * 0.007); c, d = proj(0.03 - k * 0.01, T, 0.148 + k * 0.007)
    sh.add(f'<path d="M {a:.1f} {b:.1f} L {c:.1f} {d:.1f}" stroke="#3A3024" stroke-width=".9"/>')
# chain staple on the back-left corner
a, b = proj(x0 + 0.01, T, D - 0.02)
sh.add(f'<rect x="{a-7:.1f}" y="{b-5:.1f}" width="14" height="8" fill="url(#ironV)" stroke="#0E0F10"/>')
# two iron clasps over the fore-edge
for xc in (-0.08, 0.08):
    poly([proj(xc - .012, T, 0.035), proj(xc + .012, T, 0.035), proj(xc + .012, T, z0), proj(xc - .012, T, z0)], "#5A3A26")
    poly([proj(xc - .012, T, z0), proj(xc + .012, T, z0), proj(xc + .012, T * 0.35, z0 - .003), proj(xc - .012, T * 0.35, z0 - .003)], "url(#ironV)")
    a, b = proj(xc, T * 0.2, z0)
    sh.add(f'<circle cx="{a:.1f}" cy="{b:.1f}" r="3" fill="#2E2F31" stroke="#0E0F10"/>')
# letters of credit poking out, with wax seals on tags
for (xc, w, seed) in ((-0.03, 0.07, 1), (0.10, 0.05, 2)):
    y = T * 0.55
    poly([proj(xc - w / 2, y, z0 + inset), proj(xc + w / 2, y, z0 + inset), proj(xc + w / 2, y, z0 - 0.03), proj(xc - w / 2, y, z0 - 0.03)], "#E4DCC6", "#8F8468", .7)
    a, b = proj(xc, y, z0 - 0.03)
    sh.add(f'<path d="M {a:.1f} {b:.1f} L {a-3:.1f} {b+26:.1f}" stroke="#D8CCAE" stroke-width="3"/>')
    sh.add(f'<circle cx="{a-3:.1f}" cy="{b+32:.1f}" r="8" fill="url(#wax)" stroke="#3A160E"/>'
           f'<circle cx="{a-3:.1f}" cy="{b+32:.1f}" r="4" fill="none" stroke="#5A2418" stroke-width="1.2"/>')
sh.flecks(View(0, 0, 1), (proj(x0, T, z0)[0], -proj(0, T, z1)[1], proj(x1, T, z1)[0], -proj(0, T, z0)[1] + 20), 40, "#2E1E12", 9, 1, 2.5, .5)

# orthos
Fo = View(800, 690, OS)
sh.path(Fo, f"M {x0} 0 L {x1} 0 L {x1} {B} L {x0} {B} Z", "url(#calf)", "#14120E", .8)
sh.path(Fo, f"M {x0+inset} {B} L {x1-inset} {B} L {x1-inset} {T-B} L {x0+inset} {T-B} Z", "url(#pages)", "#14120E", .6)
sh.path(Fo, f"M {x0} {T-B} L {x1} {T-B} L {x1} {T} L {x0} {T} Z", "url(#calf)", "#14120E", .8)
for xc in (-0.08, 0.08):
    sh.path(Fo, f"M {xc-.012} {T} L {xc+.012} {T} L {xc+.012} {T*0.35} L {xc-.012} {T*0.35} Z", "url(#ironV)", "#0E0F10", .6)
for i in range(1, 10):
    y = B + (T - 2 * B) * i / 10
    sh.path(Fo, f"M {x0+inset} {y:.4f} L {x1-inset} {y:.4f}", stroke="#8F8468", sw=.5)
sh.text(Fo.x(0), Fo.y(T) - 10, "0.30 m", 10, "#9A9078", "middle")
So = View(1030, 690, OS)
sh.path(So, f"M -0.11 0 L 0.11 0 L 0.11 {B} L -0.11 {B} Z", "url(#calf)", "#14120E", .8)
sh.path(So, f"M -0.104 {B} L 0.104 {B} L 0.104 {T-B} L -0.104 {T-B} Z", "url(#pages)", "#14120E", .6)
sh.path(So, f"M -0.11 {T-B} L 0.11 {T-B} L 0.11 {T} L -0.11 {T} Z", "url(#calf)", "#14120E", .8)
sh.path(So, f"M -0.125 {T-0.02} C -0.132 {T-0.01} -0.132 {B+0.01} -0.125 {B} L -0.11 {B} L -0.11 {T-0.02} Z", "#5A3A26", "#14120E", .6)  # rounded spine
for xx in (-0.09, -0.07, -0.05, -0.03):
    pass
sh.text(So.x(0), So.y(T) - 10, "0.22 m · spine left", 10, "#9A9078", "middle")

# grab + note
grab(sh, *proj(0.0, T, D * 0.5), "GRAB · one hand", 16, 24)
sh.text(40, 150, "UNBREAKABLE (999) · IGNIS RUINS IT: CHARRED LEDGER = 0 COIN, CHAIN DROPS", 10.5, "#C4542E", "start", extra='letter-spacing="1"')

sh.callout(*proj(x0 + 0.03, T, z1 - 0.03), 90, 280, "IRON BOSSES", "5, blackened, domed")
sh.callout(*proj(-0.02, T, 0.155), 300, 250, "PAPER LABEL", "“Livre des changes”")
sh.callout(*proj(-0.29, 0.004, 0.24), 60, 400, "LIBRARY CHAIN", "0.60 m, 22 links")
sh.callout(ex + 30, ey + 8, 60, 640, "WRENCHED STAPLE", "desk splinter still on")
sh.callout(*proj(x1, T * 0.5, z0 + 0.01), 890, 480, "FORE-EDGE", "rag paper, 480 leaves")
sh.callout(*proj(0.10, T * 0.55, z0 - 0.03), 890, 560, "LETTERS OF CREDIT", "wax-sealed on tags")
sh.callout(*proj(0.08, T * 0.3, z0), 890, 410, "IRON CLASPS", "strap + pin, 2")

sh.palette([("#5A3A26", "calf cover"), ("#6B4F33", "oak boards"), ("#D8CCAE", "rag paper"),
            ("#2E2F31", "blackened iron"), ("#8A3A2A", "sealing wax")])
sh.write()
