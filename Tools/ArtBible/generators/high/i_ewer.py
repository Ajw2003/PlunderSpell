from lib import *

s = Sheet("silver-ewer")
SIL = "#B8B4A8"; GILT = "#C9A227"; TARN = "#5A5850"; NIEL = "#2A2622"; ENAM = "#3E5470"
g_sil = s.lg(shade(SIL, 1.4), shade(SIL, .4), 0, 0, 1, 0, mid=SIL)
g_silH = s.lg(shade(SIL, .7), shade(SIL, .38), 0, 0, 1, 0, mid=shade(SIL, 1.45))
g_gilt = s.lg(shade(GILT, 1.35), shade(GILT, .5), 0, 0, 1, 0, mid=GILT)
g_glint = s.rg(SIL, SIL, .16, 0, .5, .5, .5)
PROF = [(0, .05), (.012, .05), (.018, .038), (.035, .03), (.06, .055), (.12, .08), (.17, .072), (.21, .05), (.245, .034), (.27, .036), (.3, .046)]


def body_path(cx, gy, k, sx=1.0):
    L = []; R = []
    for z, r in PROF:
        L.append((cx - r * k * sx, gy - z * k)); R.append((cx + r * k * sx, gy - z * k))
    pts = R + L[::-1]
    # smooth through points with Catmull-Rom -> cubic
    def cr(P):
        d = f"M{P[0][0]:.1f},{P[0][1]:.1f}"
        for i in range(len(P) - 1):
            p0 = P[i - 1] if i > 0 else P[i]; p1 = P[i]; p2 = P[i + 1]; p3 = P[i + 2] if i + 2 < len(P) else P[i + 1]
            c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6); c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
            d += f" C{c1[0]:.1f},{c1[1]:.1f} {c2[0]:.1f},{c2[1]:.1f} {p2[0]:.1f},{p2[1]:.1f}"
        return d
    return cr(R) + f" L{L[-1][0]:.1f},{L[-1][1]:.1f}" + cr(L[::-1])[cr(L[::-1]).index(' '):] + " Z"


