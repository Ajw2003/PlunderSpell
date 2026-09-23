"""Hand-authored vector illustration: a damp treasure cave with a portal at its centre.

Every shape is written out as SVG geometry; the only randomness is a seeded scatter of
coins, drips and moss so the output is identical on every run.

Run:  python3 Tools/cave_scene_svg.py
Out:  docs/generated/damp-cave-treasure-portal.svg
"""

import math
import random
from pathlib import Path

WIDTH, HEIGHT = 1600, 900
PORTAL_X, PORTAL_Y = 800, 400
OUTPUT_PATH = Path(__file__).resolve().parent.parent / "docs" / "generated" / "damp-cave-treasure-portal.svg"

rng = random.Random(1337)


def star_path(cx, cy, outer, inner, points=5, rotation=-90):
    """Closed path for an n-pointed star centred on (cx, cy)."""
    coords = []
    for i in range(points * 2):
        radius = outer if i % 2 == 0 else inner
        angle = math.radians(rotation + i * 180 / points)
        coords.append(f"{cx + radius * math.cos(angle):.1f},{cy + radius * math.sin(angle):.1f}")
    return "M" + " L".join(coords) + " Z"


def sparkle(cx, cy, size, delay):
    """Four-point glint that twinkles on gold and gems."""
    s, t = size, size * 0.18
    path = (f"M{cx},{cy - s} L{cx + t},{cy - t} L{cx + s},{cy} L{cx + t},{cy + t} "
            f"L{cx},{cy + s} L{cx - t},{cy + t} L{cx - s},{cy} L{cx - t},{cy - t} Z")
    return (f'<path d="{path}" fill="#fffbe0" opacity="0">'
            f'<animate attributeName="opacity" values="0;0.95;0" dur="3.2s" begin="{delay:.2f}s" '
            f'repeatCount="indefinite"/></path>')


def coin(x, y, scale=1.0, tilt=0.0):
    return (f'<g transform="translate({x:.1f} {y:.1f}) rotate({tilt:.1f}) scale({scale:.2f})">'
            f'<use href="#coin"/></g>')


def coin_on_edge(x, y, scale=1.0, tilt=0.0):
    return (f'<g transform="translate({x:.1f} {y:.1f}) rotate({tilt:.1f}) scale({scale:.2f})">'
            f'<use href="#coinEdge"/></g>')


def gem(x, y, color_id, scale=1.0, tilt=0.0):
    return (f'<g transform="translate({x:.1f} {y:.1f}) rotate({tilt:.1f}) scale({scale:.2f})">'
            f'<path d="M-10,-4 L-5,-11 L5,-11 L10,-4 L0,10 Z" fill="url(#{color_id})" stroke="#0b0b12" stroke-width="0.8"/>'
            f'<path d="M-10,-4 L10,-4 M-5,-11 L-3,-4 L0,10 M5,-11 L3,-4 L0,10" stroke="#ffffff" stroke-opacity="0.35" stroke-width="0.8" fill="none"/>'
            f'<path d="M-5,-10 L-1,-10 L-3,-5 Z" fill="#fff" opacity="0.7"/></g>')


def wizard_hat(x, y, scale, color, tilt, star_color="#ffe9a8"):
    """Pointed hat with a drooping tip; (x, y) is the centre of the brim."""
    stars = "".join(
        f'<path d="{star_path(sx, sy, r, r * 0.45)}" fill="{star_color}"/>'
        for sx, sy, r in ((-14, -48, 7), (12, -78, 5), (-2, -104, 4), (22, -34, 4))
    )
    moon = ('<path d="M-22,-72 a9,9 0 1,0 8,13 a7,7 0 1,1 -8,-13 Z" '
            f'fill="{star_color}" opacity="0.9"/>')
    return f'''
    <g transform="translate({x} {y}) rotate({tilt}) scale({scale})" style="color:{color}">
      <ellipse cx="0" cy="6" rx="80" ry="15" fill="#000" opacity="0.35"/>
      <ellipse cx="0" cy="2" rx="78" ry="18" fill="currentColor"/>
      <ellipse cx="0" cy="2" rx="78" ry="18" fill="url(#hatShade)"/>
      <path d="M-40,-2 C-32,-50 -18,-100 -2,-135 C6,-150 22,-162 50,-150 C30,-146 18,-136 14,-118
               C20,-80 32,-40 40,-2 Q0,8 -40,-2 Z" fill="currentColor"/>
      <path d="M8,-120 C18,-80 30,-40 40,-2 Q20,4 10,4 C12,-40 8,-90 -2,-135 C2,-128 5,-124 8,-120 Z"
            fill="#000" opacity="0.28"/>
      <path d="M-30,-60 C-26,-80 -18,-100 -8,-120" stroke="#fff" stroke-opacity="0.18" stroke-width="4" fill="none" stroke-linecap="round"/>
      <path d="M-39,-6 Q0,4 39,-6 L36,-22 Q0,-12 -35,-22 Z" fill="#c99a2e"/>
      <path d="M-39,-6 Q0,4 39,-6" stroke="#6e4f12" stroke-width="1.5" fill="none"/>
      <rect x="-8" y="-20" width="16" height="14" rx="2" fill="none" stroke="#ffe08a" stroke-width="3"/>
      {moon}{stars}
      <path d="M48,-151 l3,-3 l3,3 l-3,3 Z" fill="{star_color}"/>
    </g>'''


