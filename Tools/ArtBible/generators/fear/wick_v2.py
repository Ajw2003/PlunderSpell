"""The Wick, second round (#386): the owner chose it and asked for more detail and a more imposing,
daunting figure. Two sheets:

  wick-v2-1-true-form   front and side at true scale, with the arch and ceiling it must pass
  wick-v2-2-the-turn    the turn in four stages, and a close-up of the head

The idea that ties it together: the man becomes his lantern. His ribs open into the lantern's bars,
the candle burns in his chest, his head is a cage of bone with the kettle hat lifted on top as the
lantern's vented cap, and the face hangs open below it like scraped horn panes. The real lantern
lies dark at his feet: its flame left it and went into him.

Run from the repo root:  python3 Tools/ArtBible/generators/fear/wick_v2.py
"""
import math
from lib import *  # noqa: F403
from true_forms import WOOL, WOAD, LEATH, IRON, HORN, SKIN, FLESH, BONE, FLAME, glow, flame, kettle_hat

ASH = "#6B5238"     # the glaive's ash haft, as on the Lantern Warden's sheet
GLOW_HORN = "#E8C987"   # horn pane lit from inside: the light that leaks between the ribs


def lerp(a, b, t):
    return a + (b - a) * t


def ease(t, start, end):
    """0 before start, 1 after end, smooth between: lets each part of the turn begin at its own moment."""
    if t <= start:
        return 0.0
    if t >= end:
        return 1.0
    u = (t - start) / (end - start)
    return u * u * (3 - 2 * u)


class Body:
    """Key points of the front view at turn amount t (0 = the human warden, 1 = the Wick), in cm, y up negative."""
    def __init__(self, t):
        self.t = t
        g = ease(t, .25, 1)               # growth
        self.g = g
        self.crown = -(180 + 130 * g)
        self.sh_y = -(150 + 112 * g)      # top of the shoulders
        self.sw = 24 + 20 * g             # shoulder half width
        self.neck_y = self.sh_y + 3 + 9 * g   # the head sinks between risen shoulders
        self.hip_y = -(97 + 43 * g)
        self.waist = 21 + 5 * g
        self.hem_y = self.hip_y + 31 - 6 * g
        self.hem_w = 28 + 10 * g
        self.foot_x = 9 + 11 * g
        self.open = ease(t, .45, .95)     # the chest and coat opening
        self.head_open = ease(t, .2, .75)


def legs(s, b):
    out = []
    gwo = s.form(WOAD); gs = s.form(SKIN)
    for sg in (-1, 1):
        hx = sg * (10 + 3 * b.g); kx = sg * (12 + 9 * b.g); fx = sg * b.foot_x
        ky = b.hip_y * .48
        out.append(limb([(hx, b.hip_y + 4), (kx, ky), (fx, -9)], [8.5 - b.g, 6.5 - 1.6 * b.g, 4.6 - 1.2 * b.g], gwo, outline=shade(WOAD, .4)))
        out.append(f'<path d="M{fx-5},{-15} C{fx-6},{-30} {kx-6},{ky+10} {kx-4},{ky}" fill="none" stroke="{shade(WOAD,.5)}" stroke-width="1.6" opacity=".55"/>')
        out.append(f'<path d="M{(hx+kx)/2-7:.1f},{ky*1.35:.1f} L{(hx+kx)/2+7:.1f},{ky*1.35:.1f}" stroke="{LEATH}" stroke-width="2.6"/>' if b.g < .5 else '')
        if b.open > .2:   # the hose splits at the knee and the leg shows through, too thin
            out.append(f'<path d="M{kx-5},{ky-4} l2,5 l2,-4 l2,6 l2,-5 L{kx+4},{ky+9} L{kx-4},{ky+9} Z" fill="{gs}" stroke="{shade(SKIN,.45)}" stroke-width=".3" opacity="{b.open:.2f}"/>')
            out.append(f'<path d="M{kx-1},{ky+2} l.6,6" stroke="{BONE}" stroke-width="1.1" opacity="{b.open:.2f}"/>')
        # turnshoes, then toes bursting through them
        out.append(f'<path d="M{fx-6},-11 L{fx+4},-11 Q{fx+6},-5 {fx+6},0 L{fx-8},0 Q{fx-8.5},-6 {fx-6},-11 Z" fill="{s.form(LEATH)}" stroke="{BLACK}" stroke-width=".35"/>')
        if b.open > .3:
            for k in (-1, 0, 1):
                out.append(f'<path d="M{fx+k*3},-2 Q{fx+k*5+sg*3},1 {fx+k*7+sg*8},0.5" fill="none" stroke="{shade(SKIN,.55)}" stroke-width="1.3" stroke-linecap="round" opacity="{b.open:.2f}"/>')
    return ''.join(out)


