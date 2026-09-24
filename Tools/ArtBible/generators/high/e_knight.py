from lib import *

s = Sheet("household-knight")
MAIL = "#7C8288"; IRON = "#6F7479"; WOAD = "#3E5470"; WOOL = "#C4B89C"; KER = "#7E2A26"; LEATH = "#5A4030"; OAK = "#5E4630"
p_mail = mail_pattern(s, "mail", MAIL, 1.25)
p_mail_d = mail_pattern(s, "maild", shade(MAIL, .62), 1.25)
g_woad = s.form(WOAD); g_iron = s.lg(shade(IRON, 1.4), shade(IRON, .4), 0, 0, 1, 0, mid=IRON); g_leath = s.form(LEATH)
g_iron_v = s.lg(shade(IRON, 1.3), shade(IRON, .5), 0, 0, 0, 1, mid=IRON)
g_blade = s.lg(shade(MAIL, 1.5), shade(MAIL, .6), 0, 0, 1, 0, mid=MAIL)
g_mshade = s.lg("#FFFFFF", "#14120E", 0, 0, 1, 0, mid="#14120E")
s.defs[-1] = s.defs[-1].replace('stop-color="#FFFFFF"', 'stop-color="#FFFFFF" stop-opacity=".15"').replace('offset=".5" stop-color="#14120E"', 'offset=".5" stop-color="#14120E" stop-opacity="0"').replace('offset="1" stop-color="#14120E"', 'offset="1" stop-color="#14120E" stop-opacity=".55"')
g_shield_sh = s.lg("#FFFFFF", "#14120E", 0, 0, 1, 1, mid="#14120E")
s.defs[-1] = s.defs[-1].replace('stop-color="#FFFFFF"', 'stop-color="#FFFFFF" stop-opacity=".12"').replace('offset=".5" stop-color="#14120E"', 'offset=".5" stop-color="#14120E" stop-opacity="0"').replace('offset="1" stop-color="#14120E"', 'offset="1" stop-color="#14120E" stop-opacity=".5"')

SUR_F = "M-20,-150 Q0,-146 20,-150 C22,-135 21,-118 19,-106 C23,-86 25,-70 28,-56 Q15,-53 3,-56 L0,-80 L-3,-56 Q-15,-53 -28,-56 C-25,-70 -23,-86 -19,-106 C-21,-118 -22,-135 -20,-150 Z"
s.clip(SUR_F, "sf")
SUR_S = "M-12,-150 Q-2,-152 9,-149 C14,-134 14,-118 12,-106 C16,-88 18,-70 20,-56 Q0,-52 -18,-56 C-16,-70 -15,-88 -13,-106 C-15,-122 -15,-138 -12,-150 Z"
s.clip(SUR_S, "ss")
SHIELD = "M-15,-60 C-14,-54 -8,-48 0,-44 C8,-48 14,-54 15,-60 L15,-78 C15,-82 14,-84 12,-84 L-12,-84 C-14,-84 -15,-82 -15,-78 Z"


def shield(cx, cy, sc=1.0):
    # heater 0.60 x 0.78 m -> local coords above are +-15 x 40 at sc=2 ; drawn around (0,-64)
    t = f'translate({cx},{cy}) scale({sc * 2}) translate(0,64)'
    s.clip(SHIELD, "shc")
    o = [f'<g transform="{t}">',
         f'<path d="{SHIELD}" fill="{KER}" stroke="#14120E" stroke-width=".25"/>',
         f'<path d="M-13.2,-60 C-12.2,-55 -7,-50 0,-46.2 C7,-50 12.2,-55 13.2,-60 L13.2,-78 C13.2,-81 12.5,-82.2 11,-82.2 L-11,-82.2 C-12.5,-82.2 -13.2,-81 -13.2,-78 Z" fill="{WOAD}"/>',
         f'<path d="M-13.2,-58 L0,-72 L13.2,-58 L13.2,-53.5 L0,-67.5 L-13.2,-53.5 Z" fill="{WOOL}" clip-path="url(#shc)"/>',
         f'<path d="{SHIELD}" fill="{g_shield_sh}"/>',
         '<g clip-path="url(#shc)">' + s.flecks(-15, -84, 30, 40, 40, "#1E1B17", .1, .4, .5) +
         f'<path d="M-15,-80 L-13,-81 L-15,-75 Z" fill="{OAK}" opacity=".8"/><path d="M11,-50 L15,-60 L15,-52 Z" fill="{OAK}" opacity=".7"/>'
         f'<path d="M-6,-80 l3,4 M4,-74 l-2,5 M-9,-66 l4,2" stroke="{OAK}" stroke-width=".35"/></g>',
         f'<path d="{SHIELD}" fill="none" stroke="{shade(WOOL,1.2)}" stroke-width=".25" opacity=".5"/>',
         '</g>']
    return ''.join(o)