def sword(x, y, angle, scale=1.0, blade_length=270, grip_color="#5a2d1a", guard_color="#c9a13b"):
    """Straight sword with hilt at (x, y); blade points along +y before rotation."""
    tip = blade_length
    return f'''
    <g transform="translate({x} {y}) rotate({angle}) scale({scale})">
      <path d="M-9,52 L9,52 L9,{tip - 26} L0,{tip} L-9,{tip - 26} Z" fill="url(#steel)" stroke="#23262c" stroke-width="1.2"/>
      <path d="M0,58 L0,{tip - 20}" stroke="#dfe7ee" stroke-width="2" opacity="0.7"/>
      <path d="M-9,52 L-9,{tip - 26} L0,{tip}" stroke="#ffffff" stroke-width="1" fill="none" opacity="0.45"/>
      <rect x="-5" y="8" width="10" height="38" rx="3" fill="{grip_color}"/>
      <path d="M-5,14 L5,19 M-5,22 L5,27 M-5,30 L5,35 M-5,38 L5,43" stroke="#2a130a" stroke-width="2"/>
      <path d="M-40,44 C-30,52 -15,48 0,48 C15,48 30,52 40,44 C38,54 30,58 18,56 L-18,56 C-30,58 -38,54 -40,44 Z"
            fill="{guard_color}" stroke="#5b4210" stroke-width="1.2"/>
      <circle cx="0" cy="51" r="4" fill="#b3122e" stroke="#5b4210"/>
      <circle cx="0" cy="3" r="8" fill="{guard_color}" stroke="#5b4210" stroke-width="1.2"/>
      <circle cx="-2" cy="1" r="2.5" fill="#fff4c4" opacity="0.8"/>
    </g>'''


def axe(x, y, angle, scale=1.0):
    """Bearded battle axe; (x, y) is the butt of the haft, head at -y."""
    return f'''
    <g transform="translate({x} {y}) rotate({angle}) scale({scale})">
      <rect x="-6" y="-300" width="12" height="300" rx="5" fill="url(#wood)" stroke="#2b170b" stroke-width="1.2"/>
      <path d="M-6,-60 L6,-56 M-6,-50 L6,-46 M-6,-40 L6,-36 M-6,-30 L6,-26" stroke="#3a2415" stroke-width="3"/>
      <path d="M6,-290 C40,-300 70,-320 82,-330 C96,-296 98,-236 84,-196 C70,-210 40,-228 6,-232 Z"
            fill="url(#steel)" stroke="#22252b" stroke-width="1.5"/>
      <path d="M82,-330 C96,-296 98,-236 84,-196" stroke="#f2f6fa" stroke-width="3" fill="none" opacity="0.7"/>
      <path d="M-6,-286 C-24,-290 -34,-280 -40,-262 C-30,-256 -20,-252 -6,-248 Z" fill="url(#steel)" stroke="#22252b" stroke-width="1.2"/>
      <rect x="-9" y="-296" width="18" height="70" rx="3" fill="#3b3f47" stroke="#1b1d21"/>
      <circle cx="0" cy="-280" r="3" fill="#9aa3ad"/><circle cx="0" cy="-242" r="3" fill="#9aa3ad"/>
      <circle cx="0" cy="-306" r="7" fill="#6d737c" stroke="#1b1d21"/>
    </g>'''


def stalagmite(x, base_y, half_width, height, fill="url(#rockLit)", lean=0):
    tip_x = x + lean
    return (f'<path d="M{x - half_width},{base_y} C{x - half_width * 0.6},{base_y - height * 0.45} '
            f'{tip_x - half_width * 0.25},{base_y - height * 0.88} {tip_x},{base_y - height} '
            f'C{tip_x + half_width * 0.25},{base_y - height * 0.88} {x + half_width * 0.6},{base_y - height * 0.45} '
            f'{x + half_width},{base_y} Z" fill="{fill}"/>'
            f'<path d="M{tip_x - 2},{base_y - height + 8} C{x - half_width * 0.3},{base_y - height * 0.6} '
            f'{x - half_width * 0.45},{base_y - height * 0.3} {x - half_width * 0.55},{base_y - 4}" '
            f'stroke="#9fd4e0" stroke-opacity="0.18" stroke-width="3" fill="none"/>')


def stalactite(x, width, length, fill="url(#rockHang)"):
    half = width / 2
    return (f'<path d="M{x - half},-5 C{x - half * 0.7},{length * 0.4} {x - half * 0.2},{length * 0.85} {x},{length} '
            f'C{x + half * 0.2},{length * 0.85} {x + half * 0.7},{length * 0.4} {x + half},-5 Z" fill="{fill}"/>'
            f'<path d="M{x - half * 0.4},{length * 0.2} C{x - half * 0.3},{length * 0.5} {x - half * 0.1},{length * 0.8} {x - 1},{length - 6}" '
            f'stroke="#a8e2ee" stroke-opacity="0.22" stroke-width="2" fill="none"/>')


def drip(x, start_y, end_y, delay, duration):
    """A water drop that swells at a stalactite tip then falls and splashes."""
    fall = end_y - start_y
    return f'''
    <g transform="translate({x} {start_y})">
      <path d="M0,-4 C3,2 5,6 0,10 C-5,6 -3,2 0,-4 Z" fill="url(#water)">
        <animateTransform attributeName="transform" type="translate" values="0,0;0,0;0,{fall}" keyTimes="0;0.55;1"
          keySplines="0 0 1 1;0.5 0 1 1" calcMode="spline" dur="{duration}s" begin="{delay}s" repeatCount="indefinite"/>
        <animate attributeName="opacity" values="0;1;1;0" keyTimes="0;0.3;0.98;1" dur="{duration}s" begin="{delay}s" repeatCount="indefinite"/>
      </path>
      <ellipse cx="0" cy="{fall + 6}" rx="2" ry="0.8" fill="none" stroke="#bfefff" stroke-width="1.2" opacity="0">
        <animate attributeName="rx" values="2;2;22" keyTimes="0;0.97;1" dur="{duration}s" begin="{delay}s" repeatCount="indefinite"/>
        <animate attributeName="ry" values="0.8;0.8;6" keyTimes="0;0.97;1" dur="{duration}s" begin="{delay}s" repeatCount="indefinite"/>
        <animate attributeName="opacity" values="0;0;0.8" keyTimes="0;0.97;1" dur="{duration}s" begin="{delay}s" repeatCount="indefinite"/>
      </ellipse>
    </g>'''


