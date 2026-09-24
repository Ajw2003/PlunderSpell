from lib import *

s = Sheet("alaunt-hound")
FAWN = "#9C7B55"; MASK = "#3A3026"; LEATH = "#5A4030"; IRON = "#6F7479"; WOAD = "#3E5470"; WOOL = "#C4B89C"; KER = "#7E2A26"
g_fawn = s.lg(shade(FAWN, 1.2), shade(FAWN, .55), 0, 0, 0, 1, mid=FAWN)
g_fawn_h = s.form(FAWN)
g_far = s.lg(shade(FAWN, .6), shade(FAWN, .35), 0, 0, 0, 1)
g_mask = s.form(MASK)
g_coat = s.lg(shade(WOAD, 1.2), shade(WOAD, .55), 0, 0, 0, 1, mid=WOAD)
g_leath = s.form(LEATH, False)
g_iron = s.form(IRON)
g_brind = s.lg(MASK, MASK, 0, 0, 0, 1)

BODY = ("M20,-72 C5,-70 -25,-70 -42,-67 C-50,-65 -53,-58 -51,-50 C-49,-44 -44,-40 -36,-40 "
        "C-25,-43 -12,-44 0,-40 C10,-36 20,-35 28,-40 C35,-45 38,-54 38,-60 C40,-66 44,-70 47,-72 "
        "L44,-84 C36,-84 28,-78 20,-72 Z")
s.clip(BODY, "body")
COAT = "M16,-74 C0,-72 -25,-72 -43,-68 C-46,-62 -47,-56 -46,-51 C-30,-49 -10,-48 8,-49 L26,-51 C27,-60 24,-68 16,-74 Z"
s.clip(COAT, "coat")
HEAD = ("M40,-83 C44,-85 52,-85 57,-83.5 C60,-81.5 63,-80.5 70,-80.5 C73,-80.5 74.5,-77.5 74.5,-75 L73.5,-72 "
        "C70.5,-70 66,-68.5 61,-67.5 C58,-65.5 55,-64.5 50,-65.5 C45,-67.5 41,-72 40,-76 Z")


def leg(pts, ws, fill, stroke=None):
    o = [limb(pts, ws, fill, stroke or "#1E1B17", .4)]
    px, py = pts[-1]
    o.append(f'<ellipse cx="{px+2}" cy="-2.6" rx="5.5" ry="2.8" fill="{fill}"' + (f' stroke="{stroke}" stroke-width=".35"' if stroke else '') + '/>')
    if stroke:
        o.append(f'<path d="M{px+4},-3.5 l2.5,2.8 M{px+6},-3.5 l2.2,2.6" stroke="#14120E" stroke-width=".5"/>')
    return ''.join(o)


def spikes_along(x0, y0, x1, y1, n, out_dx, out_dy):
    o = []
    for i in range(n):
        t = (i + .5) / n; x = x0 + (x1 - x0) * t; y = y0 + (y1 - y0) * t
        o.append(f'<path d="M{x-1.1:.1f},{y:.1f} L{x+out_dx:.1f},{y+out_dy:.1f} L{x+1.1:.1f},{y:.1f} Z" fill="{g_iron}" stroke="#14120E" stroke-width=".2"/>')
    return ''.join(o)


SX = 650
S = [f'<g transform="translate({SX},690) scale(2.2)">', '<ellipse cx="-4" cy="0" rx="62" ry="2.8" fill="#0B0A08" opacity=".7"/>']
S.append(leg([(20, -52), (15, -36), (12, -10), (14, -2)], [6, 4.5, 3.3, 3], g_far))
S.append(leg([(-34, -52), (-24, -31), (-36, -12), (-33, -2)], [9, 5, 3.3, 3], g_far))
# tail
S.append(f'<path d="M-49,-64 C-60,-60 -66,-50 -68,-36 C-68.5,-33 -66.5,-33 -66,-36 C-64,-48 -58,-57 -48,-60 Z" fill="{g_fawn_h}" stroke="{shade(FAWN,.4)}" stroke-width=".35"/>')
S.append(f'<path d="{BODY}" fill="{g_fawn}" stroke="{shade(FAWN,.4)}" stroke-width=".45"/>')
# brindle + musculature + soot
br = []
for i in range(22):
    x = -48 + i * 4 + s.rng.random() * 2
    br.append(f'<path d="M{x:.1f},-66 C{x+2:.1f},-58 {x-1:.1f},-50 {x+1.5:.1f},-40" fill="none" stroke="{MASK}" stroke-width="{.5+s.rng.random()*.6:.2f}" opacity=".35"/>')
