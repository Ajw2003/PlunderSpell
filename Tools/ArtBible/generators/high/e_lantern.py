from lib import *

s = Sheet("lantern-warden")
WOOL = "#C4B89C"; WOAD = "#3E5470"; LEATH = "#5A4030"; IRON = "#6F7479"; ASH = "#6B5238"; HORN = "#BFA57A"; SKIN = "#A07A5C"; MAD = "#C4542E"

g_wool = s.form(WOOL); g_wool_d = s.form(shade(WOOL, .82)); g_woad = s.form(WOAD); g_leath = s.form(LEATH)
g_iron = s.form(IRON); g_ash = s.form(ASH); g_skin = s.form(SKIN)
g_horn = s.rg(shade(HORN, 1.35), HORN, 1, 1, .5, .55, .6)
g_hat = s.lg(shade(IRON, 1.3), shade(IRON, .45), 0, 0, 1, .6, mid=IRON)
g_lamp = s.rg(MAD, MAD, .55, 0, .5, .5, .5)
g_lamp2 = s.rg(HORN, HORN, .35, 0, .5, .5, .5)
g_flame = s.rg("#F0D8A0", MAD, 1, 1, .5, .7, .6)

TORSO = "M-24,-147 Q-18,-152 -9,-151 Q0,-147 9,-151 Q18,-152 24,-147 C25,-138 23,-120 21,-106 L22,-98 C25,-86 27,-76 28,-66 Q0,-61 -28,-66 C-27,-76 -25,-86 -22,-98 L-21,-106 C-23,-120 -25,-138 -24,-147 Z"
s.clip(TORSO, "torsoF")
TORSO_S = "M-11,-150 Q-2,-153 7,-149 C13,-140 14,-126 12,-110 L13,-100 C15,-88 16,-76 17,-66 Q0,-62 -16,-66 C-15,-78 -14,-90 -13,-100 L-12,-110 C-15,-126 -15,-140 -11,-150 Z"
s.clip(TORSO_S, "torsoS")


def leg(x, sgn, far=False):
    # front-view hose leg centred x
    d = f"M{x-8},-68 L{x-7.5},-50 C{x-8.5},-40 {x-7.5},-24 {x-5},-10 L{x+3},-10 C{x+5},-24 {x+6},-40 {x+6},-50 L{x+7},-68 Z"
    shoe = f"M{x-6},-11 L{x+4},-11 Q{x+6},-5 {x+6},0 L{x-8},0 Q{x-8.5},-6 {x-6},-11 Z"
    return (f'<path d="{d}" fill="{g_woad}" stroke="{shade(WOAD,.45)}" stroke-width=".35"/>'
            f'<path d="M{x-7.5},-52 L{x+6},-52 L{x+6},-49 L{x-7.5},-49 Z" fill="{LEATH}"/>'
            f'<path d="M{x+3},-46 C{x+5},-34 {x+4},-22 {x+2},-12" fill="none" stroke="{shade(WOAD,.5)}" stroke-width="1.6" opacity=".6"/>'
            f'<path d="M{x-6},-46 C{x-6.5},-36 {x-6},-24 {x-4},-14" fill="none" stroke="{shade(WOAD,1.35)}" stroke-width=".7" opacity=".6"/>'
            f'<path d="{shoe}" fill="{g_leath}" stroke="#14120E" stroke-width=".35"/>'
            f'<path d="M{x-7},-1.2 L{x+5.5},-1.2" stroke="#14120E" stroke-width=".9"/>')


def sleeve(pts, w0, w1):
    # tapered tube through pts (list of (x,y)), width w0->w1
    (x0, y0), (x1, y1) = pts
    dx, dy = x1 - x0, y1 - y0; L = math.hypot(dx, dy); nx, ny = -dy / L, dx / L
    a = (x0 + nx * w0, y0 + ny * w0); b = (x1 + nx * w1, y1 + ny * w1); c = (x1 - nx * w1, y1 - ny * w1); d = (x0 - nx * w0, y0 - ny * w0)
    return f"M{a[0]:.1f},{a[1]:.1f} L{b[0]:.1f},{b[1]:.1f} Q{x1+dx/L*w1:.1f},{y1+dy/L*w1:.1f} {c[0]:.1f},{c[1]:.1f} L{d[0]:.1f},{d[1]:.1f} Q{x0-dx/L*w0:.1f},{y0-dy/L*w0:.1f} {a[0]:.1f},{a[1]:.1f} Z"