def coat(s, b, cid):
    out = []
    gw = s.form(WOOL)
    sw, sy, hy, hw, wy, ww = b.sw, b.sh_y, b.hem_y, b.hem_w, b.hip_y, b.waist
    hump = 9 * b.g
    d = (f"M{-sw},{sy+hump*.2:.1f} Q{-sw*.55:.1f},{sy-hump*.6:.1f} {-8-4*b.g:.1f},{b.neck_y:.1f} L{8+4*b.g:.1f},{b.neck_y:.1f} "
         f"Q{sw*.55:.1f},{sy-hump*.6:.1f} {sw},{sy+hump*.2:.1f} C{sw-1},{sy+30} {ww+2},{wy-30} {ww},{wy} "
         f"C{ww+3},{wy+10} {hw-2},{hy-8} {hw},{hy} Q0,{hy+5} {-hw},{hy} C{-hw+2},{hy-8} {-ww-3},{wy+10} {-ww},{wy} "
         f"C{-ww-2},{wy-30} {-sw+1},{sy+30} {-sw},{sy+hump*.2:.1f} Z")
    s.clip(d, cid)
    out.append(f'<path d="{d}" fill="{gw}" stroke="{shade(WOOL,.4)}" stroke-width=".45"/>')
    # quilting channels, pulled apart as the frame inside grows; dark tears open between them
    q = []
    step = 4 + 3 * b.g
    n = int((hw * 2) / step) + 2
    for i in range(n):
        x = -hw + i * step
        xt = x * (sw / hw)
        q.append(f'<path d="M{xt:.1f},{sy-hump} C{xt*.95:.1f},{(sy+wy)/2:.0f} {x*.85:.1f},{wy} {x:.1f},{hy+4}" fill="none" stroke="{shade(WOOL,.6)}" stroke-width=".4"/>')
    tears = []
    if b.open > .3:
        for i, (x, y0, ln) in enumerate([(-30, -.75, 22), (-22, -.45, 30), (26, -.6, 26), (34, -.3, 20), (-36, -.2, 16), (18, -.2, 18)]):
            x = x * sw / 44; y = sy + (wy - sy) * (1 + y0) * .9
            tears.append(f'<path d="M{x:.1f},{y:.1f} Q{x+2:.1f},{y+ln/2:.1f} {x+.5:.1f},{y+ln:.1f} Q{x-1.5:.1f},{y+ln/2:.1f} {x:.1f},{y:.1f} Z" fill="{FLESH}" opacity="{b.open:.2f}"/>')
            tears.append(f'<path d="M{x-1:.1f},{y+ln*.3:.1f} l2.4,0 M{x-1:.1f},{y+ln*.55:.1f} l2.6,0" stroke="{LEATH}" stroke-width=".5" opacity="{b.open:.2f}"/>')
    out.append(f'<g clip-path="url(#{cid})">' + ''.join(q) + ''.join(tears)
               + f'<path d="M{sw*.35:.1f},{sy-hump} C{sw*.8:.1f},{sy+40} {hw*.8:.1f},{wy} {hw+2},{hy+4} L{hw+6},{hy+4} L{sw+6},{sy-hump} Z" fill="{BLACK}" opacity=".32"/>'
               + f'<path d="M{-sw+2},{sy+8} C{-sw+1},{sy+40} {-ww},{wy-20} {-hw+3},{hy-6}" fill="none" stroke="{shade(WOOL,1.3)}" stroke-width="1.6" opacity=".45"/>'
               + s.flecks(-hw, sy, hw * 2, hy - sy, 90, "#3A2E1E", .2, .8, .35)
               + s.flecks(-hw, hy - 18, hw * 2, 18, 50, "#2A2418", .2, .6, .5) + '</g>')
    # the hem, torn into strips that hang lower as the man grows out of the coat
    if b.open > .2:
        for i in range(9):
            x = -hw + 4 + i * (hw * 2 - 8) / 8
            ln = (6 + (i * 7) % 11) * b.open
            out.append(f'<path d="M{x-3:.1f},{hy-1} L{x+3:.1f},{hy-1} L{x+1.5:.1f},{hy+ln:.1f} L{x-1.5:.1f},{hy+ln-2:.1f} Z" fill="{shade(WOOL,.75)}" stroke="{shade(WOOL,.4)}" stroke-width=".3"/>')
    return ''.join(out)


def chest(s, b):
    """The lacing bursts and the ribs open into the lantern's bars, with the candle burning inside."""
    o = b.open
    if o <= 0:
        out = []
        for i in range(6):   # the warden's lacing, still done up
            y = b.neck_y + 6 + i * 6
            out.append(f'<path d="M-1.8,{y} L1.8,{y+3} M1.8,{y} L-1.8,{y+3}" stroke="{LEATH}" stroke-width=".5"/>')
        return ''.join(out)
    top = b.neck_y + 6; bot = b.hip_y - 2
    w = 3 + 13 * o
    mid = (top + bot) / 2
    cav = f"M0,{top} C{-w*1.1:.1f},{top+12} {-w:.1f},{bot-14} 0,{bot} C{w:.1f},{bot-14} {w*1.1:.1f},{top+12} 0,{top} Z"
    inner = s.rg(GLOW_HORN, MADDER, 1, .9, .5, .62, .65)
    out = [f'<path d="{cav}" fill="{inner}" stroke="{BLACK}" stroke-width=".6"/>']
    # light leaking out between the ribs, in rays across the coat
    for ang in (-62, -38, -14, 14, 38, 62):
        a = math.radians(ang)
        out.append(f'<path d="M0,{mid} L{math.sin(a)*70*o:.1f},{mid-math.cos(a)*18:.1f} L{math.sin(a)*70*o+6:.1f},{mid-math.cos(a)*18+10:.1f} Z" fill="{GLOW_HORN}" opacity="{.10*o:.2f}"/>')
    # the candle: a tallow pillar grown onto the spine, burning
    cy = bot - 6
    out.append(f'<rect x="-3" y="{cy-20}" width="6" height="20" fill="{shade(BONE,.92)}" stroke="{shade(BONE,.55)}" stroke-width=".4"/>')
    out.append(f'<path d="M-3,{cy-20} q1.5,4 0,7 M3,{cy-20} q-1.2,5 .4,9" stroke="{shade(BONE,1.1)}" stroke-width="1.2" fill="none"/>')
    out.append(flame(s, 0, cy - 20, 14 * o + 2))
    # ribs: three bone bars each side, bowed outward like the lantern's iron bars
    for sg in (-1, 1):
        for k, f in enumerate((.45, .8, 1.15)):
            x = sg * w * f
            out.append(f'<path d="M{sg*1.5},{top+2} C{x:.1f},{top+8} {x*1.05:.1f},{bot-16} {sg*1.5},{bot-3}" fill="none" stroke="{BONE}" stroke-width="{2.2-k*.4:.1f}" stroke-linecap="round"/>')
    out.append(f'<path d="M0,{top} L0,{bot}" stroke="{shade(BONE,.75)}" stroke-width="2"/>')
    # torn lacing ends on both lips of the coat
    for i in range(5):
        y = top + 6 + i * (bot - top - 12) / 4
        out.append(f'<path d="M{-w*1.08:.1f},{y:.1f} l-2.5,5 M{w*1.08:.1f},{y:.1f} l2.5,5" stroke="{LEATH}" stroke-width=".6"/>')
    return ''.join(out)


