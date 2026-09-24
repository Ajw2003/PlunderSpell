from lib import *

s = Sheet("castle-crossbowman")
MAIL = "#7C8288"; JACK = "#7E2A26"; HOSE = "#6A5A45"; LEATH = "#5A4030"; OAK = "#5E4630"; HORN = "#A89878"; SKIN = "#A07A5C"
g_jack = s.form(JACK); g_jack_d = s.form(shade(JACK, .8)); g_hose = s.form(HOSE); g_leath = s.form(LEATH); g_oak = s.form(OAK, False)
g_horn = s.form(HORN, False); g_skin = s.form(SKIN); g_iron = s.form(MAIL)
p_mail = mail_pattern(s, "mail", MAIL, 1.2)
g_mailshade = s.lg("#FFFFFF", "#14120E", 0, 0, 1, 0, mid="#14120E")
s.defs[-1] = s.defs[-1].replace('stop-color="#FFFFFF"', 'stop-color="#FFFFFF" stop-opacity=".18"').replace('offset=".5" stop-color="#14120E"', 'offset=".5" stop-color="#14120E" stop-opacity="0"').replace('offset="1" stop-color="#14120E"', 'offset="1" stop-color="#14120E" stop-opacity=".55"')

JACKF = "M-23,-143 Q-12,-149 0,-146 Q12,-149 23,-143 C24,-130 22,-116 20,-104 C22,-94 23,-86 24,-78 Q0,-73 -24,-78 C-23,-86 -22,-94 -20,-104 C-22,-116 -24,-130 -23,-143 Z"
JACKS = "M-11,-147 Q0,-150 8,-145 C13,-134 14,-120 12,-106 C14,-96 15,-86 15,-78 Q0,-74 -15,-78 C-14,-88 -13,-98 -13,-106 C-15,-122 -15,-136 -11,-147 Z"
s.clip(JACKF, "jf"); s.clip(JACKS, "js")
COIF_F = "M0,-178 C-10,-178 -12,-170 -12,-160 C-12,-152 -18,-148 -24,-142 Q0,-134 24,-142 C18,-148 12,-152 12,-160 C12,-170 10,-178 0,-178 Z"
COIF_S = "M-1,-178 C-10,-178 -13,-170 -13,-160 C-13,-150 -15,-146 -14,-140 Q0,-136 12,-142 C10,-148 9,-152 9,-158 L9,-168 C8,-175 4,-178 -1,-178 Z"
s.clip(COIF_F, "cf"); s.clip(COIF_S, "cs")


def quilt(clip, x0, x1, y0, y1, step=4.5):
    out = []
    for k in range(-40, 40):
        x = k * step
        out.append(f'<path d="M{x},{y0} L{x+(y1-y0)*.55:.1f},{y1}" stroke="{shade(JACK,.55)}" stroke-width=".35"/>')
        out.append(f'<path d="M{x},{y0} L{x-(y1-y0)*.55:.1f},{y1}" stroke="{shade(JACK,.55)}" stroke-width=".35"/>')
    return f'<g clip-path="url(#{clip})">' + ''.join(out) + '</g>'


def legs_front():
    o = []
    for x in (-9, 9):
        d = f"M{x-8},-80 L{x-7.5},-50 C{x-8.5},-40 {x-7.5},-24 {x-5},-12 L{x+3},-12 C{x+5},-24 {x+6},-40 {x+6},-50 L{x+7},-80 Z"
        o.append(f'<path d="{d}" fill="{g_hose}" stroke="{shade(HOSE,.45)}" stroke-width=".35"/>')
        o.append(f'<path d="M{x+3},-46 C{x+5},-34 {x+4},-22 {x+2},-14" fill="none" stroke="{shade(HOSE,.5)}" stroke-width="1.6" opacity=".6"/>')
        # ankle boots
        o.append(f'<path d="M{x-6},-17 L{x+4},-17 L{x+4.5},-6 Q{x+6},-3 {x+6},0 L{x-8},0 Q{x-8.5},-5 {x-6.5},-6 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".35"/>')
        o.append(f'<path d="M{x-6.3},-14 L{x+4.2},-14" stroke="{shade(LEATH,1.3)}" stroke-width=".5"/><path d="M{x-7.5},-1.2 L{x+5.5},-1.2" stroke="#14120E" stroke-width=".9"/>')
    return ''.join(o)