def helm(cx, side=False):
    o = []
    if not side:
        o.append(f'<path d="M-12.5,-150 L-12.5,-178 Q-12,-183 0,-183.5 Q12,-183 12.5,-178 L12.5,-150 Q0,-147 -12.5,-150 Z" fill="{g_iron}" stroke="#14120E" stroke-width=".45"/>')
        o.append(f'<path d="M-12,-168.8 L-1.2,-168.8 L-1.2,-166.4 L-12,-166.4 Z M1.2,-168.8 L12,-168.8 L12,-166.4 L1.2,-166.4 Z" fill="#0B0A08"/>')
        o.append(f'<path d="M-1.6,-183 L1.6,-183 L1.6,-150 L-1.6,-150 Z" fill="{shade(IRON,1.2)}" stroke="#14120E" stroke-width=".25"/>')
        o.append(f'<path d="M-12.5,-171 L12.5,-171 L12.5,-169 L-12.5,-169 Z M-12.5,-166 L12.5,-166 L12.5,-164 L-12.5,-164 Z" fill="{shade(IRON,.75)}" stroke="#14120E" stroke-width=".15"/>')
        for x in (-11, -8, -5, 5, 8, 11):
            o.append(f'<circle cx="{x}" cy="-170" r=".45" fill="{shade(IRON,1.4)}"/><circle cx="{x}" cy="-165" r=".45" fill="{shade(IRON,1.4)}"/>')
        for i in range(4):
            for j in range(3):
                o.append(f'<path d="M{4+j*2.6},{-160+i*2.4} l1.4,0" stroke="#0B0A08" stroke-width=".7"/>')
        o.append(f'<path d="M-11,-176 Q-10.5,-181 -4,-182" stroke="#DCD2BA" stroke-width=".7" fill="none" opacity=".6"/>')
        o.append(s.flecks(-12, -183, 24, 33, 24, "#2A2418", .2, .6, .5))
        # fan crest, painted leather on a wooden core
        o.append(f'<path d="M-9,-183 C-9,-190 -4.5,-195 0,-195 C4.5,-195 9,-190 9,-183 Z" fill="{KER}" stroke="#14120E" stroke-width=".35"/>')
        for k in range(-3, 4):
            o.append(f'<path d="M0,-183 L{k*2.7:.1f},{-194+abs(k)*.9:.1f}" stroke="{WOOL}" stroke-width=".5" opacity=".75"/>')
        o.append(f'<rect x="-10" y="-184.5" width="20" height="2" rx="1" fill="{WOOL}" stroke="#14120E" stroke-width=".25"/>')
    else:
        o.append(f'<path d="M-11.5,-150 L-12,-178 Q-11,-183.5 0,-183.5 Q10,-183 12,-176 L13.5,-167 Q12,-160 13,-150 Q0,-147 -11.5,-150 Z" fill="{g_iron}" stroke="#14120E" stroke-width=".45"/>')
        o.append(f'<path d="M6,-168.8 L13.5,-168.8 L13.4,-166.4 L6,-166.4 Z" fill="#0B0A08"/>')
        o.append(f'<path d="M-11.8,-171 L13.4,-171 L13.4,-169 L-11.8,-169 Z M-11.8,-166 L13.4,-166 L13.4,-164 L-11.8,-164 Z" fill="{shade(IRON,.75)}" stroke="#14120E" stroke-width=".15"/>')
        for x in (-9, -5, -1, 3, 7, 11):
            o.append(f'<circle cx="{x}" cy="-170" r=".45" fill="{shade(IRON,1.4)}"/><circle cx="{x}" cy="-165" r=".45" fill="{shade(IRON,1.4)}"/>')
        for i in range(3):
            o.append(f'<path d="M9,{-159+i*2.6} l2.4,0" stroke="#0B0A08" stroke-width=".7"/>')
        o.append(f'<path d="M-4,-150 Q-6,-165 -7,-178" stroke="#14120E" stroke-width="3" opacity=".25" fill="none"/>')
        o.append(s.flecks(-12, -183, 24, 33, 20, "#2A2418", .2, .6, .5))
        o.append(f'<path d="M-2,-183 L-1.5,-195 C1,-195 3,-192 3,-187 L2.5,-183 Z" fill="{KER}" stroke="#14120E" stroke-width=".35"/>')
        o.append(f'<rect x="-10" y="-184.5" width="20" height="2" rx="1" fill="{WOOL}" stroke="#14120E" stroke-width=".25"/>')
    return ''.join(o)