def quilt_rings(x0, y0, x1, y1, w0, w1, n):
    out = []
    for i in range(1, n):
        t = i / n; x = x0 + (x1 - x0) * t; y = y0 + (y1 - y0) * t; w = w0 + (w1 - w0) * t
        dx, dy = x1 - x0, y1 - y0; L = math.hypot(dx, dy); nx, ny = -dy / L, dx / L
        out.append(f'<path d="M{x+nx*w:.1f},{y+ny*w:.1f} Q{x+dx/L*1.5:.1f},{y+dy/L*1.5:.1f} {x-nx*w:.1f},{y-ny*w:.1f}" fill="none" stroke="{shade(WOOL,.62)}" stroke-width=".35"/>')
    return ''.join(out)


def lantern(cx, top, far=False):
    # 14 cm wide, 24 cm tall horn lantern with conical cap and ring
    h = []
    h.append(f'<circle cx="{cx}" cy="{top+13}" r="46" fill="{g_lamp}"/>')
    h.append(f'<circle cx="{cx}" cy="{top+13}" r="22" fill="{g_lamp2}"/>')
    h.append(f'<path d="M{cx},{top-6} m-2.5,0 a2.5,2.5 0 1,0 5,0 a2.5,2.5 0 1,0 -5,0" fill="none" stroke="{IRON}" stroke-width=".9"/>')
    h.append(f'<path d="M{cx-7.5},{top+5} L{cx},{top-3.5} L{cx+7.5},{top+5} Z" fill="{g_iron}" stroke="#14120E" stroke-width=".3"/>')
    for k in range(-2, 3):
        h.append(f'<circle cx="{cx+k*2.6}" cy="{top+2+abs(k)*.9}" r=".7" fill="#14120E"/>')
    h.append(f'<rect x="{cx-7}" y="{top+5}" width="14" height="17" fill="{g_horn}" stroke="{shade(IRON,.6)}" stroke-width=".5"/>')
    h.append(f'<path d="M{cx},{top+19} C{cx-3},{top+15} {cx-1},{top+11} {cx},{top+8} C{cx+1},{top+11} {cx+3},{top+15} {cx},{top+19} Z" fill="{g_flame}"/>')
    h.append(f'<rect x="{cx-.6}" y="{top+18.5}" width="1.2" height="2.5" fill="{WOOL}"/>')
    for k in (-7, 0, 7):
        h.append(f'<rect x="{cx+k-.7}" y="{top+5}" width="1.4" height="17" fill="{IRON}"/>')
    h.append(f'<rect x="{cx-8}" y="{top+21.5}" width="16" height="3" fill="{g_iron}" stroke="#14120E" stroke-width=".3"/>')
    h.append(f'<rect x="{cx-8}" y="{top+4}" width="16" height="1.6" fill="{shade(IRON,.7)}"/>')
    h.append(f'<path d="M{cx-5},{top+7} L{cx-4},{top+20}" stroke="#F0D8A0" stroke-width=".5" opacity=".6"/>')
    h.append(s.flecks(cx - 7, top + 5, 14, 17, 10, "#3A2E1E", .2, .6, .5))
    return ''.join(h)