S.append('<g clip-path="url(#body)">' + ''.join(br) +
         f'<path d="M-40,-40 C-30,-44 -12,-45 0,-41 C10,-37 20,-36 28,-41 L30,-30 L-40,-30 Z" fill="#14120E" opacity=".35"/>'
         f'<path d="M22,-60 C26,-52 26,-45 22,-40" fill="none" stroke="{shade(FAWN,.5)}" stroke-width="1.2" opacity=".6"/>'
         f'<path d="M-30,-60 C-40,-58 -44,-50 -40,-42" fill="none" stroke="{shade(FAWN,.5)}" stroke-width="1.4" opacity=".6"/>'
         + s.flecks(-50, -60, 90, 22, 50, "#1E1B17", .2, .6, .4) + '</g>')
# padded coat with the household arms
S.append(f'<path d="{COAT}" fill="{g_coat}" stroke="{shade(WOAD,.4)}" stroke-width=".45"/>')
S.append(f'<g clip-path="url(#coat)"><path d="M-40,-50 L-12,-68 L16,-50 L10,-50 L-12,-63 L-34,-50 Z" fill="{WOOL}"/>'
         f'<path d="{COAT}" fill="none" stroke="{KER}" stroke-width="3.2"/>'
         + ''.join(f'<path d="M{x},-74 L{x+2},-48" stroke="{shade(WOAD,.55)}" stroke-width=".35"/>' for x in range(-44, 26, 5)) +
         f'<path d="M-46,-58 C-20,-56 0,-56 26,-58 L26,-48 L-46,-48 Z" fill="#14120E" opacity=".25"/>'
         + s.flecks(-46, -72, 72, 24, 40, "#1E1B17", .2, .6, .45) + '</g>')