def ewer(cx, gy, k, view="front", dents=0, tag=""):
    m = lambda v: v * k
    o = []
    fill = g_silH if view == "hero" else g_sil
    handle = view in ("front", "hero")
    if handle:
        o.append(f'<path d="M{cx-m(.03):.1f},{gy-m(.27):.1f} C{cx-m(.12):.1f},{gy-m(.3):.1f} {cx-m(.13):.1f},{gy-m(.16):.1f} {cx-m(.07):.1f},{gy-m(.11):.1f}" fill="none" stroke="{NIEL}" stroke-width="{m(.016):.1f}" stroke-linecap="round"/>')
        o.append(f'<path d="M{cx-m(.03):.1f},{gy-m(.27):.1f} C{cx-m(.12):.1f},{gy-m(.3):.1f} {cx-m(.13):.1f},{gy-m(.16):.1f} {cx-m(.07):.1f},{gy-m(.11):.1f}" fill="none" stroke="{SIL}" stroke-width="{m(.011):.1f}" stroke-linecap="round"/>')
        o.append(f'<path d="M{cx-m(.035):.1f},{gy-m(.272):.1f} C{cx-m(.118):.1f},{gy-m(.297):.1f} {cx-m(.126):.1f},{gy-m(.17):.1f} {cx-m(.08):.1f},{gy-m(.13):.1f}" fill="none" stroke="#FFFFFF" stroke-width="1" opacity=".5"/>')
        o.append(f'<circle cx="{cx-m(.108):.1f}" cy="{gy-m(.24):.1f}" r="{m(.008):.1f}" fill="{g_gilt}" stroke="{NIEL}" stroke-width=".6"/>')
    if view == "side":
        # handle peeks behind at the left edge of the neck silhouette (hidden), spout points at viewer
        o.append(f'<path d="M{cx-m(.008):.1f},{gy-m(.28):.1f} l{m(.016):.1f},0 l0,{m(.03):.1f} l{-m(.016):.1f},0 Z" fill="{shade(SIL,.5)}"/>')
    bd = body_path(cx, gy, k)
    s.clip(bd, f"eb{view}{tag}")
    o.append(f'<path d="{bd}" fill="{fill}" stroke="{NIEL}" stroke-width="1.1"/>')
    band = []
    band.append(f'<rect x="{cx-m(.09):.1f}" y="{gy-m(.13):.1f}" width="{m(.18):.1f}" height="{m(.018):.1f}" fill="{NIEL}" opacity=".8"/>')
    for i in range(-9, 10):
        band.append(f'<path d="M{cx+m(i*.009):.1f},{gy-m(.128):.1f} l{m(.004):.1f},{m(.014):.1f}" stroke="{SIL}" stroke-width=".8"/>')
    band.append(f'<rect x="{cx-m(.09):.1f}" y="{gy-m(.03):.1f}" width="{m(.18):.1f}" height="{m(.006):.1f}" fill="{GILT}"/>')
    band.append(f'<path d="M{cx+m(.03):.1f},{gy-m(.2):.1f} C{cx+m(.06):.1f},{gy-m(.18):.1f} {cx+m(.07):.1f},{gy-m(.1):.1f} {cx+m(.05):.1f},{gy-m(.06):.1f}" fill="none" stroke="#FFFFFF" stroke-width="{m(.008):.1f}" opacity=".35"/>' if view != "hero" else
                f'<path d="M{cx-m(.01):.1f},{gy-m(.2):.1f} C{cx+m(.01):.1f},{gy-m(.17):.1f} {cx+m(.012):.1f},{gy-m(.1):.1f} {cx:.1f},{gy-m(.06):.1f}" fill="none" stroke="#FFFFFF" stroke-width="{m(.01):.1f}" opacity=".45"/>')
    band.append(s.flecks(cx - m(.08), gy - m(.3), m(.16), m(.3), 50, TARN, .3, 1.2, .5))
    band.append(f'<path d="M{cx-m(.09):.1f},{gy-m(.06):.1f} C{cx-m(.04):.1f},{gy-m(.04):.1f} {cx+m(.04):.1f},{gy-m(.04):.1f} {cx+m(.09):.1f},{gy-m(.06):.1f} L{cx+m(.09):.1f},{gy:.1f} L{cx-m(.09):.1f},{gy:.1f} Z" fill="{TARN}" opacity=".45"/>')
    for (dx, dz, r) in ([(.035, .16, .018), (-.04, .09, .014), (.05, .075, .012)][:dents]):
        band.append(f'<ellipse cx="{cx+m(dx):.1f}" cy="{gy-m(dz):.1f}" rx="{m(r):.1f}" ry="{m(r*.75):.1f}" fill="{NIEL}" opacity=".22"/>'
                    f'<path d="M{cx+m(dx-r*.8):.1f},{gy-m(dz-r*.4):.1f} Q{cx+m(dx):.1f},{gy-m(dz-r*.9):.1f} {cx+m(dx+r*.8):.1f},{gy-m(dz-r*.4):.1f}" fill="none" stroke="#FFFFFF" stroke-width="{max(m(.002),.6):.1f}" opacity=".45"/>')
    o.append(f'<g clip-path="url(#eb{view}{tag})">' + ''.join(band) + '</g>')
    # enamel medallion (front + hero show it; side shows it too, on the belly facing the viewer under the spout)
    mx = cx + (m(.01) if view == "hero" else 0)
    o.append(f'<circle cx="{mx:.1f}" cy="{gy-m(.155):.1f}" r="{m(.022):.1f}" fill="{g_gilt}" stroke="{NIEL}" stroke-width=".8"/>')
    o.append(f'<circle cx="{mx:.1f}" cy="{gy-m(.155):.1f}" r="{m(.017):.1f}" fill="{ENAM}"/>')
    o.append(f'<path d="M{mx-m(.014):.1f},{gy-m(.148):.1f} L{mx:.1f},{gy-m(.166):.1f} L{mx+m(.014):.1f},{gy-m(.148):.1f}" fill="none" stroke="{SIL}" stroke-width="{m(.005):.1f}"/>')
    # spout
    if view in ("front", "hero"):
        sp = f"M{cx+m(.06):.1f},{gy-m(.19):.1f} C{cx+m(.09):.1f},{gy-m(.2):.1f} {cx+m(.1):.1f},{gy-m(.24):.1f} {cx+m(.12):.1f},{gy-m(.275):.1f} L{cx+m(.125):.1f},{gy-m(.268):.1f} C{cx+m(.11):.1f},{gy-m(.225):.1f} {cx+m(.1):.1f},{gy-m(.17):.1f} {cx+m(.07):.1f},{gy-m(.15):.1f} Z"
        o.append(f'<path d="{sp}" fill="{g_sil}" stroke="{NIEL}" stroke-width="1"/>')
        o.append(f'<ellipse cx="{cx+m(.1225):.1f}" cy="{gy-m(.2715):.1f}" rx="{m(.006):.1f}" ry="{m(.004):.1f}" fill="{NIEL}"/>')
        o.append(f'<path d="M{cx+m(.062):.1f},{gy-m(.17):.1f} q{m(.008):.1f},{-m(.01):.1f} 0,{-m(.02):.1f}" fill="{g_gilt}" stroke="{NIEL}" stroke-width=".6"/>')
    else:
        o.append(f'<ellipse cx="{cx:.1f}" cy="{gy-m(.24):.1f}" rx="{m(.012):.1f}" ry="{m(.015):.1f}" fill="{g_sil}" stroke="{NIEL}" stroke-width="1"/>')
        o.append(f'<ellipse cx="{cx:.1f}" cy="{gy-m(.272):.1f}" rx="{m(.008):.1f}" ry="{m(.006):.1f}" fill="{NIEL}"/>')
    # rim, hinged lid, finial
    o.append(f'<rect x="{cx-m(.048):.1f}" y="{gy-m(.305):.1f}" width="{m(.096):.1f}" height="{m(.01):.1f}" rx="2" fill="{g_gilt}" stroke="{NIEL}" stroke-width=".8"/>')
    o.append(f'<path d="M{cx-m(.046):.1f},{gy-m(.305):.1f} C{cx-m(.04):.1f},{gy-m(.33):.1f} {cx+m(.04):.1f},{gy-m(.33):.1f} {cx+m(.046):.1f},{gy-m(.305):.1f} Z" fill="{fill}" stroke="{NIEL}" stroke-width="1"/>')
    o.append(f'<circle cx="{cx:.1f}" cy="{gy-m(.332):.1f}" r="{m(.008):.1f}" fill="{g_gilt}" stroke="{NIEL}" stroke-width=".7"/>')
    if handle:
        o.append(f'<path d="M{cx-m(.046):.1f},{gy-m(.31):.1f} l{-m(.012):.1f},{-m(.014):.1f} l{m(.008):.1f},{-m(.004):.1f} Z" fill="{g_gilt}" stroke="{NIEL}" stroke-width=".6"/>')
    if view == "hero":
        o.append(f'<ellipse cx="{cx:.1f}" cy="{gy-m(.012):.1f}" rx="{m(.05):.1f}" ry="{m(.012):.1f}" fill="none" stroke="{GILT}" stroke-width="1.2"/>')
    return ''.join(o)