def defs():
    return '''
  <defs>
    <radialGradient id="caveAir" cx="50%" cy="44%" r="70%">
      <stop offset="0" stop-color="#1d3a45"/>
      <stop offset="0.45" stop-color="#0f1d25"/>
      <stop offset="1" stop-color="#05080b"/>
    </radialGradient>
    <linearGradient id="backWall" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#101a20"/>
      <stop offset="1" stop-color="#1a2a30"/>
    </linearGradient>
    <linearGradient id="rockLit" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="#1a2226"/>
      <stop offset="0.55" stop-color="#34454b"/>
      <stop offset="1" stop-color="#151b1e"/>
    </linearGradient>
    <linearGradient id="rockLitRight" x1="1" y1="0" x2="0" y2="0">
      <stop offset="0" stop-color="#1a2226"/>
      <stop offset="0.55" stop-color="#3a4c52"/>
      <stop offset="1" stop-color="#151b1e"/>
    </linearGradient>
    <linearGradient id="rockHang" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#0b1013"/>
      <stop offset="0.7" stop-color="#26343a"/>
      <stop offset="1" stop-color="#3f5a61"/>
    </linearGradient>
    <linearGradient id="rockDark" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#0d1316"/>
      <stop offset="1" stop-color="#040607"/>
    </linearGradient>
    <linearGradient id="floor" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#1c2b30"/>
      <stop offset="0.4" stop-color="#172226"/>
      <stop offset="1" stop-color="#0a0f11"/>
    </linearGradient>
    <radialGradient id="portalGlow" cx="50%" cy="50%" r="50%">
      <stop offset="0" stop-color="#b8f7ff" stop-opacity="0.75"/>
      <stop offset="0.3" stop-color="#5fd0e8" stop-opacity="0.35"/>
      <stop offset="0.6" stop-color="#7a4fd6" stop-opacity="0.15"/>
      <stop offset="1" stop-color="#7a4fd6" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="portalCore" cx="50%" cy="50%" r="50%">
      <stop offset="0" stop-color="#ffffff"/>
      <stop offset="0.18" stop-color="#c9fbff"/>
      <stop offset="0.45" stop-color="#3fb6d9"/>
      <stop offset="0.75" stop-color="#5a2fb0"/>
      <stop offset="1" stop-color="#1b0c3a"/>
    </radialGradient>
    <radialGradient id="floorLight" cx="50%" cy="50%" r="50%">
      <stop offset="0" stop-color="#7fe6f5" stop-opacity="0.45"/>
      <stop offset="0.5" stop-color="#6a58d8" stop-opacity="0.15"/>
      <stop offset="1" stop-color="#6a58d8" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="dais" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#55707a"/>
      <stop offset="1" stop-color="#1c2629"/>
    </linearGradient>
    <linearGradient id="puddle" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#7fe0f0" stop-opacity="0.55"/>
      <stop offset="0.5" stop-color="#3b5f8a" stop-opacity="0.45"/>
      <stop offset="1" stop-color="#0b1a22" stop-opacity="0.8"/>
    </linearGradient>
    <radialGradient id="gold" cx="40%" cy="35%" r="70%">
      <stop offset="0" stop-color="#fff2a8"/>
      <stop offset="0.35" stop-color="#f2c04a"/>
      <stop offset="0.8" stop-color="#b07a18"/>
      <stop offset="1" stop-color="#6b440a"/>
    </radialGradient>
    <linearGradient id="goldMetal" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#fff0a0"/>
      <stop offset="0.45" stop-color="#e2ad34"/>
      <stop offset="1" stop-color="#7c5210"/>
    </linearGradient>
    <linearGradient id="steel" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="#8b96a3"/>
      <stop offset="0.45" stop-color="#e7eef4"/>
      <stop offset="0.55" stop-color="#b5c0ca"/>
      <stop offset="1" stop-color="#5d6772"/>
    </linearGradient>
    <linearGradient id="wood" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="#3d2312"/>
      <stop offset="0.5" stop-color="#7a4a26"/>
      <stop offset="1" stop-color="#3d2312"/>
    </linearGradient>
    <linearGradient id="chestWood" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#7a4523"/>
      <stop offset="1" stop-color="#3b1f0e"/>
    </linearGradient>
    <linearGradient id="hatShade" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#fff" stop-opacity="0.15"/>
      <stop offset="1" stop-color="#000" stop-opacity="0.45"/>
    </linearGradient>
    <linearGradient id="water" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#e6fbff"/>
      <stop offset="1" stop-color="#6ccde4"/>
    </linearGradient>
    <linearGradient id="gemRed" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ff7a8a"/><stop offset="1" stop-color="#8a0a22"/></linearGradient>
    <linearGradient id="gemGreen" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8dffb5"/><stop offset="1" stop-color="#0b6b35"/></linearGradient>
    <linearGradient id="gemBlue" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#9ad8ff"/><stop offset="1" stop-color="#0d3f8f"/></linearGradient>
    <linearGradient id="gemPurple" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#e0a8ff"/><stop offset="1" stop-color="#4b137a"/></linearGradient>
    <radialGradient id="vignette" cx="50%" cy="48%" r="75%">
      <stop offset="0.55" stop-color="#000" stop-opacity="0"/>
      <stop offset="1" stop-color="#000" stop-opacity="0.85"/>
    </radialGradient>

    <filter id="blurBig" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="28"/></filter>
    <filter id="blurMid" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="10"/></filter>
    <filter id="blurSmall" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="3"/></filter>
    <filter id="rockGrain" x="0" y="0" width="100%" height="100%">
      <feTurbulence type="fractalNoise" baseFrequency="0.012 0.03" numOctaves="4" seed="7" result="noise"/>
      <feColorMatrix in="noise" type="matrix"
        values="0 0 0 0 0.55  0 0 0 0 0.72  0 0 0 0 0.76  0 0 0 1.4 -0.62"/>
    </filter>

    <clipPath id="portalWindow"><ellipse cx="0" cy="0" rx="120" ry="190"/></clipPath>

    <g id="coin">
      <ellipse cx="0" cy="1.5" rx="12" ry="5" fill="#6b440a"/>
      <ellipse cx="0" cy="0" rx="12" ry="5" fill="url(#gold)" stroke="#7c5210" stroke-width="0.8"/>
      <ellipse cx="0" cy="0" rx="7.5" ry="2.8" fill="none" stroke="#fff2a8" stroke-opacity="0.55" stroke-width="0.8"/>
    </g>
    <g id="coinEdge">
      <ellipse cx="0" cy="0" rx="4" ry="12" fill="url(#goldMetal)" stroke="#6b440a" stroke-width="0.8"/>
      <ellipse cx="-1" cy="-2" rx="1.2" ry="7" fill="#fff6c8" opacity="0.6"/>
    </g>
  </defs>'''


