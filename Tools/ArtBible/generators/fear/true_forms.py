"""Three true-form options for the Lantern Warden (#386). Each keeps pieces of the human form
(kettle hat, quilted gambeson, woad hose, the horn lantern's flame) so a player knows which guard
it was, and each moves the lantern's light somewhere new, because the light is the tell.

Run from the repo root:  python3 Tools/ArtBible/generators/fear/true_forms.py
"""
import math
from lib import *  # noqa: F403

WOOL = "#C4B89C"; WOAD = "#3E5470"; LEATH = "#5A4030"; IRON = "#6F7479"; HORN = "#BFA57A"; SKIN = "#A07A5C"
FLESH = "#6E2E24"   # madder darkened: the inside of the man, read as blood
BONE = "#DCD2BA"     # vellum: teeth, ribs, wing membranes
FLAME = "#F0D8A0"

K = 2.2   # px per cm on the sheet (1 m = 220 px), same as the Lantern Warden's sheet
TRAITS2 = "1 m = 220 px · ground y 690"


def glow(s, cx, cy, r, core=FLAME):
    g1 = s.rg(MADDER, MADDER, .55, 0, .5, .5, .5)
    g2 = s.rg(core, HORN, .9, 0, .5, .5, .5)
    return f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{g1}"/><circle cx="{cx}" cy="{cy}" r="{r*.35:.1f}" fill="{g2}"/>'


def flame(s, cx, base, h=9):
    g = s.rg(FLAME, MADDER, 1, 1, .5, .7, .6)
    return (f'<path d="M{cx},{base} C{cx-h*.35},{base-h*.45} {cx-h*.1},{base-h*.75} {cx},{base-h} '
            f'C{cx+h*.1},{base-h*.75} {cx+h*.35},{base-h*.45} {cx},{base} Z" fill="{g}"/>')


def kettle_hat(s, cx, cy, tilt=0, scale=1.0):
    g = s.lg(shade(IRON, 1.3), shade(IRON, .45), 0, 0, 1, .6, mid=IRON)
    return (f'<g transform="translate({cx},{cy}) rotate({tilt}) scale({scale})">'
            f'<path d="M-10.5,0 C-10.5,-8 -6,-12 0,-12 C6,-12 10.5,-8 10.5,0 Z" fill="{g}" stroke="{BLACK}" stroke-width=".4"/>'
            f'<path d="M0,-12 L0,0" stroke="{shade(IRON,1.35)}" stroke-width=".6"/>'
            f'<path d="M-20,3 Q0,-2.5 20,3 L19,5 Q0,.5 -19,5 Z" fill="{g}" stroke="{BLACK}" stroke-width=".4"/></g>')


def quilt(x0, x1, y0, y1, step=4, col=None):
    col = col or shade(WOOL, .62)
    return ''.join(f'<path d="M{x},{y0} C{x+.6},{(y0*2+y1)/3:.0f} {x-.4},{(y0+y1*2)/3:.0f} {x},{y1}" fill="none" stroke="{col}" stroke-width=".35"/>'
                   for x in range(int(x0), int(x1) + 1, step))