def belt(s, b):
    y = b.hip_y + 2
    gl = s.form(LEATH)
    if b.open < .4:
        return (f'<path d="M{-b.waist-.5},{y-3} Q0,{y+1} {b.waist+.5},{y-3} L{b.waist+1},{y+1.5} Q0,{y+5.5} {-b.waist-1},{y+1.5} Z" fill="{gl}" stroke="{BLACK}" stroke-width=".3"/>'
                f'<rect x="-4" y="{y-3}" width="5" height="5.5" fill="none" stroke="{IRON}" stroke-width=".8"/>')
    # burst: two loose ends, the buckle still on one
    return (f'<path d="M{-b.waist-1},{y-2} Q{-b.waist*.5:.1f},{y+3} {-6},{y+6} M{b.waist+1},{y-2} Q{b.waist*.6:.1f},{y+2} {9},{y+9}" fill="none" stroke="{LEATH}" stroke-width="4"/>'
            f'<rect x="7" y="{y+6}" width="5" height="5.5" fill="none" stroke="{IRON}" stroke-width=".8"/>')


def arm(s, b, sg):
    """sg = -1 is the glaive arm (viewer's left), +1 the grasping arm."""
    out = []
    gw = s.form(WOOL); gs = s.form(SKIN)
    g = b.g
    sx, sy = sg * (b.sw - 4), b.sh_y + 7
    ex, ey = sg * (b.sw + 6 + 18 * g), b.sh_y + 40 + 38 * g
    wx, wy = sg * (b.sw + 3 + 4 * g), b.sh_y + 70 + 84 * g
    if sg < 0 and g < .5:   # still the warden's grip on the upright glaive
        ex, ey = sg * (b.sw + 4), b.sh_y + 31
        wx, wy = sg * (b.sw + 9), b.sh_y + 24
    out.append(limb([(sx, sy), (ex, ey), (wx, wy)], [6.5 + 3.5 * g, 5.4 + 2.2 * g, 4.4 + g], gw, outline=shade(WOOL, .4)))
    out.append(rings((sx, sy), (ex, ey), 6.5 + 2 * g, 5.4 + 1.5 * g, 7, shade(WOOL, .62)))
    out.append(rings((ex, ey), (wx, wy), 5.4 + 1.5 * g, 4.4 + g, 6, shade(WOOL, .62)))
    if sg < 0:
        if g < .5:   # the glaive, upright in his fist
            gx = wx - 1
            out.append(f'<ellipse cx="{gx}" cy="{wy+1}" rx="4" ry="4.5" fill="{gs}" stroke="{shade(SKIN,.5)}" stroke-width=".3"/>')
            out.append(glaive_upright(s, gx))
        else:        # the haft has gone into the forearm; the blade comes out of the wrist, point to the floor
            bl = 40 + 30 * g
            out.append(f'<path d="M{ex},{ey} L{wx},{wy}" stroke="{ASH}" stroke-width="2.4" opacity=".55"/>')
            out.append(f'<path d="M{wx-3},{wy-2} L{wx+3},{wy-2} L{wx+2.6},{wy+6} L{wx-2.6},{wy+6} Z" fill="{IRON}" stroke="{BLACK}" stroke-width=".3"/>')
            out.append(f'<path d="M{wx-2.4},{wy+6} L{wx+3.2},{wy+6} C{wx+7},{wy+bl*.4:.0f} {wx+6},{wy+bl*.8:.0f} {wx+.5},{wy+bl} C{wx-3},{wy+bl*.7:.0f} {wx-3.5},{wy+bl*.35:.0f} {wx-2.4},{wy+6} Z" '
                       f'fill="{s.form(IRON)}" stroke="{BLACK}" stroke-width=".4"/>')
            out.append(f'<path d="M{wx+3.2},{wy+7} C{wx+6.5},{wy+bl*.4:.0f} {wx+5.5},{wy+bl*.8:.0f} {wx+.5},{wy+bl}" fill="none" stroke="{VELLUM}" stroke-width=".6" opacity=".75"/>')
            out.append(f'<path d="M{wx-2.4},{wy+12} l-5,-4 l1.5,7 Z" fill="{IRON}" stroke="{BLACK}" stroke-width=".3"/>')   # the back fluke
            for k in range(3):   # skin grown over the socket
                out.append(f'<path d="M{wx-4},{wy-4+k*2.6} q4,1.6 8,0" fill="none" stroke="{shade(SKIN,.6)}" stroke-width=".7"/>')
    else:
        if g < .5 and b.t < .2:   # the lantern still in hand
            out.append(f'<ellipse cx="{wx}" cy="{wy+2}" rx="4.2" ry="4" fill="{gs}" stroke="{shade(SKIN,.5)}" stroke-width=".3"/>')
            out.append(lantern_box(s, wx, wy + 11, lit=True))
        else:   # the hand opens: fingers too long, claws of bone
            hx, hy = wx + sg * 1, wy + 2
            out.append(limb([(wx, wy), (hx, hy + 9)], [4.4 + g, 4.6 + g], gs, outline=shade(SKIN, .45)))
            ln = 10 + 30 * g
            for j, dx in enumerate((-4.5, -1.5, 1.5, 4.5)):
                tx = hx + dx * (1.3 + .6 * g) + sg * 2
                ty = hy + 9 + ln - abs(dx) * 1.4
                out.append(f'<path d="M{hx+dx:.1f},{hy+9} C{hx+dx*1.2:.1f},{hy+9+ln*.4:.1f} {tx-sg*2:.1f},{ty-ln*.3:.1f} {tx:.1f},{ty:.1f}" fill="none" stroke="{shade(SKIN,.82)}" stroke-width="{1.4+.6*g:.1f}" stroke-linecap="round"/>')
                out.append(f'<path d="M{tx:.1f},{ty:.1f} l{sg*.8:.1f},3.2" stroke="{BONE}" stroke-width="1.1" stroke-linecap="round"/>')
            out.append(f'<path d="M{hx-sg*5},{hy+12} C{hx-sg*9},{hy+16} {hx-sg*10},{hy+22} {hx-sg*9},{hy+27}" fill="none" stroke="{shade(SKIN,.82)}" stroke-width="1.6" stroke-linecap="round"/>')
    return ''.join(out)


