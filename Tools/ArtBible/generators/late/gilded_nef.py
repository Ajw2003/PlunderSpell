from common_enemy import *

HS, OS = 760, 480
sh = Sheet("gilded-nef")
sh.frame(f"{AGE_NAME} · PLUNDER · 1100 COIN · 4 ST", "Gilded Nef",
         "0.52 × 0.18 × 0.56 m · ≤ 3k tris · 1024²", f"hero 1 m = {HS} px · ortho 1 m = {OS} px", glow_xy=("32%", "60%"))
item_under(sh, 0.1, 0.1 * HS, f"0.1 m = {0.1*HS:.0f} px (hero) · {0.1*OS:.0f} px (ortho)")

sh.lin("gilt", [(0, "#E3C35A"), (.35, "#C9A227"), (.8, "#7A5E14"), (1, "#4A3808")], 0, 0, 0, 1)
sh.lin("giltH", [(0, "#6E5410"), (.3, "#E3C35A"), (.5, "#F0D97A"), (.7, "#C9A227"), (1, "#5A440E")])
sh.lin("silv", [(0, "#E6E4DC"), (.4, "#B8B6AE"), (1, "#5A5A55")], 0, 0, 0, 1)
sh.lin("silvH", [(0, "#5A5A55"), (.35, "#D6D4CC"), (.55, "#B8B6AE"), (1, "#4A4A46")])