S.append(f'<path d="M26,-58 C27,-50 26,-48 24,-45 M-44,-58 L-46,-46" stroke="{LEATH}" stroke-width="1.1" fill="none"/>')
S.append(f'<path d="M24,-52 C14,-46 4,-44 -2,-42" stroke="{LEATH}" stroke-width="1.2" fill="none"/>')
# near legs
S.append(leg([(26, -56), (23, -38), (25, -10), (28, -2)], [7, 5, 3.6, 3.2], g_fawn_h, shade(FAWN, .4)))
S.append(leg([(-38, -54), (-29, -32), (-43, -14), (-40, -2)], [11, 5.5, 3.5, 3.1], g_fawn_h, shade(FAWN, .4)))
S.append(f'<path d="M-35,-60 C-44,-56 -46,-46 -38,-40" fill="none" stroke="{shade(FAWN,1.3)}" stroke-width=".8" opacity=".5"/>')
# neck + collar with spikes
S.append(f'<path d="M33,-80 L42,-82.5 L47,-70 L38,-67 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".35"/>')
S.append(spikes_along(33.5, -80, 42.5, -82.5, 4, -2.6, -2.2))
S.append(spikes_along(38, -67, 47, -70, 4, 1, 3.6))
S.append(f'<path d="M35.5,-74 L44.5,-76.5" stroke="{shade(LEATH,1.4)}" stroke-width=".4"/>')
S.append(f'<circle cx="45.5" cy="-73" r="1.6" fill="none" stroke="{IRON}" stroke-width=".7"/>')
# head
S.append(f'<path d="{HEAD}" fill="{g_fawn_h}" stroke="{shade(FAWN,.4)}" stroke-width=".45"/>')
S.append(f'<path d="M57,-83 C60,-81 64,-80 70,-80.5 C73,-80.5 74.5,-77.5 74.5,-75 L73.5,-72 C70.5,-70 66,-68.5 61,-67.5 C58,-65.5 55,-64.5 50,-65.5 C52,-70 55,-76 57,-83 Z" fill="{g_mask}" opacity=".9"/>')
S.append(f'<ellipse cx="72.8" cy="-77" rx="2.2" ry="1.9" fill="#14120E"/>')
S.append(f'<path d="M73,-72.5 C69,-70.8 64,-70 60,-70.5" fill="none" stroke="#14120E" stroke-width=".6"/>')
S.append(f'<path d="M62,-70.2 L63,-68.4 L64,-70.3 M67,-71 L67.8,-69.3 L68.6,-71.1" fill="{WOOL}" stroke="none"/>')
S.append(f'<ellipse cx="54.5" cy="-80.8" rx="1.5" ry="1.1" fill="#14120E"/><circle cx="54.9" cy="-81.1" r=".35" fill="{WOOL}"/>')
S.append(f'<path d="M51,-83.2 Q54,-84.4 57,-82.8" fill="none" stroke="{shade(FAWN,.45)}" stroke-width=".6"/>')
S.append(f'<path d="M44,-83.5 C41,-85.5 37,-85 36.5,-81.5 C38.5,-79.5 42,-80 44.5,-82 Z" fill="{g_mask}" stroke="#14120E" stroke-width=".3"/>')
S.append(f'<path d="M46,-72 C48,-75 50,-76 53,-75" fill="none" stroke="{shade(FAWN,.5)}" stroke-width=".7"/>')
S.append(f'<path d="M42,-84.5 C47,-86.8 53,-85 57,-83.8" fill="none" stroke="{shade(FAWN,1.35)}" stroke-width=".7" opacity=".7"/>')
S.append('</g>')

FX = 310
F = [f'<g transform="translate({FX},690) scale(2.2)">', '<ellipse cx="0" cy="0" rx="26" ry="2.4" fill="#0B0A08" opacity=".7"/>']
# hind legs peeking behind, coat sides
F.append(f'<path d="M-20,-70 C-24,-62 -24,-54 -22,-48 L22,-48 C24,-54 24,-62 20,-70 Z" fill="{g_coat}" stroke="{shade(WOAD,.4)}" stroke-width=".4"/>')
F.append(f'<path d="M-22,-50 L22,-50 L22,-48 L-22,-48 Z" fill="{KER}"/>')
F.append(f'<path d="M-15,-50 L-14,-12 L-13,-2 M15,-50 L14,-12 L13,-2" stroke="{shade(FAWN,.45)}" stroke-width="5.5" stroke-linecap="round" fill="none"/>')
# chest
F.append(f'<path d="M-14,-70 C-17,-60 -16,-46 -10,-38 Q0,-34 10,-38 C16,-46 17,-60 14,-70 Z" fill="{g_fawn_h}" stroke="{shade(FAWN,.4)}" stroke-width=".4"/>')
F.append(f'<path d="M-4,-66 C-5,-56 -3,-44 0,-38 C3,-44 5,-56 4,-66 Z" fill="{shade(FAWN,1.25)}" opacity=".45"/>')
for x in (-9, 9):
    F.append(leg([(x, -46), (x * 1.05, -30), (x * 1.05, -10), (x * 1.05, -2)], [5.5, 4.2, 3.5, 3.3], g_fawn_h, shade(FAWN, .4)))
