"""Three options for the creature that replaces the debt (#386): a celestial being that hunts the
company through time, arrives physically in the castle as the portal's clock runs low, and takes
artifacts. Lapis (#7A6AA0) is the bible's colour for voice and portals; the creature comes through
time the way the portal does, so it is the one enemy allowed lapis. That exception is the owner's
to approve and is flagged on every plate.

Run from the repo root:  python3 Tools/ArtBible/generators/fear/creatures.py
"""
import math
from lib import *  # noqa: F403

PEWTER = "#8C8A80"      # tarnished silver-grey: rings, dial, plates (not gold: orpiment is value only)
DEEP = "#2A2638"        # lapis darkened toward bone black: the creature's body
BONE = VELLUM
GHOST = "#B9B2D6"       # lapis lightened: the light inside it

PX = 180                # 1 m = 180 px, so a 2.8 m being fits
K = PX / 100.0          # px per cm
TRAITS2 = "1 m = 180 px · ground y 690"
LAPIS_NOTE = "lapis used: bible exception, the owner's call"


def eye(cx, cy, w, ang=0, open_=1.0):
    h = w * .42 * open_
    return (f'<g transform="translate({cx:.1f},{cy:.1f}) rotate({ang:.0f})">'
            f'<path d="M{-w/2:.1f},0 Q0,{-h:.1f} {w/2:.1f},0 Q0,{h:.1f} {-w/2:.1f},0 Z" fill="{BONE}" stroke="{BLACK}" stroke-width=".5"/>'
            f'<circle cx="0" cy="0" r="{h*.62:.1f}" fill="{BLACK}"/><circle cx="{-h*.2:.1f}" cy="{-h*.2:.1f}" r="{h*.15:.1f}" fill="{GHOST}"/></g>')