HX, HG, HK = 360, 655, 1350
parts = [f'<ellipse cx="{HX}" cy="{HG+3}" rx="130" ry="14" fill="#0B0A08" opacity=".7"/>', f'<circle cx="{HX}" cy="{HG-230}" r="260" fill="{g_glint}"/>']
parts.append(ewer(HX, HG, HK, "hero", dents=2))
FX, SX, OK = 800, 1040, 700
parts.append(ewer(FX, 690, OK, "front")); parts.append(ewer(SX, 690, OK, "side"))
parts.append(view_label(HX, "HERO 3/4 · 1 m = 1350 px", 730)); parts.append(view_label(FX, "FRONT", 730)); parts.append(view_label(SX, "SIDE", 730))
parts.append(scale_bar(880, 330, 70, "0.1 m = 70 px (orthos)"))
# dent states inset
parts.append(text(700, 170, "DENT STATES (shape keys, worth unchanged)", 10.5, "#9A9078", extra=' letter-spacing="1"'))
for i, d in enumerate((0, 2, 3)):
    x = 760 + i * 150
    parts.append(ewer(x, 290, 300, "front", dents=d, tag=f"i{i}"))
    parts.append(text(x, 305, ("pristine", "> 5 m/s", "> 9 m/s")[i], 10, "#635C4C", "middle"))
parts.append(grab(HX - 150, HG - 290, "GRAB · HANDLE / NECK", -10, -12, "end"))
parts.append(grab(HX - 20, HG - 360, "", 0, 0))
C = [callout(HX, HG - 452, 560, 380, "Hinged lid + gilt finial", "thumbpiece at the handle"),
     callout(HX + 150, HG - 360, 560, 430, "Long spout, soldered", "sleeve joint gilt"),
     callout(HX + 15, HG - 210, 560, 480, "Enamel medallion, 5.8 cm", "champlevé: the arms"),
     callout(HX + 90, HG - 170, 560, 530, "Niello band, chased", "tarnish sits in the cuts"),
     callout(HX + 50, HG - 75, 560, 580, "Raised belly, 0.16 m", "one hammered sheet, 1 mm"),
     callout(HX + 60, HG - 15, 560, 630, "Foot ring, parcel-gilt", "rubbed to silver on the edge")]
parts += C
parts.append(text(40, 150, "UNBREAKABLE — dents, never shatters", 11, "#C4542E"))
head = item_frame(s, "Silver Ewer", 180, 2, "0.22 × 0.16 × 0.34 m · ≤ 1.5k tris · 1024²", "orthos 1 m = 700 px · fragility 999 (dents)")
write(s, head, parts, palette([("silver", SIL), ("parcel gilt", GILT), ("tarnish", TARN), ("niello", NIEL), ("woad enamel", ENAM)]))