F = ['<g transform="translate(470,690) scale(2.2)">', '<ellipse cx="0" cy="0" rx="36" ry="2.6" fill="#0B0A08" opacity=".7"/>']
# legs: mail chausses, poleyns, spurs
for x in (-10, 10):
    d = f"M{x-9},-80 L{x-8.5},-52 C{x-9.5},-40 {x-8},-24 {x-5.5},-8 L{x+4},-8 C{x+6},-24 {x+7},-40 {x+7},-52 L{x+8},-80 Z"
    F.append(f'<path d="{d}" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".4"/><path d="{d}" fill="{g_mshade}"/>')
    F.append(f'<path d="M{x-6},-8 L{x+4},-8 Q{x+6.5},-4 {x+6},0 L{x-8},0 Q{x-8.5},-5 {x-6},-8 Z" fill="{p_mail_d}" stroke="#14120E" stroke-width=".35"/>')
    F.append(f'<ellipse cx="{x-1}" cy="-43" rx="5.8" ry="5" fill="{g_iron}" stroke="#14120E" stroke-width=".35"/>')
    F.append(f'<path d="M{x-7},-4 L{x+5},-4" stroke="{LEATH}" stroke-width="1"/>')
# right arm down with sword (viewer left)
F.append(f'<path d="M-32,-84 L-45,-8 L-43,-7 L-30.5,-83 Z" fill="{g_blade}" stroke="#14120E" stroke-width=".3"/>')
F.append(f'<path d="M-31.5,-82 L-44,-9" stroke="{shade(MAIL,.5)}" stroke-width=".4"/>')
F.append(f'<path d="M-38,-86 L-24,-83 L-24.3,-81.6 L-38.3,-84.6 Z" fill="{g_iron}" stroke="#14120E" stroke-width=".3"/>')
F.append(f'<path d="{tube((-24,-143),(-29,-112),7,6)}" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".4"/>')
F.append(f'<path d="{tube((-29,-112),(-31,-90),6,5)}" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".4"/>')
F.append(f'<path d="{tube((-24,-143),(-31,-90),7,5)}" fill="{g_mshade}"/>')
F.append(f'<ellipse cx="-31" cy="-88" rx="5" ry="4.8" fill="{p_mail_d}" stroke="#14120E" stroke-width=".35"/>')
F.append(f'<path d="M-31,-92.5 L-30,-95 L-32.5,-95.5 Z" fill="{g_iron}"/><circle cx="-30" cy="-96.5" r="1.8" fill="{g_iron}" stroke="#14120E" stroke-width=".3"/>')
# hauberk skirt visible below surcoat, and body
F.append(f'<path d="M-25,-60 Q0,-54 25,-60 L26,-47 Q0,-41 -26,-47 Z" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".35"/>')
F.append(f'<path d="{SUR_F}" fill="{g_woad}" stroke="{shade(WOAD,.4)}" stroke-width=".45"/>')
F.append(f'<g clip-path="url(#sf)">'
         f'<path d="M-30,-86 L0,-120 L30,-86 L30,-76 L0,-110 L-30,-76 Z" fill="{WOOL}"/>'
         f'<path d="{SUR_F}" fill="none" stroke="{KER}" stroke-width="5"/>'
         f'<path d="M-8,-104 C-10,-80 -12,-60 -14,-40 M8,-104 C10,-80 12,-60 14,-40 M-18,-100 C-20,-80 -22,-60 -26,-40" fill="none" stroke="#14120E" stroke-width="1.6" opacity=".35"/>'
         f'<path d="M4,-150 C16,-130 20,-100 30,-40 L34,-40 L34,-152 Z" fill="#14120E" opacity=".3"/>'
         + s.flecks(-30, -80, 60, 40, 60, "#1E1B17", .2, .7, .45) + '</g>')