# neck & collar
F.append(f'<path d="M-12,-80 C-13,-74 -13,-70 -12,-66 L12,-66 C13,-70 13,-74 12,-80 Z" fill="{g_fawn_h}" stroke="{shade(FAWN,.4)}" stroke-width=".4"/>')
F.append(f'<path d="M-12.5,-74 Q0,-70 12.5,-74 L12.5,-68 Q0,-64 -12.5,-68 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
for i, x in enumerate(range(-11, 12, 4)):
    yy = -69.5 + abs(x) * -.06
    F.append(f'<path d="M{x-1},{yy+1:.1f} L{x},{yy-3.8:.1f} L{x+1},{yy+1:.1f} Z" fill="{g_iron}" stroke="#14120E" stroke-width=".2"/>')
# head front: broad skull, ears, muzzle foreshortened
F.append(f'<path d="M-11,-80 C-12,-84.5 -6,-85 0,-85 C6,-85 12,-84.5 11,-80 C11,-75 8,-71 6,-69 L-6,-69 C-8,-71 -11,-75 -11,-80 Z" fill="{g_fawn_h}" stroke="{shade(FAWN,.4)}" stroke-width=".4"/>')
F.append(f'<path d="M-10,-83 C-15,-85 -17,-80 -14,-76 C-12,-77 -11,-79 -10,-80 Z M10,-83 C15,-85 17,-80 14,-76 C12,-77 11,-79 10,-80 Z" fill="{g_mask}" stroke="#14120E" stroke-width=".3"/>')
F.append(f'<path d="M-6.5,-78 C-7,-72 -6,-67 0,-66 C6,-67 7,-72 6.5,-78 Q0,-80 -6.5,-78 Z" fill="{g_mask}"/>')
F.append(f'<ellipse cx="0" cy="-76.6" rx="3.2" ry="2" fill="#14120E"/>')
F.append(f'<path d="M-5,-70 Q0,-68 5,-70" fill="none" stroke="#14120E" stroke-width=".6"/>')
F.append(f'<ellipse cx="-5" cy="-81" rx="1.4" ry="1" fill="#14120E"/><ellipse cx="5" cy="-81" rx="1.4" ry="1" fill="#14120E"/>')
F.append(f'<path d="M-2,-85 Q0,-83 2,-85" fill="none" stroke="{shade(FAWN,.5)}" stroke-width=".5"/>')
F.append('</g>')

P = lambda x, y: (round(SX + 2.2 * x, 1), round(690 + 2.2 * y, 1))
Fp = lambda x, y: (round(FX + 2.2 * x, 1), round(690 + 2.2 * y, 1))
C = [
    callout(*P(40, -78), 960, 420, "Spiked collar, 5 cm", "black leather, 8 iron spikes"),
    callout(*P(66, -76), 960, 480, "Alaunt head, broad jaw", "dark mask, rose ears, 0.30 m"),
    callout(*P(25, -50), 960, 540, "Breast strap + girth", "leather, iron buckle"),
    callout(*P(-40, -18), 960, 600, "Hind leg: long hock", "sickle stance, gallop driver"),
    vcallout(*P(-56, -54), 420, 380, "Fawn brindle coat", "short hair, soot at belly"),
    vcallout(*P(-2, -64), 610, 330, "Padded coat of arms", "quilted woad, chevron, bordure"),
    vcallout(*Fp(0, -78), 225, 395, "Muzzle, front", "skull 0.22 m wide"),
]
parts = [human(), metre_ladder(), view_label(SX, "SIDE (MAIN)"), view_label(FX, "FRONT"), ''.join(S), ''.join(F), ''.join(C),
         text(40, 150, "head 0.85 m · withers 0.72 m · nose-tail 1.42 m", 10.5, "#635C4C")]
head = frame_open(s, f"{AGE_NAME} · ENEMY · SPECIAL", "Alaunt War-hound", "H 0.85 m · ≤ 6k tris · 2048²", "1 m = 220 px · ground y 690")
write(s, head, parts, palette([("fawn coat", FAWN), ("dark mask", MASK), ("collar leather", LEATH), ("spike iron", IRON), ("woad coat", WOAD), ("wool argent", WOOL), ("kermes gules", KER)]))