# ─────────────────────────────── A. THE OPHAN OF HOURS ───────────────────────────────
def ophan():
    s = Sheet("creature-a-the-ophan-of-hours")
    f = [f'<g transform="translate(490,690) scale({K})">', f'<ellipse cx="0" cy="0" rx="70" ry="4" fill="#0B0A08" opacity=".7"/>']
    cy = -158; R = 118
    core = s.rg(GHOST, LAPIS, 1, 0, .5, .5, .5)
    halo = s.rg(LAPIS, LAPIS, .45, 0, .5, .5, .5)
    gring = s.lg(shade(PEWTER, 1.25), shade(PEWTER, .45), 0, 0, 1, 1, mid=PEWTER)
    f.append(f'<circle cx="0" cy="{cy}" r="{R*1.25:.0f}" fill="{halo}"/>')
    # vellum pages hanging beneath like a torn hem: the hours it has eaten
    for i, x in enumerate(range(-40, 44, 10)):
        ln = 34 + (i * 37 % 19)
        f.append(f'<path d="M{x-4},{cy+40} L{x+4},{cy+40} L{x+3+(i%3-1)*2},{cy+40+ln} L{x-3},{cy+40+ln-4} Z" fill="{shade(BONE,.82)}" stroke="{shade(BONE,.5)}" stroke-width=".4" opacity=".9"/>')
        for k in range(3):
            f.append(f'<path d="M{x-2.5},{cy+48+k*7} L{x+2.5},{cy+48+k*7}" stroke="{shade(BONE,.45)}" stroke-width=".5"/>')
    # three rings: the back half of each first, the core, then the front halves, so they interlock
    def ring(rx, ry, rot, w):
        return f'<ellipse cx="0" cy="0" rx="{rx}" ry="{ry}" transform="translate(0,{cy}) rotate({rot})" fill="none" stroke="{gring}" stroke-width="{w}"/>'
    def ring_edge(rx, ry, rot, w):
        return (f'<ellipse cx="0" cy="0" rx="{rx}" ry="{ry}" transform="translate(0,{cy}) rotate({rot})" fill="none" stroke="{BLACK}" stroke-width="{w+1.2}"/>'
                + ring(rx, ry, rot, w))
    f.append(ring_edge(R, R, 0, 10))
    f.append(ring_edge(R * .82, R * .26, -18, 8))
    f.append(f'<circle cx="0" cy="{cy}" r="34" fill="{core}"/>')
    f.append(f'<circle cx="0" cy="{cy}" r="11" fill="{DEEP}" stroke="{GHOST}" stroke-width="1"/>')
    f.append(ring_edge(R * .28, R * .86, 24, 8))
    # hour marks on the great ring, worn
    for h in range(24):
        a = math.radians(h * 15)
        r0, r1 = R - 4, R + (4 if h % 6 else 6)
        f.append(f'<path d="M{math.sin(a)*r0:.1f},{cy-math.cos(a)*r0:.1f} L{math.sin(a)*r1:.1f},{cy-math.cos(a)*r1:.1f}" stroke="{BLACK}" stroke-width="{1.6 if h % 6 == 0 else .8}"/>')
    # eyes set in the rings, every one open and every one looking a different way
    for h in (1, 3, 5, 8, 10, 13, 16, 18, 21, 23):
        a = math.radians(h * 15 + 7)
        f.append(eye(math.sin(a) * R, cy - math.cos(a) * R, 11, h * 15 + 90))
    for u in (.15, .4, .65, .9):
        a = u * 2 * math.pi
        x, y = math.cos(a) * R * .82, math.sin(a) * R * .26
        r = math.radians(-18)
        f.append(eye(x * math.cos(r) - y * math.sin(r), cy + x * math.sin(r) + y * math.cos(r), 7.5, -18))
    for u in (.08, .3, .55, .78):
        a = u * 2 * math.pi
        x, y = math.cos(a) * R * .28, math.sin(a) * R * .86
        r = math.radians(24)
        f.append(eye(x * math.cos(r) - y * math.sin(r), cy + x * math.sin(r) + y * math.cos(r), 7.5, 114))
    f.append(s.flecks(-R, cy - R, 2 * R, 2 * R, 90, "#2A2418", .2, .9, .35))
    f.append('</g>')

    P = lambda x, y: (round(490 + K * x, 1), round(690 + K * y, 1))
    C = [callout(*P(0, cy - R - 4), 860, 160, "Great ring, 2.36 m across", "24 hour marks, tarnished pewter"),
         callout(*P(-72.6, cy - 93), 400, 185, "Eyes in every ring", "each looks its own way", side="left"),
         callout(*P(8, cy), 860, 250, "The core: portal light", "lapis, the colour your portal is"),
         callout(*P(36, cy + 92), 860, 330, "Folios hang under it", "the hours it has eaten"),
         callout(*P(0, -2), 400, 640, "Floats 0.35 m up", "turns its rings edge-on in arches", side="left")]
    rows = [("At ten paces", "A slow wheel of light filling the corridor, turning, with eyes. Nothing in the castle looks like it."),
            ("The hint it gives", "It sees everything at once, so hiding is no use. Only giving it what it wants, or reaching the portal first."),
            ("Bible", LAPIS_NOTE)]
    head = frame_open(s, "FEAR PASS · THE CREATURE · OPTION A", "The Ophan of Hours", "H 2.80 m · rings 2.36 m · ≤ 12k tris", TRAITS2, glow_cx="40%", glow_cy="45%", glow_col="#1E1A2A")
    parts = [ladder_and_human(PX, 3.0), view_label(490, "FRONT"), ''.join(f), ''.join(C), notes(860, 410, rows, width=300)]
    write_plate(s, head, parts, palette([("tarnished pewter", PEWTER), ("eye (vellum)", BONE), ("pupil (bone black)", BLACK), ("portal light (lapis)", LAPIS), ("core", GHOST), ("folio", shade(BONE, .82))]))