def background():
    wall_texture = (
        '<rect width="1600" height="620" filter="url(#rockGrain)" opacity="0.55"/>'
    )
    back_rocks = '''
    <path d="M0,120 C120,90 180,160 260,140 C340,120 380,200 470,210 C540,220 560,150 640,160
             L640,560 L0,560 Z" fill="#0e171b"/>
    <path d="M960,160 C1040,140 1080,210 1150,200 C1250,185 1300,110 1400,130 C1480,150 1540,100 1600,110
             L1600,560 L960,560 Z" fill="#0e171b"/>
    <path d="M540,560 C560,420 620,300 700,230 C740,200 860,200 900,230 C980,300 1040,420 1060,560 Z"
          fill="#132127" opacity="0.9"/>
    <path d="M0,300 C80,280 150,330 240,320 C330,310 380,370 460,400 L460,560 L0,560 Z" fill="#152328"/>
    <path d="M1140,400 C1220,370 1270,310 1360,320 C1450,330 1520,280 1600,300 L1600,560 L1140,560 Z" fill="#152328"/>'''
    wet_streaks = "".join(
        f'<path d="M{x},{y} q{rng.uniform(-6, 6):.1f},{length / 2:.1f} {rng.uniform(-3, 3):.1f},{length:.1f}" '
        f'stroke="#9fe3f0" stroke-opacity="{rng.uniform(0.06, 0.16):.2f}" stroke-width="{rng.uniform(1, 2.5):.1f}" fill="none"/>'
        for x, y, length in (
            (rng.uniform(20, 1580), rng.uniform(140, 380), rng.uniform(60, 180)) for _ in range(40)
        )
        if not 560 < x < 1040
    )
    back_spires = "".join(
        stalagmite(x, 570, w, h, fill="#101b20")
        for x, w, h in ((500, 30, 150), (560, 18, 90), (1060, 26, 130), (1110, 16, 80), (380, 22, 110), (1230, 28, 120))
    )
    return f'''
  <rect width="1600" height="900" fill="url(#caveAir)"/>
  <rect width="1600" height="620" fill="url(#backWall)" opacity="0.6"/>
  {wall_texture}
  {back_rocks}
  {wet_streaks}
  {back_spires}'''


def floor():
    moss = "".join(
        f'<ellipse cx="{x:.0f}" cy="{y:.0f}" rx="{rng.uniform(8, 22):.0f}" ry="{rng.uniform(3, 6):.0f}" '
        f'fill="#2f5a3a" opacity="{rng.uniform(0.35, 0.7):.2f}"/>'
        for x, y in ((rng.uniform(0, 1600), rng.uniform(575, 880)) for _ in range(45))
    )
    pebbles = "".join(
        f'<ellipse cx="{x:.0f}" cy="{y:.0f}" rx="{r:.1f}" ry="{r * 0.55:.1f}" fill="#2a383d"/>'
        f'<ellipse cx="{x - r * 0.3:.1f}" cy="{y - r * 0.2:.1f}" rx="{r * 0.4:.1f}" ry="{r * 0.2:.1f}" fill="#a8dbe6" opacity="0.18"/>'
        for x, y, r in ((rng.uniform(0, 1600), rng.uniform(590, 890), rng.uniform(3, 10)) for _ in range(70))
    )
    return f'''
  <path d="M0,560 C200,540 400,580 600,565 C750,555 850,555 1000,565 C1200,580 1400,545 1600,560 L1600,900 L0,900 Z" fill="url(#floor)"/>
  <path d="M0,560 C200,540 400,580 600,565 C750,555 850,555 1000,565 C1200,580 1400,545 1600,560" stroke="#6fb8c8" stroke-opacity="0.18" stroke-width="2" fill="none"/>
  <ellipse cx="800" cy="680" rx="620" ry="170" fill="url(#floorLight)"/>
  {moss}
  {pebbles}'''