def nef(v, detail=True, deck=True):
    P = lambda d, f, s="#2A2A28", w=1, **k: sh.path(v, d, f, s, w, **k)
    # foot: lobed dome, baluster stem with knop, cup under the hull
    P("M -0.085 0.0 L 0.085 0.0 C 0.085 0.02 0.06 0.035 0.03 0.045 L -0.03 0.045 C -0.06 0.035 -0.085 0.02 -0.085 0.0 Z", "url(#giltH)")
    for x0 in (-0.05, -0.017, 0.017, 0.05):
        sh.path(v, f"M {x0} 0.003 C {x0*0.9} 0.02 {x0*0.6} 0.035 {x0*0.4} 0.044", stroke="#7A5E14", sw=1)
    P("M -0.012 0.045 L 0.012 0.045 C 0.02 0.07 0.02 0.09 0.012 0.10 L -0.012 0.10 C -0.02 0.09 -0.02 0.07 -0.012 0.045 Z", "url(#silvH)")
    P("M -0.024 0.075 C -0.028 0.085 -0.028 0.092 -0.024 0.1 L 0.024 0.1 C 0.028 0.092 0.028 0.085 0.024 0.075 Z", "url(#giltH)")
    P("M -0.05 0.10 L 0.05 0.10 C 0.045 0.115 0.03 0.125 0.0 0.128 C -0.03 0.125 -0.045 0.115 -0.05 0.10 Z", "url(#giltH)")
    # rigging behind the masts first
    if detail:
        for (a, b) in (((0.0, 0.53), (0.27, 0.33)), ((0.0, 0.53), (-0.21, 0.31)), ((0.15, 0.43), (0.29, 0.335)),
                       ((-0.14, 0.44), (-0.225, 0.31)), ((0.0, 0.47), (0.15, 0.42)), ((0.0, 0.47), (-0.14, 0.43))):
            sh.path(v, f"M {a[0]} {a[1]} L {b[0]} {b[1]}", stroke="#D6D4CC", sw=.9)
        for x in (-0.03, -0.015, 0.015, 0.03):  # shrouds
            sh.path(v, f"M 0.0 0.49 L {x} 0.255", stroke="#D6D4CC", sw=.7)
    # masts
    P("M -0.004 0.25 L 0.004 0.25 L 0.003 0.555 L -0.003 0.555 Z", "url(#silvH)", w=.5)
    P("M 0.146 0.27 L 0.154 0.27 L 0.153 0.44 L 0.147 0.44 Z", "url(#silvH)", w=.5)
    P("M -0.144 0.30 L -0.136 0.30 L -0.137 0.45 L -0.143 0.45 Z", "url(#silvH)", w=.5)
    # bowsprit
    P("M 0.22 0.285 L 0.30 0.335 L 0.298 0.342 L 0.216 0.293 Z", "url(#silvH)", w=.5)
    # furled sails on yards
    P("M -0.10 0.465 L 0.10 0.465 C 0.08 0.44 -0.08 0.44 -0.10 0.465 Z", "url(#silv)")
    P("M -0.10 0.468 L 0.10 0.468 L 0.10 0.462 L -0.10 0.462 Z", "url(#giltH)", w=.4)
    P("M 0.09 0.415 L 0.21 0.415 C 0.19 0.40 0.11 0.40 0.09 0.415 Z", "url(#silv)")
    P("M -0.20 0.40 L -0.08 0.47 L -0.083 0.475 L -0.203 0.405 Z", "url(#giltH)", w=.4)  # lateen yard
    # fighting tops
    P("M -0.028 0.49 L 0.028 0.49 L 0.024 0.525 L -0.024 0.525 Z", "url(#giltH)")
    for x in (-0.02, -0.008, 0.004, 0.016):
        sh.path(v, f"M {x} 0.525 L {x+0.006} 0.525 L {x+0.006} 0.532 L {x} 0.532 Z", "#C9A227")
    P("M 0.137 0.415 L 0.163 0.415 L 0.16 0.432 L 0.14 0.432 Z", "url(#giltH)", w=.6)
    if detail:  # two tiny cast sailors in the main top
        for x in (-0.012, 0.012):
            sh.add(f'<circle cx="{v.x(x):.1f}" cy="{v.y(0.534):.1f}" r="{0.006*v.s:.1f}" fill="url(#silv)" stroke="#2A2A28" stroke-width=".6"/>')
    # pennant
    P("M 0.003 0.555 L 0.06 0.545 L 0.045 0.55 L 0.06 0.555 L 0.003 0.56 Z", "url(#silv)", w=.5)
    # hull
    hull = "M -0.215 0.31 L -0.205 0.22 C -0.185 0.155 -0.09 0.128 0.0 0.128 C 0.10 0.128 0.185 0.155 0.222 0.225 L 0.245 0.29 L 0.19 0.265 C 0.10 0.25 -0.08 0.25 -0.125 0.255 L -0.125 0.31 Z"
    P(hull, "url(#gilt)", w=1.2)
    if detail:
        sh.add(f'<clipPath id="hc{id(v)}"><path d="{v.p(hull)}"/></clipPath><g clip-path="url(#hc{id(v)})">')
        for y in (0.15, 0.175, 0.20, 0.225):
            sh.path(v, f"M -0.23 {y+0.02} C -0.1 {y-0.012} 0.1 {y-0.012} 0.25 {y+0.03}", stroke="#B8B6AE", sw=2.2)
            sh.path(v, f"M -0.23 {y+0.022} C -0.1 {y-0.01} 0.1 {y-0.01} 0.25 {y+0.032}", stroke="#2A2A28", sw=.6)
        sh.add('</g>')
        # niello scroll band + gunports
        sh.path(v, "M -0.12 0.238 C -0.06 0.232 0.06 0.232 0.18 0.245 L 0.18 0.232 C 0.06 0.22 -0.06 0.22 -0.12 0.226 Z", "#2A2A28")
        for i in range(12):
            x = -0.11 + i * 0.024
            sh.path(v, f"M {x} 0.231 C {x+0.006} 0.238 {x+0.012} 0.226 {x+0.018} 0.233", stroke="#D6D4CC", sw=.8)
    # sterncastle and forecastle with crenellated rails
    P("M -0.215 0.31 L -0.12 0.31 L -0.12 0.345 L -0.215 0.345 Z", "url(#silvH)")
    P("M 0.15 0.27 L 0.245 0.29 L 0.25 0.33 L 0.15 0.32 Z", "url(#silvH)")
    if detail:
        for i in range(5):
            x = -0.212 + i * 0.02
            sh.path(v, f"M {x} 0.345 L {x+0.011} 0.345 L {x+0.011} 0.357 L {x} 0.357 Z", "url(#giltH)", "#4A3808", .5)
        for i in range(5):
            x = 0.152 + i * 0.019
            y = 0.32 + (x - 0.15) * 0.1
            sh.path(v, f"M {x:.3f} {y:.3f} L {x+0.01:.3f} {y+0.001:.3f} L {x+0.01:.3f} {y+0.013:.3f} L {x:.3f} {y+0.012:.3f} Z", "url(#giltH)", "#4A3808", .5)
        for i in range(3):  # stern windows
            x = -0.205 + i * 0.03
            sh.path(v, f"M {x} 0.318 L {x+0.016} 0.318 L {x+0.016} 0.335 L {x} 0.335 Z", "#2A2A28")
        # gilt rubbed back to silver where hands lift it
        for (x, y) in ((-0.17, 0.33), (0.2, 0.31), (0.0, 0.14)):
            sh.add(f'<ellipse cx="{v.x(x):.1f}" cy="{v.y(y):.1f}" rx="{0.018*v.s:.1f}" ry="{0.006*v.s:.1f}" fill="#D6D4CC" opacity=".55"/>')
        sh.flecks(v, (-0.2, 0.13, 0.24, 0.30), 30, "#4A4A46", 3, .001, .003, .5)
    if deck:  # three-quarter: deck and the hinged salt lid seen from above
        P("M -0.12 0.255 C -0.06 0.275 0.08 0.275 0.19 0.265 C 0.08 0.25 -0.08 0.245 -0.12 0.255 Z", "url(#silv)", w=.6)
        P("M -0.06 0.258 C -0.03 0.266 0.03 0.266 0.06 0.26 C 0.03 0.253 -0.03 0.252 -0.06 0.258 Z", "url(#giltH)", w=.6)