def head_front():
    o = [f'<path d="{COIF_F}" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".45"/>',
         f'<path d="{COIF_F}" fill="{g_mailshade}"/>',
         f'<path d="M-7,-166 Q-7.5,-155 0,-151.5 Q7.5,-155 7,-166 Q0,-169 -7,-166 Z" fill="{g_skin}" stroke="{shade(MAIL,.3)}" stroke-width=".8"/>',
         f'<path d="M-5,-161.5 L-1.8,-161.5 M1.8,-161.5 L5,-161.5" stroke="#14120E" stroke-width=".8"/>',
         f'<path d="M0,-161 L-.8,-157 L.8,-157" fill="none" stroke="{shade(SKIN,.5)}" stroke-width=".4"/>',
         f'<path d="M-4,-156 Q0,-153 4,-156 L3,-153.5 Q0,-152 -3,-153.5 Z" fill="{shade(SKIN,.55)}" opacity=".7"/>',
         f'<path d="M-7,-166 Q0,-168 7,-166 L7,-164 Q0,-166 -7,-164 Z" fill="#14120E" opacity=".5"/>',
         # cervelliere skull cap
         f'<path d="M-11,-169 C-11,-176 -6,-178.8 0,-178.8 C6,-178.8 11,-176 11,-169 Q0,-167 -11,-169 Z" fill="{g_iron}" stroke="#14120E" stroke-width=".4"/>',
         f'<path d="M-9,-175 Q-5,-178 -1,-178.3" stroke="#DCD2BA" stroke-width=".6" fill="none" opacity=".6"/>',
         # coif ventail lace
         f'<path d="M-8,-152 Q0,-148 8,-152" fill="none" stroke="{LEATH}" stroke-width=".9"/>',
         '<g clip-path="url(#cf)">' + s.flecks(-24, -178, 48, 40, 30, "#2A2418", .2, .6, .5) + '</g>']
    return ''.join(o)


def crossbow_front():
    o = []
    # prod: composite horn, recurved, spans 0.76 m, at y -76
    o.append(f'<path d="M-38,-80 C-30,-75 -12,-74 0,-74.5 C12,-74 30,-75 38,-80 L38.5,-78 C30,-72.5 12,-72 0,-72 C-12,-72 -30,-72.5 -38.5,-78 Z" fill="{g_horn}" stroke="#14120E" stroke-width=".4"/>')
    o.append(f'<path d="M-37,-79 C-28,-74.6 -12,-74.2 0,-74.4" fill="none" stroke="{shade(HORN,1.35)}" stroke-width=".5"/>')
    for x in (-24, -12, 12, 24):
        o.append(f'<path d="M{x},-75.2 L{x},-72.4" stroke="{LEATH}" stroke-width=".9"/>')
    # string drawn back to nut (behind, reads as a V)
    o.append(f'<path d="M-38,-79 L0,-84 L38,-79" fill="none" stroke="#C4B89C" stroke-width=".45"/>')
    # stock foreshortened, hands
    o.append(f'<path d="M-3,-104 L3,-104 L2.5,-72 L-2.5,-72 Z" fill="{g_oak}" stroke="#14120E" stroke-width=".35"/>')
    o.append(f'<circle cx="0" cy="-84" r="1.6" fill="{shade(HORN,1.2)}" stroke="#14120E" stroke-width=".3"/>')
    # stirrup
    o.append(f'<path d="M-5,-72 C-6,-64 -4,-60 0,-60 C4,-60 6,-64 5,-72" fill="none" stroke="{g_iron}" stroke-width="1.5"/>')
    o.append(f'<path d="M-5,-72 C-6,-64 -4,-60 0,-60 C4,-60 6,-64 5,-72" fill="none" stroke="#14120E" stroke-width=".3"/>')
    return ''.join(o)


