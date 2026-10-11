"""Draws the concept plates for the wizards proposal as inline SVG strings.

The plates use fixed dark colours on purpose: they copy the HUD proposal's concept sheets,
which stay dark in both page themes.
"""
from html import escape

INK = "#0D0B08"
VELLUM = "#DCD2BA"
DIM = "#9A9078"
FAINT = "#635C4C"
LINE = "#332D22"
BRASS = "#B58A3C"
CALF = "#7A5233"
OAK = "#5A4026"
SKIN = "#C9A98A"
LAPIS = "#9A8BC4"

# Example party: username, the colour they picked
PARTY = [("Hollowmere", "#4F7299"), ("nyx_77", "#8E3446"), ("Pip", "#6E8A3A"), ("GrumboldTheUnwashed", "#CDBF9F")]


def rgb(hex_colour):
    return tuple(int(hex_colour[i:i + 2], 16) for i in (1, 3, 5))


def darker(hex_colour, factor=0.72):
    return "#%02X%02X%02X" % tuple(int(c * factor) for c in rgb(hex_colour))


def thread_for(colour):
    """Thread that shows up on this cloth: dark on pale colours, pale on the rest."""
    r, g, b = rgb(colour)
    return "#2A231A" if 0.299 * r + 0.587 * g + 0.114 * b > 150 else VELLUM


def fit_size(name, size, width):
    """Font size that fits the name inside `width`: long names get smaller, never squashed."""
    return round(min(size, width / (len(name) * 0.52)), 2)


def name_text(name, cx, cy, size, width, colour):
    s = fit_size(name, size, width)
    return (f'<text x="{cx}" y="{cy}" text-anchor="middle" dominant-baseline="middle" font-family="Eczar, Georgia, serif" '
            f'font-weight="700" font-size="{s}" fill="{colour}">{escape(name)}</text>')


def mono(x, y, text, size=16, fill=DIM, anchor="start", spacing=0):
    return (f'<text x="{x}" y="{y}" text-anchor="{anchor}" font-family="Overpass Mono, monospace" '
            f'font-size="{size}" letter-spacing="{spacing}" fill="{fill}">{text}</text>')


# ---------- the hat, and the wizard under it ----------

def hat(colour, name):
    """The one hat, brim centred on (0, -287), with the name on the band."""
    st = f'stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"'
    c, band = darker(colour), darker(colour, 0.5)
    return (f'<ellipse cx="0" cy="-287" rx="42" ry="8" fill="{c}" {st}/>'
            f'<path d="M-21 -289 Q-14 -340 2 -372 Q11 -392 32 -405 Q19 -386 16 -366 Q13 -332 21 -289 Z" fill="{c}" {st}/>'
            f'<path d="M-21.5 -288 Q0 -281 21.5 -288 L19 -305 Q0 -299 -19 -305 Z" fill="{band}" {st}/>'
            + name_text(name, 0, -294, 9, 36, thread_for(band)))


def placed_hat(x, y, scale, rotate, colour, name):
    """A loose hat with its brim centre at (x, y)."""
    return f'<g transform="translate({x} {y}) rotate({rotate}) scale({scale}) translate(0 287)">{hat(colour, name)}</g>'


