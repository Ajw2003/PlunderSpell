# Writes docs/art/concept/lair/{lair,market,haggle}.svg (diegetic Lair + Market proposal). Run from the repo root:
#   python3 Tools/ArtBible/generators/diegetic/lair_market.py
# then render: NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs lair
import math, random
M = "Overpass Mono, monospace"
E = "Eczar, Georgia, serif"
HALO = 'stroke="#0E0C09" stroke-width="3" paint-order="stroke" stroke-linejoin="round"'

def T(x, y, s, fill="#9A9078", size=11, anchor="start", ls=0, font=M, w=None, extra=""):
    ww = f' font-weight="{w}"' if w else ""
    return f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}" text-anchor="{anchor}" letter-spacing="{ls}"{ww} {extra}>{s}</text>'

def base(h1, title, right=""):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 800" width="1200" height="800">
<rect width="1200" height="800" fill="#14120E"/>
<rect x="12" y="12" width="1176" height="776" fill="none" stroke="#332D22"/>
{T(40,54,h1,"#635C4C",12,ls=3)}{T(40,96,title,"#DCD2BA",38,font=E,w=700)}{right}
'''

def pal(items, y=748):
    s = '<g id="palette">'
    for i, (c, n) in enumerate(items):
        x = 40 + i * 190
        s += f'<rect x="{x}" y="{y}" width="34" height="18" fill="{c}" stroke="#332D22" stroke-width=".6"/>' + T(x+40, y+8, c, size=10) + T(x+40, y+19, n, "#635C4C", 10)
    return s + '</g>\n'
PAL = [("#DCD2BA","vellum"),("#5FA288","verdigris"),("#C9A227","orpiment"),("#C4542E","madder"),("#7A6AA0","lapis"),("#1E1A14","ash")]

def callout(tx, ty, title, subs, sx, sy, px, py, col="#DCD2BA", anchor="start"):
    s = f'<line x1="{sx}" y1="{sy}" x2="{px}" y2="{py}" stroke="{col}" stroke-width=".9" opacity=".75"/>'
    s += f'<circle cx="{px}" cy="{py}" r="3.2" fill="#14120E" stroke="{col}" stroke-width="1.2"/>'
    s += T(tx, ty, title, col, 11, anchor, ls=.5, w=600, extra=HALO)
    for i, l in enumerate(subs):
        s += T(tx, ty+14+i*13, l, "#B4AA90", 9.5, anchor, extra=HALO)
    return s

DEFS = '''<defs>
<linearGradient id="gold" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#F0D875"/><stop offset=".5" stop-color="#C9A227"/><stop offset="1" stop-color="#7A5F12"/></linearGradient>
<linearGradient id="brass" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#E2BE6A"/><stop offset=".5" stop-color="#A9812F"/><stop offset="1" stop-color="#5E4515"/></linearGradient>
<linearGradient id="wood" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#5A442C"/><stop offset="1" stop-color="#2A1F14"/></linearGradient>
<linearGradient id="woodH" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#6A5035"/><stop offset="1" stop-color="#2E2216"/></linearGradient>
<linearGradient id="vellum" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#E6DDC4"/><stop offset="1" stop-color="#B8AD90"/></linearGradient>
<linearGradient id="skin" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#8A6A52"/><stop offset="1" stop-color="#4A362A"/></linearGradient>
<linearGradient id="ceil" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#070605"/><stop offset="1" stop-color="#1A1610"/></linearGradient>
<linearGradient id="floor" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2A241A"/><stop offset="1" stop-color="#0F0D09"/></linearGradient>
<linearGradient id="wallL" x1="1" y1="0" x2="0" y2="0"><stop offset="0" stop-color="#3A332A"/><stop offset="1" stop-color="#16130E"/></linearGradient>
<linearGradient id="wallR" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#3A3228"/><stop offset="1" stop-color="#16130E"/></linearGradient>
<linearGradient id="back" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#26211A"/><stop offset="1" stop-color="#3C3429"/></linearGradient>
<linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#07070C"/><stop offset="1" stop-color="#241C14"/></linearGradient>
<linearGradient id="ground" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2A241A"/><stop offset="1" stop-color="#0E0C09"/></linearGradient>
<linearGradient id="counter" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#6A5236"/><stop offset="1" stop-color="#3A2A1A"/></linearGradient>
<radialGradient id="gA" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#E0A04A" stop-opacity=".6"/><stop offset=".5" stop-color="#E0A04A" stop-opacity=".18"/><stop offset="1" stop-color="#E0A04A" stop-opacity="0"/></radialGradient>
<radialGradient id="gF" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#F08A3A" stop-opacity=".75"/><stop offset=".45" stop-color="#C4542E" stop-opacity=".25"/><stop offset="1" stop-color="#C4542E" stop-opacity="0"/></radialGradient>
<radialGradient id="lap" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#B6A8E0" stop-opacity=".95"/><stop offset=".4" stop-color="#7A6AA0" stop-opacity=".5"/><stop offset="1" stop-color="#7A6AA0" stop-opacity="0"/></radialGradient>
<radialGradient id="vig" cx=".5" cy=".55" r=".62"><stop offset=".4" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity=".97"/></radialGradient>
<radialGradient id="vig2" cx=".5" cy=".5" r=".72"><stop offset=".6" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity=".7"/></radialGradient>
</defs>'''

def flame(cx, by, h=30, w=9, c1="#E0A04A", c2="#FFE9A8"):
    return (f'<path d="M{cx-w} {by} C{cx-w-2} {by-h*.4} {cx-2} {by-h*.6} {cx} {by-h} C{cx+2} {by-h*.55} {cx+w+2} {by-h*.4} {cx+w} {by}Z" fill="{c1}"/>'
            f'<path d="M{cx-w*.5} {by} C{cx-w*.6} {by-h*.3} {cx-1} {by-h*.4} {cx} {by-h*.62} C{cx+1} {by-h*.35} {cx+w*.6} {by-h*.3} {cx+w*.5} {by}Z" fill="{c2}"/>')

def goblet(x, y, k=1.0):
    return (f'<g transform="translate({x} {y}) scale({k})"><ellipse cx="0" cy="2" rx="12" ry="3" fill="#000" opacity=".45"/>'
            '<path d="M-10 -34 H10 C11 -22 6 -16 2 -14 V-6 H8 V-2 H-8 V-6 H-2 V-14 C-6 -16 -11 -22 -10 -34Z" fill="url(#gold)" stroke="#5E4A10" stroke-width=".6"/>'
            '<ellipse cx="0" cy="-34" rx="10" ry="2.2" fill="#F0D875"/><path d="M-6 -30 C-6 -22 -4 -19 -2 -18" stroke="#FFF2B0" stroke-width="1" fill="none" opacity=".7"/></g>')

def coffer(x, y, k=1.0):
    return (f'<g transform="translate({x} {y}) scale({k})"><ellipse cx="0" cy="2" rx="26" ry="4" fill="#000" opacity=".45"/>'
            '<rect x="-22" y="-22" width="44" height="22" fill="url(#wood)" stroke="#1A120A"/>'
            '<path d="M-22 -22 C-22 -36 22 -36 22 -22Z" fill="url(#woodH)" stroke="#1A120A"/>'
            '<rect x="-22" y="-14" width="44" height="4" fill="url(#gold)"/><rect x="-14" y="-36" width="4" height="36" fill="url(#gold)" opacity=".9"/><rect x="10" y="-36" width="4" height="36" fill="url(#gold)" opacity=".9"/>'
            '<rect x="-4" y="-20" width="8" height="9" rx="1" fill="#F0D875" stroke="#5E4A10" stroke-width=".6"/>'
            '<path d="M-10 -36 L-6 -41 L6 -41 L10 -36" fill="none"/><circle cx="-4" cy="-37" r="3" fill="#F0D875"/><circle cx="5" cy="-38" r="2.4" fill="#C9A227"/></g>')

def candlestick(x, y, k=1.0):
    return (f'<g transform="translate({x} {y}) scale({k})"><ellipse cx="0" cy="2" rx="10" ry="3" fill="#000" opacity=".45"/>'
            '<path d="M-9 0 C-9 -5 -4 -5 -2 -9 H2 C4 -5 9 -5 9 0Z" fill="url(#gold)" stroke="#5E4A10" stroke-width=".5"/>'
            '<rect x="-1.8" y="-30" width="3.6" height="22" fill="url(#gold)"/><ellipse cx="0" cy="-20" rx="4.5" ry="1.8" fill="#C9A227"/>'
            '<path d="M-5 -34 H5 L3.5 -30 H-3.5Z" fill="url(#gold)"/></g>')

# =============== SHEET 1: THE LAIR
def wall_courses(xn, xf, ytn, ytf, ybn, ybf):
    s = ''
    n = 14
    for k in range(n + 1):
        f = k / n
        s += f'<line x1="{xn}" y1="{ytn+f*(ybn-ytn):.1f}" x2="{xf}" y2="{ytf+f*(ybf-ytf):.1f}" stroke="#0A0806" stroke-width="1" opacity=".75"/>'
    for k in range(n):
        f0, f1 = k / n, (k + 1) / n
        for j in range(7):
            t = (j + (.5 if k % 2 else 0)) / 7
            if t > 1: continue
            x = xn + (xf - xn) * (t ** 1.0)
            ya = ytn + (ytf - ytn) * t + f0 * ((ybn - ytn) + ((ybf - ybn) - (ytf - ytn)) * t)
            yb = ytn + (ytf - ytn) * t + f1 * ((ybn - ytn) + ((ybf - ybn) - (ytf - ytn)) * t)
            s += f'<line x1="{x:.1f}" y1="{ya:.1f}" x2="{x:.1f}" y2="{yb:.1f}" stroke="#0A0806" stroke-width=".8" opacity=".6"/>'
    return s

def lair():
    random.seed(7)
    s = base("THE LAIR · A PLACE, NOT A MENU", "The Lair",
             T(1160,54,"≈ 14 × 10 m vaulted cellar · outside time","#9A9078",12,"end") + T(1160,76,"one fire, one candle, falling into black","#9A9078",12,"end"))
    s += DEFS
    s += '<clipPath id="sc"><rect x="40" y="120" width="840" height="560"/></clipPath>'
    s += '<clipPath id="bw"><path d="M230 420V300C230 240 330 205 465 205C600 205 700 240 700 300V420Z"/></clipPath>'
    s += '<g clip-path="url(#sc)"><rect x="40" y="120" width="840" height="560" fill="#050403"/>'
    s += '<rect x="230" y="205" width="470" height="215" fill="#14110C"/>'
    s += '<polygon points="40,120 880,120 700,205 230,205" fill="url(#ceil)"/>'
    s += '<polygon points="40,120 230,205 230,420 40,680" fill="url(#wallL)"/><polygon points="880,120 700,205 700,420 880,680" fill="url(#wallR)"/>'
    s += '<polygon points="230,420 700,420 880,680 40,680" fill="url(#floor)"/>'
    s += '<g clip-path="url(#bw)"><rect x="230" y="205" width="470" height="215" fill="url(#back)"/>'
    for r in range(16):
        y = 205 + r * 14
        s += f'<line x1="230" y1="{y}" x2="700" y2="{y}" stroke="#0A0806" stroke-width="1" opacity=".7"/>'
        for x in range(230 + (r % 2) * 20, 700, 40):
            s += f'<line x1="{x}" y1="{y}" x2="{x}" y2="{y+14}" stroke="#0A0806" stroke-width=".8" opacity=".55"/>'
    s += '</g>'
    s += wall_courses(40, 230, 120, 205, 680, 420) + wall_courses(880, 700, 120, 205, 680, 420)
    # ribs of the barrel vault
    for t in (0, .22, .45, .7):
        xl = 40 + 190 * t; xr = 880 - 180 * t; ys = 150 + 140 * t; h = 170 - 85 * t
        s += f'<path d="M{xl:.0f} {ys+2*h:.0f} V{ys:.0f} Q{(xl+xr)/2:.0f} {ys-2*h:.0f} {xr:.0f} {ys:.0f} V{ys+2*h:.0f}" fill="none" stroke="#2E281F" stroke-width="{9-t*6:.1f}" opacity=".85"/>'
        s += f'<path d="M{xl:.0f} {ys:.0f} Q{(xl+xr)/2:.0f} {ys-2*h:.0f} {xr:.0f} {ys:.0f}" fill="none" stroke="#0A0806" stroke-width="1" />'
    # flagstones
    for i in range(10):
        xb = 230 + i * 470 / 9
        s += f'<line x1="{xb:.1f}" y1="420" x2="{465+(xb-465)*3.9:.1f}" y2="680" stroke="#0A0806" stroke-width=".9" opacity=".7"/>'
    for k in range(1, 10):
        y = 420 + 260 * ((k / 10) ** 1.7)
        xl = 465 + (230 - 465) * (y - 330) / 90; xr = 465 + (700 - 465) * (y - 330) / 90
        s += f'<line x1="{xl:.1f}" y1="{y:.1f}" x2="{xr:.1f}" y2="{y:.1f}" stroke="#0A0806" stroke-width=".9" opacity=".7"/>'
    # glows
    s += '<circle cx="297" cy="330" r="190" fill="url(#lap)" opacity=".55"/>'
    s += '<ellipse cx="300" cy="470" rx="170" ry="60" fill="url(#lap)" opacity=".28"/>'
    s += '<circle cx="632" cy="395" r="300" fill="url(#gF)"/><ellipse cx="640" cy="470" rx="220" ry="70" fill="url(#gF)" opacity=".5"/>'
    s += '<circle cx="355" cy="494" r="130" fill="url(#gA)" opacity=".85"/>'
    # portal arch
    s += '<path d="M250 428V305Q250 245 297 245Q344 245 344 305V428Z" fill="#0B0816"/>'
    s += '<path d="M258 428V308Q258 256 297 256Q336 256 336 308V428Z" fill="url(#lap)"/>'
    s += '<path d="M258 428V308Q258 256 297 256Q336 256 336 308V428Z" fill="#8F80C8" opacity=".35"/>'
    s += '<path d="M250 428V305Q250 245 297 245Q344 245 344 305V428" fill="none" stroke="#5A5042" stroke-width="12"/>'
    s += '<path d="M250 428V305Q250 245 297 245Q344 245 344 305V428" fill="none" stroke="#2A241B" stroke-width="12" stroke-dasharray="1 17" />'
    s += '<path d="M243 428V305Q243 238 297 238Q351 238 351 305V428" fill="none" stroke="#7A6E5A" stroke-width="1.2" opacity=".6"/>'
    s += '<path d="M297 232 l-5 9 h10z" fill="#7A6AA0"/>'
    for i in range(5):
        a = math.pi * (1 + i / 4); s += f'<path d="M{297+40*math.cos(a):.0f} {305+52*math.sin(a):.0f}" />'
    s += '<g fill="none" stroke="#DCD2BA" stroke-width="1" opacity=".5"><path d="M280 300 Q297 270 314 300"/><path d="M276 330 Q297 290 318 330" opacity=".6"/><path d="M285 360 Q297 330 309 360" opacity=".5"/></g>'
    # portal stones (four plinths)
    for i in range(4):
        x = 252 + i * 24
        s += f'<path d="M{x} 428 h20 l3 10 h-26z" fill="#4A4234" stroke="#0A0806" stroke-width=".8"/><path d="M{x+2} 428 h16" stroke="#5FA288" stroke-width="1.6" opacity=".9"/>'
        s += f'<circle cx="{x+10}" cy="433" r="3" fill="#5FA288" opacity=".7"/>'
    # weapon rack
    s += '<g><ellipse cx="430" cy="418" rx="52" ry="5" fill="#000" opacity=".4"/>'
    s += '<rect x="383" y="288" width="7" height="132" fill="url(#woodH)"/><rect x="470" y="288" width="7" height="132" fill="url(#woodH)"/>'
    s += '<rect x="383" y="296" width="94" height="6" fill="url(#wood)"/><rect x="383" y="372" width="94" height="6" fill="url(#wood)"/>'
    # bronze sword
    s += '<path d="M408 300 C402 330 404 355 410 380 C416 355 418 330 412 300Z" fill="#B0703A" stroke="#4A2A12" stroke-width=".8" transform="rotate(0)"/><path d="M410 304 V374" stroke="#E0A468" stroke-width=".8" opacity=".7"/>'
    s += '<rect x="402" y="380" width="16" height="3" fill="#5A3A1E"/><rect x="408" y="383" width="4" height="22" fill="#3A2A1A"/><circle cx="410" cy="407" r="3" fill="#B0703A"/>'
    # flintlock
    s += '<path d="M428 372 L470 366 L470 372 L430 380Z" fill="#2A2A2E" stroke="#0A0A0C" stroke-width=".6"/><path d="M428 376 C420 380 418 392 412 402 L424 398 C426 392 432 386 436 382Z" fill="url(#wood)" stroke="#1A120A" stroke-width=".6" transform="translate(30 -12)"/>'
    s += '<path d="M452 364 l5 -7 l2 3 l-3 5z" fill="#4A4A52"/>'
    # halberd
    s += '<line x1="448" y1="288" x2="448" y2="414" stroke="#3A2A1A" stroke-width="3"/><path d="M448 286 L444 306 L448 312 L452 306Z" fill="#8A8A90"/><path d="M448 296 C458 292 462 300 456 308 L448 304Z" fill="#6A6A72"/>'
    s += '</g>'
    # Market door
    s += '<path d="M495 420V365Q495 340 520 340Q545 340 545 365V420Z" fill="url(#wood)" stroke="#0A0806" stroke-width="2"/>'
    s += '<path d="M489 420V365Q489 334 520 334Q551 334 551 365V420" fill="none" stroke="#6A5E4A" stroke-width="7"/>'
    for x in (505, 520, 535): s += f'<line x1="{x}" y1="343" x2="{x}" y2="420" stroke="#140E08" stroke-width="1"/>'
    s += '<rect x="495" y="362" width="50" height="4" fill="#1E1A16"/><rect x="495" y="392" width="50" height="4" fill="#1E1A16"/><circle cx="535" cy="385" r="2.6" fill="#8A7A5A"/>'
    s += '<rect x="496" y="417" width="48" height="3" fill="#E0A04A" opacity=".55"/>'
    # hearth
    s += '<path d="M570 420V330L580 312H685L695 330V420Z" fill="#3A3228" stroke="#0A0806"/>'
    for r in range(8): s += f'<line x1="570" y1="{330+r*12}" x2="695" y2="{330+r*12}" stroke="#0A0806" stroke-width=".8" opacity=".6"/>'
    s += '<rect x="562" y="324" width="141" height="9" fill="#4E4538" stroke="#0A0806"/>'
    s += '<path d="M592 420V362Q592 346 632 346Q672 346 672 362V420Z" fill="#0A0604"/>'
    s += '<ellipse cx="632" cy="418" rx="42" ry="6" fill="#C4542E" opacity=".8"/>'
    s += '<circle cx="632" cy="396" r="46" fill="url(#gF)"/>'
    s += flame(618, 416, 52, 12, "#E0763A", "#FFD580") + flame(640, 416, 62, 14, "#D8602E", "#FFE0A0") + flame(656, 416, 38, 9, "#E0763A", "#FFD580")
    s += '<line x1="604" y1="412" x2="662" y2="408" stroke="#2A2018" stroke-width="5" stroke-linecap="round"/>'
    # floor loot
    s += coffer(268, 484, 1.1) + candlestick(352, 478, 1.1) + goblet(320, 480, 1.15) + goblet(300, 498, .8)
    s += flame(352, 448, 12, 3, "#E0A04A", "#FFF0B0")
    # table
    s += '<ellipse cx="490" cy="622" rx="230" ry="22" fill="#000" opacity=".45"/>'
    for x in (322, 640): s += f'<rect x="{x}" y="556" width="14" height="60" fill="url(#woodH)" stroke="#140E08" stroke-width=".6"/>'
    for x in (382, 600): s += f'<rect x="{x}" y="548" width="10" height="46" fill="#241A10"/>'
    # strongboxes under table
    for i, (x, n) in enumerate([(350, "I"), (430, "II"), (510, "III"), (590, "IV")]):
        y = 616 - i % 2 * 6
        s += f'<g transform="translate({x} {y})"><ellipse cx="0" cy="3" rx="34" ry="5" fill="#000" opacity=".5"/>'
        s += '<rect x="-30" y="-24" width="60" height="24" fill="url(#wood)" stroke="#140E08"/><path d="M-30 -24 C-30 -38 30 -38 30 -24Z" fill="url(#woodH)" stroke="#140E08"/>'
        s += '<rect x="-22" y="-38" width="6" height="38" fill="#3A3A3E"/><rect x="16" y="-38" width="6" height="38" fill="#3A3A3E"/><rect x="-30" y="-14" width="60" height="4" fill="#2E2E32"/>'
        for rx in (-19, 19): s += f'<circle cx="{rx}" cy="-30" r="1.3" fill="#7A7A82"/><circle cx="{rx}" cy="-6" r="1.3" fill="#7A7A82"/>'
        s += f'<rect x="-8" y="-24" width="16" height="13" rx="2" fill="#46464C" stroke="#0A0A0C" stroke-width=".6"/>{T(0,-14.2,n,"#DCD2BA",9,"middle",w=600)}</g>'
    s += '<polygon points="362,486 618,486 682,548 298,548" fill="url(#counter)" stroke="#140E08"/>'
    s += '<polygon points="298,548 682,548 682,562 298,562" fill="#2A1E12" stroke="#140E08"/>'
    for k in range(1, 6): s += f'<line x1="{362-k*(64/6)*1:.0f}" y1="{486+k*62/6:.0f}" x2="{618+k*(64/6):.0f}" y2="{486+k*62/6:.0f}" stroke="#241A10" stroke-width=".7" opacity=".7"/>'
    s += '<circle cx="490" cy="500" r="130" fill="url(#gA)" opacity=".5"/>'
    # ledger
    s += '<polygon points="388,496 492,493 492,541 372,543" fill="url(#vellum)" stroke="#3A2E1A"/><polygon points="492,493 598,496 614,543 492,541" fill="url(#vellum)" stroke="#3A2E1A"/>'
    s += '<path d="M492 493 V541" stroke="#6A5E44" stroke-width="2"/><polygon points="372,543 614,543 612,548 374,548" fill="#4A2A1A"/>'
    cols = [(388, 440), (440, 492), (492, 546), (546, 600)]
    for i, (a, b) in enumerate(cols):
        if i: s += f'<line x1="{a}" y1="496" x2="{a}" y2="542" stroke="#7A6E54" stroke-width=".8"/>'
        cx = (a + b) / 2 + (0 if i < 2 else 3)
        s += T(cx, 506, ["I","II","III","IV"][i], "#3A2A1A", 10, "middle", font=E, w=700)
        for r in range(4):
            y = 513 + r * 7
            w = 20 + ((i * 7 + r * 5) % 14)
            s += f'<path d="M{a+6} {y} q{w/4} -3 {w/2} 0 t{w/2} 0" fill="none" stroke="#4A3A28" stroke-width=".8" opacity=".75"/>'
    s += '<line x1="372" y1="508" x2="612" y2="508" stroke="#7A6E54" stroke-width=".6" opacity=".7" transform="translate(0 0)"/>'
    # candle on table
    s += '<circle cx="352" cy="470" r="60" fill="url(#gA)"/><ellipse cx="352" cy="518" rx="12" ry="3.5" fill="url(#brass)"/><rect x="348" y="486" width="8" height="32" fill="#DCD2BA"/><rect x="348" y="486" width="3" height="32" fill="#fff" opacity=".25"/>'
    s += flame(352, 485, 17, 4.5, "#E0A04A", "#FFF0B0")
    # century dial
    dx, dy = 800, 535
    s += f'<ellipse cx="{dx}" cy="630" rx="62" ry="12" fill="#000" opacity=".5"/>'
    s += f'<path d="M{dx} 560 L{dx-44} 628 M{dx} 560 L{dx+44} 628 M{dx} 560 L{dx} 634" stroke="url(#brass)" stroke-width="5" stroke-linecap="round"/>'
    s += f'<line x1="{dx}" y1="470" x2="{dx}" y2="560" stroke="#7A5E22" stroke-width="5"/>'
    s += f'<circle cx="{dx}" cy="{dy-18}" r="68" fill="url(#gA)" opacity=".5"/>'
    for rx, ry, rot, bead in [(64, 20, -18, "#E8CE80"), (52, 30, 34, "#C4542E"), (39, 22, -55, "#5FA288"), (26, 14, 12, "#7A6AA0")]:
        s += f'<g transform="rotate({rot} {dx} {dy-18})"><ellipse cx="{dx}" cy="{dy-18}" rx="{rx}" ry="{ry}" fill="none" stroke="#2A1E08" stroke-width="5.5"/><ellipse cx="{dx}" cy="{dy-18}" rx="{rx}" ry="{ry}" fill="none" stroke="url(#brass)" stroke-width="3.6"/><circle cx="{dx+rx}" cy="{dy-18}" r="4.5" fill="{bead}" stroke="#2A1E08" stroke-width=".8"/></g>'
    s += f'<circle cx="{dx}" cy="{dy-18}" r="8" fill="#B6A8E0"/><circle cx="{dx}" cy="{dy-18}" r="3" fill="#fff" opacity=".7"/>'
    s += '</g>'  # end clip group content (vignette next)
    s += '<g clip-path="url(#sc)"><rect x="40" y="120" width="840" height="560" fill="url(#vig)"/></g>'
    s += '<rect x="40" y="120" width="840" height="560" fill="none" stroke="#332D22"/>'
    # callouts
    s += callout(60, 148, "Portal arch", ["the haul lands here,", "as objects"], 140, 180, 288, 290)
    s += callout(300, 148, "Ledger", ["one column per wizard (#130)"], 380, 180, 440, 520)
    s += callout(490, 148, "Weapon rack", ["carry arms between Ages"], 520, 180, 418, 330)
    s += callout(690, 148, "Market door", ["toward the Market"], 720, 178, 530, 372)
    s += callout(905, 148, "Hearth", ["the one fire"], 903, 160, 650, 372)
    s += callout(905, 236, "Century dial", ["turn to choose the Age"], 903, 250, 840, 510)
    s += callout(60, 520, "Ready up", ["all four hands on", "the portal stones", "→ 3-count"], 150, 556, 261, 434, "#5FA288")
    s += callout(330, 700, "Strongboxes", ["coins in are banked"], 410, 690, 452, 602)
    s = s.replace('<g clip-path="url(#sc)"><rect x="40" y="120"', '<g clip-path="url(#sc)"><rect x="40" y="120"', 1)
    # plan inset
    x0, y0, w, h = 910, 540, 240, 150
    s += T(x0, y0 - 10, "PLAN · TOP-DOWN · NOT TO SCALE", "#635C4C", 10, ls=1)
    s += f'<rect x="{x0}" y="{y0}" width="{w}" height="{h}" fill="#1A160F" stroke="#7A6E5A" stroke-width="3"/>'
    for i in range(1, 8): s += f'<line x1="{x0+i*30}" y1="{y0}" x2="{x0+i*30}" y2="{y0+h}" stroke="#2A241A" stroke-width=".6"/>'
    for i in range(1, 5): s += f'<line x1="{x0}" y1="{y0+i*30}" x2="{x0+w}" y2="{y0+i*30}" stroke="#2A241A" stroke-width=".6"/>'
    s += f'<rect x="{x0+4}" y="{y0+60}" width="14" height="60" fill="#7A6AA0" opacity=".85"/>'
    for i in range(4): s += f'<circle cx="{x0+11}" cy="{y0+68+i*14}" r="2.6" fill="#5FA288"/>'
    s += T(x0 + 24, y0 + 94, "PORTAL", "#B6A8E0", 9)
    s += f'<rect x="{x0+40}" y="{y0+4}" width="44" height="10" fill="#6A5236"/>' + T(x0 + 40, y0 + 28, "RACK", "#B4AA90", 9)
    s += f'<rect x="{x0+104}" y="{y0}" width="30" height="5" fill="#140E08"/><rect x="{x0+104}" y="{y0+1}" width="30" height="3" fill="#E0A04A"/>' + T(x0 + 104, y0 + 28, "DOOR", "#B4AA90", 9)
    s += f'<rect x="{x0+176}" y="{y0+4}" width="56" height="20" fill="#4E4538"/><rect x="{x0+186}" y="{y0+8}" width="36" height="12" fill="#C4542E"/>' + T(x0 + 192, y0 + 40, "HEARTH", "#E0A070", 9)
    s += f'<rect x="{x0+86}" y="{y0+62}" width="92" height="30" fill="url(#counter)" stroke="#140E08"/>' + T(x0 + 132, y0 + 81, "LEDGER TABLE", "#DCD2BA", 9, "middle")
    for i in range(4): s += f'<rect x="{x0+90+i*22}" y="{y0+104}" width="16" height="12" fill="#3A3A3E" stroke="#0A0A0C"/>'
    s += T(x0 + 86, y0 + 140, "STRONGBOXES ×4", "#B4AA90", 9)
    s += f'<circle cx="{x0+208}" cy="{y0+100}" r="22" fill="none" stroke="#A9812F" stroke-width="2.4"/><circle cx="{x0+208}" cy="{y0+100}" r="14" fill="none" stroke="#A9812F" stroke-width="2"/><circle cx="{x0+208}" cy="{y0+100}" r="6" fill="#B6A8E0"/>' + T(x0 + 208, y0 + 140, "DIAL", "#B4AA90", 9, "middle")
    s += pal(PAL) + '</svg>'
    open("docs/art/concept/lair/lair.svg", "w", encoding="utf-8").write(s)

# =============== SHEET 2: THE MARKET
def qpt(p0, p1, p2, t):
    return ((1-t)**2*p0[0]+2*(1-t)*t*p1[0]+t*t*p2[0], (1-t)**2*p0[1]+2*(1-t)*t*p1[1]+t*t*p2[1])

def lantern(x, y, r=46):
    return (f'<circle cx="{x:.1f}" cy="{y+8:.1f}" r="{r}" fill="url(#gA)"/><line x1="{x:.1f}" y1="{y-6:.1f}" x2="{x:.1f}" y2="{y:.1f}" stroke="#1A1610"/>'
            f'<rect x="{x-4:.1f}" y="{y:.1f}" width="8" height="12" rx="1.5" fill="#F0B050" stroke="#2A1E0C" stroke-width="1"/><rect x="{x-2:.1f}" y="{y+2:.1f}" width="4" height="8" fill="#FFF0B0"/>')

def slate(x, y, l1, l2, post_h=44):
    return (f'<rect x="{x-2}" y="{y+36}" width="4" height="{post_h}" fill="url(#woodH)"/><ellipse cx="{x}" cy="{y+36+post_h}" rx="9" ry="2.5" fill="#000" opacity=".4"/>'
            f'<rect x="{x-52}" y="{y}" width="104" height="38" rx="2" fill="#26231D" stroke="#5A4A32" stroke-width="3"/>'
            f'<rect x="{x-48}" y="{y+4}" width="96" height="30" fill="none" stroke="#DCD2BA" stroke-width=".5" opacity=".25"/>'
            + T(x, y+17, l1, "#DCD2BA", 9, "middle", extra='opacity=".92"') + T(x, y+30, l2, "#DCD2BA", 9, "middle", extra='opacity=".92"'))

def stripes(x0, x1, y0, y1, c1, c2, n=8):
    s = ''; w = (x1 - x0) / n
    for i in range(n):
        s += f'<polygon points="{x0+i*w:.1f},{y0} {x0+(i+1)*w:.1f},{y0} {x0+(i+1)*w+3:.1f},{y1} {x0+i*w-3:.1f},{y1}" fill="{c1 if i%2==0 else c2}"/>'
        s += f'<path d="M{x0+i*w-3:.1f} {y1} q{w/2+1.5:.1f} 9 {w+3:.1f} 0" fill="{c1 if i%2==0 else c2}"/>'
    return s

def stall_goldsmith(cx, gy):
    s = f'<ellipse cx="{cx}" cy="{gy+4}" rx="110" ry="12" fill="#000" opacity=".45"/>'
    s += f'<rect x="{cx-82}" y="{gy-100}" width="164" height="70" fill="#1E150C"/><circle cx="{cx}" cy="{gy-62}" r="92" fill="url(#gA)"/>'
    s += f'<rect x="{cx-82}" y="{gy-88}" width="164" height="3" fill="#5A442C"/>'
    for i in range(4): s += f'<rect x="{cx-70+i*38}" y="{gy-85}" width="26" height="10" rx="1" fill="url(#brass)" opacity=".8"/>'
    for x in (cx-84, cx+84): s += f'<rect x="{x-3}" y="{gy-138}" width="6" height="{138}" fill="url(#woodH)"/>'
    s += stripes(cx-98, cx+98, gy-138, gy-100, "#8C8068", "#3A3228")
    s += f'<polygon points="{cx-98},{gy-138} {cx+98},{gy-138} {cx+88},{gy-150} {cx-88},{gy-150}" fill="#4A3F30"/>'
    # counter
    s += f'<polygon points="{cx-95},{gy-34} {cx+95},{gy-34} {cx+102},{gy-24} {cx-102},{gy-24}" fill="url(#counter)" stroke="#140E08"/><rect x="{cx-102}" y="{gy-24}" width="204" height="24" fill="#2E2014" stroke="#140E08"/>'
    for i in range(1, 6): s += f'<line x1="{cx-102+i*34}" y1="{gy-24}" x2="{cx-102+i*34}" y2="{gy}" stroke="#140E08" stroke-width=".8"/>'
    # balance scales
    bx, by = cx - 24, gy - 34
    s += f'<rect x="{bx-8}" y="{by-3}" width="16" height="3" fill="url(#brass)"/><line x1="{bx}" y1="{by-3}" x2="{bx}" y2="{by-50}" stroke="url(#brass)" stroke-width="3"/>'
    s += f'<line x1="{bx-34}" y1="{by-49}" x2="{bx+34}" y2="{by-53}" stroke="url(#brass)" stroke-width="3" stroke-linecap="round"/><circle cx="{bx}" cy="{by-51}" r="3.5" fill="#E2BE6A"/>'
    for sx, sy in ((bx-34, by-49), (bx+34, by-53)):
        s += f'<path d="M{sx} {sy} L{sx-14} {sy+27} M{sx} {sy} L{sx+14} {sy+27}" stroke="#A9812F" stroke-width=".9" fill="none"/>'
        s += f'<path d="M{sx-16} {sy+27} H{sx+16} C{sx+13} {sy+34} {sx-13} {sy+34} {sx-16} {sy+27}Z" fill="url(#brass)" stroke="#3A2A0C" stroke-width=".6"/>'
    s += f'<circle cx="{bx-34}" cy="{by-25}" r="4" fill="#C9A227"/><circle cx="{bx-29}" cy="{by-24}" r="3.4" fill="#F0D875"/>'
    s += f'<rect x="{cx+30}" y="{gy-42}" width="22" height="8" fill="url(#gold)"/><rect x="{cx+56}" y="{gy-40}" width="18" height="6" fill="#C9A227"/>'
    # anvil + forge ember
    ax, ay = cx + 128, gy
    s += f'<ellipse cx="{ax}" cy="{ay+2}" rx="26" ry="5" fill="#000" opacity=".5"/><rect x="{ax-10}" y="{ay-18}" width="20" height="18" fill="url(#woodH)"/>'
    s += f'<path d="M{ax-28} {ay-34} H{ax+22} C{ax+34} {ay-32} {ax+34} {ay-30} {ax+24} {ay-26} H{ax+12} L{ax+14} {ay-18} H{ax-14} L{ax-12} {ay-26} H{ax-22} C{ax-28} {ay-27} {ax-30} {ay-30} {ax-28} {ay-34}Z" fill="#4A4A52" stroke="#0A0A0C"/>'
    s += f'<path d="M{ax-24} {ay-33} H{ax+20}" stroke="#9A9AA6" stroke-width="1.4"/>'
    s += f'<circle cx="{ax-4}" cy="{ay-36}" r="8" fill="url(#gF)"/><circle cx="{ax-4}" cy="{ay-36}" r="2" fill="#F0A050"/>'
    s += slate(cx - 170, gy - 74, "SILVER ×1.3", "GILT PLATE")
    return s

def stall_pardoner(cx, gy):
    s = f'<ellipse cx="{cx}" cy="{gy+4}" rx="100" ry="12" fill="#000" opacity=".45"/>'
    s += f'<rect x="{cx-76}" y="{gy-108}" width="152" height="80" fill="#1A1410"/><circle cx="{cx}" cy="{gy-70}" r="90" fill="url(#gA)" opacity=".8"/>'
    for x in (cx-80, cx+80): s += f'<rect x="{x-3}" y="{gy-120}" width="6" height="120" fill="url(#woodH)"/>'
    s += f'<polygon points="{cx-96},{gy-114} {cx},{gy-160} {cx+96},{gy-114}" fill="#2E2620" stroke="#0E0C09"/>'
    for i in range(7): s += f'<line x1="{cx-96+i*16}" y1="{gy-114}" x2="{cx}" y2="{gy-160}" stroke="#14100C" stroke-width=".8" opacity=".6"/>'
    # cross on the gable
    s += f'<rect x="{cx-2.5}" y="{gy-186}" width="5" height="30" fill="#DCD2BA"/><rect x="{cx-10}" y="{gy-178}" width="20" height="5" fill="#DCD2BA"/>'
    s += f'<rect x="{cx-90}" y="{gy-114}" width="180" height="5" fill="#4A3F30"/>'
    # hanging relics
    for i, x in enumerate((cx-62, cx-30, cx+2, cx+34, cx+64)):
        ln = 20 + (i * 9 % 16)
        s += f'<line x1="{x}" y1="{gy-109}" x2="{x}" y2="{gy-109+ln}" stroke="#7A6E54" stroke-width=".9"/>'
        y = gy - 109 + ln
        if i % 3 == 0: s += f'<path d="M{x-6} {y} h12 v12 h-12z M{x-8} {y} l8 -7 l8 7" fill="#C9A227" stroke="#5E4A10" stroke-width=".6"/>'
        elif i % 3 == 1: s += f'<rect x="{x-6}" y="{y}" width="12" height="16" rx="2" fill="#3A3A3E" stroke="#8A8A92" stroke-width=".8"/><circle cx="{x}" cy="{y+8}" r="3" fill="#C4542E" opacity=".85"/>'
        else: s += f'<path d="M{x-2} {y} h4 v16 h-4z M{x-8} {y+5} h16 v4 h-16z" fill="#DCD2BA"/>'
    # counter + candles
    s += f'<polygon points="{cx-88},{gy-34} {cx+88},{gy-34} {cx+95},{gy-24} {cx-95},{gy-24}" fill="url(#counter)" stroke="#140E08"/><rect x="{cx-95}" y="{gy-24}" width="190" height="24" fill="#2E2014" stroke="#140E08"/>'
    s += f'<rect x="{cx-16}" y="{gy-20}" width="32" height="14" fill="none" stroke="#DCD2BA" stroke-width=".8" opacity=".5"/><rect x="{cx-2}" y="{gy-20}" width="4" height="14" fill="#DCD2BA" opacity=".5"/>'
    for i, x in enumerate((cx-62, cx-48, cx+44, cx+60, cx+74)):
        hh = 14 + i * 3 % 9
        s += f'<circle cx="{x}" cy="{gy-48}" r="22" fill="url(#gA)" opacity=".7"/><rect x="{x-3}" y="{gy-34-hh}" width="6" height="{hh}" fill="#DCD2BA"/>' + flame(x, gy-34-hh, 9, 2.6, "#E0A04A", "#FFF0B0")
    s += f'<path d="M{cx-22} {gy-34} h44 v-8 h-44z" fill="#2A2A30" stroke="#8A8A92" stroke-width=".6"/><rect x="{cx-14}" y="{gy-42}" width="28" height="4" fill="#7A6AA0" opacity=".5"/>'
    s += slate(cx + 134, gy - 74, "PSALTERS, INTACT", "CHURCH PLATE")
    return s

def stall_fence(cx, gy):
    s = f'<ellipse cx="{cx}" cy="{gy+4}" rx="100" ry="12" fill="#000" opacity=".45"/>'
    s += f'<circle cx="{cx}" cy="{gy-50}" r="100" fill="url(#gA)" opacity=".45"/>'
    # cart body
    s += f'<polygon points="{cx-70},{gy-48} {cx+60},{gy-48} {cx+52},{gy-16} {cx-62},{gy-16}" fill="url(#wood)" stroke="#140E08"/>'
    for i in range(1, 5): s += f'<line x1="{cx-70+i*26}" y1="{gy-48}" x2="{cx-62+i*23}" y2="{gy-16}" stroke="#140E08" stroke-width=".8"/>'
    # handles
    s += f'<line x1="{cx-70}" y1="{gy-40}" x2="{cx-120}" y2="{gy-10}" stroke="#4A3826" stroke-width="5" stroke-linecap="round"/><line x1="{cx-120}" y1="{gy-10}" x2="{cx-120}" y2="{gy+2}" stroke="#3A2A1A" stroke-width="4"/>'
    # junk
    s += f'<ellipse cx="{cx-46}" cy="{gy-56}" rx="12" ry="9" fill="#6A5A44"/><path d="M{cx-12} {gy-48} v-20 h14 v20z" fill="#5A4A38" stroke="#140E08" stroke-width=".6"/><path d="M{cx+20} {gy-48} c-2 -22 24 -22 22 0z" fill="#7A4A2A" stroke="#140E08" stroke-width=".6"/>'
    s += f'<line x1="{cx+36}" y1="{gy-64}" x2="{cx+60}" y2="{gy-96}" stroke="#8A8A92" stroke-width="2"/><path d="M{cx-30} {gy-48} q4 -28 10 -30" stroke="#6A6A72" stroke-width="3" fill="none"/>'
    # tarp
    s += f'<path d="M{cx-72} {gy-96} L{cx+30} {gy-110} L{cx+78} {gy-70} L{cx+64} {gy-44} Q{cx+40} {gy-60} {cx+26} {gy-46} Q{cx} {gy-62} {cx-18} {gy-46} Q{cx-40} {gy-62} {cx-62} {gy-50}Z" fill="#4A4A3A" stroke="#14120E"/>'
    for i in range(5): s += f'<path d="M{cx-60+i*30} {gy-98+i*(-3)} Q{cx-52+i*30} {gy-72} {cx-58+i*28} {gy-52}" fill="none" stroke="#2A2A20" stroke-width="1.1" opacity=".8"/>'
    s += f'<line x1="{cx-72}" y1="{gy-96}" x2="{cx-72}" y2="{gy-48}" stroke="#4A3826" stroke-width="4"/>'
    # wheel
    wx, wy = cx - 6, gy - 12
    s += f'<circle cx="{wx}" cy="{wy}" r="18" fill="none" stroke="#2A1E12" stroke-width="5"/><circle cx="{wx}" cy="{wy}" r="18" fill="none" stroke="#5A442C" stroke-width="2.5"/>'
    for i in range(8):
        a = i * math.pi / 4; s += f'<line x1="{wx}" y1="{wy}" x2="{wx+17*math.cos(a):.1f}" y2="{wy+17*math.sin(a):.1f}" stroke="#4A3826" stroke-width="1.6"/>'
    s += f'<circle cx="{wx}" cy="{wy}" r="3.5" fill="#6A6A72"/>'
    s += lantern(cx - 96, gy - 52, 36)
    s += slate(cx + 140, gy - 74, "ANYTHING", "PAID AT ONCE")
    return s

def stall_antiq(cx, gy):
    s = f'<ellipse cx="{cx}" cy="{gy+4}" rx="90" ry="12" fill="#000" opacity=".45"/><circle cx="{cx}" cy="{gy-80}" r="110" fill="url(#gA)" opacity=".5"/>'
    s += f'<rect x="{cx-64}" y="{gy-150}" width="128" height="150" fill="url(#woodH)" stroke="#140E08" stroke-width="1.5"/>'
    s += f'<path d="M{cx-72} {gy-150} H{cx+72} L{cx+62} {gy-164} H{cx-62}Z" fill="#4A3826" stroke="#140E08"/>'
    s += f'<rect x="{cx-56}" y="{gy-142}" width="112" height="116" fill="#17110A" stroke="#A9812F" stroke-width="1.2"/>'
    for y in (gy-104, gy-66): s += f'<rect x="{cx-56}" y="{y}" width="112" height="3" fill="#5A442C"/>'
    s += f'<rect x="{cx-56}" y="{gy-142}" width="112" height="116" fill="#7A6AA0" opacity=".08"/><line x1="{cx-50}" y1="{gy-138}" x2="{cx-30}" y2="{gy-118}" stroke="#fff" opacity=".18" stroke-width="2"/>'
    # top shelf: globe + hourglass
    s += f'<circle cx="{cx-28}" cy="{gy-122}" r="14" fill="#2A4A52" stroke="#A9812F" stroke-width="1.4"/><path d="M{cx-38} {gy-126} q8 -6 14 2 M{cx-30} {gy-116} q6 -3 10 3" stroke="#5FA288" fill="none" stroke-width="1.2"/><path d="M{cx-34} {gy-106} h12 l-3 -4 h-6z" fill="url(#brass)"/>'
    s += f'<path d="M{cx+14} {gy-136} h20 l-8 14 l8 14 h-20 l8 -14z" fill="none" stroke="#C0B090" stroke-width="1.4"/><path d="M{cx+18} {gy-110} h12 l-6 -6z" fill="#C0A860"/>'
    # mid shelf: curved sword + flintlock + skull
    s += f'<path d="M{cx-50} {gy-84} Q{cx-10} {gy-98} {cx+10} {gy-70}" stroke="#9A9AA6" stroke-width="2.4" fill="none"/><rect x="{cx-54}" y="{gy-90}" width="8" height="10" fill="#6A4A2A" transform="rotate(-12 {cx-50} {gy-85})"/>'
    s += f'<path d="M{cx+14} {gy-78} h28 v4 h-26z" fill="#3A3A40"/><path d="M{cx+14} {gy-76} q-6 4 -4 12 l8 -2z" fill="#5A442C"/><circle cx="{cx+46}" cy="{gy-82}" r="6" fill="#DCD2BA"/>'
    # lower shelf
    s += f'<path d="M{cx-48} {gy-32} h30 l-3 -24 h-24z" fill="#6A5A44" opacity=".85"/><path d="M{cx-10} {gy-32} l10 -26 l12 26z" fill="#4A4A52"/><circle cx="{cx+30}" cy="{gy-40}" r="9" fill="#8A6A3A"/><path d="M{cx+22} {gy-40} h16" stroke="#3A2A12"/>'
    s += f'<rect x="{cx-64}" y="{gy-20}" width="128" height="20" fill="#2E2014" stroke="#140E08"/><circle cx="{cx}" cy="{gy-10}" r="3" fill="#A9812F"/>'
    s += lantern(cx + 78, gy - 120, 34)
    s += slate(cx - 140, gy - 74, "OUT OF ITS AGE", "GOOD CONDITION")
    return s

def wizard(x, y, k=1.5):
    s = f'<g transform="translate({x} {y}) scale({k})"><ellipse cx="0" cy="1" rx="22" ry="4" fill="#000" opacity=".5"/>'
    s += '<ellipse cx="-30" cy="1" rx="12" ry="2.5" fill="#E0A04A" opacity=".25"/>'
    s += '<path d="M-17 0 L-11 -48 Q-2 -56 8 -50 L16 0Z" fill="#2A2240" stroke="#7A6AA0" stroke-width=".8"/>'
    s += '<path d="M-11 -48 Q-2 -56 8 -50" stroke="#B6A8E0" stroke-width="1.2" fill="none" opacity=".8"/><path d="M-3 -46 V0" stroke="#14101C" stroke-width="1"/>'
    s += '<circle cx="-2" cy="-58" r="7" fill="#14101C"/><path d="M-12 -63 L-30 -92 Q-10 -88 10 -63Z" fill="#2A2240" stroke="#7A6AA0" stroke-width=".6" transform="rotate(-8 -2 -63)"/>'
    s += '<ellipse cx="-2" cy="-63" rx="14" ry="3.4" fill="#201A30" stroke="#7A6AA0" stroke-width=".6"/>'
    s += '<path d="M-8 -46 Q-20 -40 -29 -30" stroke="#2A2240" stroke-width="7" fill="none" stroke-linecap="round"/><path d="M-8 -46 Q-20 -40 -29 -30" stroke="#7A6AA0" stroke-width=".8" fill="none" opacity=".6"/>'
    s += '<circle cx="-30" cy="-29" r="3.4" fill="#6A5242"/>' + goblet(-30, -26, .7).replace('<ellipse cx="0" cy="2" rx="12" ry="3" fill="#000" opacity=".45"/>', '')
    s += '<path d="M8 -46 Q14 -38 12 -26" stroke="#2A2240" stroke-width="6" fill="none" stroke-linecap="round"/></g>'
    return s

def market():
    random.seed(3)
    s = base("THE MARKET · BETWEEN THE AGES", "The Market",
             T(1160,54,"a lantern-lit yard · one well · four stalls","#9A9078",12,"end") + T(1160,76,"each stall chalks what it wants tonight","#9A9078",12,"end"))
    s += DEFS
    S0, S1 = 120, 650
    s += f'<clipPath id="ms"><rect x="40" y="{S0}" width="1120" height="{S1-S0}"/></clipPath><g clip-path="url(#ms)">'
    s += f'<rect x="40" y="{S0}" width="1120" height="{S1-S0}" fill="url(#sky)"/>'
    for _ in range(40): s += f'<circle cx="{random.uniform(50,1150):.0f}" cy="{random.uniform(125,230):.0f}" r="{random.choice([.7,1,1.3])}" fill="#DCD2BA" opacity="{random.uniform(.25,.7):.2f}"/>'
    s += '<circle cx="1000" cy="168" r="14" fill="#DCD2BA" opacity=".85"/><circle cx="1006" cy="164" r="12" fill="#0E0D14"/>'
    # skyline silhouettes: four Ages
    sil = '#120F0B'
    s += f'<path d="M60 340 V250 h12 v-10 h10 v10 h12 v-10 h10 v10 h12 V340Z" fill="{sil}"/><rect x="88" y="276" width="5" height="9" fill="#E0A04A" opacity=".8"/>'
    s += f'<path d="M240 340 V300 L275 262 L310 300 V340Z M310 340 V310 L340 284 L370 310 V340Z" fill="{sil}"/><rect x="268" y="304" width="5" height="8" fill="#E0A04A" opacity=".8"/><rect x="332" y="316" width="5" height="8" fill="#E0A04A" opacity=".7"/>'
    s += f'<path d="M520 340 V280 h16 V250 h12 V280 h16 V340Z" fill="{sil}"/><path d="M880 340 V300 h30 V220 L895 205 L910 220 V300 h30 V340Z" fill="{sil}"/>'
    s += f'<path d="M700 340 V290 h60 V270 h10 V340Z" fill="{sil}"/><path d="M1040 340 V270 h18 V250 h8 V270 h18 V340Z" fill="{sil}"/><rect x="1056" y="290" width="5" height="9" fill="#E0A04A" opacity=".7"/>'
    s += f'<rect x="40" y="338" width="1120" height="3" fill="#0C0A07"/>'
    # ground
    s += f'<rect x="40" y="340" width="1120" height="{S1-340}" fill="url(#ground)"/>'
    s += '<ellipse cx="600" cy="480" rx="420" ry="130" fill="#3A3226" opacity=".35"/><ellipse cx="600" cy="480" rx="330" ry="95" fill="#4A4032" opacity=".25"/>'
    for r in range(14):
        y = 345 + (r / 14) ** 1.6 * 305; sp = 22 + r * 5
        for x in range(int(40 - (r * 7) % sp), 1160, int(sp)):
            s += f'<path d="M{x} {y:.0f} q{sp/2:.0f} -{2+r*.3:.1f} {sp:.0f} 0" fill="none" stroke="#0C0A07" stroke-width=".9" opacity=".6"/>'
    # back stalls
    s += stall_goldsmith(430, 395) + stall_pardoner(770, 395)
    # well
    wx, wy = 600, 470
    s += f'<ellipse cx="{wx}" cy="{wy+16}" rx="64" ry="14" fill="#000" opacity=".5"/><circle cx="{wx}" cy="{wy-10}" r="130" fill="url(#gA)" opacity=".5"/>'
    s += f'<path d="M{wx-52} {wy-30} V{wy+10} A52 14 0 0 0 {wx+52} {wy+10} V{wy-30}Z" fill="#4A4234" stroke="#0E0C09"/>'
    for r in range(3): s += f'<path d="M{wx-52} {wy-20+r*12} A52 14 0 0 0 {wx+52} {wy-20+r*12}" fill="none" stroke="#14100C" stroke-width=".9"/>'
    for x in (-36, -14, 10, 32): s += f'<line x1="{wx+x}" y1="{wy-18+(x%3)*3}" x2="{wx+x}" y2="{wy-5+(x%3)*3}" stroke="#14100C" stroke-width=".8" opacity=".7" />'
    s += f'<ellipse cx="{wx}" cy="{wy-30}" rx="52" ry="14" fill="#5A5040" stroke="#0E0C09"/><ellipse cx="{wx}" cy="{wy-30}" rx="40" ry="9.5" fill="#05060A"/><ellipse cx="{wx}" cy="{wy-29}" rx="30" ry="6" fill="#7A6AA0" opacity=".35"/>'
    s += f'<rect x="{wx-50}" y="{wy-92}" width="5" height="62" fill="url(#woodH)"/><rect x="{wx+45}" y="{wy-92}" width="5" height="62" fill="url(#woodH)"/><rect x="{wx-52}" y="{wy-96}" width="104" height="6" fill="url(#wood)"/>'
    s += f'<line x1="{wx}" y1="{wy-90}" x2="{wx}" y2="{wy-44}" stroke="#8A7A5A"/><path d="M{wx-6} {wy-44} h12 l-2 9 h-8z" fill="#5A442C" stroke="#14100C" stroke-width=".6"/>'
    s += lantern(wx, wy - 96, 54)
    # front-side stalls
    s += stall_fence(190, 505) + stall_antiq(1030, 505)
    # lantern strings
    for p0, p1, p2 in (((40, 205), (330, 285), (600, 215)), ((600, 215), (880, 285), (1160, 205))):
        s += f'<path d="M{p0[0]} {p0[1]} Q{p1[0]} {p1[1]} {p2[0]} {p2[1]}" fill="none" stroke="#1A1610" stroke-width="1.6"/>'
        for t in (.12, .3, .5, .7, .88):
            x, y = qpt(p0, p1, p2, t); s += lantern(x, y + 2, 40)
    # wizard
    s += wizard(560, 628, 1.7)
    s += '</g>'
    s += f'<rect x="40" y="{S0}" width="1120" height="{S1-S0}" fill="none" stroke="#332D22"/>'
    s += f'<g clip-path="url(#ms)"><rect x="40" y="{S0}" width="1120" height="{S1-S0}" fill="url(#vig2)"/></g>'
    # callouts
    s += callout(290, 150, "THE GOLDSMITH", ["buys metal · slow and exact,", "pays by weight, dents matter little"], 430, 192, 430, 262, "#DCD2BA")
    s += callout(690, 150, "THE PARDONER", ["buys relics, church plate, psalters", "pious until money is mentioned"], 770, 192, 770, 235, "#DCD2BA")
    s += callout(60, 560, "THE FENCE", ["buys anything · impatient,", "barely haggles, pays at once"], 150, 556, 170, 506)
    s += callout(1140, 560, "THE ANTIQUARIAN", ["buys curios, arms from other Ages", "patient, fickle, a collector"], 1040, 556, 1030, 470, anchor="end")
    s += callout(640, 600, "Heavy goblet, carried by hand", ["no teleport, no inventory screen"], 636, 604, 545, 570, "#5FA288")
    s += T(40, 676, "You carry the loot here by hand — it weighs what it weighed in the castle.", "#DCD2BA", 13)
    s += T(40, 696, "Slate boards: what each stall wants tonight. A wanted piece raises the vendor's limit; the Fence's price is the baseline.", "#9A9078", 10.5)
    s += pal(PAL) + '</svg>'
    open("docs/art/concept/lair/market.svg", "w", encoding="utf-8").write(s)

# =============== SHEET 3: ONE HAGGLE
PW, PH, PY = 270, 320, 120
def px(i): return 40 + i * (1120 - PW) / 3

def smithy(cx, mood, lean=0, smoke=1):
    """goldsmith behind the counter, counter top at local y=235. mood: peer, talk, tense, calm"""
    s = ''
    s += f'<path d="M{cx-44} 236 Q{cx-40} 188 {cx-14+lean} 182 H{cx+14+lean} Q{cx+40} 188 {cx+44} 236Z" fill="#3A2E24" stroke="#140E08"/>'
    s += f'<path d="M{cx-26} 236 L{cx-20} 192 H{cx+20} L{cx+26} 236Z" fill="#6A4A2A" stroke="#2A1A0C"/><path d="M{cx-18} 200 H{cx+18}" stroke="#2A1A0C" stroke-width="1.2"/><rect x="{cx-10}" y="212" width="20" height="10" rx="2" fill="#4A321C" stroke="#2A1A0C" stroke-width=".8"/>'
    hx = cx + lean
    s += f'<rect x="{hx-5}" y="176" width="10" height="10" fill="#6A5242"/>'
    s += f'<circle cx="{hx}" cy="158" r="21" fill="url(#skin)" stroke="#2A1A12"/>'
    s += f'<path d="M{hx-22} 156 Q{hx-24} 190 {hx} 196 Q{hx+24} 190 {hx+22} 156 Q{hx+10} 176 {hx} 176 Q{hx-10} 176 {hx-22} 156Z" fill="#8C8070" stroke="#3A342A" stroke-width=".8"/>'
    s += f'<path d="M{hx-22} 148 Q{hx} 114 {hx+22} 148 Q{hx} 138 {hx-22} 148Z" fill="#241C14" stroke="#0E0A06"/>'
    ey = 154
    if mood == "peer":
        s += f'<path d="M{hx-13} {ey-5} l9 3 M{hx+13} {ey-5} l-9 3" stroke="#3A342A" stroke-width="2.4"/><path d="M{hx-12} {ey} h7 M{hx+5} {ey} h7" stroke="#0E0A06" stroke-width="2"/>'
    elif mood == "tense":
        s += f'<path d="M{hx-13} {ey-6} l9 4 M{hx+13} {ey-6} l-9 4" stroke="#3A342A" stroke-width="2.6"/><path d="M{hx-12} {ey+1} h7 M{hx+5} {ey+1} h7" stroke="#0E0A06" stroke-width="2"/><circle cx="{hx-8}" cy="{ey+1}" r="1" fill="#DCD2BA"/>'
    else:
        s += f'<path d="M{hx-13} {ey-6} q5 -3 9 0 M{hx+13} {ey-6} q-5 -3 -9 0" stroke="#3A342A" stroke-width="2.2" fill="none"/><circle cx="{hx-8}" cy="{ey}" r="2" fill="#0E0A06"/><circle cx="{hx+8}" cy="{ey}" r="2" fill="#0E0A06"/>'
    s += f'<path d="M{hx-4} {ey+4} q-2 8 4 8" stroke="#2A1A12" fill="none" stroke-width="1.4"/>'
    # pipe
    s += f'<path d="M{hx-14} 168 H{hx-34}" stroke="#2A1A0C" stroke-width="3.2" stroke-linecap="round"/><path d="M{hx-36} 161 h10 v12 h-10z" fill="#4A321C" stroke="#14100C"/><ellipse cx="{hx-31}" cy="161" rx="5" ry="1.8" fill="#C4542E"/>'
    # smoke
    for i in range(smoke):
        o = i * 7
        s += f'<path d="M{hx-31+o*.3} 158 C{hx-44-o} 140 {hx-18+o} 128 {hx-34-o} 108 C{hx-46-o} 92 {hx-26} 84 {hx-36-o} 66" fill="none" stroke="#B4AA90" stroke-width="{3+smoke*.9:.1f}" stroke-linecap="round" opacity="{.5-i*.08:.2f}"/>'
    return s

def panel_bg(i, title_n):
    x = px(i)
    s = f'<clipPath id="pc{i}"><rect x="{x:.1f}" y="{PY}" width="{PW}" height="{PH}"/></clipPath><g clip-path="url(#pc{i})"><g transform="translate({x:.1f} {PY})">'
    s += f'<rect width="{PW}" height="{PH}" fill="#1A140C"/>'
    for r in range(12): s += f'<line x1="0" y1="{r*20}" x2="{PW}" y2="{r*20}" stroke="#0E0A06" stroke-width=".9" opacity=".7"/>'
    s += f'<circle cx="190" cy="140" r="170" fill="url(#gA)"/>'
    # shelf with brass things
    s += '<rect x="20" y="86" width="230" height="5" fill="url(#wood)"/>'
    for k in range(5): s += f'<rect x="{34+k*44}" y="{70 if k%2 else 74}" width="{16 if k%2 else 22}" height="{16 if k%2 else 12}" rx="2" fill="url(#brass)" opacity=".55"/>'
    return s

def panel_counter(extra=''):
    s = f'<polygon points="0,236 {PW},236 {PW},256 0,256" fill="url(#counter)" stroke="#140E08"/><rect x="0" y="256" width="{PW}" height="{PH-256}" fill="#2A1E12"/>'
    for k in range(1, 6): s += f'<line x1="{k*45}" y1="256" x2="{k*45}" y2="{PH}" stroke="#140E08" stroke-width=".9"/>'
    s += '<line x1="0" y1="262" x2="270" y2="262" stroke="#140E08" stroke-width=".6" opacity=".6"/>'
    return s

def panel_end(i, n):
    return (f'</g></g><rect x="{px(i):.1f}" y="{PY}" width="{PW}" height="{PH}" fill="none" stroke="#635C4C" stroke-width="1.2"/>'
            f'<circle cx="{px(i)+16:.1f}" cy="{PY+16}" r="9" fill="#14120E" stroke="#DCD2BA"/>' + T(px(i)+16, PY+19.5, str(n), "#DCD2BA", 10, "middle", w=600))

def coin(x, y, r=9):
    return f'<ellipse cx="{x}" cy="{y}" rx="{r}" ry="{r*.38:.1f}" fill="#7A5F12"/><ellipse cx="{x}" cy="{y-2}" rx="{r}" ry="{r*.38:.1f}" fill="url(#gold)" stroke="#5E4A10" stroke-width=".6"/><ellipse cx="{x}" cy="{y-2}" rx="{r*.55:.1f}" ry="{r*.2:.1f}" fill="none" stroke="#8A6C14" stroke-width=".6"/>'

def bubble(x, y, w, lines):
    h = 14 * len(lines) + 12
    s = f'<path d="M{x} {y} h{w} v{h} h-{w-40} l-12 14 l2 -14 h-{30} z" fill="#DCD2BA" stroke="#14120E"/>'
    for i, l in enumerate(lines): s += T(x + 10, y + 18 + i * 14, l, "#1E1A14", 10.5, extra='font-style="italic"')
    return s

def pipes_row(x, y):
    s = ''
    for i in range(3):
        px_ = x + i * 34; c = "#DCD2BA" if i < 2 else "#4A4438"
        s += f'<path d="M{px_} {y} h20" stroke="{c}" stroke-width="2.4" stroke-linecap="round"/><path d="M{px_+18} {y-9} h9 v11 h-9z" fill="none" stroke="{c}" stroke-width="2"/>'
        if i < 2: s += f'<path d="M{px_+22} {y-12} q-4 -5 0 -9" fill="none" stroke="#B4AA90" stroke-width="1.2"/>'
    return s

def arrow(x1, y1, x2, y2, c="#9A9078", dash=False):
    a = math.atan2(y2 - y1, x2 - x1); L = 7
    d = ' stroke-dasharray="4 3"' if dash else ''
    return (f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{c}" stroke-width="1.3"{d}/>'
            f'<path d="M{x2} {y2} L{x2-L*math.cos(a-.4):.1f} {y2-L*math.sin(a-.4):.1f} L{x2-L*math.cos(a+.4):.1f} {y2-L*math.sin(a+.4):.1f}Z" fill="{c}"/>')

def box(x, y, w, h, text, c="#9A9078", fill="#1A160F", size=10.5, lines=None):
    s = f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="3" fill="{fill}" stroke="{c}" stroke-width="1.2"/>'
    ls = lines or [text]
    for i, l in enumerate(ls):
        s += T(x + w / 2, y + h / 2 + 4 - (len(ls) - 1) * 7 + i * 14, l, "#DCD2BA", size, "middle")
    return s

def haggle():
    s = base("THE MARKET · ONE HAGGLE", "Plus, Satis, Vale",
             T(1160,54,"at the Goldsmith's counter · patience 3","#9A9078",12,"end") + T(1160,76,"the words are voice commands, also bindable to keys (#57)","#9A9078",12,"end"))
    s += DEFS
    # panel 1
    s += panel_bg(0, 1) + smithy(190, "peer", -10, 1) + panel_counter()
    s += '<circle cx="90" cy="214" r="70" fill="url(#gA)"/>' + goblet(90, 236, 1.9)
    s += '<path d="M150 230 q26 -3 40 -8" stroke="#6A5242" stroke-width="8" stroke-linecap="round"/>'
    s += '<g stroke="#DCD2BA" stroke-width="1.4" fill="none" opacity=".7"><path d="M128 180 q12 8 22 8"/><path d="M130 170 q12 4 20 4"/></g>' + panel_end(0, 1)
    # panel 2
    s += panel_bg(1, 2) + smithy(196, "talk", 0, 1) + panel_counter()
    s += goblet(60, 236, 1.7)
    for j, (cx_, cy_) in enumerate([(118, 238), (124, 233), (130, 228), (150, 240), (160, 235), (172, 239)]): s += coin(cx_, cy_, 10)
    s += '<g stroke="#DCD2BA" stroke-width="1.3" opacity=".6"><line x1="200" y1="226" x2="178" y2="226"/><line x1="204" y1="232" x2="184" y2="232"/><line x1="198" y1="238" x2="180" y2="238"/></g>'
    s += '<path d="M196 232 q-8 2 -14 4" stroke="#6A5242" stroke-width="8" stroke-linecap="round"/>'
    s += bubble(14, 30, 170, ["“Forty, and I'm", "robbing myself.”"]) + panel_end(1, 2)
    # panel 3
    s += panel_bg(2, 3) + smithy(196, "tense", 4, 3) + panel_counter()
    s += goblet(60, 236, 1.7)
    for cx_, cy_ in [(110, 238), (116, 233), (122, 228), (142, 240), (152, 235)]: s += coin(cx_, cy_, 10)
    # drumming fingers
    for j in range(4):
        fx = 178 + j * 11
        s += f'<ellipse cx="{fx}" cy="{232-(j%2)*3}" rx="4" ry="9" fill="#7A5E4A" stroke="#2A1A12" stroke-width=".6" transform="rotate({-8+j*3} {fx} 232)"/>'
        s += f'<path d="M{fx-5} 223 q-3 -4 0 -8 M{fx+5} 223 q3 -4 0 -8" stroke="#DCD2BA" stroke-width="1" fill="none" opacity=".7"/>'
    s += '<path d="M172 238 q-10 4 -4 6" stroke="#DCD2BA" stroke-width="1" fill="none"/>'
    s += T(176, 280, "tap tap tap", "#B4AA90", 9.5, extra='font-style="italic"')
    s += T(16, 150, "PLUS", "#A090D0", 52, font=E, w=700, ls=2, extra='font-style="italic" transform="rotate(-9 16 150)" stroke="#14100C" stroke-width="2.5" paint-order="stroke"')
    s += '<g stroke="#7A6AA0" stroke-width="1.6" fill="none" opacity=".8"><path d="M40 172 v-14 M36 162 l4 -5 l4 5"/><path d="M110 150 v-14 M106 140 l4 -5 l4 5"/></g>'
    s += '<g>' + pipes_row(20, 34) + '</g>'
    s += T(20, 54, "his patience, shown by him:", "#DCD2BA", 9, extra='font-style="italic"') + T(20, 67, "fingers, pipe, a look", "#DCD2BA", 9, extra='font-style="italic"')
    s += panel_end(2, 3)
    # panel 4
    s += panel_bg(3, 4) + smithy(200, "calm", 0, 1) + panel_counter()
    s += goblet(70, 236, 1.7)
    s += '<g>'
    for cx_, cy_ in [(128, 238), (134, 233)]: s += coin(cx_, cy_, 10)
    s += '</g>'
    s += '<path d="M0 214 L60 206 L112 232 L70 262 L0 262Z" fill="#2A2240" stroke="#7A6AA0" stroke-width=".8"/>'
    s += '<path d="M96 232 C112 218 140 220 156 234 L158 246 C140 252 112 252 98 246Z" fill="url(#skin)" stroke="#2A1A12" stroke-width=".8"/>'
    for j in range(4): s += f'<ellipse cx="{156+j*8}" cy="{233+j*2}" rx="9" ry="3.6" fill="#7A5E4A" stroke="#2A1A12" stroke-width=".5" transform="rotate({10+j*4} {156+j*8} {233+j*2})"/>'
    s += coin(150, 236, 10)
    s += T(14, 150, "SATIS", "#A090D0", 44, font=E, w=700, ls=2, extra='font-style="italic" transform="rotate(-4 14 150)" stroke="#14100C" stroke-width="2.5" paint-order="stroke"')
    s += '<path d="M200 118 q10 -6 18 0" stroke="#B4AA90" stroke-width="1.2" fill="none"/>'
    s += panel_end(3, 4)
    caps = [("Set it down.", "He looks it over."), ("Opening offer — below", "what it's worth to him"), ("Ask 10% more. Over his", "limit, patience drops."), ("Take it — or VALE and walk", "away (he'll remember)")]
    for i, (a, b) in enumerate(caps):
        s += T(px(i), PY + PH + 24, a, "#DCD2BA", 11.5) + T(px(i), PY + PH + 40, b, "#DCD2BA", 11.5)
    # flow
    s += f'<line x1="40" y1="500" x2="1160" y2="500" stroke="#332D22"/>' + T(40, 524, "THE ROUND, AS RULES", "#635C4C", 11, ls=3)
    fy = 580
    s += box(40, fy - 24, 90, 48, "", "#9A9078", lines=["Offer", "on counter"])
    s += arrow(130, fy, 168, fy) + box(170, fy - 24, 80, 48, "PLUS", "#7A6AA0", "#1C1830", 13)
    s += arrow(250, fy, 290, fy)
    s += f'<polygon points="380,{fy-44} 470,{fy} 380,{fy+44} 290,{fy}" fill="#1A160F" stroke="#9A9078" stroke-width="1.2"/>' + T(380, fy - 2, "ask ≤", "#DCD2BA", 10.5, "middle") + T(380, fy + 12, "limit?", "#DCD2BA", 10.5, "middle")
    s += arrow(470, fy, 520, fy, "#5FA288") + T(495, fy - 8, "yes", "#5FA288", 10, "middle")
    s += box(522, fy - 24, 130, 48, "", "#5FA288", lines=["accept, or meet", "halfway"])
    s += arrow(652, fy, 700, fy) + box(702, fy - 24, 130, 48, "", "#7A6AA0", "#1C1830", 10.5, lines=["SATIS", "take the coins"])
    s += arrow(380, fy + 44, 380, fy + 72, "#C4542E") + T(390, fy + 62, "no", "#C4542E", 10)
    s += box(300, fy + 74, 160, 36, "", "#C4542E", lines=["patience −1 (−2 if >1.2×)"], size=9.5)
    s += arrow(460, fy + 92, 520, fy + 92, "#C4542E") + T(490, fy + 84, "at 0", "#C4542E", 10, "middle")
    s += box(522, fy + 74, 210, 36, "", "#C4542E", lines=["won't buy it tonight"], size=10.5)
    s += f'<path d="M300 {fy+92} H270 V{fy+30}" fill="none" stroke="#9A9078" stroke-width="1.2" stroke-dasharray="4 3"/>' + arrow(270, fy + 60, 270, fy + 30, "#9A9078")
    s += T(262, fy + 108, "ask again", "#9A9078", 9.5, "end")
    s += f'<path d="M85 {fy-24} V{fy-52} H935 V{fy-24}" fill="none" stroke="#9A9078" stroke-width="1.2" stroke-dasharray="4 3"/>' + arrow(935, fy - 40, 935, fy - 24)
    s += T(510, fy - 58, "at any point", "#9A9078", 10, "middle")
    s += box(850, fy - 24, 170, 48, "", "#7A6AA0", "#1C1830", 10.5, lines=["VALE", "walk away, he remembers"])
    s = s.replace(T(510, fy - 58, "at any point", "#9A9078", 10, "middle"), T(510, fy - 58, "at any point", "#9A9078", 10, "middle", extra=HALO))
    s += T(1034, fy - 4, "next open", "#9A9078", 9.5) + T(1034, fy + 10, "−10% same night", "#9A9078", 9.5)
    s += T(40, 720, "limit = worth × interest × mood (hidden) · opening = limit × 0.55–0.70", "#DCD2BA", 12.5)
    s += pal(PAL) + '</svg>'
    open("docs/art/concept/lair/haggle.svg", "w", encoding="utf-8").write(s)

lair(); market(); haggle()