def glaive_upright(s, x, top=-205):
    gi = s.form(IRON); ga = s.form(ASH)
    return (f'<rect x="{x-1.5}" y="-160" width="3" height="160" fill="{ga}" stroke="{shade(ASH,.5)}" stroke-width=".3"/>'
            f'<rect x="{x-1.9}" y="-170" width="3.8" height="14" fill="{gi}" stroke="{BLACK}" stroke-width=".3"/>'
            f'<path d="M{x-1.6},-170 L{x+2.2},-170 C{x+5.5},-180 {x+5.5},-194 {x-.5},{top} C{x-2.4},-196 {x-3.2},-186 {x-3},-178 L{x-6.5},-182 L{x-4},-175 L{x-1.6},-170 Z" fill="{gi}" stroke="{BLACK}" stroke-width=".35"/>')


def lantern_box(s, cx, top, lit=True, tilt=0):
    out = [f'<g transform="rotate({tilt},{cx},{top+12})">']
    if lit:
        out.append(glow(s, cx, top + 13, 40))
    out.append(f'<path d="M{cx-7.5},{top+5} L{cx},{top-3.5} L{cx+7.5},{top+5} Z" fill="{IRON}" stroke="{BLACK}" stroke-width=".3"/>')
    out.append(f'<rect x="{cx-7}" y="{top+5}" width="14" height="17" fill="{HORN if lit else shade(HORN,.45)}" stroke="{shade(IRON,.6)}" stroke-width=".5"/>')
    if lit:
        out.append(flame(s, cx, top + 19, 10))
    else:
        out.append(f'<rect x="{cx-1}" y="{top+16}" width="2" height="5" fill="{shade(BONE,.6)}"/><path d="M{cx},{top+15} q-1,-3 .5,-6 q1,-3 -.5,-6" fill="none" stroke="#635C4C" stroke-width=".5"/>')
    for k in (-7, 0, 7):
        out.append(f'<rect x="{cx+k-.7}" y="{top+5}" width="1.4" height="17" fill="{IRON}"/>')
    out.append(f'<rect x="{cx-8}" y="{top+21.5}" width="16" height="3" fill="{IRON}" stroke="{BLACK}" stroke-width=".3"/></g>')
    return ''.join(out)