def wizard(x, y, s, colour, name, back=False, glow=False, carrying=None):
    """One wizard standing with feet at (x, y), scaled by s. carrying=(colour, name) puts a hat in the gloved hand."""
    st = f'stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"'
    shade = 'fill="#000" stroke="none"'
    p = ['<ellipse cx="0" cy="0" rx="72" ry="9" fill="#000" opacity=".45"/>',
         f'<path d="M-32 -14 H-8 V-2 H-40 Q-40 -10 -32 -14 Z" fill="#2A2118" {st}/>',
         f'<path d="M8 -14 H32 Q40 -10 40 -2 H8 Z" fill="#2A2118" {st}/>',
         f'<path d="M-36 -248 L36 -248 L60 -12 Q0 -4 -60 -12 Z" fill="{colour}" {st}/>',
         f'<path d="M0 -248 L36 -248 L60 -12 Q30 -7 0 -6 Z" {shade} opacity=".16"/>',
         f'<path d="M-58 -32 Q0 -24 58 -32 L60 -12 Q0 -4 -60 -12 Z" {shade} opacity=".3"/>',
         f'<path d="M-43 -178 H43 V-164 H-43 Z" fill="{OAK}" {st}/>']
    if not back:
        p.append(f'<path d="M0 -164 V-8" stroke="{INK}" stroke-width="1.6" opacity=".55"/>'
                 f'<rect x="-8" y="-180" width="16" height="18" fill="none" stroke="{BRASS}" stroke-width="3"/>'
                 f'<path d="M-36 -164 V-158" stroke="{OAK}" stroke-width="3"/>'
                 f'<rect x="-46" y="-158" width="22" height="30" rx="2" fill="{CALF}" {st}/>'
                 f'<path d="M-46 -151 L-39 -158 M-24 -151 L-31 -158 M-46 -135 L-39 -128 M-24 -135 L-31 -128" stroke="{BRASS}" stroke-width="3"/>'
                 f'<path d="M6 -164 Q2 -150 10 -142" fill="none" stroke="{BRASS}" stroke-width="1.6"/>'
                 f'<circle cx="11" cy="-136" r="6.5" fill="{BRASS}" {st}/>'
                 f'<path d="M24 -164 Q16 -138 34 -134 Q52 -138 44 -164 Z" fill="{CALF}" {st}/>'
                 f'<path d="M25 -160 Q34 -156 43 -160" fill="none" stroke="{colour}" stroke-width="2.4"/>')
    for side in (-1, 1):
        p.append(f'<path d="M{40*side} -246 Q{64*side} -230 {72*side} -160 L{46*side} -150 Q{44*side} -200 {30*side} -225 Z" fill="{colour}" {st}/>'
                 f'<path d="M{70*side} -170 L{72*side} -160 L{46*side} -150 L{45*side} -160 Z" {shade} opacity=".3"/>')
    if glow:
        p.append('<circle cx="-60" cy="-146" r="20" fill="url(#palmglow)"/>')
    glove_x = -60 if back else 60
    if carrying:
        p.append(placed_hat(glove_x - 4, -122, 0.95, 8, *carrying))
    p.append(f'<circle cx="-60" cy="-146" r="9.5" fill="{OAK if back else SKIN}" {st}/>'
             f'<circle cx="60" cy="-146" r="9.5" fill="{SKIN if back else OAK}" {st}/>'
             f'<path d="M-44 -250 Q0 -262 44 -250 L56 -210 Q0 -196 -56 -210 Z" fill="{colour}" {st}/>'
             f'<path d="M-56 -210 Q0 -196 56 -210 L54 -217 Q0 -204 -54 -217 Z" {shade} opacity=".25"/>'
             f'<circle cx="0" cy="-272" r="17" fill="{"#3A2E22" if back else SKIN}" {st}/>')
    if not back:
        p.append(f'<path d="M-17 -276 Q0 -292 17 -276 Q17 -290 0 -291 Q-17 -290 -17 -276 Z" fill="#000" opacity=".35"/>'
                 f'<circle cx="-6" cy="-271" r="1.9" fill="{INK}"/><circle cx="6" cy="-271" r="1.9" fill="{INK}"/>'
                 f'<circle cx="0" cy="-238" r="7" fill="{BRASS}" {st}/>')
    p.append(hat(colour, name))
    return f'<g transform="translate({x} {y}) scale({s})">{"".join(p)}</g>'


# ---------- plate frame and labels ----------