def quiver(x, far=False):
    f = shade(LEATH, .6) if far else g_leath
    o = [f'<path d="M{x-5},-108 L{x+5},-108 L{x+6},-72 Q{x},-69 {x-6},-72 Z" fill="{f}" stroke="#14120E" stroke-width=".35"/>',
         f'<path d="M{x-5.5},-100 L{x+5.5},-100 M{x-5.8},-84 L{x+5.8},-84" stroke="{shade(LEATH,1.35)}" stroke-width=".5"/>']
    for i, dx in enumerate((-3.5, -1, 1.5, 3.8)):
        o.append(f'<path d="M{x+dx},-108 L{x+dx},-114" stroke="{OAK}" stroke-width=".8"/>'
                 f'<path d="M{x+dx},-114 l-1.2,-4 l1.2,1 l1.2,-1 Z" fill="#C4B89C" stroke="{shade(HORN,.5)}" stroke-width=".2"/>')
    return ''.join(o)


F = []
F.append('<g transform="translate(470,690) scale(2.2)">')
F.append('<ellipse cx="0" cy="0" rx="30" ry="2.4" fill="#0B0A08" opacity=".7"/>')
F.append(legs_front())
F.append(f'<path d="{JACKF}" fill="{g_jack}" stroke="{shade(JACK,.4)}" stroke-width=".45"/>')
F.append(quilt("jf", -30, 30, -150, -70))
F.append(f'<g clip-path="url(#jf)"><path d="M6,-150 C18,-126 20,-100 26,-72 L30,-72 L30,-152 Z" fill="#14120E" opacity=".3"/>'
         f'<path d="M-22,-140 C-22,-124 -21,-110 -23,-82" fill="none" stroke="{shade(JACK,1.4)}" stroke-width="1.3" opacity=".45"/>'
         + s.flecks(-24, -120, 48, 45, 60, "#1E1B17", .2, .7, .4) + '</g>')