def puddles():
    spots = ((560, 790, 120, 18), (1040, 800, 150, 20), (250, 700, 90, 12), (1340, 690, 100, 13), (820, 845, 170, 20))
    out = []
    for i, (x, y, rx, ry) in enumerate(spots):
        out.append(f'''
    <ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="url(#puddle)"/>
    <ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="none" stroke="#b8f2ff" stroke-opacity="0.25" stroke-width="1.5"/>
    <ellipse cx="{x - rx * 0.2:.0f}" cy="{y - ry * 0.3:.0f}" rx="{rx * 0.35:.0f}" ry="{ry * 0.18:.1f}" fill="#d8fbff" opacity="0.25"/>
    <ellipse cx="{x + rx * 0.2:.0f}" cy="{y}" rx="4" ry="1.5" fill="none" stroke="#d8fbff" stroke-width="1" opacity="0">
      <animate attributeName="rx" values="4;{rx * 0.5:.0f}" dur="3.6s" begin="{i * 0.7:.1f}s" repeatCount="indefinite"/>
      <animate attributeName="ry" values="1.5;{ry * 0.45:.1f}" dur="3.6s" begin="{i * 0.7:.1f}s" repeatCount="indefinite"/>
      <animate attributeName="opacity" values="0.7;0" dur="3.6s" begin="{i * 0.7:.1f}s" repeatCount="indefinite"/>
    </ellipse>''')
    # Portal reflection shimmering in the big front puddle.
    out.append('<ellipse cx="820" cy="845" rx="40" ry="10" fill="#c9fbff" opacity="0.35" filter="url(#blurSmall)">'
               '<animate attributeName="opacity" values="0.2;0.45;0.2" dur="4s" repeatCount="indefinite"/></ellipse>')
    return "".join(out)


def portal():
    rune_marks = []
    for i in range(14):
        a = math.radians(i * 360 / 14 - 90)
        rx, ry = 150 * math.cos(a), 222 * math.sin(a)
        glyph = ("M-4,-6 L0,6 L4,-6", "M-5,0 L5,0 M0,-6 L0,6", "M-4,-6 L4,6 M4,-6 L-4,6",
                 "M-4,6 L-4,-6 L4,0 L-4,0", "M0,-6 L-5,4 L5,4 Z")[i % 5]
        rune_marks.append(
            f'<path d="{glyph}" transform="translate({rx:.1f} {ry:.1f})" stroke="#9ff3ff" stroke-width="2" fill="none" '
            f'stroke-linecap="round" stroke-linejoin="round"><animate attributeName="stroke-opacity" '
            f'values="0.35;1;0.35" dur="3s" begin="{i * 0.21:.2f}s" repeatCount="indefinite"/></path>')
    spiral_arms = "".join(
        f'<path d="M0,0 C40,-20 90,10 100,60 C108,110 70,160 10,170" stroke="{color}" stroke-width="{w}" '
        f'fill="none" stroke-linecap="round" opacity="{op}" transform="rotate({rot})"/>'
        for rot, color, w, op in (
            (0, "#e6feff", 7, 0.7), (60, "#7fe3f5", 10, 0.55), (120, "#b58cff", 9, 0.5),
            (180, "#e6feff", 6, 0.6), (240, "#7fe3f5", 10, 0.5), (300, "#b58cff", 8, 0.55))
    )
    inner_arms = "".join(
        f'<path d="M0,0 C20,-10 45,5 50,30 C54,55 35,80 5,85" stroke="#ffffff" stroke-width="4" '
        f'fill="none" stroke-linecap="round" opacity="0.6" transform="rotate({rot})"/>'
        for rot in (30, 150, 270)
    )
    motes = "".join(
        f'<circle cx="{rng.uniform(-110, 110):.0f}" cy="{rng.uniform(-60, 60):.0f}" r="{rng.uniform(1, 2.8):.1f}" fill="#e8fdff">'
        f'<animate attributeName="cy" values="{rng.uniform(60, 180):.0f};{rng.uniform(-240, -140):.0f}" dur="{rng.uniform(4, 8):.1f}s" '
        f'begin="{rng.uniform(0, 5):.1f}s" repeatCount="indefinite"/>'
        f'<animate attributeName="opacity" values="0;1;0" dur="{rng.uniform(4, 8):.1f}s" begin="{rng.uniform(0, 5):.1f}s" repeatCount="indefinite"/></circle>'
        for _ in range(26)
    )
    return f'''
  <!-- Portal glow on the cave -->
  <ellipse cx="{PORTAL_X}" cy="{PORTAL_Y}" rx="420" ry="400" fill="url(#portalGlow)" filter="url(#blurBig)">
    <animate attributeName="opacity" values="0.8;1;0.8" dur="5s" repeatCount="indefinite"/>
  </ellipse>

  <!-- Dais -->
  <ellipse cx="800" cy="690" rx="330" ry="46" fill="#0d1417"/>
  <ellipse cx="800" cy="680" rx="320" ry="44" fill="url(#dais)"/>
  <ellipse cx="800" cy="656" rx="265" ry="38" fill="#1a2327"/>
  <ellipse cx="800" cy="648" rx="260" ry="36" fill="url(#dais)"/>
  <ellipse cx="800" cy="626" rx="210" ry="30" fill="#1a2327"/>
  <ellipse cx="800" cy="618" rx="205" ry="28" fill="url(#dais)"/>
  <ellipse cx="800" cy="618" rx="205" ry="28" fill="none" stroke="#9ff3ff" stroke-opacity="0.35" stroke-width="2"/>
  <path d="M640,684 l10,-4 M700,700 l12,-2 M900,702 l14,-3 M1010,690 l8,-5 M600,652 l12,-3 M985,655 l10,-4"
        stroke="#0a0f11" stroke-width="2"/>

  <!-- Ring of standing stone -->
  <g transform="translate({PORTAL_X} {PORTAL_Y})">
    <ellipse cx="0" cy="8" rx="162" ry="232" fill="none" stroke="#0a1013" stroke-width="40"/>
    <ellipse cx="0" cy="0" rx="150" ry="222" fill="none" stroke="#4a5f66" stroke-width="36"
             stroke-dasharray="58 4"/>
    <ellipse cx="0" cy="0" rx="150" ry="222" fill="none" stroke="#6f8990" stroke-width="10"
             stroke-dasharray="58 4" transform="translate(-4 -6)" opacity="0.6"/>
    <ellipse cx="0" cy="0" rx="132" ry="204" fill="none" stroke="#9ff3ff" stroke-width="3" opacity="0.6" filter="url(#blurSmall)"/>
    {"".join(rune_marks)}
    <path d="M-60,-212 q-10,-20 6,-34 q14,12 4,30 Z M70,-210 q14,-16 4,-34 q-16,10 -8,32 Z" fill="#2f5a3a" opacity="0.8"/>
    <path d="M-150,40 q-18,6 -22,24 q20,4 26,-10 Z M150,60 q20,2 26,20 q-22,4 -28,-8 Z" fill="#2f5a3a" opacity="0.8"/>

    <!-- The vortex itself -->
    <g clip-path="url(#portalWindow)">
      <ellipse cx="0" cy="0" rx="120" ry="190" fill="url(#portalCore)"/>
      <g transform="scale(0.72 1.1)">
        <g>
          {spiral_arms}
          <animateTransform attributeName="transform" type="rotate" from="0" to="360" dur="14s" repeatCount="indefinite"/>
        </g>
        <g>
          {inner_arms}
          <animateTransform attributeName="transform" type="rotate" from="360" to="0" dur="7s" repeatCount="indefinite"/>
        </g>
      </g>
      <ellipse cx="0" cy="0" rx="34" ry="52" fill="#ffffff" opacity="0.85" filter="url(#blurMid)">
        <animate attributeName="rx" values="30;40;30" dur="3s" repeatCount="indefinite"/>
        <animate attributeName="ry" values="48;60;48" dur="3s" repeatCount="indefinite"/>
      </ellipse>
      {motes}
    </g>
    <ellipse cx="0" cy="0" rx="120" ry="190" fill="none" stroke="#e6feff" stroke-width="4" opacity="0.8"/>
  </g>'''