# ─────────────────────────────── A. THE WICK ───────────────────────────────
def the_wick():
    s = Sheet("true-form-a-the-wick")
    f = [f'<g transform="translate(470,690) scale({K})">', f'<ellipse cx="0" cy="0" rx="34" ry="2.6" fill="#0B0A08" opacity=".7"/>']
    gw = s.form(WOOL); gwo = s.form(WOAD); gs = s.form(SKIN); gf = s.form(FLESH)
    # stilted legs: thin, too long, hose split at the knee, the shoes burst by long toes
    for sg in (-1, 1):
        x = 9 * sg
        f.append(f'<path d="M{x-6},-112 L{x-4},-60 C{x-5},-40 {x-4},-22 {x-2.5},-8 L{x+2.5},-8 C{x+3.5},-22 {x+4},-40 {x+3.5},-60 L{x+6},-112 Z" fill="{gwo}" stroke="{shade(WOAD,.45)}" stroke-width=".35"/>')
        f.append(f'<path d="M{x-4},-62 L{x+3.5},-62 L{x+3},-57 L{x-3.5},-57 Z" fill="{gs}"/>')  # skin through the torn hose
        f.append(f'<path d="M{x-4},-60 l2,4 l1.5,-3 l2,4 l2,-5" fill="none" stroke="{shade(WOAD,.4)}" stroke-width=".5"/>')
        f.append(f'<path d="M{x-5},-9 L{x+4},-9 Q{x+5},-4 {x+5},0 L{x-6},0 Q{x-6.5},-5 {x-5},-9 Z" fill="{shade(LEATH,.9)}" stroke="{BLACK}" stroke-width=".35"/>')
        for t in (-1, 0, 1):
            f.append(f'<path d="M{x+t*3},-2 Q{x+t*5+sg*2},1 {x+t*6+sg*5},0.4" fill="none" stroke="{shade(SKIN,.55)}" stroke-width="1.2" stroke-linecap="round"/>')
    # gambeson, stretched long and narrow, split open down the lacing
    torso = "M-24,-207 Q-12,-196 0,-199 Q12,-196 24,-207 C22,-185 19,-160 16,-140 L18,-130 C21,-122 24,-114 26,-104 Q0,-99 -26,-104 C-24,-114 -21,-122 -18,-130 L-16,-140 C-19,-160 -22,-185 -23,-200 Z"
    s.clip(torso, "wickT")
    f.append(f'<path d="{torso}" fill="{gw}" stroke="{shade(WOOL,.4)}" stroke-width=".45"/>')
    f.append(f'<g clip-path="url(#wickT)">{quilt(-26, 26, -206, -98)}'
             f'<path d="M10,-202 C20,-170 22,-130 28,-100 L30,-100 L30,-206 Z" fill="{BLACK}" opacity=".3"/>'
             + s.flecks(-26, -150, 52, 50, 50, "#3A2E1E", .2, .7, .4) + '</g>')
    # the rib cage: the lacing torn, the coat parted like a mouth, ribs inside
    cav = "M0,-198 C-9,-185 -11,-160 -8,-134 C-6,-126 -3,-122 0,-120 C3,-122 6,-126 8,-134 C11,-160 9,-185 0,-198 Z"
    f.append(f'<path d="{cav}" fill="{gf}" stroke="{BLACK}" stroke-width=".5"/>')
    for i in range(6):
        y = -188 + i * 10
        w = 7.5 - abs(i - 2.2) * .8
        f.append(f'<path d="M-{w:.1f},{y+3} Q0,{y-2} {w:.1f},{y+3}" fill="none" stroke="{BONE}" stroke-width="1.4" stroke-linecap="round"/>')
    f.append(f'<path d="M0,-196 L0,-124" stroke="{shade(BONE,.7)}" stroke-width="1.1"/>')
    for i in range(5):  # torn lacing ends hanging from both lips
        y = -186 + i * 12
        f.append(f'<path d="M-8,{y} l-2,5 M8,{y} l2,5" stroke="{LEATH}" stroke-width=".6"/>')
    # belt, hanging loose
    f.append(f'<path d="M-17,-136 Q-8,-131 -2,-134 M2,-133 Q9,-130 17,-136" fill="none" stroke="{LEATH}" stroke-width="3.2"/>')
    # arms: far too long, elbows low, fingers 25 cm reaching past the knees
    for sg, reach in ((-1, -48), (1, -42)):
        sx = 22 * sg
        ex, ey = 38 * sg, -152
        hx, hy = 33 * sg, -98
        f.append(limb([(sx, -202), (ex, ey), (hx, hy)], [6.5, 5, 3.8], gw, outline=shade(WOOL, .4)))
        f.append(rings((sx, -202), (ex, ey), 6.5, 5, 7, shade(WOOL, .62)))
        f.append(limb([(hx, hy), (hx + 1 * sg, hy + 8)], [3.8, 3.4], gs, outline=shade(SKIN, .45)))
        for j, dx in enumerate((-3, -1, 1, 3)):
            tipx = hx + sg * 2 + dx * 1.6
            tipy = reach - j % 2 * 4
            f.append(f'<path d="M{hx+dx},{hy+8} C{hx+dx*1.2},{hy+20} {tipx-sg*2},{(hy+tipy)/2+10:.0f} {tipx},{tipy}" fill="none" stroke="{shade(SKIN,.8)}" stroke-width="1.6" stroke-linecap="round"/>')
            f.append(f'<path d="M{tipx},{tipy} l{sg*.6},2.4" stroke="{BONE}" stroke-width="1" stroke-linecap="round"/>')
    # the head opened like a lantern: the face split into four flaps peeled back and down
    f.append(f'<path d="M-6,-199 L-5,-207 L5,-207 L6,-199 Z" fill="{gs}" stroke="{shade(SKIN,.45)}" stroke-width=".4"/>')
    f.append(glow(s, 0, -218, 75))
    for ang, ln in ((-78, 22), (-38, 20), (38, 20), (78, 22)):
        a = math.radians(ang)
        tx, ty = math.sin(a) * ln, -207 - math.cos(a) * ln * .35 + 8
        bx = 4.5 if ang > 0 else -4.5
        f.append(f'<path d="M{bx*.4:.1f},-208 C{bx*1.5:.1f},-222 {tx*1.1:.1f},{ty-16:.1f} {tx:.1f},{ty:.1f} '
                 f'C{tx*.8:.1f},{ty-6:.1f} {bx*.9:.1f},-210 {bx*.2:.1f},-206 Z" fill="{gs}" stroke="{shade(SKIN,.4)}" stroke-width=".45"/>')
        f.append(f'<path d="M{bx*.5:.1f},-209 C{bx*1.4:.1f},-219 {tx*1.0:.1f},{ty-12:.1f} {tx*.97:.1f},{ty-1:.1f}" fill="none" stroke="{FLESH}" stroke-width="2.4"/>')
    # one eye left on the outer left flap, still open, and half a jaw of teeth on the right
    f.append(f'<path d="M-19.5,-206 l3.5,-1.4 l1.4,1.8 l-3.6,1.3 Z" fill="{BONE}" stroke="{BLACK}" stroke-width=".3"/><circle cx="-17.2" cy="-206.2" r=".8" fill="{BLACK}"/>')
    for k in range(4):
        f.append(f'<path d="M{15+k*1.6:.1f},{-209+k*1.4:.1f} l1.2,2.6 l.8,-2.9 Z" fill="{BONE}"/>')
    # the candle's flame burns in the open throat, where the head was
    f.append(f'<ellipse cx="0" cy="-208" rx="5" ry="2" fill="{FLESH}"/>')
    f.append(flame(s, 0, -208, 20))
    # the kettle hat, still on, held up on a stalk of spine above the flame
    f.append(f'<path d="M-1.4,-224 C-2,-230 -1.2,-235 0,-238 C1.2,-235 2,-230 1.4,-224 Z" fill="{BONE}" stroke="{BLACK}" stroke-width=".3"/>')
    for y in (-226, -230, -234):
        f.append(f'<path d="M-2,{y} L2,{y}" stroke="{shade(BONE,.6)}" stroke-width=".5"/>')
    f.append(f'<path d="M0,-224 L0,-215" stroke="{shade(BONE,.85)}" stroke-width="1.2" opacity=".6"/>')
    f.append(kettle_hat(s, 0, -238, tilt=-7))
    f.append('</g>')

    P = lambda x, y: (round(470 + K * x, 1), round(690 + K * y, 1))
    C = [callout(*P(4, -245), 840, 150, "Kettle hat, still worn", "on a stalk of spine: crown 2.40 m"),
         callout(*P(1, -220), 840, 215, "Lantern flame in the throat", "the light rises from hip to head"),
         callout(*P(19, -209), 840, 275, "The face, peeled in four", "flaps curl back; half a jaw left"),
         callout(*P(6, -170), 840, 345, "Coat parted down the lacing", "ribs inside; madder only as blood"),
         callout(*P(37, -60), 840, 420, "Fingers 25 cm, below the knee", "arms reach 0.45 m from the floor"),
         callout(*P(12, -60), 840, 480, "Hose split at the knee", "legs 1.12 m, stilted"),
         callout(*P(-17, -206), 385, 250, "One eye left", "on the peeled face", side="left")]
    rows = [("At ten paces", "The light is at head height now, not at the hip, and it sways with a slow stride. A tall thing in a quilted coat."),
            ("The hint it gives", "It sees only what its own flame lights. Darkness hides you; stay out of the swinging cone.")]
    head = frame_open(s, "FEAR PASS · LANTERN WARDEN · TRUE FORM · OPTION A", "The Wick", "H 2.40 m · fits 2.59 m arch · ≤ 8k tris", TRAITS2, glow_cx="40%", glow_cy="38%")
    parts = [ladder_and_human(220, 2.5), view_label(470, "FRONT"), ''.join(f), ''.join(C), notes(840, 545, rows)]
    write_plate(s, head, parts, palette([("gambeson wool", WOOL), ("woad hose", WOAD), ("skin", SKIN), ("blood (madder)", FLESH), ("bone (vellum)", BONE), ("flame", MADDER), ("black iron", IRON)]))