def head(s, b, scale=1.0):
    """Three moments: the warden's own head; the head cracking with light; the lantern-head."""
    t = b.t; y0 = b.neck_y
    gs = s.form(SKIN); gwd = s.form(shade(WOOL, .82))
    out = []
    if t < .2:
        out.append(f'<path d="M-11,{y0-17} L11,{y0-17} L11.5,{y0-1} Q0,{y0+5} -11.5,{y0-1} Z" fill="{gwd}" stroke="{shade(WOOL,.4)}" stroke-width=".35"/>')
        out.append(f'<path d="M-8,{y0-16} Q-8.5,{y0-4} 0,{y0-.5} Q8.5,{y0-4} 8,{y0-16} Z" fill="{gs}" stroke="{shade(SKIN,.45)}" stroke-width=".35"/>')
        out.append(f'<path d="M-5.5,{y0-11.5} L-2,{y0-11.5} M2,{y0-11.5} L5.5,{y0-11.5}" stroke="{BLACK}" stroke-width=".8"/>')
        out.append(kettle_hat(s, 0, y0 - 17))
        return ''.join(out)
    if t < .6:
        # the face splits down the middle and light comes out of the crack; the hat lifts off the skull
        lift = 10 * b.head_open
        out.append(glow(s, 0, y0 - 10, 40 * b.head_open + 8))
        out.append(f'<path d="M-11,{y0-17} L11,{y0-17} L11.5,{y0-1} Q0,{y0+5} -11.5,{y0-1} Z" fill="{gwd}" stroke="{shade(WOOL,.4)}" stroke-width=".35"/>')
        out.append(f'<path d="M-8,{y0-16} Q-8.5,{y0-4} 0,{y0-.5} Q8.5,{y0-4} 8,{y0-16} Z" fill="{gs}" stroke="{shade(SKIN,.45)}" stroke-width=".35"/>')
        w = 1 + 3 * b.head_open
        out.append(f'<path d="M0,{y0-17-lift} L{w},{y0-12} L{-w*.6:.1f},{y0-8} L{w*.8:.1f},{y0-4} L0,{y0}" fill="none" stroke="{GLOW_HORN}" stroke-width="{w:.1f}" stroke-linejoin="bevel"/>')
        out.append(f'<path d="M-6,{y0-11.5} L-2.5,{y0-12.5} M2.5,{y0-12.5} L6,{y0-11.5}" stroke="{BLACK}" stroke-width=".9"/>')
        out.append(f'<path d="M-3,{y0-4} Q0,{y0-1} 3,{y0-4}" fill="{FLESH}" stroke="{BLACK}" stroke-width=".4"/>')
        for k in (-1, 1):   # bone bars starting to push up through the scalp
            out.append(f'<path d="M{k*5},{y0-16} L{k*5.5},{y0-17-lift}" stroke="{BONE}" stroke-width="1.4"/>')
        out.append(kettle_hat(s, 0, y0 - 17 - lift, tilt=-4 * b.head_open))
        return ''.join(out)
    # the lantern-head: a cage of four bone bars rising out of the neck, the flame inside, the hat as the cap
    o = b.head_open
    H = 30 * o + 8      # cage height
    cw = 9 + 5 * o      # cage half width at its widest
    top = y0 - H
    out.append(glow(s, 0, y0 - H * .45, 95 * o + 20))
    out.append(f'<ellipse cx="0" cy="{y0-H*.45:.1f}" rx="{cw*1.6:.1f}" ry="{H*.6:.1f}" fill="{GLOW_HORN}" opacity=".18"/>')
    # the two back bars, dimmer
    for sg in (-1, 1):
        out.append(f'<path d="M{sg*3},{y0} C{sg*cw*.7:.1f},{y0-H*.3:.1f} {sg*cw*.75:.1f},{top+H*.25:.1f} {sg*2.5},{top+1}" fill="none" stroke="{shade(BONE,.55)}" stroke-width="1.5"/>')
    # the flame, tall and still, the candle wick a blackened tongue
    out.append(f'<path d="M-1.6,{y0-1} L1.6,{y0-1} L.8,{y0-6} L-.8,{y0-6} Z" fill="{FLESH}"/>')
    out.append(flame(s, 0, y0 - 5, H * .78))
    # the front bars: spine and jaw-hinge bones, bowed like the lantern's iron
    for sg in (-1, 1):
        out.append(f'<path d="M{sg*5},{y0+1} C{sg*cw*1.15:.1f},{y0-H*.3:.1f} {sg*cw*1.1:.1f},{top+H*.3:.1f} {sg*4},{top+1}" fill="none" stroke="{BONE}" stroke-width="2.4" stroke-linecap="round"/>')
        for k in range(3):   # vertebra knuckles along each bar
            yy = y0 - H * (.2 + .25 * k)
            xx = sg * cw * (1.05 - .05 * k)
            out.append(f'<ellipse cx="{xx:.1f}" cy="{yy:.1f}" rx="2" ry="1.4" fill="{BONE}" stroke="{BLACK}" stroke-width=".3"/>')
    # a ring of iron where the bars meet: the lantern's own carrying ring, gone into the bone
    out.append(f'<ellipse cx="0" cy="{top+1}" rx="5" ry="1.6" fill="none" stroke="{IRON}" stroke-width="1.2"/>')
    # the kettle hat as the vented cap, with vent holes punched through
    out.append(kettle_hat(s, 0, top - 1, tilt=-5, scale=1.15 + .1 * o))
    for k in range(-2, 3):
        out.append(f'<circle cx="{k*3.4:.1f}" cy="{top-7+abs(k)*1.2:.1f}" r=".9" fill="{BLACK}"/>')
    # smoke from the vents
    out.append(f'<path d="M-3,{top-15} q-3,-6 1,-11 q4,-5 0,-10 M4,{top-14} q3,-7 -1,-12" fill="none" stroke="#635C4C" stroke-width=".7" opacity=".7"/>')
    # the face, peeled down in four flaps that hang over the shoulders like scraped horn panes
    for sg in (-1, 1):
        for k, (dx, ln) in enumerate(((7, 20), (15, 16))):
            x0 = sg * 5; tx = sg * (dx + 4); ty = y0 + ln * o
            out.append(f'<path d="M{x0},{y0} C{sg*(dx-2)},{y0-3} {tx+sg*3},{ty-ln*.5:.1f} {tx},{ty:.1f} C{tx-sg*4},{ty-3:.1f} {sg*(dx-4)},{y0+4} {x0-sg*1.5},{y0+1.5} Z" '
                       f'fill="{gs}" stroke="{shade(SKIN,.4)}" stroke-width=".45" opacity=".97"/>')
            out.append(f'<path d="M{x0-sg*.5},{y0+.5} C{sg*(dx-3)},{y0+2} {tx+sg*1},{ty-ln*.45:.1f} {tx-sg*1},{ty-1.5:.1f}" fill="none" stroke="{FLESH}" stroke-width="1.8"/>')
    # the eye still open on the outer left flap, and half the jaw on the right with its teeth
    out.append(f'<path d="M-20,{y0+7} l3.6,-1.4 l1.4,1.8 l-3.6,1.3 Z" fill="{BONE}" stroke="{BLACK}" stroke-width=".3"/><circle cx="-17.7" cy="{y0+6.8}" r=".85" fill="{BLACK}"/>')
    for k in range(5):
        out.append(f'<path d="M{9+k*1.8:.1f},{y0+3+k*1.6:.1f} l1.3,2.8 l.9,-3.1 Z" fill="{BONE}"/>')
    return ''.join(out)