def frame(w, h, eyebrow, title, right_lines, body, swatches=()):
    defs = (f'<defs><radialGradient id="vig" cx="45%" cy="55%" r="75%"><stop offset="0" stop-color="#2A2218"/>'
            f'<stop offset="1" stop-color="#14120E"/></radialGradient>'
            f'<radialGradient id="palmglow"><stop offset="0" stop-color="{LAPIS}" stop-opacity=".9"/>'
            f'<stop offset="1" stop-color="{LAPIS}" stop-opacity="0"/></radialGradient>'
            f'<pattern id="grid" width="40" height="40" patternUnits="userSpaceOnUse"><path d="M40 0 H0 V40" fill="none" stroke="#fff" stroke-opacity=".035"/></pattern></defs>')
    out = [f'<svg viewBox="0 0 {w} {h}" xmlns="http://www.w3.org/2000/svg" role="img">', defs,
           f'<rect width="{w}" height="{h}" fill="url(#vig)"/><rect width="{w}" height="{h}" fill="url(#grid)"/>',
           f'<rect x="12" y="12" width="{w-24}" height="{h-24}" fill="none" stroke="{LINE}" stroke-width="1.5"/>',
           mono(60, 70, eyebrow, 17, FAINT, spacing=5),
           f'<text x="60" y="128" font-family="Eczar, Georgia, serif" font-weight="700" font-size="54" fill="{VELLUM}">{title}</text>']
    for i, line in enumerate(right_lines):
        out.append(mono(w - 60, 70 + i * 28, line, 17, DIM, "end"))
    out.append(body)
    for i, (hexv, name) in enumerate(swatches):
        sx = 60 + i * 230
        out.append(f'<rect x="{sx}" y="{h-68}" width="44" height="26" fill="{hexv}" stroke="{LINE}"/>'
                   + mono(sx + 54, h - 58, hexv, 15) + mono(sx + 54, h - 40, name, 15, FAINT))
    out.append('</svg>')
    return "".join(out)


def label(x, y, title, sub, anchor, to):
    tx, ty = to
    lx = x + (12 if anchor == "start" else -12)
    lead = f'<path d="M{lx+(6 if anchor=="start" else -6)} {y-6} H{lx} L{tx} {ty}" fill="none" stroke="{DIM}" stroke-width="1.2"/>'
    dot = f'<circle cx="{tx}" cy="{ty}" r="4" fill="none" stroke="{VELLUM}" stroke-width="1.6"/>'
    tx0 = x + (24 if anchor == "start" else -24)
    return lead + dot + mono(tx0, y, title, 20, VELLUM, anchor) + mono(tx0, y + 24, sub, 16, DIM, anchor)


def panel(x, y, w, h):
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="#17140F" stroke="{LINE}" stroke-width="1.5"/>'


# ---------- plate 1: the wizard ----------

def plate_wizard():
    cx, fy, s = 600, 700, 1.3
    g = lambda lx, ly: (cx + lx * s, fy + ly * s)
    name, colour = PARTY[0]
    r, gr, b = rgb(colour)
    body = [f'<path d="M60 {fy} H1140" stroke="{LINE}" stroke-width="1.5"/>',
            wizard(cx, fy, s, colour, name, glow=True),
            label(430, 250, "Hat", "the same tall hat", "end", g(-6, -340)),
            mono(406, 298, "for every player", 16, DIM, "end"),
            label(430, 470, "Grimoire at the hip", "the spellbook, ready to raise", "end", g(-35, -143)),
            label(430, 580, "Bare hand", "casts; glows when you speak", "end", g(-62, -146)),
            label(770, 300, "Hat band", "your name, all the way round", "start", g(15, -294)),
            label(770, 400, "Cape and robe", "the colour you picked", "start", g(48, -225)),
            label(770, 490, "Portal watch", "on a chain from the belt", "start", g(11, -136)),
            label(770, 570, "Coin pouch", "heavier as you sell", "start", g(34, -146)),
            label(770, 650, "Gloved hand", "grabs and carries loot", "start", g(62, -146))]
    return frame(1200, 800, "THE WIZARD · ONE OUTFIT FOR EVERY PLAYER", "The Wizard",
                 [f"front view · {escape(name)}", "only the colour and the name change"],
                 "".join(body),
                 [(colour, f"rgb {r} {gr} {b} · picked"), (BRASS, "brass"), (CALF, "calfskin"), (OAK, "glove leather")])


# ---------- plate 2: the lobby, where colour and name are set ----------