# ─────────────────────────────── B. THE HOLLOW COAT ───────────────────────────────
def hollow_coat():
    s = Sheet("true-form-b-the-hollow-coat")
    f = [f'<g transform="translate(470,690) scale({K})">', f'<ellipse cx="0" cy="0" rx="46" ry="3" fill="#0B0A08" opacity=".7"/>']
    gw = s.form(WOOL); gs = s.form(SKIN)
    # what is left of the man at the hem: hose and shoes lying empty
    f.append(f'<path d="M-30,-3 C-24,-7 -14,-6 -8,-4 L-8,0 L-31,0 Z" fill="{shade(WOAD,.8)}" stroke="{shade(WOAD,.4)}" stroke-width=".35"/>')
    f.append(f'<path d="M-38,-4 L-29,-4 Q-27,-1 -27,0 L-39,0 Z" fill="{LEATH}"/>')
    f.append(f'<path d="M22,-3 C28,-6 36,-5 41,-2 L41,0 L22,0 Z" fill="{shade(WOAD,.7)}"/>')
    # the coat stands up on its own, a quilted bell to 2.0 m, hem trailing
    coat = "M-15,-196 Q0,-201 15,-196 C19,-170 24,-130 30,-90 C34,-60 38,-30 40,-6 Q0,2 -40,-6 C-38,-30 -34,-60 -30,-90 C-24,-130 -19,-170 -15,-196 Z"
    s.clip(coat, "coatB")
    f.append(f'<path d="{coat}" fill="{gw}" stroke="{shade(WOOL,.4)}" stroke-width=".45"/>')
    q = ''.join(f'<path d="M{x*.38:.1f},-200 C{x*.5:.1f},-150 {x*.8:.1f},-80 {x:.1f},0" fill="none" stroke="{shade(WOOL,.62)}" stroke-width=".35"/>' for x in range(-40, 41, 5))
    f.append(f'<g clip-path="url(#coatB)">{q}<path d="M8,-200 C22,-130 30,-60 42,0 L44,0 L44,-200 Z" fill="{BLACK}" opacity=".3"/>'
             + s.flecks(-40, -60, 80, 58, 70, "#2A2418", .2, .7, .45) + '</g>')
    # quilting channels split open; pale wings and feelers press out of the slits
    rng = s.rng
    for i, (x, y, ln) in enumerate([(-14, -150, 22), (-6, -120, 26), (4, -160, 18), (12, -110, 24), (-20, -90, 22), (20, -75, 22), (-2, -70, 26), (-26, -45, 18), (26, -40, 18), (8, -40, 22)]):
        f.append(f'<path d="M{x},{y} Q{x+1.5},{y+ln/2} {x},{y+ln} Q{x-1.5},{y+ln/2} {x},{y} Z" fill="{BLACK}"/>')
        sg = -1 if x < 0 else 1
        wy = y + ln * .35
        f.append(f'<path d="M{x},{wy} C{x+sg*7},{wy-9} {x+sg*14},{wy-5} {x+sg*13},{wy+2} C{x+sg*9},{wy+4} {x+sg*4},{wy+3} {x},{wy+5} Z" fill="{BONE}" opacity=".92" stroke="{shade(BONE,.6)}" stroke-width=".3"/>')
        f.append(f'<circle cx="{x+sg*8.5:.1f}" cy="{wy-1.5:.1f}" r="1.6" fill="{BLACK}"/><circle cx="{x+sg*8.5:.1f}" cy="{wy-1.5:.1f}" r=".6" fill="{shade(BONE,.7)}"/>')
        for k in range(2):
            a = rng.uniform(-1, 1)
            f.append(f'<path d="M{x},{y+ln*.7:.1f} q{sg*4+a*2:.1f},{3+k*2} {sg*(7+k*3):.1f},{7+k*3}" fill="none" stroke="{shade(BONE,.85)}" stroke-width=".45"/>')
    # the empty coif: a hood with nothing in it, feathered antennae fanning from the dark
    f.append(f'<path d="M-10,-213 C-11,-205 -9,-198 -8,-196 L8,-196 C9,-198 11,-205 10,-213 C6,-218 -6,-218 -10,-213 Z" fill="{shade(WOOL,.82)}" stroke="{shade(WOOL,.4)}" stroke-width=".4"/>')
    f.append(f'<ellipse cx="0" cy="-205" rx="6.5" ry="7.5" fill="{BLACK}"/>')
    for ang in (-48, -26, -8, 10, 30, 52):
        a = math.radians(ang)
        tx, ty = math.sin(a) * 36, -206 - math.cos(a) * 30
        f.append(f'<path d="M0,-205 Q{tx*.4:.1f},{ty+8:.1f} {tx:.1f},{ty:.1f}" fill="none" stroke="{shade(BONE,.9)}" stroke-width=".6"/>')
        for t in (.45, .6, .75, .9):
            px, py = tx * t, -205 + (ty + 205) * t
            nx, ny = math.cos(a) * 2.2, math.sin(a) * 2.2
            f.append(f'<path d="M{px-nx:.1f},{py-ny:.1f} L{px+nx:.1f},{py+ny:.1f}" stroke="{shade(BONE,.75)}" stroke-width=".35"/>')
    # one arm: three joints long, lifts the lantern high and forward, swinging it ahead
    f.append(limb([(14, -190), (34, -176), (44, -212), (58, -222)], [5, 3.6, 3, 2.4], gw, outline=shade(WOOL, .4)))
    f.append(f'<path d="M58,-222 L62,-223" stroke="{shade(SKIN,.6)}" stroke-width="2.2" stroke-linecap="round"/>')
    lx, ly = 63, -214
    f.append(glow(s, lx, ly, 52))
    f.append(f'<path d="M{lx},{ly-14} l0,3" stroke="{IRON}" stroke-width=".8"/>')
    f.append(f'<path d="M{lx-7.5},{ly-4} L{lx},{ly-12} L{lx+7.5},{ly-4} Z" fill="{IRON}" stroke="{BLACK}" stroke-width=".3"/>')
    f.append(f'<rect x="{lx-7}" y="{ly-4}" width="14" height="17" fill="{HORN}" stroke="{shade(IRON,.6)}" stroke-width=".5"/>')
    f.append(flame(s, lx, ly + 10, 9))
    f.append(f'<rect x="{lx-8}" y="{ly+12}" width="16" height="3" fill="{IRON}"/>')
    # the other arm: thin as a moth's leg, trailing to the floor
    f.append(limb([(-14, -190), (-30, -150), (-44, -90), (-50, -30)], [4.5, 2.6, 1.8, 1.2], shade(WOOL, .8), outline=shade(WOOL, .4)))
    f.append(f'<path d="M-50,-30 l-2,14 M-50,-30 l2,15 M-50,-30 l-5,12" stroke="{shade(BONE,.75)}" stroke-width=".7" fill="none"/>')
    # the kettle hat, fallen at its feet
    f.append(kettle_hat(s, 30, -4, tilt=18, scale=.9))
    f.append('</g>')

    P = lambda x, y: (round(470 + K * x, 1), round(690 + K * y, 1))
    C = [callout(*P(63, -205), 840, 150, "Lantern lifted to 2.15 m", "on a three-jointed arm, swung ahead"),
         callout(*P(18, -232), 840, 215, "The coif, empty", "feathered feelers fan out 0.36 m"),
         callout(*P(12, -100), 840, 300, "Quilting split open", "pale wings press out of every slit"),
         callout(*P(-52, -22), 360, 640, "Moth-leg arm", "trails the floor, taps", side="left"),
         callout(*P(30, -8), 840, 380, "Kettle hat, fallen", "the man's hose and shoes lie empty")]
    rows = [("At ten paces", "A coat with nobody in it, walking, and the lantern held far too high and too far ahead of it."),
            ("The hint it gives", "It feels for movement with the feelers. Freeze and it passes you; move and it turns.")]
    head = frame_open(s, "FEAR PASS · LANTERN WARDEN · TRUE FORM · OPTION B", "The Hollow Coat", "H 2.30 m (feelers) · coat 2.0 m · ≤ 8k tris", TRAITS2, glow_cx="55%", glow_cy="30%")
    parts = [ladder_and_human(220, 2.5), view_label(470, "FRONT"), ''.join(f), ''.join(C), notes(840, 450, rows)]
    write_plate(s, head, parts, palette([("gambeson wool", WOOL), ("woad hose", WOAD), ("wing (vellum)", BONE), ("hollow (bone black)", BLACK), ("horn pane", HORN), ("flame", MADDER), ("black iron", IRON)]))