def front(s, t, cx, k, cid):
    b = Body(t)
    out = [f'<g transform="translate({cx},690) scale({k})">', f'<ellipse cx="0" cy="0" rx="{38+20*b.g:.0f}" ry="2.8" fill="#0B0A08" opacity=".75"/>']
    if t >= .2:   # the lantern on the floor, dark: its flame has gone into him
        out.append(lantern_box(s, 30 + 14 * b.g, -15, lit=False, tilt=78))
    out.append(legs(s, b))
    out.append(arm(s, b, -1))
    out.append(coat(s, b, cid))
    out.append(chest(s, b))
    out.append(belt(s, b))
    out.append(arm(s, b, 1))
    out.append(head(s, b))
    out.append('</g>')
    return ''.join(out), b


def side_view(s, cx, k):
    """The Wick in profile, facing right: hunched forward 24 degrees from the hip, head low and forward."""
    b = Body(1)
    gw = s.form(WOOL); gwo = s.form(WOAD)
    out = [f'<g transform="translate({cx},690) scale({k})">', f'<ellipse cx="6" cy="0" rx="46" ry="2.8" fill="#0B0A08" opacity=".75"/>']
    hy = b.hip_y
    # legs: far then near, knee forward, long shin, heel lifted
    for dx, sh in ((-6, .6), (2, 1)):
        col = shade(WOAD, sh)
        out.append(limb([(dx, hy + 4), (dx + 14, hy * .5), (dx + 2, -10)], [8.5 * sh + 1, 5.6, 3.8], col, outline=shade(WOAD, .4 * sh)))
        out.append(f'<path d="M{dx-6},-11 L{dx+6},-11 Q{dx+16},-6 {dx+18},0 L{dx-6},0 Z" fill="{shade(LEATH,sh)}" stroke="{BLACK}" stroke-width=".3"/>')
        out.append(f'<path d="M{dx+16},-1 l7,1 M{dx+15},-3 l8,-.5" stroke="{shade(SKIN,.55*sh+.15)}" stroke-width="1.2" stroke-linecap="round"/>')
    # the torso, drawn upright in local space and rotated about the hip
    L = hy - b.sh_y   # torso length (negative up)
    tor = (f"M-14,{hy+30} C-18,{hy+10} -20,{hy-20} -22,{hy-50} C-26,{hy-80} -26,{hy-105} -18,{b.sh_y-6} "
           f"C-8,{b.sh_y-14} 10,{b.sh_y-10} 18,{b.sh_y+4} C24,{b.sh_y+30} 22,{hy-40} 18,{hy-10} C17,{hy+8} 18,{hy+24} 20,{hy+30} Z")
    s.clip(tor, "wickSideT")
    rot = f'rotate(24,0,{hy})'
    q = ''.join(f'<path d="M{x},{b.sh_y-16} C{x+1},{hy-60} {x-1},{hy-10} {x*1.1:.1f},{hy+32}" fill="none" stroke="{shade(WOOL,.6)}" stroke-width=".45"/>' for x in range(-24, 24, 6))
    out.append(f'<g transform="{rot}">')
    out.append(f'<path d="{tor}" fill="{gw}" stroke="{shade(WOOL,.4)}" stroke-width=".45"/>')
    out.append(f'<g clip-path="url(#wickSideT)">{q}<path d="M-26,{b.sh_y} C-20,{hy-60} -16,{hy} -18,{hy+32} L-30,{hy+32} L-30,{b.sh_y} Z" fill="{BLACK}" opacity=".35"/>'
               + f'<path d="M10,{b.sh_y+20} C14,{hy-60} 12,{hy-20} 10,{hy+4}" fill="none" stroke="{GLOW_HORN}" stroke-width="3" opacity=".35"/>'
               + s.flecks(-26, b.sh_y, 50, hy - b.sh_y + 30, 70, "#3A2E1E", .2, .8, .35) + '</g>')
    # the spine's ridge through the back of the coat
    for i in range(8):
        yy = b.sh_y + 6 + i * (hy - b.sh_y - 10) / 7
        out.append(f'<ellipse cx="-22.5" cy="{yy:.1f}" rx="1.8" ry="2.6" fill="{BONE}" stroke="{BLACK}" stroke-width=".3"/>')
    # the open chest seen edge-on: ribs fanning forward, light inside
    out.append(f'<path d="M16,{b.sh_y+14} C26,{b.sh_y+30} 26,{hy-30} 17,{hy-6} Z" fill="{s.rg(GLOW_HORN, MADDER, 1, .9, .3, .5, .7)}" stroke="{BLACK}" stroke-width=".5"/>')
    for i in range(5):
        yy = b.sh_y + 22 + i * 14
        out.append(f'<path d="M17,{yy} C26,{yy-2} 30,{yy+6} 27,{yy+11}" fill="none" stroke="{BONE}" stroke-width="1.8" stroke-linecap="round"/>')
    # the hem strips
    for i in range(5):
        x = -14 + i * 8
        out.append(f'<path d="M{x},{hy+29} l4,0 l-1,{8+i%3*4} l-2,-2 Z" fill="{shade(WOOL,.75)}"/>')
    # the head, low and forward between the shoulders
    nx, ny = 10, b.sh_y + 2
    out.append(f'<g transform="translate({nx},{ny}) rotate(-24)">')
    hb = Body(1); hb.neck_y = 0
    out.append(head(s, hb))
    out.append('</g>')
    out.append('</g>')
    # arms hang from the rotated shoulder, the glaive arm nearest
    ang = math.radians(24)
    sx = 0 + (0 * math.cos(ang) - (b.sh_y + 8 - hy) * math.sin(ang))
    sy = hy + (0 * math.sin(ang) + (b.sh_y + 8 - hy) * math.cos(ang))
    out.append(limb([(sx - 6, sy), (sx + 4, sy + 66), (sx + 22, sy + 128)], [7, 6, 5], shade(WOOL, .6), outline=shade(WOOL, .3)))
    out.append(limb([(sx + 22, sy + 128), (sx + 26, sy + 162)], [4.5, 4], shade(SKIN, .6), outline=shade(SKIN, .3)))
    out.append(limb([(sx, sy), (sx + 12, sy + 70), (sx + 38, sy + 128)], [10, 7.6, 5.4], gw, outline=shade(WOOL, .4)))
    out.append(rings((sx, sy), (sx + 12, sy + 70), 10, 7.6, 8, shade(WOOL, .62)))
    out.append(rings((sx + 12, sy + 70), (sx + 38, sy + 128), 7.6, 5.4, 7, shade(WOOL, .62)))
    wx, wy = sx + 38, sy + 128
    bl = 70
    a2 = math.radians(28)   # the blade carried forward and down, along the forearm's line
    tx, ty = wx + math.sin(a2) * bl, wy + math.cos(a2) * bl
    out.append(f'<path d="M{wx-3},{wy} L{wx+4},{wy-1} C{wx+20},{wy+bl*.35:.0f} {tx+6:.1f},{ty-bl*.3:.1f} {tx:.1f},{ty:.1f} C{tx-8:.1f},{ty-bl*.35:.1f} {wx+2},{wy+bl*.4:.0f} {wx-3},{wy} Z" fill="{s.form(IRON)}" stroke="{BLACK}" stroke-width=".4"/>')
    out.append(f'<path d="M{wx+4},{wy} C{wx+20},{wy+bl*.35:.0f} {tx+6:.1f},{ty-bl*.3:.1f} {tx:.1f},{ty:.1f}" fill="none" stroke="{VELLUM}" stroke-width=".6" opacity=".7"/>')
    out.append('</g>')
    return ''.join(out)