# ─────────────────────────────── B. THE TITHE-ABBOT ───────────────────────────────
def abbot():
    s = Sheet("creature-b-the-tithe-abbot")
    f = [f'<g transform="translate(490,690) scale({K})">', f'<ellipse cx="0" cy="0" rx="55" ry="3.5" fill="#0B0A08" opacity=".7"/>']
    robe_col = shade(BONE, .55)
    grobe = s.form(robe_col)
    fade = s.lg(robe_col, robe_col, 0, 0, 0, 1)
    s.defs[-1] = s.defs[-1].replace(f'<stop offset="1" stop-color="{robe_col}"/>', f'<stop offset="1" stop-color="{robe_col}" stop-opacity="0"/>')
    # the robe: too long, narrow at the shoulders, its hem not touching the floor but thinning to smoke
    robe = "M-17,-232 Q0,-238 17,-232 C22,-200 26,-150 30,-100 C33,-70 36,-50 38,-30 L-38,-30 C-36,-50 -33,-70 -30,-100 C-26,-150 -22,-200 -17,-232 Z"
    s.clip(robe, "abbotR")
    f.append(f'<path d="{robe}" fill="{grobe}" stroke="{shade(robe_col,.45)}" stroke-width=".5"/>')
    f.append(f'<path d="M-38,-31 C-30,-20 -34,-8 -26,0 C-14,-8 -8,-2 0,-6 C8,0 16,-10 24,-2 C32,-8 30,-22 38,-31 Z" fill="{fade}"/>')
    folds = ''.join(f'<path d="M{x*.45:.1f},-232 C{x*.6:.1f},-170 {x*.85:.1f},-90 {x:.1f},-30" fill="none" stroke="{shade(robe_col,.6)}" stroke-width=".8"/>' for x in (-26, -14, -4, 8, 20, 30))
    f.append(f'<g clip-path="url(#abbotR)">{folds}<path d="M6,-236 C22,-160 30,-80 40,-30 L44,-30 L44,-236 Z" fill="{BLACK}" opacity=".3"/></g>')
    # a cowl falling from a too-long neck
    f.append(f'<path d="M-15,-232 C-16,-246 -10,-254 0,-255 C10,-254 16,-246 15,-232 Q0,-226 -15,-232 Z" fill="{shade(robe_col,.85)}" stroke="{shade(robe_col,.45)}" stroke-width=".5"/>')
    f.append(f'<path d="M-4,-250 L-3,-262 L3,-262 L4,-250 Z" fill="{DEEP}"/>')
    # the head: a sundial face, no eyes, its gnomon a blade standing out of it
    halo = s.rg(LAPIS, LAPIS, .55, 0, .5, .5, .5)
    gdial = s.lg(shade(PEWTER, 1.2), shade(PEWTER, .5), 0, 0, 1, 1, mid=PEWTER)
    hc = -275
    f.append(f'<circle cx="0" cy="{hc}" r="40" fill="{halo}"/>')
    f.append(f'<circle cx="0" cy="{hc}" r="24" fill="none" stroke="{LAPIS}" stroke-width="1" opacity=".7"/>')
    f.append(f'<circle cx="0" cy="{hc}" r="15" fill="{gdial}" stroke="{BLACK}" stroke-width=".6"/>')
    for h in range(12):
        a = math.radians(-90 + h * 15)
        f.append(f'<path d="M{math.cos(a)*5:.1f},{hc+3+math.sin(a)*5:.1f} L{math.cos(a)*13.5:.1f},{hc+3+math.sin(a)*13.5:.1f}" stroke="{BLACK}" stroke-width=".55"/>')
    f.append(f'<path d="M-1.4,{hc+3} L0,{hc-24} L1.4,{hc+3} Z" fill="{shade(PEWTER,1.3)}" stroke="{BLACK}" stroke-width=".4"/>')
    f.append(f'<path d="M0,{hc+3} L-9,{hc+12}" stroke="{BLACK}" stroke-width="1.2" opacity=".6"/>')  # the gnomon's shadow, the hour it is
    # inner arms hold the open ledger at the chest: the demand, written
    f.append(limb([(-14, -222), (-20, -190), (-14, -170)], [4, 3.2, 2.6], shade(robe_col, .8)))
    f.append(limb([(14, -222), (20, -190), (14, -170)], [4, 3.2, 2.6], shade(robe_col, .8)))
    f.append(f'<path d="M-22,-176 L0,-170 L22,-176 L22,-150 L0,-145 L-22,-150 Z" fill="{shade(BONE,.95)}" stroke="{BLACK}" stroke-width=".5"/>')
    f.append(f'<path d="M0,-170 L0,-145" stroke="{shade(BONE,.5)}" stroke-width=".6"/>')
    for k in range(5):
        y = -168 + k * 4
        f.append(f'<path d="M-19,{y} L-3,{y+1.5} M3,{y+1.5} L19,{y}" stroke="{shade(BONE,.35)}" stroke-width=".55"/>')
    f.append(f'<path d="M-14,-158 l3,-3 l3,3 l-3,3 Z M8,-158 l3,-3 l3,3 l-3,3 Z" fill="{shade(BONE,.4)}"/>')  # sketches of the wanted artifacts
    # outer arms: long, many-jointed, hanging past the knee, fingers open to take
    for sg in (-1, 1):
        f.append(limb([(18 * sg, -228), (36 * sg, -200), (44 * sg, -150), (46 * sg, -104), (44 * sg, -66)], [4.5, 3.4, 2.8, 2.2, 1.8], shade(robe_col, .75)))
        f.append(limb([(44 * sg, -66), (46 * sg, -58)], [1.8, 1.6], DEEP, outline=BLACK))
        for d in (-3, -1, 1, 3):
            f.append(f'<path d="M{46*sg},-58 C{(46+d)*sg},-50 {(47+d*1.6)*sg:.1f},-44 {(46+d*2)*sg},-36" fill="none" stroke="{DEEP}" stroke-width="1.1" stroke-linecap="round"/>')
    f.append(s.flecks(-32, -150, 64, 110, 60, "#2A2418", .2, .8, .35, clip="abbotR"))
    f.append('</g>')

    P = lambda x, y: (round(490 + K * x, 1), round(690 + K * y, 1))
    C = [callout(*P(0, -298), 860, 150, "Sundial face, no eyes", "its gnomon's shadow tells the hour"),
         callout(*P(-24, -282), 400, 190, "A ring of portal light", "lapis halo, 0.8 m", side="left"),
         callout(*P(16, -162), 860, 265, "The ledger, held open", "the demand and its sketches"),
         callout(*P(46, -48), 860, 360, "Hands that take", "fingers 0.22 m, lapis-black"),
         callout(*P(-26, -6), 400, 640, "Hem thins to smoke", "never touches the floor", side="left")]
    rows = [("At ten paces", "A monk far too tall, stooped under the vault, reading. Its head is a clock face, and it is reading about you."),
            ("The hint it gives", "It knows from the ledger who carries what it wants. It walks to them first; the empty-handed are found last."),
            ("Bible", LAPIS_NOTE)]
    head = frame_open(s, "FEAR PASS · THE CREATURE · OPTION B", "The Tithe-Abbot", "H 2.98 m (gnomon) · stoops to 2.5 m · ≤ 12k tris", TRAITS2, glow_cx="40%", glow_cy="30%", glow_col="#1E1A2A")
    parts = [ladder_and_human(PX, 3.0), view_label(490, "FRONT"), ''.join(f), ''.join(C), notes(860, 450, rows, width=300)]
    write_plate(s, head, parts, palette([("robe (vellum, dimmed)", robe_col), ("ledger (vellum)", BONE), ("sundial pewter", PEWTER), ("halo (lapis)", LAPIS), ("hands (deep lapis)", DEEP), ("bone black", BLACK)]))