def plate_lobby():
    body = []
    for i, (name, colour) in enumerate(PARTY):
        x = 165 + i * 290
        body.append(wizard(x, 520, 0.86, colour, name))
        shown = escape(name) if len(name) <= 14 else escape(name[:13]) + "…"
        body.append(f'<text x="{x}" y="574" text-anchor="middle" font-family="Eczar, Georgia, serif" font-weight="700" font-size="28" fill="{VELLUM}">{shown}</text>')
        body.append(mono(x, 600, "rgb %d %d %d" % rgb(colour), 15, DIM, "middle"))

    # left: the colour picker, shown at the moment Pip lands on nyx_77's exact colour
    body.append(mono(60, 652, "YOUR COLOUR", 15, FAINT, spacing=3) + panel(60, 668, 530, 252))
    want = rgb(PARTY[1][1])
    body.append(f'<rect x="84" y="692" width="96" height="96" fill="{PARTY[1][1]}" stroke="{LINE}"/>')
    tints = ("#B5544A", "#5E8A4E", "#4F6EA0")
    for k, (letter, v) in enumerate(zip("RGB", want)):
        y = 704 + k * 36
        kx = 230 + v / 255 * 260
        body.append(mono(204, y + 6, letter, 17, VELLUM)
                    + f'<path d="M230 {y} H490" stroke="{LINE}" stroke-width="6" stroke-linecap="round"/>'
                    + f'<path d="M230 {y} H{kx:.0f}" stroke="{tints[k]}" stroke-width="6" stroke-linecap="round"/>'
                    + f'<circle cx="{kx:.0f}" cy="{y}" r="9" fill="{VELLUM}" stroke="{INK}" stroke-width="2"/>'
                    + mono(566, y + 6, str(v), 17, VELLUM, "end"))
    body.append(f'<rect x="84" y="810" width="190" height="40" fill="none" stroke="{LINE}" stroke-dasharray="4 4"/>'
                + mono(179, 836, "Wear this colour", 15, FAINT, "middle")
                + mono(292, 826, "nyx_77 already wears exactly", 14, "#C4542E")
                + mono(292, 846, "this. First to pick it keeps it.", 14, "#C4542E")
                + mono(84, 892, "any colour is allowed except an exact match", 14, DIM))

    # right: the name on the hat, shown for a long username
    body.append(mono(620, 652, "NAME ON YOUR HAT", 15, FAINT, spacing=3) + panel(620, 668, 520, 252))
    long_name = PARTY[3][0]
    body.append(f'<circle cx="652" cy="702" r="9" fill="none" stroke="{VELLUM}" stroke-width="2"/><circle cx="652" cy="702" r="4.5" fill="{VELLUM}"/>'
                + mono(674, 708, f"Use my username · {escape(long_name)}", 15, VELLUM)
                + f'<circle cx="652" cy="740" r="9" fill="none" stroke="{DIM}" stroke-width="2"/>'
                + mono(674, 746, "Choose a different name for the game", 15, DIM)
                + f'<rect x="674" y="760" width="300" height="34" fill="none" stroke="{LINE}"/>'
                + mono(686, 783, "e.g. Grum", 15, FAINT))
    band = darker(darker(PARTY[3][1]), 0.7)
    body.append(mono(652, 828, "on the band:", 14, DIM)
                + f'<path d="M652 846 Q880 872 1108 846 L1104 892 Q880 914 656 892 Z" fill="{band}" stroke="{INK}" stroke-width="2"/>'
                + name_text(long_name, 880, 876, 30, 400, VELLUM)
                + mono(652, 912, "long names shrink to fit; a shorter name stitches larger", 13, DIM))
    return frame(1200, 950, "THE WIZARDS · SET IN THE LOBBY", "Four of a party",
                 ["pick any colour, keep it if you were first", "the name on your hat is yours to set"], "".join(body))


# ---------- plate 3: reading who is who ----------

def plate_distance():
    body = []
    panels = [("FAR · THE COLOUR", ["Down a corridor the colour", "reads before anything else."]),
              ("FACING YOU · THE HAT BAND", ["Face to face, the name", "is on the band."]),
              ("BEHIND · THE HAT BAND", ["The band runs all the way", "round, so the name reads", "from behind too."])]
    for i, (head, cap) in enumerate(panels):
        px = 60 + i * 365
        body.append(f'<clipPath id="p{i}"><rect x="{px}" y="180" width="350" height="500"/></clipPath>')
        body.append(mono(px, 166, head, 16, VELLUM, spacing=3))
        inner = [f'<rect x="{px}" y="180" width="350" height="500" fill="#0E0C09"/>']
        if i == 0:
            inner.append(f'<ellipse cx="{px+175}" cy="560" rx="220" ry="160" fill="#2A2218" opacity=".9"/>')
            for j, (name, colour) in enumerate(PARTY):
                inner.append(wizard(px + 52 + j * 82, 640, 0.5, colour, name, back=j % 2 == 1))
        else:
            inner.append(f'<ellipse cx="{px+175}" cy="430" rx="230" ry="240" fill="#2A2218" opacity=".9"/>')
            name, colour = PARTY[1] if i == 1 else PARTY[2]
            inner.append(wizard(px + 175, 1080, 2.5, colour, name, back=i == 2))
        body.append(f'<g clip-path="url(#p{i})">{"".join(inner)}</g>')
        body.append(f'<rect x="{px}" y="180" width="350" height="500" fill="none" stroke="{LINE}" stroke-width="1.5"/>')
        for k, line in enumerate(cap):
            body.append(mono(px, 712 + k * 24, line, 16, DIM))
    return frame(1200, 800, "THE WIZARDS · TELLING WHO IS WHO", "Who is who",
                 ["colour from afar, the name up close", "the name is only on the hat"], "".join(body))