# ─────────────────────────────── sheet 1: the true form at scale ───────────────────────────────
def sheet_true_form():
    s = Sheet("wick-v2-1-true-form")
    K = 1.8
    fr, b = front(s, 1, 400, K, "wickF")
    sd = side_view(s, 690, K)
    G = 690
    marks = []
    y = G - 2.59 * 180
    marks.append(f'<path d="M100,{y:.0f} L820,{y:.0f}" stroke="{MADDER}" stroke-width=".8" stroke-dasharray="6 5" opacity=".55"/>'
                 + text(110, y - 6, "arch 2.59 m: it stoops through", 10, "#9A9078"))
    P = lambda x, y: (round(400 + K * x, 1), round(690 + K * y, 1))
    S = lambda x, y: (round(690 + K * x, 1), round(690 + K * y, 1))
    C = [callout(*P(-6, b.crown + 6), 960, 175, "Kettle hat as the lantern's cap", "vents punched through; smoke"),
         callout(*P(9, b.neck_y - 20), 960, 225, "Head: a cage of four bone bars", "the flame burns inside, 0.30 m"),
         callout(*P(-17, b.neck_y + 9), 960, 275, "Face peeled into four panes", "hangs open; one eye still looks"),
         callout(*P(14, (b.neck_y + b.hip_y) / 2), 960, 330, "Ribs opened into lantern bars", "the candle burns in the chest"),
         callout(*P(-60, -80), 960, 385, "The glaive grown into the arm", "blade 0.70 m from the wrist"),
         callout(*P(66, -60), 960, 440, "Fingers 0.40 m, bone-tipped", "the hand that catches"),
         callout(*P(44, -6), 960, 495, "His lantern, dark on the floor", "its flame left it for him"),
         callout(*S(22, -200), 960, 550, "Hunched 24° from the hip", "the light carried low and ahead")]
    rows = [("Size", "3.10 m standing, shoulders 0.88 m across. Stoops to 2.50 m under the 2.59 m arch; the light goes first.")]
    head_ = frame_open(s, "FEAR PASS · LANTERN WARDEN · TRUE FORM · ROUND 2", "The Wick", "H 3.10 m · stoops to 2.50 m · ≤ 10k tris", "1 m = 180 px · ground y 690", glow_cx="35%", glow_cy="30%")
    parts = [metre_ladder(px_per_m=180, top_m=3.0), human(px_per_m=180), ''.join(marks), view_label(400, "FRONT"), view_label(690, "SIDE"), fr, sd, ''.join(C), notes(960, 610, rows, width=210)]
    write_plate(s, head_, parts, palette([("gambeson wool", WOOL), ("woad hose", WOAD), ("skin", SKIN), ("blood (madder)", FLESH), ("bone (vellum)", BONE), ("lit horn", GLOW_HORN), ("flame", MADDER), ("black iron", IRON)]))