# ─────────────────────────────── C. THE CRAWLER ───────────────────────────────
def crawler():
    s = Sheet("true-form-c-the-crawler")
    ox = 520
    f = [f'<g transform="translate({ox},690) scale({K})">', f'<ellipse cx="0" cy="0" rx="120" ry="3" fill="#0B0A08" opacity=".7"/>']
    gw = s.form(WOOL, horizontal=False); gwo = s.form(WOAD); gs = s.form(SKIN)
    # SIDE VIEW, facing right. Hind legs: the man's own legs, bent backwards at the knee
    for dx, sh in ((-4, .62), (0, 1)):
        col = shade(WOAD, sh)
        f.append(limb([(-78 + dx, -88), (-96 + dx, -50), (-80 + dx, -6)], [8, 6, 4.5], col, outline=shade(WOAD, .4 * sh)))
        f.append(f'<path d="M{-86+dx},-8 L{-70+dx},-8 Q{-62+dx},-4 {-61+dx},0 L{-87+dx},0 Z" fill="{shade(LEATH,sh)}" stroke="{BLACK}" stroke-width=".3"/>')
    # forelegs: the arms, grown long, elbows high over the back like a spider's
    for dx, sh in ((-6, .62), (0, 1)):
        col = shade(WOOL, .82 * sh)
        f.append(limb([(40 + dx, -98), (62 + dx, -142), (92 + dx, -60), (100 + dx, -2)], [6, 4.4, 3.2, 2.2], col, outline=shade(WOOL, .4 * sh)))
        for t in (-4, 0, 4):
            f.append(f'<path d="M{100+dx},-2 l{t+3},2" stroke="{shade(SKIN,.55*sh+.2)}" stroke-width="1.3" stroke-linecap="round"/>')
    # the body, horizontal: the gambeson stretched over a long back, hem hanging under like a skirt
    body = "M-86,-92 C-80,-112 -60,-128 -30,-130 C0,-130 26,-120 42,-108 C50,-102 52,-94 48,-88 C40,-84 30,-84 20,-86 C4,-80 -16,-76 -40,-78 C-58,-80 -70,-78 -78,-80 C-84,-82 -88,-86 -86,-92 Z"
    s.clip(body, "crawlB")
    f.append(f'<path d="{body}" fill="{gw}" stroke="{shade(WOOL,.4)}" stroke-width=".45"/>')
    q = ''.join(f'<path d="M{x},-124 C{x+2},-110 {x+1},-90 {x+3},-70" fill="none" stroke="{shade(WOOL,.62)}" stroke-width=".35"/>' for x in range(-86, 52, 5))
    f.append(f'<g clip-path="url(#crawlB)">{q}<path d="M-90,-84 C-40,-80 10,-82 52,-90 L52,-70 L-90,-70 Z" fill="{BLACK}" opacity=".35"/>'
             + s.flecks(-86, -120, 136, 46, 70, "#3A2E1E", .2, .7, .35) + '</g>')
    # spine ridge pushing through the quilting
    def bez(p0, p1, p2, p3, u):
        return tuple((1-u)**3*a + 3*(1-u)**2*u*b + 3*(1-u)*u*u*c + u**3*d for a, b, c, d in zip(p0, p1, p2, p3))
    knuckles = [bez((-86, -92), (-80, -112), (-60, -128), (-30, -130), u) for u in (.35, .55, .75, .92)]
    knuckles += [bez((-30, -130), (0, -130), (26, -120), (42, -108), u) for u in (.1, .28, .46, .64, .8, .94)]
    for x, y in knuckles:
        y += 1.2
        f.append(f'<ellipse cx="{x:.1f}" cy="{y:.1f}" rx="2.6" ry="1.8" fill="{BONE}" stroke="{BLACK}" stroke-width=".3"/>')
    # ribs pressing through the cloth along the flank
    for i in range(7):
        x = -46 + i * 9
        f.append(f'<path d="M{x},-118 C{x+3},-108 {x+2},-96 {x-1},-86" fill="none" stroke="{shade(WOOL,.5)}" stroke-width="1.1" opacity=".7"/>')
    # the belly: the front lacing split into a long toothed mouth running under the body
    f.append(f'<path d="M-50,-77 C-30,-60 10,-62 30,-84 C12,-74 -26,-72 -50,-77 Z" fill="{FLESH}" stroke="{BLACK}" stroke-width=".5"/>')
    for i in range(12):
        x = -46 + i * 6.6
        top = -77 + (x + 50) / 80 * -6 + math.sin((x + 50) / 80 * math.pi) * 3
        bot = -77 + math.sin((x + 50) / 80 * math.pi) * 16 + (x + 50) / 80 * -7
        f.append(f'<path d="M{x:.1f},{top:.1f} l1.4,4.5 l1.4,-4.5 Z" fill="{BONE}"/>')
        f.append(f'<path d="M{x+1.6:.1f},{bot:.1f} l1.2,-3.8 l1.2,3.8 Z" fill="{BONE}"/>')
    # the neck, hanging down; the head upside down, chin up, hat brim on the floor side
    f.append(limb([(44, -100), (60, -88), (66, -58)], [6, 5, 4.5], gs, outline=shade(SKIN, .45)))
    hx, hy = 67, -46
    f.append(f'<g transform="translate({hx},{hy}) rotate(180)">'
             f'<path d="M-8,-15 Q-8.5,-3 0,0.5 Q8.5,-3 8,-15 Z" fill="{gs}" stroke="{shade(SKIN,.45)}" stroke-width=".35"/>'
             f'<path d="M-5.5,-10.5 L-2,-10.5 M2,-10.5 L5.5,-10.5" stroke="{BLACK}" stroke-width=".8"/>'
             f'<path d="M-3,-3 Q0,-1 3,-3" stroke="{FLESH}" stroke-width="1.6" fill="none"/></g>')
    f.append(kettle_hat(s, hx, hy + 2, tilt=180))
    # the lantern carried in its mouth, swinging at ankle height
    lx, ly = 70, -22
    f.append(glow(s, lx, ly, 46))
    f.append(f'<path d="M{hx+1},{hy+12} L{lx},{ly-11}" stroke="{IRON}" stroke-width=".8"/>')
    f.append(f'<path d="M{lx-7.5},{ly-4} L{lx},{ly-11} L{lx+7.5},{ly-4} Z" fill="{IRON}" stroke="{BLACK}" stroke-width=".3"/>')
    f.append(f'<rect x="{lx-7}" y="{ly-4}" width="14" height="17" fill="{HORN}" stroke="{shade(IRON,.6)}" stroke-width=".5"/>')
    f.append(flame(s, lx, ly + 10, 9))
    f.append(f'<rect x="{lx-8}" y="{ly+12}" width="16" height="3" fill="{IRON}"/>')
    f.append('</g>')

    P = lambda x, y: (round(ox + K * x, 1), round(690 + K * y, 1))
    C = [callout(*P(62, -142), 840, 170, "Arms grown into forelegs", "elbows over the back at 1.42 m"),
         callout(*P(-10, -66), 385, 620, "The lacing, now a mouth", "1.0 m long, under the belly", side="left"),
         callout(*P(-30, -129), 385, 205, "Spine through the quilting", "body 2.4 m long", side="left"),
         callout(*P(66, -40), 840, 470, "Head hangs upside down", "the hat brim nearly on the floor"),
         callout(*P(70, -12), 840, 530, "Lantern held in the teeth", "the light now at the ankle"),
         callout(*P(-96, -50), 330, 500, "Legs bent backwards", "the woad hose still on", side="left")]
    rows = [("At ten paces", "A light down on the floor where no light should be, and something long and low behind it."),
            ("The hint it gives", "Nose to the floor, it follows your footsteps. Shut a door behind you and the trail ends there.")]
    head = frame_open(s, "FEAR PASS · LANTERN WARDEN · TRUE FORM · OPTION C", "The Crawler", "H 1.42 m · L 2.4 m · ≤ 8k tris", TRAITS2, glow_cx="55%", glow_cy="80%")
    parts = [ladder_and_human(220, 2.5), view_label(ox, "SIDE"), ''.join(f), ''.join(C), notes(840, 300, rows)]
    write_plate(s, head, parts, palette([("gambeson wool", WOOL), ("woad hose", WOAD), ("skin", SKIN), ("mouth (madder)", FLESH), ("teeth, spine", BONE), ("flame", MADDER), ("black iron", IRON)]))


if __name__ == "__main__":
    the_wick()
    hollow_coat()
    crawler()
