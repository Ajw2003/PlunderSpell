from lib import *

s = Sheet("arm-reliquary")
SIL = "#B8B4A8"; GILT = "#C9A227"; CRY = "#CBD3CF"; GAR = "#6E1F2A"; BONE = "#DCD2BA"; NIEL = "#2A2622"
g_sil = s.lg(shade(SIL, 1.35), shade(SIL, .45), 0, 0, 1, 0, mid=SIL)
g_sil_hero = s.lg(shade(SIL, .55), shade(SIL, .4), 0, 0, 1, 0, mid=shade(SIL, 1.3))
g_gilt = s.lg(shade(GILT, 1.35), shade(GILT, .5), 0, 0, 1, 0, mid=GILT)
g_gilt_v = s.lg(shade(GILT, 1.3), shade(GILT, .5), 0, 0, 0, 1, mid=GILT)
g_cry = s.rg("#FFFFFF", CRY, .9, 1, .35, .3, .7)
g_gar = s.rg(shade(GAR, 1.6), GAR, 1, 1, .35, .3, .6)
g_glint = s.rg(GILT, GILT, .25, 0, .5, .5, .5)


def reliquary(cx, gy, k, view="front"):
    """Draw at centre x, ground y, k px per metre. view: front | side | hero."""
    m = lambda v: v * k
    o = []
    hero = view == "hero"
    fill_s = g_sil_hero if hero else g_sil
    W = .12 if view != "side" else .10
    # plinth
    for (w, z0, z1, f) in ((.16, 0, .03, g_gilt), (.13, .03, .06, fill_s)):
        ww = w if view != "side" else w * .88
        o.append(f'<rect x="{cx-m(ww/2):.1f}" y="{gy-m(z1):.1f}" width="{m(ww):.1f}" height="{m(z1-z0):.1f}" fill="{f}" stroke="{NIEL}" stroke-width="1"/>')
        if hero:
            o.append(f'<ellipse cx="{cx}" cy="{gy-m(z1):.1f}" rx="{m(ww/2):.1f}" ry="{m(ww/2)*.3:.1f}" fill="{shade(GILT,1.2) if f==g_gilt else shade(SIL,1.25)}" stroke="{NIEL}" stroke-width=".8"/>')
    # gem row on plinth
    for i in range(-3, 4):
        o.append(f'<circle cx="{cx+m(i*.02):.1f}" cy="{gy-m(.015):.1f}" r="{m(.005):.1f}" fill="{g_gar}" stroke="{NIEL}" stroke-width=".6"/>')
    # sleeve (forearm)
    b, t = W / 2, W * .36
    sleeve = f"M{cx-m(b):.1f},{gy-m(.06):.1f} C{cx-m(b*1.02):.1f},{gy-m(.2):.1f} {cx-m(t*1.2):.1f},{gy-m(.3):.1f} {cx-m(t):.1f},{gy-m(.38):.1f} L{cx+m(t):.1f},{gy-m(.38):.1f} C{cx+m(t*1.2):.1f},{gy-m(.3):.1f} {cx+m(b*1.02):.1f},{gy-m(.2):.1f} {cx+m(b):.1f},{gy-m(.06):.1f} Z"
    s.clip(sleeve, f"sl{view}")
    o.append(f'<path d="{sleeve}" fill="{fill_s}" stroke="{NIEL}" stroke-width="1.1"/>')
    folds = []
    for i in range(-3, 4):
        x0 = cx + m(i * b / 3.4)
        folds.append(f'<path d="M{x0:.1f},{gy-m(.06):.1f} C{x0+m(.004):.1f},{gy-m(.18):.1f} {x0-m(.006):.1f},{gy-m(.28):.1f} {cx+m(i*t/3.4):.1f},{gy-m(.38):.1f}" fill="none" stroke="{shade(SIL,.5)}" stroke-width="1.1" opacity=".7"/>')
        folds.append(f'<path d="M{x0+2:.1f},{gy-m(.06):.1f} C{x0+m(.004)+2:.1f},{gy-m(.18):.1f} {x0-m(.006)+2:.1f},{gy-m(.28):.1f} {cx+m(i*t/3.4)+1.5:.1f},{gy-m(.38):.1f}" fill="none" stroke="{shade(SIL,1.4)}" stroke-width=".7" opacity=".6"/>')
    o.append(f'<g clip-path="url(#sl{view})">' + ''.join(folds) + s.flecks(cx - m(b), gy - m(.38), m(W), m(.32), 60, NIEL, .3, 1.1, .45) + '</g>')
    # gilt edge bands
    for sx in (-1, 1):
        o.append(f'<path d="M{cx+sx*m(b-.004):.1f},{gy-m(.06):.1f} C{cx+sx*m(b*1.0-.004):.1f},{gy-m(.2):.1f} {cx+sx*m(t*1.2-.004):.1f},{gy-m(.3):.1f} {cx+sx*m(t-.003):.1f},{gy-m(.38):.1f}" fill="none" stroke="{GILT}" stroke-width="{m(.006):.1f}"/>')
    # crystal window (front + hero only)
    if view != "side":
        wx = cx - (m(.012) if hero else 0)
        rx = m(.025) * (.8 if hero else 1)
        o.append(f'<ellipse cx="{wx:.1f}" cy="{gy-m(.21):.1f}" rx="{rx+m(.008):.1f}" ry="{m(.053):.1f}" fill="{g_gilt}" stroke="{NIEL}" stroke-width="1"/>')
        for a in range(0, 360, 30):
            px = wx + (rx + m(.008)) * math.cos(math.radians(a)); py = gy - m(.21) + m(.053) * math.sin(math.radians(a))
            o.append(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="{m(.0035):.1f}" fill="{shade(GILT,1.3)}" stroke="{NIEL}" stroke-width=".4"/>')
        o.append(f'<ellipse cx="{wx:.1f}" cy="{gy-m(.21):.1f}" rx="{rx:.1f}" ry="{m(.045):.1f}" fill="{BONE}" stroke="{NIEL}" stroke-width=".8"/>')
        o.append(f'<path d="M{wx-rx*.35:.1f},{gy-m(.245):.1f} C{wx-rx*.5:.1f},{gy-m(.21):.1f} {wx+rx*.4:.1f},{gy-m(.2):.1f} {wx+rx*.2:.1f},{gy-m(.172):.1f}" fill="none" stroke="{shade(BONE,.6)}" stroke-width="{m(.012):.1f}" stroke-linecap="round"/>')
        o.append(f'<path d="M{wx-rx*.9:.1f},{gy-m(.19):.1f} L{wx+rx*.9:.1f},{gy-m(.2):.1f}" stroke="{GAR}" stroke-width="{m(.006):.1f}" opacity=".7"/>')
        o.append(f'<ellipse cx="{wx:.1f}" cy="{gy-m(.21):.1f}" rx="{rx:.1f}" ry="{m(.045):.1f}" fill="{g_cry}" opacity=".55"/>')
        o.append(f'<path d="M{wx-rx*.6:.1f},{gy-m(.235):.1f} Q{wx-rx*.3:.1f},{gy-m(.25):.1f} {wx:.1f},{gy-m(.247):.1f}" stroke="#FFFFFF" stroke-width="1.4" fill="none" opacity=".8"/>')
    else:
        o.append(f'<path d="M{cx+m(.049):.1f},{gy-m(.26):.1f} Q{cx+m(.058):.1f},{gy-m(.21):.1f} {cx+m(.049):.1f},{gy-m(.16):.1f}" fill="{g_gilt}" stroke="{NIEL}" stroke-width="1"/>')
    # cuff band with garnets
    cw = t * 1.25
    o.append(f'<rect x="{cx-m(cw):.1f}" y="{gy-m(.395):.1f}" width="{m(2*cw):.1f}" height="{m(.04):.1f}" rx="{m(.006):.1f}" fill="{g_gilt}" stroke="{NIEL}" stroke-width="1"/>')
    for i in (-2, -1, 0, 1, 2):
        o.append(f'<ellipse cx="{cx+m(i*cw*.38):.1f}" cy="{gy-m(.375):.1f}" rx="{m(.0055):.1f}" ry="{m(.008):.1f}" fill="{g_gar}" stroke="{shade(GILT,.5)}" stroke-width="1"/>')
    o.append(f'<path d="M{cx-m(cw):.1f},{gy-m(.39):.1f} L{cx+m(cw):.1f},{gy-m(.39):.1f}" stroke="{shade(GILT,1.4)}" stroke-width=".8"/>')
    # hand of blessing
    hz = .395
    if view != "side":
        pw = .042
        palm = f"M{cx-m(pw):.1f},{gy-m(hz):.1f} L{cx-m(pw*1.05):.1f},{gy-m(.44):.1f} Q{cx-m(pw*.9):.1f},{gy-m(.465):.1f} {cx-m(.02):.1f},{gy-m(.462):.1f} L{cx+m(pw):.1f},{gy-m(.46):.1f} L{cx+m(pw):.1f},{gy-m(hz):.1f} Z"
        o.append(f'<path d="{palm}" fill="{fill_s}" stroke="{NIEL}" stroke-width="1"/>')
        # folded ring + little fingers
        o.append(f'<path d="M{cx-m(.04):.1f},{gy-m(.455):.1f} q{m(.018):.1f},{-m(.018):.1f} {m(.036):.1f},0" fill="{shade(SIL,.8)}" stroke="{NIEL}" stroke-width=".8"/>')
        # raised index + middle
        for fx, top in ((-.004, .52), (.017, .515)):
            o.append(f'<rect x="{cx+m(fx):.1f}" y="{gy-m(top):.1f}" width="{m(.019):.1f}" height="{m(top-.455):.1f}" rx="{m(.009):.1f}" fill="{fill_s}" stroke="{NIEL}" stroke-width="1"/>')
            o.append(f'<path d="M{cx+m(fx+.003):.1f},{gy-m(top-.025):.1f} h{m(.013):.1f} M{cx+m(fx+.003):.1f},{gy-m(top-.045):.1f} h{m(.013):.1f}" stroke="{shade(SIL,.5)}" stroke-width=".8"/>')
        # thumb
        o.append(f'<path d="M{cx+m(pw):.1f},{gy-m(.43):.1f} C{cx+m(.06):.1f},{gy-m(.44):.1f} {cx+m(.065):.1f},{gy-m(.465):.1f} {cx+m(.052):.1f},{gy-m(.47):.1f} C{cx+m(.045):.1f},{gy-m(.46):.1f} {cx+m(.043):.1f},{gy-m(.45):.1f} {cx+m(pw):.1f},{gy-m(.445):.1f} Z" fill="{fill_s}" stroke="{NIEL}" stroke-width="1"/>')
        # gilt ring on the finger
        o.append(f'<rect x="{cx+m(-.005):.1f}" y="{gy-m(.475):.1f}" width="{m(.021):.1f}" height="{m(.006):.1f}" fill="{GILT}"/>')
    else:
        o.append(f'<path d="M{cx-m(.03):.1f},{gy-m(hz):.1f} L{cx-m(.028):.1f},{gy-m(.465):.1f} C{cx-m(.024):.1f},{gy-m(.5):.1f} {cx-m(.018):.1f},{gy-m(.52):.1f} {cx-m(.006):.1f},{gy-m(.52):.1f} C{cx+m(.004):.1f},{gy-m(.515):.1f} {cx+m(.006):.1f},{gy-m(.49):.1f} {cx+m(.01):.1f},{gy-m(.465):.1f} C{cx+m(.03):.1f},{gy-m(.462):.1f} {cx+m(.045):.1f},{gy-m(.45):.1f} {cx+m(.04):.1f},{gy-m(.435):.1f} L{cx+m(.03):.1f},{gy-m(hz):.1f} Z" fill="{fill_s}" stroke="{NIEL}" stroke-width="1"/>')
        o.append(f'<path d="M{cx-m(.01):.1f},{gy-m(.5):.1f} h{m(.012):.1f} M{cx-m(.012):.1f},{gy-m(.48):.1f} h{m(.014):.1f}" stroke="{shade(SIL,.5)}" stroke-width=".8"/>')
    return ''.join(o)


HX, HG, HK = 370, 650, 950
parts = []
parts.append(f'<ellipse cx="{HX}" cy="{HG+4}" rx="120" ry="16" fill="#0B0A08" opacity=".7"/>')
parts.append(f'<circle cx="{HX}" cy="{HG-240}" r="230" fill="{g_glint}"/>')
parts.append(reliquary(HX, HG, HK, "hero"))
# hero rim light + wear
parts.append(fracture(f"M{HX-18},{HG-199} l-14,-10 M{HX-12},{HG-221} l-10,-16 M{HX-4},{HG-196} l8,14 M{HX-10},{HG-180} l-4,18"))
parts.append(fracture(f"M{HX-8},{HG-431} l30,0", 1.8))
# orthos
FX, SX, OK = 800, 1040, 500
parts.append(reliquary(FX, 690, OK, "front"))
parts.append(reliquary(SX, 690, OK, "side"))
parts.append(fracture(f"M{FX-6},{690-OK*.458:.0f} l22,0", 1.4))
parts.append(view_label(HX, "HERO 3/4 · 1 m = 950 px", 690 + 40))
parts.append(view_label(FX, "FRONT", 730)); parts.append(view_label(SX, "SIDE", 730))
parts.append(scale_bar(880, 370, 50, "0.1 m = 50 px (orthos)"))
# grabs (hero)
parts.append(grab(HX - 50, HG - 110, "GRAB · ONE HAND", -12, -12, "end"))
parts.append(grab(HX + 76, HG - 20, "GRAB · PLINTH", 12, 4))
parts.append(grab(FX + 38, 690 - 55, "", 0, 0))
# callouts
parts.append(callout(HX + 22, HG - 505, 560, 175, "Hand of blessing", "cast silver, 2 raised fingers"))
parts.append(callout(HX + 32, HG - 360, 560, 235, "Cuff band + 5 garnets", "silver-gilt, cabochon set"))
parts.append(callout(HX - 12, HG - 230, 560, 300, "Rock-crystal window", "9 × 5 cm, FRAGILE: 3 m/s"))
parts.append(callout(HX + 55, HG - 150, 560, 365, "Silver sleeve, chased folds", "0.8 mm sheet on oak core"))
parts.append(callout(HX - 70, HG - 15, 262, 560, "Stepped plinth, gilt", "garnet row, felt underside", side="left"))
parts.append(callout(HX - 22, HG - 200, 262, 250, "The relic: a forearm bone", "wrapped in red silk", side="left"))
parts.append(text(560, 440, "Fracture (madder, dashed):", 10.5, "#C4542E"))
parts.append(text(560, 455, "window stars; the raised", 10.5, "#9A9078"))
parts.append(text(560, 470, "fingers snap at the knuckle", 10.5, "#9A9078"))
head = item_frame(s, "Arm Reliquary", 600, 1, "0.16 × 0.14 × 0.52 m · ARTIFACT · ≤ 1.5k tris · 1024²", "orthos 1 m = 500 px · fragility 3 m/s")
write(s, head, parts, palette([("silver", SIL), ("gilt (orpiment)", GILT), ("rock crystal", CRY), ("garnet", GAR), ("relic bone", BONE), ("niello / soot", NIEL)]))