def glaive(x, top=-205):
    out = []
    out.append(f'<rect x="{x-1.5}" y="-160" width="3" height="160" fill="{g_ash}" stroke="{shade(ASH,.5)}" stroke-width=".3"/>')
    for y in range(-150, -5, 17):
        out.append(f'<path d="M{x-1.5},{y} l3,1.2" stroke="{shade(ASH,.6)}" stroke-width=".3"/>')
    out.append(f'<rect x="{x-1.9}" y="-5" width="3.8" height="5" fill="{g_iron}"/>')
    out.append(f'<rect x="{x-1.9}" y="-170" width="3.8" height="14" fill="{g_iron}" stroke="#14120E" stroke-width=".3"/>')
    for y in (-167, -162):
        out.append(f'<circle cx="{x}" cy="{y}" r=".6" fill="#14120E"/>')
    out.append(f'<path d="M{x-1.6},-170 L{x+2.2},-170 C{x+5.5},-180 {x+5.5},-194 {x-.5},{top} C{x-2.4},-196 {x-3.2},-186 {x-3},-178 L{x-6.5},-182 L{x-4},-175 L{x-1.6},-170 Z" fill="{g_iron}" stroke="#14120E" stroke-width=".35"/>')
    out.append(f'<path d="M{x+2.2},-171 C{x+5},-181 {x+5},-193 {x-.5},{top}" fill="none" stroke="#DCD2BA" stroke-width=".55" opacity=".75"/>')
    out.append(f'<path d="M{x},-172 C{x+1.5},-181 {x+1.2},-190 {x-.5},-198" fill="none" stroke="{shade(IRON,.55)}" stroke-width=".5"/>')
    return ''.join(out)


front = []
front.append(f'<g transform="translate(470,690) scale(2.2)">')
front.append(f'<ellipse cx="0" cy="0" rx="30" ry="2.4" fill="#0B0A08" opacity=".7"/>')
front.append(glaive(-33))
front.append(leg(-9, -1)); front.append(leg(9, 1))
# gambeson body
front.append(f'<path d="{TORSO}" fill="{g_wool}" stroke="{shade(WOOL,.4)}" stroke-width=".45"/>')
q = []
for x in range(-26, 28, 4):
    q.append(f'<path d="M{x},-152 C{x+.6},-120 {x-.4},-95 {x*1.1:.1f},-60" fill="none" stroke="{shade(WOOL,.62)}" stroke-width=".35"/>')
front.append(f'<g clip-path="url(#torsoF)">' + ''.join(q) +
             f'<path d="M-28,-66 Q0,-61 28,-66 L28,-72 Q0,-67 -28,-72 Z" fill="{shade(WOOL,.6)}" opacity=".5"/>'
             f'<path d="M8,-150 C20,-130 22,-100 28,-66 L30,-66 L30,-152 Z" fill="#14120E" opacity=".28"/>'
             f'<path d="M-23,-146 C-24,-130 -22,-110 -26,-70" fill="none" stroke="{shade(WOOL,1.3)}" stroke-width="1.4" opacity=".5"/>'
             + s.flecks(-28, -120, 56, 58, 60, "#3A2E1E", .2, .7, .35) +
             s.flecks(-28, -80, 56, 16, 40, "#2A2418", .2, .5, .5) + '</g>')
# collar
front.append(f'<path d="M-10,-151 Q0,-146 10,-151 L11,-145 Q0,-140 -11,-145 Z" fill="{g_wool_d}" stroke="{shade(WOOL,.4)}" stroke-width=".35"/>')
# lacing
for i in range(6):
    y = -143 + i * 6
    front.append(f'<path d="M-1.8,{y} L1.8,{y+3} M1.8,{y} L-1.8,{y+3}" stroke="{LEATH}" stroke-width=".5"/>')