# ─────────────────────────────── C. THE HOUND OF THE ANGLES ───────────────────────────────
def hound():
    s = Sheet("creature-c-the-hound-of-the-angles")
    ox = 640
    f = [f'<g transform="translate({ox},690) scale({K})">', f'<ellipse cx="10" cy="0" rx="150" ry="4" fill="#0B0A08" opacity=".7"/>']
    gplate = s.lg(shade(DEEP, 1.6), DEEP, 0, 0, 0, 1)
    edge = GHOST
    # a corner of a room, left: where it came from. The wall's corner line, and the angle smoking
    f.append(f'<path d="M-200,0 L-200,-300" stroke="#635C4C" stroke-width="1.2"/><path d="M-200,0 L-250,-20" stroke="#635C4C" stroke-width="1.2"/>')
    smoke = s.rg(LAPIS, LAPIS, .5, 0, .5, .5, .5)
    f.append(f'<ellipse cx="-196" cy="-110" rx="26" ry="110" fill="{smoke}"/>')
    # the hind end still coming out of the corner: shards, straight edges only
    for i, (x, y, w, h) in enumerate([(-188, -120, 14, 8), (-178, -96, 10, 6), (-182, -146, 12, 7), (-170, -132, 9, 5), (-165, -108, 8, 5)]):
        f.append(f'<path d="M{x},{y} l{w},{-h*.4:.1f} l{w*.3:.1f},{h} l{-w*1.1:.1f},{h*.2:.1f} Z" fill="{DEEP}" stroke="{edge}" stroke-width=".5"/>')
    # body: all angles, no curve anywhere; long, lean, ribbed with straight plates
    body = [(-160, -128), (-110, -150), (-40, -142), (20, -150), (70, -134), (88, -112), (40, -100), (-30, -106), (-100, -98), (-150, -108)]
    f.append('<path d="M' + ' L'.join(f'{x},{y}' for x, y in body) + f' Z" fill="{gplate}" stroke="{edge}" stroke-width=".7" stroke-linejoin="miter"/>')
    for x in range(-130, 70, 16):
        f.append(f'<path d="M{x},{-146 + (x % 3)} L{x+8},{-104}" stroke="{BLACK}" stroke-width="1.2" opacity=".7"/>')
    # legs: three segments each, every joint a sharp angle; feet are points, not paws
    legs = [[(-130, -112), (-150, -70), (-128, -36), (-140, 0)], [(-110, -108), (-122, -66), (-100, -34), (-110, 0)],
            [(40, -110), (62, -70), (46, -34), (60, 0)], [(60, -112), (86, -72), (70, -36), (84, 0)]]
    for j, pts in enumerate(legs):
        sh = .7 if j % 2 == 0 else 1
        w = [4.5, 3.2, 2.4, 1.2]
        out, inn = [], []
        for i in range(3):
            (x0, y0), (x1, y1) = pts[i], pts[i + 1]
            dx, dy = x1 - x0, y1 - y0; L = math.hypot(dx, dy); nx, ny = -dy / L, dx / L
            poly = [(x0 + nx * w[i], y0 + ny * w[i]), (x1 + nx * w[i + 1], y1 + ny * w[i + 1]), (x1 - nx * w[i + 1], y1 - ny * w[i + 1]), (x0 - nx * w[i], y0 - ny * w[i])]
            f.append('<path d="M' + ' L'.join(f'{a:.1f},{b:.1f}' for a, b in poly) + f' Z" fill="{shade(DEEP, sh*1.3)}" stroke="{edge}" stroke-width=".45" opacity="{.75 if sh < 1 else 1}"/>')
    # neck and head: a long wedge, the jaw hinged open, a hollow tongue reaching out
    f.append(f'<path d="M70,-134 L118,-160 L126,-150 L88,-112 Z" fill="{gplate}" stroke="{edge}" stroke-width=".6"/>')
    f.append(f'<path d="M112,-166 L178,-152 L132,-144 Z" fill="{shade(DEEP,1.4)}" stroke="{edge}" stroke-width=".7"/>')
    f.append(f'<path d="M120,-146 L172,-130 L124,-138 Z" fill="{shade(DEEP,1.2)}" stroke="{edge}" stroke-width=".7"/>')
    f.append(f'<path d="M128,-144 L172,-146 L174,-133 L130,-140 Z" fill="{BLACK}"/>')
    for i in range(6):
        x = 134 + i * 7
        f.append(f'<path d="M{x},{-147.5+i*.6:.1f} l2,4 l2,-4.3 Z" fill="{BONE}"/>')
    f.append(f'<path d="M136,-141 L196,-150 L208,-142" fill="none" stroke="{GHOST}" stroke-width="2.4"/><path d="M136,-141 L196,-150 L208,-142" fill="none" stroke="{DEEP}" stroke-width="1"/>')
    f.append(f'<path d="M122,-159 l7,1 l-1,3 l-7,-1 Z" fill="{GHOST}"/>')  # one slit of light for an eye
    # blue-grey ichor smoking off every angle of its back
    for x, y in ((-110, -150), (-40, -142), (20, -150), (70, -134), (118, -160)):
        f.append(f'<path d="M{x},{y} q-3,-10 2,-18 q4,-8 -1,-16" fill="none" stroke="{GHOST}" stroke-width=".8" opacity=".5"/>')
    f.append('</g>')

    P = lambda x, y: (round(ox + K * x, 1), round(690 + K * y, 1))
    C = [callout(*P(-196, -170), 400, 180, "Out of a room's corner", "it enters through angles", side="left"),
         callout(*P(20, -150), 860, 165, "Plates, never a curve", "lapis-black, lit along each edge"),
         callout(*P(200, -147), 860, 225, "A hollow tongue", "it drinks time, not blood"),
         callout(*P(84, 0), 860, 300, "Feet are points", "ticks on stone, like a clock"),
         callout(*P(60, -100), 860, 360, "Shoulder 1.5 m, long 3.6 m", "drops to 1.0 m under a low arch")]
    rows = [("At ten paces", "Smoke curling from the corner of a room, then a long dark animal of hard edges stepping out of the angle itself."),
            ("The hint it gives", "It can only come out of corners. Round rooms, the spiral stair and the towers have none; there it must walk."),
            ("Bible", LAPIS_NOTE)]
    head = frame_open(s, "FEAR PASS · THE CREATURE · OPTION C", "The Hound of the Angles", "H 1.66 m (head) · L 3.6 m · ≤ 12k tris", TRAITS2, glow_cx="30%", glow_cy="60%", glow_col="#1E1A2A")
    parts = [ladder_and_human(PX, 3.0), view_label(ox, "SIDE"), ''.join(f), ''.join(C), notes(860, 500, rows, width=300)]
    write_plate(s, head, parts, palette([("body (deep lapis)", DEEP), ("edge light", GHOST), ("smoke (lapis)", LAPIS), ("teeth (vellum)", BONE), ("bone black", BLACK)]))


if __name__ == "__main__":
    ophan()
    abbot()
    hound()