# ---------- plate 4: what is left when a wizard falls ----------

def plate_fallen():
    fallen_name, fallen_colour = PARTY[1]
    body = []
    # left: the hat where the wizard fell
    body.append(mono(60, 166, "WHERE THEY FELL", 16, VELLUM, spacing=3) + panel(60, 180, 530, 420))
    body.append(f'<ellipse cx="325" cy="470" rx="230" ry="110" fill="#2A2218" opacity=".9"/>'
                f'<ellipse cx="325" cy="500" rx="120" ry="22" fill="#000" opacity=".5"/>'
                f'<ellipse cx="325" cy="498" rx="96" ry="14" fill="none" stroke="#4A4032" stroke-width="2" stroke-dasharray="3 6"/>'
                + "".join(f'<circle cx="{x}" cy="{y}" r="2" fill="#6B6152" opacity=".5"/>'
                          for x, y in ((250, 490), (410, 486), (380, 506), (270, 508), (300, 482), (355, 480))))
    body.append(placed_hat(370, 492, 1.6, -76, fallen_colour, fallen_name))
    body.append(mono(84, 580, "the body is gone; the hat stays, name and all", 15, DIM))
    # right: a friend carrying it home
    body.append(mono(610, 166, "CARRIED HOME", 16, VELLUM, spacing=3) + panel(610, 180, 530, 420))
    body.append(f'<clipPath id="fallen-r"><rect x="610" y="180" width="530" height="420"/></clipPath>'
                f'<g clip-path="url(#fallen-r)">'
                f'<ellipse cx="875" cy="340" rx="120" ry="150" fill="url(#palmglow)" opacity=".5"/>'
                f'<ellipse cx="875" cy="340" rx="80" ry="125" fill="none" stroke="{LAPIS}" stroke-width="5"/>'
                f'<ellipse cx="875" cy="340" rx="68" ry="110" fill="none" stroke="{LAPIS}" stroke-width="1.5" opacity=".6"/>'
                f'<path d="M740 545 H1010" stroke="{LINE}" stroke-width="1.5"/>'
                + wizard(875, 545, 0.82, PARTY[2][1], PARTY[2][0], back=True, carrying=(fallen_colour, fallen_name))
                + '</g>')
    body.append(mono(634, 580, "Pip carries nyx_77's hat to the portal", 15, DIM))
    return frame(1200, 650, "THE WIZARDS · WHEN A WIZARD FALLS", "Only the hat",
                 ["the hat keeps its colour and its name", "carry it through the portal to bring them back"], "".join(body))


# ---------- plate 5: the name and colour on belongings ----------