F.append(f'<path d="M-24,-78 Q0,-73 24,-78 L24,-80.5 Q0,-75.5 -24,-80.5 Z" fill="{shade(JACK,.55)}"/>')
# belt, buckle, belt hook, quiver at his right hip
F.append(f'<path d="M-20.5,-106 Q0,-102 20.5,-106 L21,-101.5 Q0,-97.5 -21,-101.5 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
F.append(f'<rect x="-3" y="-106" width="5" height="5.5" fill="none" stroke="{MAIL}" stroke-width=".8"/>')
F.append(f'<path d="M15,-101 L15,-94 M13,-94 L17,-94 M13,-94 Q11,-92 12,-89 M17,-94 Q19,-92 18,-89" fill="none" stroke="{shade(MAIL,.8)}" stroke-width=".9"/>')
F.append(quiver(-27))
# arms: both forward to the stock
F.append(f'<path d="{tube((-22,-141),(-26,-112),6,5.2)}" fill="{g_jack}" stroke="{shade(JACK,.4)}" stroke-width=".4"/>')
F.append(rings((-22, -141), (-26, -112), 6, 5.2, 7, shade(JACK, .55)))
F.append(f'<path d="{tube((-26,-112),(-6,-101),5,4.2)}" fill="{g_jack_d}" stroke="{shade(JACK,.4)}" stroke-width=".4"/>')
F.append(f'<path d="{tube((22,-141),(26,-112),6,5.2)}" fill="{g_jack}" stroke="{shade(JACK,.4)}" stroke-width=".4"/>')
F.append(rings((22, -141), (26, -112), 6, 5.2, 7, shade(JACK, .55)))
F.append(crossbow_front())
F.append(f'<path d="{tube((26,-112),(6,-98),5,4.2)}" fill="{g_jack_d}" stroke="{shade(JACK,.4)}" stroke-width=".4"/>')
F.append(f'<ellipse cx="-4.5" cy="-101" rx="4" ry="3.6" fill="{g_skin}" stroke="{shade(SKIN,.5)}" stroke-width=".3"/>')
F.append(f'<ellipse cx="4.5" cy="-97" rx="4" ry="3.6" fill="{g_skin}" stroke="{shade(SKIN,.5)}" stroke-width=".3"/>')
F.append(head_front())
F.append('</g>')

# ── side ──
S = []
S.append('<g transform="translate(820,690) scale(2.2)">')
S.append('<ellipse cx="0" cy="0" rx="34" ry="2.2" fill="#0B0A08" opacity=".7"/>')
S.append(quiver(-14, far=True))
S.append(f'<path d="M-9,-80 L-8,-50 C-9,-38 -8,-24 -6,-12 L3,-12 C4,-24 4,-38 5,-50 L6,-80 Z" fill="{shade(HOSE,.6)}"/>')
S.append(f'<path d="M-7,-17 L4,-17 L4.5,-6 Q12,-4 13,0 L-8,0 Z" fill="{shade(LEATH,.55)}"/>')
S.append(f'<path d="{tube((-2,-142),(6,-112),5,4.5)}" fill="{shade(JACK,.6)}"/>')
S.append(f'<path d="M-7,-80 L-6,-50 C-7.5,-38 -6.5,-24 -4.5,-12 L4,-12 C6,-24 6.5,-38 6,-50 L8,-80 Z" fill="{g_hose}" stroke="{shade(HOSE,.45)}" stroke-width=".35"/>')
S.append(f'<path d="M-5.5,-17 L4.5,-17 L5,-6 Q13,-4 14,0 L-7,0 Q-7.5,-6 -5.5,-7 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".35"/>')
S.append(f'<path d="M-6.5,-1.2 L13,-1.2" stroke="#14120E" stroke-width=".9"/>')
S.append(f'<path d="{JACKS}" fill="{g_jack}" stroke="{shade(JACK,.4)}" stroke-width=".45"/>')
S.append(quilt("js", -20, 20, -150, -70))
S.append(f'<g clip-path="url(#js)"><path d="M-16,-150 C-15,-120 -14,-100 -16,-74 L-8,-74 C-8,-100 -9,-125 -6,-152 Z" fill="#14120E" opacity=".3"/>'
         + s.flecks(-16, -110, 32, 34, 40, "#1E1B17", .2, .7, .4) + '</g>')
S.append(f'<path d="M-12.5,-106 Q0,-103 12.5,-106 L13,-101.5 Q0,-98.5 -13,-101.5 Z" fill="{g_leath}" stroke="#14120E" stroke-width=".3"/>')
S.append(f'<path d="M11,-101 L11,-94 M11,-94 Q14,-92 13,-88 M11,-94 Q9,-92 10,-88" fill="none" stroke="{shade(MAIL,.8)}" stroke-width=".9"/>')
S.append(f'<path d="{COIF_S}" fill="{p_mail}" stroke="{shade(MAIL,.35)}" stroke-width=".45"/>')
S.append(f'<path d="{COIF_S}" fill="{g_mailshade}"/>')
S.append(f'<path d="M4,-166 L8.8,-165 L9.8,-161.5 L8.6,-160.5 L9.3,-157.5 L8.2,-156.5 L7.5,-153.8 L5,-153 Z" fill="{g_skin}" stroke="{shade(MAIL,.3)}" stroke-width=".6"/>')
S.append(f'<path d="M6,-162.3 L8,-162.1" stroke="#14120E" stroke-width=".8"/>')
S.append(f'<path d="M-11.5,-169 C-11.5,-176 -6,-178.8 -1,-178.8 C5,-178.8 9.5,-176 9.5,-169 Q0,-167 -11.5,-169 Z" fill="{g_iron}" stroke="#14120E" stroke-width=".4"/>')
S.append('<g clip-path="url(#cs)">' + s.flecks(-14, -178, 26, 40, 20, "#2A2418", .2, .6, .5) + '</g>')
# crossbow in profile: stock from butt under arm to prod, pointing down-forward
S.append(f'<path d="M-4,-112 L2,-110 L44,-83 L46,-80 L43,-78 L-1,-104 L-6,-106 Z" fill="{g_oak}" stroke="#14120E" stroke-width=".4"/>')
S.append(f'<path d="M4,-106 L6,-98 L8,-98 L7,-103" fill="none" stroke="{shade(MAIL,.7)}" stroke-width=".8"/>')
S.append(f'<circle cx="12" cy="-103.3" r="1.6" fill="{shade(HORN,1.2)}" stroke="#14120E" stroke-width=".3"/>')
S.append(f'<path d="M12,-104.8 L42,-86" stroke="#C4B89C" stroke-width=".45"/>')
S.append(f'<path d="M14,-105 L34,-92.5" stroke="{OAK}" stroke-width=".9"/><path d="M34,-92.5 l2.5,1.2 l-2.8,.4 Z" fill="{MAIL}"/>')
S.append(f'<path d="M40,-88 C43,-90 45,-88 44,-84 L41,-82 C39,-84 38,-86 40,-88 Z" fill="{g_horn}" stroke="#14120E" stroke-width=".4"/>')
S.append(f'<path d="M44,-81 C49,-80 52,-76 50,-72 C47,-72 45,-76 43,-78" fill="none" stroke="{MAIL}" stroke-width="1.4"/>')
# near arm
S.append(f'<path d="{tube((0,-141),(2,-112),6,5.2)}" fill="{g_jack}" stroke="{shade(JACK,.4)}" stroke-width=".4"/>')
S.append(rings((0, -141), (2, -112), 6, 5.2, 7, shade(JACK, .55)))
S.append(f'<path d="{tube((2,-112),(14,-100),5,4.2)}" fill="{g_jack_d}" stroke="{shade(JACK,.4)}" stroke-width=".4"/>')
S.append(f'<ellipse cx="15" cy="-99" rx="4" ry="3.6" fill="{g_skin}" stroke="{shade(SKIN,.5)}" stroke-width=".3"/>')
S.append('</g>')

P = lambda x, y: (round(820 + 2.2 * x, 1), round(690 + 2.2 * y, 1))
Fp = lambda x, y: (round(470 + 2.2 * x, 1), round(690 + 2.2 * y, 1))
C = [
    callout(*P(5, -177), 960, 290, "Cervellière skull cap", "iron, worn over the coif"),
    callout(*P(-11, -160), 960, 340, "Mail coif, 8 mm rings", "riveted, ventail laced"),
    callout(*P(10, -125), 960, 395, "Padded jack, 45° quilt", "kermes-red wool, to the hip"),
    callout(*P(12, -103), 960, 450, "Nut + tickler trigger", "horn nut, iron tickler"),
    callout(*P(40, -86), 960, 505, "Composite prod, 0.76 m", "horn + sinew, bound"),
    callout(*P(49, -74), 960, 560, "Stirrup, forged iron", "foot goes here to span"),
    callout(*P(6, -40), 960, 615, "Hose + ankle boots", "brown wool, black leather"),
    callout(*Fp(-27, -114), 385, 330, "Bolt quiver, 12 bolts", "stiff leather, fletch up", side="left"),
    callout(*Fp(15, -92), 385, 610, "Spanning belt hook", "double claw, iron", side="left"),
]
parts = [human(), metre_ladder(), view_label(470, "FRONT"), view_label(820, "SIDE"), ''.join(F), ''.join(S), ''.join(C)]
head = frame_open(s, f"{AGE_NAME} · ENEMY · RANGED", "Castle Crossbowman", "H 1.78 m · ≤ 8k tris · 2048²", "1 m = 220 px · ground y 690")
write(s, head, parts, palette([("mail steel", MAIL), ("kermes jack", JACK), ("hose wool", HOSE), ("leather", LEATH), ("oak stock", OAK), ("horn prod", HORN), ("skin", SKIN)]))