# ─────────────────────────────── sheet 2: the turn, and the head up close ───────────────────────────────
def sheet_turn():
    s = Sheet("wick-v2-2-the-turn")
    K = 1.1
    stages = [(0.0, 165, "1 · SEEN", "0.0 s", "lantern drops"),
              (0.42, 315, "2 · CAP LIFTS", "0.4 s", "face splits"),
              (0.72, 480, "3 · COAT BURSTS", "0.9 s", "ribs open, grows"),
              (1.0, 665, "4 · TURNED", "1.6 s", "hunts, for good")]
    figs, labels = [], []
    for i, (t, x, lab, when, what) in enumerate(stages):
        f, _ = front(s, t, x, K, f"turnC{i}")
        figs.append(f)
        labels.append(text(x, 712, lab, 10.5, "#9A9078", "middle", extra=' letter-spacing="1.5"')
                      + text(x, 727, f"{when} · {what}", 9.5, "#635C4C", "middle"))
    # arrow of time under the stages
    figs.append(f'<path d="M130,742 L740,742" stroke="#635C4C" stroke-width="1"/><path d="M740,742 l-6,-3 l0,6 Z" fill="#635C4C"/>')
    # the close-up of the head, in an inset
    ix, iy, iw, ih = 770, 120, 390, 470
    inset = [f'<rect x="{ix}" y="{iy}" width="{iw}" height="{ih}" fill="#17140F" stroke="#332D22"/>',
             text(ix + 14, iy + 22, "THE HEAD, CLOSE", 10.5, "#635C4C", extra=' letter-spacing="2.5"'),
             text(ix + iw - 14, iy + 22, "1 cm = 6 px", 10, "#635C4C", "end")]
    hb = Body(1)
    hb.neck_y = 0
    k = 6.0
    hx, hy = ix + iw / 2, iy + 330
    s.clip(f"M{ix},{iy} h{iw} v{ih} h{-iw} Z", "insetClip")
    inset.append(f'<g clip-path="url(#insetClip)"><g transform="translate({hx},{hy}) scale({k})">')
    # shoulders of the coat under the head, for context
    inset.append(f'<path d="M-44,30 Q-30,4 -12,2 L12,2 Q30,4 44,30 L44,40 L-44,40 Z" fill="{s.form(WOOL)}" stroke="{shade(WOOL,.4)}" stroke-width=".3"/>')
    inset.append(head(s, hb))
    inset.append('</g></g>')
    Q = lambda x, y: (round(hx + k * x, 1), round(hy + k * y, 1))
    inset.append(callout(*Q(-20, 7), ix + 18, iy + 430, "Last eye", "tracks you", side="right"))
    inset.append(callout(*Q(11, 6), ix + iw - 18, iy + 430, "Half a jaw", "it still chews", side="left"))
    inset.append(callout(*Q(0, -45), ix + iw - 18, iy + 60, "Vented cap", "smoke from the holes", side="left"))
    inset.append(callout(*Q(-12, -20), ix + 18, iy + 110, "Bone bars", "with knuckles", side="right"))
    rows = [("The turn, proposed", "1.6 s from being seen to the hunt. For the first 0.9 s it is busy turning: the one moment to run."),
            ("Its counter, proposed", "It sees only by its own flame. Out of the light you are hidden; in it, you are seen.")]
    head_ = frame_open(s, "FEAR PASS · LANTERN WARDEN · TRUE FORM · ROUND 2", "The Wick: the turn", "four stages · 1 m = 110 px", "the lantern goes dark as he lights", glow_cx="55%", glow_cy="40%")
    parts = [metre_ladder(px_per_m=110, top_m=3.0), ''.join(figs), ''.join(labels), ''.join(inset), notes(ix, 610, rows, width=400)]
    write_plate(s, head_, parts, '')


if __name__ == "__main__":
    sheet_true_form()
    sheet_turn()