def ceiling():
    hanging = [(60, 90, 170), (150, 60, 110), (240, 70, 210), (330, 40, 90), (420, 60, 150),
               (520, 40, 100), (600, 55, 130), (690, 30, 70), (910, 34, 80), (990, 60, 140),
               (1080, 40, 95), (1170, 70, 190), (1270, 45, 120), (1360, 80, 220), (1450, 50, 130),
               (1540, 90, 180)]
    shapes = "".join(stalactite(x, w, l) for x, w, l in hanging)
    drops = "".join(
        f'<ellipse cx="{x}" cy="{l + 3}" rx="2.5" ry="3.5" fill="url(#water)" opacity="0.85"/>'
        for x, _, l in hanging
    )
    return f'''
  <path d="M0,0 L1600,0 L1600,70 C1500,110 1420,60 1330,90 C1220,130 1120,70 1000,100 C900,120 700,120 600,100
           C480,70 380,130 260,95 C160,70 80,110 0,80 Z" fill="url(#rockDark)"/>
  {shapes}
  {drops}'''


def left_group():
    """Hat hung on a stalagmite, open chest with a hat on the gold, sword leaning on a boulder, shield."""
    chest_coins = "".join(
        coin(360 + rng.uniform(-70, 70), 628 + rng.uniform(-14, 10), rng.uniform(0.8, 1.1), rng.uniform(-25, 25))
        for _ in range(28)
    )
    spill = "".join(
        coin(x, y, rng.uniform(0.8, 1.1), rng.uniform(-20, 20))
        for x, y in ((rng.uniform(280, 520), rng.uniform(730, 780)) for _ in range(26))
    )
    return f'''
  <!-- Tall stalagmite with a hat waiting on its tip -->
  {stalagmite(200, 700, 70, 250)}
  <ellipse cx="200" cy="702" rx="80" ry="12" fill="#000" opacity="0.35"/>
  {wizard_hat(206, 468, 0.72, "#4b2a8a", -8)}

  <!-- Boulder with a sword leaning against it -->
  <path d="M40,800 C30,740 80,700 140,705 C200,710 250,740 250,790 C250,830 60,840 40,800 Z" fill="url(#rockLit)"/>
  <path d="M70,730 C100,712 150,708 190,716" stroke="#a8dbe6" stroke-opacity="0.2" stroke-width="3" fill="none"/>
  <ellipse cx="120" cy="712" rx="30" ry="6" fill="#2f5a3a" opacity="0.75"/>
  {sword(262, 560, 18, 1.0, 270, grip_color="#2c3a6e", guard_color="#b9c4cf")}

  <!-- Open treasure chest -->
  <g>
    <ellipse cx="370" cy="768" rx="140" ry="18" fill="#000" opacity="0.45"/>
    <path d="M262,640 L250,560 C290,540 440,540 480,560 L470,640 Z" fill="url(#chestWood)" stroke="#1f0f06" stroke-width="2"/>
    <path d="M258,610 L474,610 M254,585 L478,585" stroke="#2a1508" stroke-width="2" opacity="0.7"/>
    <path d="M300,641 L292,553 M436,641 L444,553" stroke="#8a8f96" stroke-width="10"/>
    <path d="M262,640 L250,560 C290,540 440,540 480,560 L470,640" fill="none" stroke="#8a8f96" stroke-width="5"/>
    <path d="M270,640 C300,610 440,610 470,640 Q370,660 270,640 Z" fill="url(#gold)"/>
    {chest_coins}
    <rect x="260" y="640" width="220" height="120" rx="6" fill="url(#chestWood)" stroke="#1f0f06" stroke-width="2"/>
    <path d="M264,675 L476,675 M264,710 L476,710 M264,742 L476,742" stroke="#2a1508" stroke-width="2" opacity="0.7"/>
    <rect x="292" y="640" width="14" height="120" fill="#8a8f96" stroke="#3a3d42"/>
    <rect x="434" y="640" width="14" height="120" fill="#8a8f96" stroke="#3a3d42"/>
    <rect x="260" y="640" width="220" height="10" fill="#9aa1a8" stroke="#3a3d42"/>
    <rect x="356" y="650" width="28" height="34" rx="4" fill="url(#goldMetal)" stroke="#5b4210" stroke-width="1.5"/>
    <circle cx="370" cy="664" r="4" fill="#1a0f05"/><rect x="368" y="664" width="4" height="10" fill="#1a0f05"/>
    <circle cx="299" cy="660" r="2.5" fill="#d7dde2"/><circle cx="299" cy="740" r="2.5" fill="#d7dde2"/>
    <circle cx="441" cy="660" r="2.5" fill="#d7dde2"/><circle cx="441" cy="740" r="2.5" fill="#d7dde2"/>
    <path d="M262,650 L262,758" stroke="#fff" stroke-opacity="0.12" stroke-width="3"/>
  </g>
  {wizard_hat(400, 624, 0.62, "#1f5a7a", 12, star_color="#d8f6ff")}
  {gem(318, 628, "gemRed", 1.1, -10)}{gem(446, 632, "gemGreen", 1.0, 14)}
  {coin_on_edge(338, 622, 1.0, 20)}
  {spill}
  {gem(300, 770, "gemBlue", 1.1, 8)}{gem(510, 752, "gemPurple", 0.9, -20)}

  <!-- Round shield propped against the chest -->
  <g transform="translate(520 722) rotate(-8)">
    <ellipse cx="0" cy="0" rx="46" ry="52" fill="#6b1f1f" stroke="#3a3d42" stroke-width="7"/>
    <path d="M0,-52 L0,52 M-46,0 L46,0" stroke="#c9a13b" stroke-width="6"/>
    <circle cx="0" cy="0" r="12" fill="url(#steel)" stroke="#2a2d31" stroke-width="2"/>
    <path d="M-30,-34 C-16,-46 10,-48 26,-38" stroke="#fff" stroke-opacity="0.25" stroke-width="3" fill="none"/>
  </g>
  {sparkle(330, 612, 9, 0.2)}{sparkle(420, 640, 7, 1.3)}{sparkle(470, 760, 6, 2.1)}{sparkle(250, 600, 8, 0.9)}'''