def plate_things():
    name, colour = PARTY[0]
    st = f'stroke="{INK}" stroke-width="2.5" stroke-linejoin="round"'
    cells = []

    def cell(i, title, sub, art):
        cx = 60 + (i % 2) * 547
        cy = 170 + (i // 2) * 300
        cells.append(panel(cx, cy, 533, 285))
        cells.append(f'<g transform="translate({cx+266} {cy+112})">{art}</g>')
        cells.append(f'<text x="{cx+22}" y="{cy+238}" font-family="Eczar, Georgia, serif" font-weight="600" font-size="25" fill="{VELLUM}">{title}</text>')
        cells.append(mono(cx + 22, cy + 266, sub, 15, DIM))

    cell(0, "Strongbox", "a wax seal in your colour, your name on the plate",
         f'<path d="M-90 -20 H90 V70 H-90 Z" fill="{OAK}" {st}/>'
         f'<path d="M-90 -20 Q-90 -70 0 -72 Q90 -70 90 -20 Z" fill="{CALF}" {st}/>'
         f'<path d="M-60 -66 V70 M60 -66 V70" stroke="{BRASS}" stroke-width="7"/>'
         f'<path d="M-90 10 H90 M-90 40 H90" stroke="{INK}" stroke-width="1.4" opacity=".6"/>'
         f'<rect x="-48" y="18" width="96" height="26" fill="{BRASS}" {st}/>' + name_text(name, 0, 32, 17, 84, INK)
         + f'<circle cx="0" cy="-20" r="20" fill="{colour}" {st}/><circle cx="0" cy="-20" r="12" fill="none" stroke="{darker(colour)}" stroke-width="2"/>')
    led = [f'<path d="M-170 -80 H170 V85 H-170 Z" fill="{VELLUM}" {st}/>']
    for j, (n, c) in enumerate(PARTY):
        x = -127 + j * 85
        ink = darker(c, 0.8) if thread_for(c) == VELLUM else darker(c, 0.6)
        led.append(name_text(n, x, -56, 16, 74, ink))
        led.append(f'<path d="M{x-36} -40 H{x+36}" stroke="{ink}" stroke-width="1.5"/>')
        for r in range(4):
            led.append(f'<path d="M{x-26} {-14+r*24} H{x+12+((j+r)%3)*5}" stroke="{ink}" stroke-width="2" opacity=".75"/>')
        if j:
            led.append(f'<path d="M{x-42} -76 V82" stroke="#9A8C6E" stroke-width="1.2"/>')
    cell(1, "Ledger", "each column headed by a name, in its colour of ink", "".join(led))
    cell(2, "Coin pouch", "a drawstring in your colour, your name stitched on",
         f'<path d="M-50 -50 Q-95 40 -40 75 Q0 88 40 75 Q95 40 50 -50 Z" fill="{CALF}" {st}/>'
         f'<path d="M-50 -50 Q0 -30 50 -50 Q30 -70 0 -66 Q-30 -70 -50 -50 Z" fill="{darker(CALF)}" {st}/>'
         f'<path d="M-48 -48 Q0 -26 48 -48 M40 -44 Q60 -10 52 20" fill="none" stroke="{colour}" stroke-width="5"/>'
         + name_text(name, 0, 24, 20, 90, VELLUM))
    cell(3, "Your own hands", "in first person, your sleeves show your colour",
         f'<g transform="translate(0 -22) scale(.9)"><circle cx="-70" cy="-10" r="44" fill="url(#palmglow)"/>'
         f'<path d="M-170 120 Q-130 40 -96 20 L-50 40 Q-80 80 -100 125 Z" fill="{colour}" {st}/>'
         f'<path d="M-96 20 L-50 40 L-56 52 L-102 32 Z" fill="#000" opacity=".3"/>'
         f'<ellipse cx="-72" cy="14" rx="30" ry="18" fill="{SKIN}" {st} transform="rotate(-20 -72 14)"/>'
         f'<path d="M170 120 Q130 40 96 30 L50 50 Q80 90 100 125 Z" fill="{colour}" {st}/>'
         f'<path d="M96 30 L50 50 L56 62 L102 42 Z" fill="#000" opacity=".3"/>'
         f'<rect x="38" y="-28" width="64" height="62" rx="18" fill="{OAK}" {st} transform="rotate(14 70 3)"/></g>')
    return frame(1200, 800, "THE WIZARDS · YOUR NAME AND COLOUR ON YOUR THINGS", "Yours",
                 [f"shown for {escape(name)}", "every player's things carry theirs"], "".join(cells))


if __name__ == "__main__":
    import pathlib
    # long names shrink, short ones keep the full size, and nothing is cut
    assert fit_size("Pip", 9, 36) == 9
    assert fit_size("GrumboldTheUnwashed", 9, 36) < 4 and "GrumboldTheUnwashed" in hat("#CDBF9F", "GrumboldTheUnwashed")
    assert thread_for("#CDBF9F") != thread_for("#4F7299")
    out = pathlib.Path(__file__).with_name("plates-preview.html")
    out.write_text("<body style='margin:0;background:#000'>" + "".join(
        f"<div style='width:1200px'>{p()}</div>" for p in (plate_wizard, plate_lobby, plate_distance, plate_fallen, plate_things)) + "</body>")
    print("wrote", out)