front.append(f'<path d="M0,-146 L0,-104" stroke="{shade(WOOL,.5)}" stroke-width=".5"/>')
# belt, buckle, pouch, dagger
front.append(f'<path d="M-21.5,-106 Q0,-102 21.5,-106 L22,-101.5 Q0,-97.5 -22,-101.5 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
front.append(f'<rect x="-4" y="-106" width="5" height="5.5" fill="none" stroke="{IRON}" stroke-width=".8"/>')
front.append(f'<path d="M1,-103 L7,-102.5 L7,-96 L5,-95" fill="none" stroke="{LEATH}" stroke-width="1.4"/>')
front.append(f'<path d="M11,-101 L19,-101 Q20,-92 16,-88 L13,-88 Q10,-92 11,-101 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
front.append(f'<path d="M11,-98 L19,-98" stroke="{shade(LEATH,.6)}" stroke-width=".5"/>')
front.append(f'<rect x="-15.5" y="-104" width="3" height="4" fill="{ASH}"/><path d="M-15.8,-100 L-12.2,-100 L-13,-82 L-14,-80 L-15,-82 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
front.append(f'<circle cx="-14" cy="-105.5" r="2" fill="{g_ash}" stroke="#14120E" stroke-width=".3"/>')
# right arm (viewer left) holding glaive
front.append(f'<path d="{sleeve([(-23,-145),(-28,-114)],6.5,5.5)}" fill="{g_wool}" stroke="{shade(WOOL,.4)}" stroke-width=".4"/>')
front.append(quilt_rings(-23, -145, -28, -114, 6.5, 5.5, 6))
front.append(f'<path d="{sleeve([(-28,-114),(-32,-124)],5.5,4.5)}" fill="{g_wool_d}" stroke="{shade(WOOL,.4)}" stroke-width=".4"/>')
front.append(f'<ellipse cx="-33" cy="-126" rx="4" ry="4.5" fill="{g_skin}" stroke="{shade(SKIN,.5)}" stroke-width=".3"/>')
front.append(f'<path d="M-36.5,-127 L-29.5,-127 M-36.5,-124.5 L-29.5,-124.5" stroke="{shade(SKIN,.55)}" stroke-width=".4"/>')
# left arm (viewer right) holding lantern forward
front.append(f'<path d="{sleeve([(23,-145),(28,-114)],6.5,5.5)}" fill="{g_wool}" stroke="{shade(WOOL,.4)}" stroke-width=".4"/>')
front.append(quilt_rings(23, -145, 28, -114, 6.5, 5.5, 6))
front.append(f'<path d="{sleeve([(28,-114),(29,-98)],5.5,4.5)}" fill="{g_wool_d}" stroke="{shade(WOOL,.4)}" stroke-width=".4"/>')
front.append(f'<ellipse cx="29" cy="-95" rx="4.2" ry="4" fill="{g_skin}" stroke="{shade(SKIN,.5)}" stroke-width=".3"/>')
front.append(lantern(29, -86))
# warm rim light from lantern on the viewer-right side
g_warm = s.lg(MAD, MAD, 0, 0, 1, 0); s.defs[-1] = s.defs[-1].replace('<stop offset="0" stop-color="#C4542E"/>', '<stop offset="0" stop-color="#C4542E" stop-opacity="0"/>').replace('<stop offset="1" stop-color="#C4542E"/>', '<stop offset="1" stop-color="#C4542E" stop-opacity=".38"/>')
front.append(f'<rect x="8" y="-152" width="22" height="90" fill="{g_warm}" clip-path="url(#torsoF)"/>')
front.append(f'<path d="M15,-66 C16,-45 15,-25 13,-11" fill="none" stroke="{MAD}" stroke-width=".8" opacity=".45"/>')
# head: coif, face, kettle hat
front.append(f'<path d="M-11,-168 L11,-168 L11.5,-152 Q0,-146 -11.5,-152 Z" fill="{g_wool_d}" stroke="{shade(WOOL,.4)}" stroke-width=".35"/>')
front.append(f'<path d="M-8,-167 Q-8.5,-155 0,-151.5 Q8.5,-155 8,-167 Z" fill="{g_skin}" stroke="{shade(SKIN,.45)}" stroke-width=".35"/>')
front.append(f'<path d="M-5.5,-162.5 L-2,-162.5 M2,-162.5 L5.5,-162.5" stroke="#14120E" stroke-width=".8"/>')
front.append(f'<path d="M0,-162 L-.8,-157.5 L.8,-157.5" fill="none" stroke="{shade(SKIN,.5)}" stroke-width=".4"/>')
front.append(f'<path d="M-2.5,-154.5 Q0,-153.8 2.5,-154.5" stroke="{shade(SKIN,.4)}" stroke-width=".5" fill="none"/>')
front.append(f'<path d="M-8,-167 L8,-167 L8,-164 Q0,-162.5 -8,-164 Z" fill="#14120E" opacity=".55"/>')
front.append(f'<path d="M-10.5,-168 C-10.5,-176 -6,-180 0,-180 C6,-180 10.5,-176 10.5,-168 Z" fill="{g_hat}" stroke="#14120E" stroke-width=".4"/>')
front.append(f'<path d="M0,-180 L0,-168" stroke="{shade(IRON,1.35)}" stroke-width=".6"/>')
front.append(f'<path d="M-20,-165 Q0,-170.5 20,-165 L19,-163 Q0,-167.5 -19,-163 Z" fill="{g_hat}" stroke="#14120E" stroke-width=".4"/>')
for x in (-9, -5, 5, 9):
    front.append(f'<circle cx="{x}" cy="-168.6" r=".55" fill="{shade(IRON,1.3)}"/>')
front.append(f'<path d="M-10.5,-168.5 Q0,-170 10.5,-168.5" fill="none" stroke="{shade(IRON,.5)}" stroke-width=".9"/>')
front.append(s.flecks(-19, -181, 38, 16, 18, "#2A2418", .2, .6, .55))
front.append('</g>')

# ── side view, facing right ──
side = []
side.append(f'<g transform="translate(820,690) scale(2.2)">')
side.append(f'<ellipse cx="0" cy="0" rx="26" ry="2.2" fill="#0B0A08" opacity=".7"/>')
# far glaive behind body
side.append(f'<g opacity=".85">{glaive(5)}</g>')
# far leg
side.append(f'<path d="M-9,-68 L-8,-50 C-9,-38 -8,-24 -6,-10 L3,-10 C4,-24 4,-38 5,-50 L6,-68 Z" fill="{shade(WOAD,.6)}"/>')
side.append(f'<path d="M-7,-11 L4,-11 Q12,-7 13,0 L-8,0 Z" fill="{shade(LEATH,.55)}"/>')
# far arm
side.append(f'<path d="{sleeve([(-2,-146),(4,-116)],5,4.5)}" fill="{shade(WOOL,.6)}"/>')
side.append(f'<ellipse cx="6" cy="-122" rx="3.5" ry="4" fill="{shade(SKIN,.6)}"/>')
# near leg
side.append(f'<path d="M-7,-68 L-6,-50 C-7.5,-38 -6.5,-24 -4.5,-10 L4,-10 C6,-24 6.5,-38 6,-50 L8,-68 Z" fill="{g_woad}" stroke="{shade(WOAD,.45)}" stroke-width=".35"/>')
side.append(f'<path d="M-6,-52 L6.5,-52 L6.5,-49 L-6,-49 Z" fill="{LEATH}"/>')
side.append(f'<path d="M-6,-45 C-7,-33 -6,-22 -4,-12" fill="none" stroke="{shade(WOAD,.5)}" stroke-width="1.6" opacity=".6"/>')
side.append(f'<path d="M-5.5,-11 L4.5,-11 Q13,-7 14,0 L-7,0 Q-7.5,-6 -5.5,-11 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".35"/>')
side.append(f'<path d="M-6.5,-1.2 L13,-1.2" stroke="#14120E" stroke-width=".9"/>')
# torso
side.append(f'<path d="{TORSO_S}" fill="{g_wool}" stroke="{shade(WOOL,.4)}" stroke-width=".45"/>')
q = []
for x in range(-16, 18, 4):
    q.append(f'<path d="M{x},-152 C{x+.5},-120 {x-.3},-95 {x*1.05:.1f},-60" fill="none" stroke="{shade(WOOL,.62)}" stroke-width=".35"/>')
side.append(f'<g clip-path="url(#torsoS)">' + ''.join(q) +
            f'<path d="M-16,-150 C-15,-120 -14,-100 -16,-64 L-8,-64 C-8,-100 -9,-125 -6,-152 Z" fill="#14120E" opacity=".3"/>'
            f'<rect x="4" y="-152" width="14" height="90" fill="{g_warm}"/>'
            + s.flecks(-16, -110, 33, 48, 45, "#3A2E1E", .2, .7, .35) + '</g>')
side.append(f'<path d="M-12.5,-106 Q0,-103 12.5,-106 L13,-101.5 Q0,-98.5 -13,-101.5 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
side.append(f'<path d="M-8,-101 L-2,-101 Q-1,-92 -4,-88 L-7,-88 Q-9,-92 -8,-101 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
# neck, coif, head
side.append(f'<path d="M-9,-168 L8,-168 L8,-152 Q0,-148 -10,-151 Z" fill="{g_wool_d}" stroke="{shade(WOOL,.4)}" stroke-width=".35"/>')
side.append(f'<path d="M2,-167 L8.5,-165 L9.5,-162 L8.2,-161 L9,-158 L8,-157 L7.8,-154.5 L4,-152.5 L1,-153 Z" fill="{g_skin}" stroke="{shade(SKIN,.45)}" stroke-width=".35"/>')
side.append(f'<path d="M5,-162.8 L7,-162.6" stroke="#14120E" stroke-width=".8"/>')
side.append(f'<path d="M-10,-168 C-10,-176 -5,-180 0,-180 C5,-180 10,-176 10,-168 Z" fill="{g_hat}" stroke="#14120E" stroke-width=".4"/>')
side.append(f'<path d="M-20,-164 Q0,-171 20,-164 L19,-162 Q0,-168 -19,-162 Z" fill="{g_hat}" stroke="#14120E" stroke-width=".4"/>')
side.append(f'<path d="M0,-180 C-1,-176 -1,-171 0,-168" stroke="{shade(IRON,1.35)}" stroke-width=".6" fill="none"/>')
side.append(s.flecks(-19, -181, 38, 16, 14, "#2A2418", .2, .6, .55))
# near arm forward with lantern
side.append(f'<path d="{sleeve([(0,-145),(3,-114)],6,5.2)}" fill="{g_wool}" stroke="{shade(WOOL,.4)}" stroke-width=".4"/>')
side.append(f'<path d="{sleeve([(3,-114),(18,-98)],5.2,4.4)}" fill="{g_wool_d}" stroke="{shade(WOOL,.4)}" stroke-width=".4"/>')
side.append(f'<ellipse cx="20" cy="-96" rx="4" ry="3.8" fill="{g_skin}" stroke="{shade(SKIN,.5)}" stroke-width=".3"/>')
side.append(lantern(20, -87))
side.append('</g>')

C = []
# right margin callouts -> side view (820 + 2.2*x, 690 + 2.2*y)
P = lambda x, y: (round(820 + 2.2 * x, 1), round(690 + 2.2 * y, 1))
C.append(callout(*P(12, -176), 960, 300, "Kettle hat, one piece", "blackened iron, 0.40 m brim"))
C.append(callout(*P(9, -160), 960, 345, "Wool arming coif", "padded, laced at the nape"))
C.append(callout(*P(12, -128), 960, 405, "Gambeson, 4 cm quilting", "undyed wool, to mid-thigh"))
C.append(callout(*P(26, -75), 960, 520, "Horn lantern, 14×24 cm", "emissive: the tell at 10 paces"))
C.append(callout(*P(-3, -94), 960, 465, "Belt, purse, ballock knife", "veg-tanned leather, iron buckle"))
C.append(callout(*P(4, -35), 960, 575, "Woad hose, gartered", "wool, darkened below knee"))
C.append(callout(*P(12, -4), 960, 630, "Turnshoes", "black leather, flat sole"))
# left gap -> front view (470 + 2.2x)
F = lambda x, y: (round(470 + 2.2 * x, 1), round(690 + 2.2 * y, 1))
C.append(callout(*F(-32, -190), 385, 210, "Glaive, 2.05 m overall", "0.35 m blade, ash haft", side="left"))
C.append(callout(*F(-33, -126), 385, 445, "Grip hand (R)", "haft canted in arches", side="left"))

parts = [human(), metre_ladder(), view_label(470, "FRONT"), view_label(820, "SIDE"),
         ''.join(front), ''.join(side), ''.join(C),
         text(420, 243, "tip 2.05 m", 10, "#635C4C")]
head = frame_open(s, f"{AGE_NAME} · ENEMY · PATROL", "Lantern Warden", "H 1.80 m · ≤ 8k tris · 2048²", "1 m = 220 px · ground y 690", glow_cx="40%", glow_cy="62%")
write(s, head, parts, palette([("gambeson wool", WOOL), ("woad hose", WOAD), ("leather", LEATH), ("black iron", IRON), ("ash haft", ASH), ("horn pane", HORN), ("skin", SKIN)]))