# hero: slightly from above, the deck showing
H = View(330, 690, HS)
sh.add(f'<ellipse cx="330" cy="692" rx="{0.12*HS:.0f}" ry="10" fill="#0B0A08" opacity=".6"/>')
nef(H)
# ortho side and bow-on front
S = View(985, 690, OS)
nef(S, detail=True, deck=False)
Fv = View(760, 690, OS)
P = lambda d, f, s="#2A2A28", w=1: sh.path(Fv, d, f, s, w)
P("M -0.085 0.0 L 0.085 0.0 C 0.085 0.02 0.06 0.035 0.03 0.045 L -0.03 0.045 C -0.06 0.035 -0.085 0.02 -0.085 0.0 Z", "url(#giltH)")
P("M -0.012 0.045 L 0.012 0.045 C 0.02 0.07 0.02 0.09 0.012 0.10 L -0.012 0.10 C -0.02 0.09 -0.02 0.07 -0.012 0.045 Z", "url(#silvH)")
P("M -0.05 0.10 L 0.05 0.10 C 0.045 0.115 0.03 0.125 0.0 0.128 C -0.03 0.125 -0.045 0.115 -0.05 0.10 Z", "url(#giltH)")
for (a, b) in (((0, 0.53), (-0.09, 0.26)), ((0, 0.53), (0.09, 0.26))):
    sh.path(Fv, f"M {a[0]} {a[1]} L {b[0]} {b[1]}", stroke="#D6D4CC", sw=.8)
P("M -0.004 0.25 L 0.004 0.25 L 0.003 0.555 L -0.003 0.555 Z", "url(#silvH)", w=.5)
P("M -0.10 0.465 L 0.10 0.465 C 0.08 0.44 -0.08 0.44 -0.10 0.465 Z", "url(#silv)")
P("M -0.028 0.49 L 0.028 0.49 L 0.024 0.525 L -0.024 0.525 Z", "url(#giltH)")
P("M -0.09 0.30 L -0.085 0.20 C -0.07 0.15 -0.03 0.128 0 0.128 C 0.03 0.128 0.07 0.15 0.085 0.20 L 0.09 0.30 Z", "url(#gilt)", w=1.2)
P("M -0.09 0.30 L 0.09 0.30 L 0.09 0.33 L -0.09 0.33 Z", "url(#silvH)")
P("M -0.004 0.30 L 0.004 0.30 L 0.03 0.335 L 0.026 0.342 Z", "url(#silvH)", w=.5)
for y in (0.16, 0.19, 0.22, 0.25):
    sh.path(Fv, f"M -0.088 {y} C -0.03 {y-0.012} 0.03 {y-0.012} 0.088 {y}", stroke="#B8B6AE", sw=1.8)
sh.text(Fv.x(0), Fv.y(0.56) - 10, "0.18 m", 10, "#9A9078", "middle")
sh.text(S.x(0.04), S.y(0.56) - 10, "0.52 m bowsprit to stern", 10, "#9A9078", "middle")

# grab points and fracture lines (rigging, masts at their steps, bowsprit)
grab(sh, H.x(0.0), H.y(0.075), "GRAB · stem", 14, 4)
grab(sh, H.x(-0.19), H.y(0.24), "GRAB · stern", -14, 22, "end")
crack(sh, [(H.x(-0.02), H.y(0.30)), (H.x(0.02), H.y(0.305))])
crack(sh, [(H.x(0.13), H.y(0.33)), (H.x(0.17), H.y(0.335))])
crack(sh, [(H.x(-0.16), H.y(0.37)), (H.x(-0.12), H.y(0.375))])
crack(sh, [(H.x(0.235), H.y(0.315)), (H.x(0.245), H.y(0.285))])
crack(sh, [(H.x(0.0), H.y(0.53)), (H.x(0.13), H.y(0.42)), (H.x(0.27), H.y(0.33))], 1.2)
sh.text(40, 150, "FRAGILE 3 m/s · RIGGING SNAPS, 3 MASTS + BOWSPRIT SHED, HULL DENTS · SPILLS SALT", 10.5, "#C4542E", "start", extra='letter-spacing="1"')

tx = 580
sh.callout(H.x(0.0), H.y(0.515), tx, 180, "FIGHTING TOP", "gilt, 2 cast sailors")
sh.callout(H.x(-0.02), H.y(0.40), tx, 235, "SILVER-WIRE RIGGING", "0.8 mm, soldered")
sh.callout(H.x(0.0), H.y(0.262), tx, 290, "SALT LID", "hinged, gilt, deck well")
sh.callout(H.x(0.12), H.y(0.235), tx, 345, "NIELLO BAND", "black scroll inlay")
sh.callout(H.x(-0.17), H.y(0.35), 60, 300, "STERNCASTLE", "crenellated gilt rail", "start")
sh.callout(H.x(-0.10), H.y(0.17), 60, 440, "HULL", "raised silver-gilt, strakes", "start")
sh.callout(H.x(-0.06), H.y(0.02), 60, 580, "LOBED FOOT", "baluster stem, knop", "start")
sh.palette([("#C9A227", "silver-gilt"), ("#B8B6AE", "silver"), ("#D6D4CC", "silver wire"),
            ("#2A2A28", "niello"), ("#5A5A55", "tarnish")])
sh.write()