def right_group():
    """Gold hoard with a sword plunged in, goblet, crown, a hat, and an axe leaning on a stalagmite."""
    pile_coins = []
    for _ in range(170):
        # Sample points inside the mound's upper surface so coins sit on it rather than float.
        u = rng.uniform(-1, 1)
        x = 1150 + u * 170
        top = 775 - 115 * (1 - u * u) ** 1.2
        y = rng.uniform(top, 780)
        pile_coins.append(coin(x, y, rng.uniform(0.75, 1.05), rng.uniform(-30, 30)))
    edge_coins = "".join(coin_on_edge(1150 + rng.uniform(-140, 140), rng.uniform(700, 770), 0.9, rng.uniform(-40, 40)) for _ in range(8))
    floor_coins = "".join(
        coin(x, y, rng.uniform(0.8, 1.0), rng.uniform(-25, 25))
        for x, y in ((rng.uniform(900, 1420), rng.uniform(780, 860)) for _ in range(22))
    )
    return f'''
  <!-- Stalagmite with an axe leaning on it -->
  {stalagmite(1440, 720, 80, 290, fill="url(#rockLitRight)", lean=-10)}
  <ellipse cx="1440" cy="722" rx="90" ry="13" fill="#000" opacity="0.35"/>
  {axe(1330, 745, 14, 0.95)}

  <!-- Gold hoard -->
  <ellipse cx="1150" cy="782" rx="200" ry="26" fill="#000" opacity="0.45"/>
  {sword(1112, 560, -14, 1.05, 200)}
  <path d="M975,782 C1000,720 1080,662 1150,660 C1225,662 1300,720 1325,782 Z" fill="url(#gold)"/>
  {"".join(pile_coins)}
  {edge_coins}

  <!-- Goblet -->
  <g transform="translate(1030 740)">
    <path d="M-22,-50 L22,-50 C22,-24 12,-12 3,-8 L3,12 C10,14 16,18 16,22 L-16,22 C-16,18 -10,14 -3,12 L-3,-8 C-12,-12 -22,-24 -22,-50 Z"
          fill="url(#goldMetal)" stroke="#5b4210" stroke-width="1.5"/>
    <ellipse cx="0" cy="-50" rx="22" ry="5" fill="#3a1f08"/>
    <ellipse cx="0" cy="-50" rx="22" ry="5" fill="none" stroke="#fff0a0" stroke-width="1.2"/>
    <circle cx="0" cy="-30" r="5" fill="url(#gemRed)" stroke="#5b4210"/>
    <path d="M-16,-44 C-15,-30 -10,-20 -4,-14" stroke="#fff" stroke-opacity="0.5" stroke-width="2" fill="none"/>
  </g>

  <!-- Crown -->
  <g transform="translate(1218 700) rotate(10)">
    <path d="M-34,10 L-38,-22 L-20,-6 L-8,-30 L4,-6 L18,-30 L26,-6 L40,-24 L34,10 Z"
          fill="url(#goldMetal)" stroke="#5b4210" stroke-width="1.5"/>
    <rect x="-35" y="4" width="70" height="10" rx="2" fill="#c99a2e" stroke="#5b4210"/>
    <circle cx="-38" cy="-24" r="3.5" fill="#fff2a8"/><circle cx="-8" cy="-32" r="3.5" fill="#fff2a8"/>
    <circle cx="18" cy="-32" r="3.5" fill="#fff2a8"/><circle cx="40" cy="-26" r="3.5" fill="#fff2a8"/>
    <circle cx="-18" cy="9" r="3.5" fill="url(#gemBlue)"/><circle cx="0" cy="9" r="4" fill="url(#gemRed)"/>
    <circle cx="18" cy="9" r="3.5" fill="url(#gemGreen)"/>
  </g>

  {wizard_hat(1262, 762, 0.8, "#7a1f4a", -16, star_color="#ffd6f0")}
  {gem(1090, 700, "gemGreen", 1.2, 10)}{gem(1180, 676, "gemPurple", 1.1, -12)}{gem(1300, 768, "gemBlue", 1.0, 18)}
  {gem(990, 770, "gemRed", 1.0, -6)}
  {floor_coins}
  {sparkle(1140, 672, 10, 0.5)}{sparkle(1060, 712, 7, 1.7)}{sparkle(1230, 690, 8, 2.6)}{sparkle(1190, 740, 6, 1.1)}{sparkle(1030, 690, 7, 3.0)}'''