F.append(f'<path d="M-1.5,-80 L0,-56 L1.5,-80" fill="#14120E" opacity=".6"/>')
# mail at chest top / aventail
F.append(f'<path d="M-20,-150 Q-14,-155 -9,-154 L9,-154 Q14,-155 20,-150 Q0,-146 -20,-150 Z" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".35"/>')
F.append(f'<path d="M-19.5,-106 Q0,-102 19.5,-106 L20,-102 Q0,-98 -20,-102 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
F.append(f'<rect x="-2.5" y="-106" width="5" height="5" fill="none" stroke="{shade(IRON,1.3)}" stroke-width=".8"/>')
# left arm + shield (viewer right)
F.append(f'<path d="{tube((24,-143),(30,-112),7,6)}" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".4"/>')
F.append(shield(27, -100, .9))
F.append(helm(0))
F.append('</g>')

S = ['<g transform="translate(820,690) scale(2.2)">', '<ellipse cx="0" cy="0" rx="34" ry="2.4" fill="#0B0A08" opacity=".7"/>']
# far arm + sword behind
S.append(f'<path d="M6,-86 L30,-18 L28,-17 L4.5,-85 Z" fill="{shade(MAIL,.7)}" stroke="#14120E" stroke-width=".3"/>')
S.append(f'<path d="{tube((-2,-142),(4,-90),6,5)}" fill="{p_mail_d}"/>')
S.append(f'<path d="M-9,-80 L-8,-52 C-9,-40 -8,-24 -6,-8 L3,-8 C4,-24 4,-40 5,-52 L6,-80 Z" fill="{p_mail_d}"/>')
S.append(f'<path d="M-7,-8 L4,-8 Q12,-5 13,0 L-8,0 Z" fill="{p_mail_d}"/>')
dS = "M-7,-80 L-6,-52 C-7.5,-40 -6.5,-24 -4.5,-8 L4,-8 C6,-24 6.5,-40 6,-52 L8,-80 Z"
S.append(f'<path d="{dS}" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".4"/><path d="{dS}" fill="{g_mshade}"/>')
S.append(f'<path d="M-5.5,-8 L4.5,-8 Q13,-5 14,0 L-7,0 Q-7.5,-5 -5.5,-8 Z" fill="{p_mail}" stroke="#14120E" stroke-width=".35"/>')
S.append(f'<path d="M-5,-3.5 L-10,-3 M-10,-3 l-1.5,-.9 l0,1.8 Z" stroke="{IRON}" stroke-width=".8"/>')
S.append(f'<path d="M3,-48 C8,-47 9,-40 5,-37 L2,-38 Z" fill="{g_iron}" stroke="#14120E" stroke-width=".35"/>')
S.append(f'<path d="M-18,-60 Q0,-54 19,-60 L20,-47 Q0,-41 -19,-47 Z" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".35"/>')
S.append(f'<path d="{SUR_S}" fill="{g_woad}" stroke="{shade(WOAD,.4)}" stroke-width=".45"/>')
S.append(f'<g clip-path="url(#ss)"><path d="{SUR_S}" fill="none" stroke="{KER}" stroke-width="5"/>'
         f'<path d="M-16,-150 C-15,-120 -13,-90 -20,-40 L-8,-40 C-7,-90 -9,-125 -6,-152 Z" fill="#14120E" opacity=".3"/>'
         f'<path d="M0,-100 C1,-80 2,-60 4,-40 M-8,-100 C-9,-80 -10,-60 -12,-40" fill="none" stroke="#14120E" stroke-width="1.6" opacity=".35"/>'
         + s.flecks(-20, -80, 42, 40, 40, "#1E1B17", .2, .7, .45) + '</g>')
S.append(f'<path d="M-13,-106 Q0,-103 13,-106 L13.5,-102 Q0,-99 -13.5,-102 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
# scabbard hanging at left hip, angled back
S.append(f'<path d="M-2,-102 L-26,-30 L-23.5,-29 L1,-101 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/><path d="M-25.2,-31.5 L-24,-28 L-22.5,-30.5" fill="{IRON}"/>')
S.append(f'<path d="M-10,-155 Q-2,-157 8,-153 L9,-148 Q0,-146 -11,-150 Z" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".35"/>')
S.append(helm(0, side=True))
# near arm + shield in three-quarter edge
S.append(f'<path d="{tube((0,-143),(8,-112),7,6)}" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".4"/>')
S.append(f'<path d="{tube((8,-112),(16,-100),6,5)}" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".4"/>')
S.append(f'<path d="M14,-140 C19,-138 22,-120 21,-98 C20,-80 16,-66 12,-60 L10,-61 C13,-70 16,-84 16,-100 C16,-118 14,-132 11,-139 Z" fill="{KER}" stroke="#14120E" stroke-width=".4"/>')
S.append(f'<path d="M16,-138 C20,-132 21,-118 20.2,-98 C19.5,-82 16.5,-70 13,-62 L12,-63 C14.5,-72 17,-84 17.5,-100 C18,-118 16.5,-130 14,-137 Z" fill="{WOAD}"/>')
S.append(f'<path d="M17.5,-110 L19.8,-106 L19.5,-100 L17.4,-104 Z" fill="{WOOL}"/>')
S.append(f'<path d="M11,-139 C14,-132 16,-118 16,-100 C16,-84 13,-70 10,-61" fill="none" stroke="{OAK}" stroke-width="1.4"/>')
S.append('</g>')

P = lambda x, y: (round(820 + 2.2 * x, 1), round(690 + 2.2 * y, 1))
Fp = lambda x, y: (round(470 + 2.2 * x, 1), round(690 + 2.2 * y, 1))
C = [
    callout(*P(1, -191), 960, 250, "Fan crest, 12 cm", "boiled leather, painted"),
    callout(*P(12, -167.6), 960, 300, "Great helm, flat top", "5 plates riveted, 2 sights"),
    callout(*P(20, -110), 960, 360, "Heater shield 0.60×0.78", "limewood, gesso, painted"),
    callout(*P(5, -120), 960, 420, "Surcoat: the household arms", "woad, chevron wool, kermes bordure"),
    callout(*P(-14, -50), 960, 480, "Hauberk hem, to the knee", "riveted mail, 8 mm"),
    callout(*P(7, -43), 960, 540, "Poleyn cop, domed", "plate iron over chausse"),
    callout(*P(-10, -3), 960, 600, "Mail chausses, prick spur", "mail laced at calf, iron spur"),
    callout(*Fp(-38, -48), 385, 470, "Arming sword, 0.95 m", "or flanged mace, 0.70 m", side="left"),
    callout(*Fp(-31, -88), 385, 395, "Mail mufflers", "palm slit, leather palm", side="left"),
]
parts = [human(), metre_ladder(), view_label(470, "FRONT"), view_label(820, "SIDE"), ''.join(F), ''.join(S), ''.join(C)]
head = frame_open(s, f"{AGE_NAME} · ENEMY · HEAVY", "Household Knight", "H 1.95 m (crest) · ≤ 12k tris · 2048²", "1 m = 220 px · ground y 690")
write(s, head, parts, palette([("mail steel", MAIL), ("helm iron", IRON), ("woad field", WOAD), ("wool argent", WOOL), ("kermes gules", KER), ("leather", LEATH), ("limewood", OAK)]))