def centre_scatter():
    """Loose coins and gems strewn across the dais steps and floor in front of the portal."""
    items = []
    for _ in range(26):
        x, y = rng.uniform(560, 1040), rng.uniform(700, 880)
        items.append(coin(x, y, rng.uniform(0.7, 1.0), rng.uniform(-30, 30)))
    for x, y, color in ((640, 668, "gemBlue"), (980, 668, "gemRed"), (720, 820, "gemPurple"), (930, 800, "gemGreen"), (860, 700, "gemBlue")):
        items.append(gem(x, y, color, 0.9, rng.uniform(-20, 20)))
    items.append(coin_on_edge(700, 640, 0.9, -10))
    items.append(coin_on_edge(905, 636, 0.9, 15))
    return "".join(items)


def foreground():
    return '''
  <path d="M0,900 L0,640 C40,650 70,700 110,760 C150,820 210,850 260,900 Z" fill="url(#rockDark)"/>
  <path d="M0,640 C40,650 70,700 110,760" stroke="#7fc6d6" stroke-opacity="0.18" stroke-width="3" fill="none"/>
  <path d="M1600,900 L1600,600 C1560,620 1520,690 1480,760 C1440,830 1380,860 1320,900 Z" fill="url(#rockDark)"/>
  <path d="M1600,600 C1560,620 1520,690 1480,760" stroke="#7fc6d6" stroke-opacity="0.18" stroke-width="3" fill="none"/>
  <path d="M0,0 L0,420 C20,380 30,300 60,260 C90,210 70,120 120,60 L160,0 Z" fill="#05090b"/>
  <path d="M1600,0 L1600,440 C1575,390 1565,300 1535,250 C1505,200 1520,110 1470,50 L1440,0 Z" fill="#05090b"/>
  <ellipse cx="60" cy="600" rx="30" ry="8" fill="#2f5a3a" opacity="0.6"/>
  <ellipse cx="1550" cy="560" rx="26" ry="7" fill="#2f5a3a" opacity="0.6"/>'''


def mist():
    banks = ((300, 820, 360, 40, 30), (1200, 830, 380, 44, 36), (800, 720, 420, 36, 26))
    return "".join(
        f'<ellipse cx="{x}" cy="{y}" rx="{rx}" ry="{ry}" fill="#bfeff7" opacity="0.10" filter="url(#blurBig)">'
        f'<animate attributeName="cx" values="{x - 40};{x + 40};{x - 40}" dur="{dur}s" repeatCount="indefinite"/></ellipse>'
        for x, y, rx, ry, dur in banks
    )


def falling_drips():
    return "".join((
        drip(240, 214, 690, 0.0, 3.4),
        drip(1360, 224, 684, 1.2, 4.1),
        drip(990, 144, 790, 2.1, 3.8),
        drip(600, 134, 780, 0.7, 4.5),
        drip(1170, 194, 660, 2.8, 3.6),
    ))


def build_svg():
    body = "".join((
        background(),
        floor(),
        portal(),
        ceiling(),
        puddles(),
        centre_scatter(),
        left_group(),
        right_group(),
        foreground(),
        mist(),
        falling_drips(),
        '<rect width="1600" height="900" fill="url(#vignette)" pointer-events="none"/>',
    ))
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {WIDTH} {HEIGHT}" width="{WIDTH}" height="{HEIGHT}" '
            f'role="img" aria-labelledby="title desc">\n'
            f'  <title id="title">Damp treasure cave with a portal</title>\n'
            f'  <desc id="desc">A dripping cave strewn with gold, gems, a goblet and a crown. A glowing portal '
            f'stands on a stone dais at its centre; swords, an axe, a shield and three wizard hats lie about, '
            f'waiting to be picked up.</desc>'
            f'{defs()}{body}\n</svg>\n')


if __name__ == "__main__":
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_PATH.write_text(build_svg(), encoding="utf-8")
    print(f"wrote {OUTPUT_PATH}")
