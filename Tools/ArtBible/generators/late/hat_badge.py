from common_enemy import *

HS, OS = 4600, 2600
sh = Sheet("jewelled-hat-badge")
sh.frame(f"{AGE_NAME} · PLUNDER · 1200 COIN · 0.2 ST", "Jewelled Hat-Badge",
         "ARTIFACT · 0.065 × 0.014 × 0.080 m · ≤ 1.5k tris · 1024²", f"hero 1 m = {HS} px · ortho 1 m = {OS} px", glow_xy=("32%", "55%"))
item_under(sh, 0.01, 0.01 * HS, f"0.01 m = {0.01*HS:.0f} px (hero) · {0.01*OS:.0f} px (ortho) — a 0.1 m bar would not fit")

sh.lin("gold", [(0, "#6E5410"), (.3, "#E3C35A"), (.45, "#F0D97A"), (.6, "#C9A227"), (1, "#5A440E")], 0, 0, 1, 1)
sh.lin("goldE", [(0, "#4A3808"), (1, "#8A6E1A")], 0, 0, 1, 0)
sh.rad("pearl", [(0, "#FFFFFF"), (.35, "#E6DFCF"), (.8, "#B9B09B"), (1, "#8F887A")], .35, .3, .7)
sh.rad("enamel", [(0, "#F4F0E6"), (.7, "#E8E2D4"), (1, "#B9B09B")], .45, .4, .7)
sh.lin("green", [(0, "#6E8A4E"), (1, "#34482A")], 0, 0, 1, 1)
sh.lin("ruby", [(0, "#B23A5A"), (.5, "#7A1E3A"), (1, "#3A0A1A")], 0, 0, 1, 1)


def quatrefoil(cx, cy, r, off):
    x = (off + math.sqrt(2 * r * r - off * off)) / 2
    notches = [(x, x), (-x, x), (-x, -x), (x, -x)]
    d = f"M {cx + notches[0][0]:.1f} {cy - notches[0][1]:.1f} "
    for (nx, ny) in notches[1:] + notches[:1]:
        d += f"A {r:.1f} {r:.1f} 0 1 0 {cx + nx:.1f} {cy - ny:.1f} "
    return d + "Z"


def badge(cx, cy, s, flat=None, detail=True):
    """Draw the badge face centred at (cx,cy) px with s px/m."""
    add = sh.add
    R, OFF = 0.0150 * s, 0.0135 * s
    add(f'<path d="{quatrefoil(cx, cy, R, OFF)}" fill="{flat or "url(#gold)"}" stroke="{flat or "#3A2C08"}" stroke-width="1.2"/>')
    if flat:
        return
    add(f'<path d="{quatrefoil(cx, cy, R*0.80, OFF*0.80)}" fill="url(#enamel)" stroke="#8A6E1A" stroke-width="1"/>')
    # beaded gold rim
    for k in range(48):
        a = k / 48 * 2 * math.pi
        rr = OFF + R * 0.9 if False else None
    for lobe in range(4):
        la = lobe * math.pi / 2
        lx, ly = cx + OFF * math.cos(la), cy - OFF * math.sin(la)
        for k in range(-5, 6):
            a = la + k * 0.26
            add(f'<circle cx="{lx + R*0.9*math.cos(a):.1f}" cy="{ly - R*0.9*math.sin(a):.1f}" r="{0.0006*s:.1f}" fill="#F0D97A"/>')
    # green enamel leaves on the diagonals
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        x0, y0 = cx + 0.006 * s * math.cos(a), cy - 0.006 * s * math.sin(a)
        x1, y1 = cx + 0.017 * s * math.cos(a), cy - 0.017 * s * math.sin(a)
        nx, ny = -math.sin(a) * 0.0035 * s, -math.cos(a) * 0.0035 * s
        add(f'<path d="M {x0:.1f} {y0:.1f} Q {(x0+x1)/2+nx:.1f} {(y0+y1)/2+ny:.1f} {x1:.1f} {y1:.1f} Q {(x0+x1)/2-nx:.1f} {(y0+y1)/2-ny:.1f} {x0:.1f} {y0:.1f} Z" fill="url(#green)" stroke="#8A6E1A" stroke-width=".8"/>')
        add(f'<path d="M {x0:.1f} {y0:.1f} L {x1:.1f} {y1:.1f}" stroke="#C9A227" stroke-width=".8"/>')
    # enamel white roses on the lobes, gold centres
    for lobe in range(4):
        la = lobe * math.pi / 2
        lx, ly = cx + OFF * 1.05 * math.cos(la), cy - OFF * 1.05 * math.sin(la)
        for p in range(5):
            pa = p * 2 * math.pi / 5 + la
            add(f'<circle cx="{lx + 0.0032*s*math.cos(pa):.1f}" cy="{ly - 0.0032*s*math.sin(pa):.1f}" r="{0.0026*s:.1f}" fill="url(#enamel)" stroke="#9F9784" stroke-width=".6"/>')
        add(f'<circle cx="{lx:.1f}" cy="{ly:.1f}" r="{0.0018*s:.1f}" fill="url(#gold)"/>')
    # central collet + table-cut balas ruby with four claws
    c = 0.0085 * s
    add(f'<rect x="{cx-c:.1f}" y="{cy-c:.1f}" width="{2*c:.1f}" height="{2*c:.1f}" fill="url(#gold)" stroke="#3A2C08" transform="rotate(45 {cx:.1f} {cy:.1f})"/>')
    g = 0.0058 * s
    add(f'<rect x="{cx-g:.1f}" y="{cy-g:.1f}" width="{2*g:.1f}" height="{2*g:.1f}" fill="url(#ruby)" stroke="#2A0612" transform="rotate(45 {cx:.1f} {cy:.1f})"/>')
    t = 0.0030 * s
    add(f'<rect x="{cx-t:.1f}" y="{cy-t:.1f}" width="{2*t:.1f}" height="{2*t:.1f}" fill="#9A2E4C" stroke="#C85A78" stroke-width=".8" transform="rotate(45 {cx:.1f} {cy:.1f})"/>')
    for k in range(4):
        a = k * math.pi / 2
        add(f'<circle cx="{cx + 0.0078*s*math.cos(a):.1f}" cy="{cy - 0.0078*s*math.sin(a):.1f}" r="{0.0012*s:.1f}" fill="#F0D97A" stroke="#6E5410" stroke-width=".6"/>')
    add(f'<path d="M {cx-g*0.6:.1f} {cy-g*0.2:.1f} L {cx-g*0.1:.1f} {cy-g*0.7:.1f}" stroke="#F4D0DC" stroke-width="1.6" opacity=".8"/>')
    # eight pearls: lobe tips and notches
    for k in range(8):
        a = k * math.pi / 4
        rr = (OFF + R + 0.0012 * s) if k % 2 == 0 else (0.0215 * s)
        px, py = cx + rr * math.cos(a), cy - rr * math.sin(a)
        if k == 6:
            continue  # the bottom tip carries the drop loop instead
        add(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="{0.0027*s:.1f}" fill="url(#pearl)" stroke="#6E6858" stroke-width=".6"/>')
    # pendant: gold loop, cap and pearl drop
    by = cy + OFF + R
    add(f'<circle cx="{cx:.1f}" cy="{by + 0.0022*s:.1f}" r="{0.0022*s:.1f}" fill="none" stroke="url(#gold)" stroke-width="{0.0012*s:.1f}"/>')
    add(f'<path d="M {cx - 0.0035*s:.1f} {by + 0.0055*s:.1f} Q {cx:.1f} {by + 0.0035*s:.1f} {cx + 0.0035*s:.1f} {by + 0.0055*s:.1f} L {cx:.1f} {by + 0.0075*s:.1f} Z" fill="url(#gold)" stroke="#3A2C08" stroke-width=".6"/>')
    dy = by + 0.0065 * s
    add(f'<path d="M {cx:.1f} {dy:.1f} C {cx + 0.0055*s:.1f} {dy + 0.004*s:.1f} {cx + 0.0055*s:.1f} {dy + 0.012*s:.1f} {cx:.1f} {dy + 0.013*s:.1f} C {cx - 0.0055*s:.1f} {dy + 0.012*s:.1f} {cx - 0.0055*s:.1f} {dy + 0.004*s:.1f} {cx:.1f} {dy:.1f} Z" fill="url(#pearl)" stroke="#6E6858" stroke-width=".7"/>')
    # gilt rubbed on the high points (bare, paler gold), enamel chips
    for (dx, dy2) in ((-0.8, -0.9), (0.9, 0.5), (0.2, -1.0)):
        add(f'<ellipse cx="{cx + dx*OFF:.1f}" cy="{cy + dy2*OFF:.1f}" rx="{0.0015*s:.1f}" ry="{0.0008*s:.1f}" fill="#F6E6A6" opacity=".7"/>')


# the drop hangs 0.0195 below the lobes: frame centre sits at 0.080 - 0.0285 above the ground
CY = 0.080 - 0.0285
# hero: three-quarter — face squeezed and turned, gold edge showing
hx, hy = 370, 690 - CY * HS
sh.add(f'<ellipse cx="{hx+10}" cy="692" rx="120" ry="9" fill="#0B0A08" opacity=".6"/>')
sh.add(f'<g transform="translate({0.004*HS*0.6:.1f},{-0.004*HS*0.25:.1f}) translate({hx},{hy}) scale(0.78,1) skewY(-6) translate({-hx},{-hy})">')
badge(hx, hy, HS, flat="#6E5410")
sh.add('</g>')
sh.add(f'<g transform="translate({0.002*HS*0.6:.1f},{-0.002*HS*0.25:.1f}) translate({hx},{hy}) scale(0.78,1) skewY(-6) translate({-hx},{-hy})">')
badge(hx, hy, HS, flat="#8A6E1A")
sh.add('</g>')
sh.add(f'<g transform="translate({hx},{hy}) scale(0.78,1) skewY(-6) translate({-hx},{-hy})">')
badge(hx, hy, HS)
sh.add('</g>')

# orthos: FRONT and SIDE (pin behind)
fx, fy = 820, 690 - CY * OS
badge(fx, fy, OS)
sx, sy = 1040, 690 - CY * OS
R_, OFF_ = 0.0150 * OS, 0.0135 * OS
top, bot = sy - (OFF_ + R_), sy + OFF_ + R_
sh.add(f'<rect x="{sx-0.0015*OS:.1f}" y="{top:.1f}" width="{0.003*OS:.1f}" height="{bot-top:.1f}" rx="3" fill="url(#goldE)" stroke="#3A2C08"/>')
sh.add(f'<rect x="{sx+0.0015*OS:.1f}" y="{sy-0.006*OS:.1f}" width="{0.0045*OS:.1f}" height="{0.012*OS:.1f}" fill="url(#ruby)" stroke="#2A0612"/>')
sh.add(f'<rect x="{sx+0.0015*OS:.1f}" y="{sy-0.0085*OS:.1f}" width="{0.0035*OS:.1f}" height="{0.017*OS:.1f}" fill="none" stroke="#C9A227" stroke-width="2"/>')
for k in (-1, 1):
    sh.add(f'<circle cx="{sx+0.003*OS:.1f}" cy="{sy + k*(OFF_+R_):.1f}" r="{0.0027*OS:.1f}" fill="url(#pearl)" stroke="#6E6858" stroke-width=".6"/>')
    sh.add(f'<circle cx="{sx+0.0035*OS:.1f}" cy="{sy + k*(OFF_*0.9):.1f}" r="{0.0028*OS:.1f}" fill="url(#enamel)" stroke="#9F9784" stroke-width=".6"/>')
# pin and catch on the back
sh.add(f'<path d="M {sx-0.0015*OS:.1f} {sy-0.02*OS:.1f} L {sx-0.006*OS:.1f} {sy-0.02*OS:.1f} L {sx-0.006*OS:.1f} {sy+0.022*OS:.1f}" fill="none" stroke="#C9A227" stroke-width="2.2"/>')
sh.add(f'<path d="M {sx-0.0015*OS:.1f} {sy+0.016*OS:.1f} L {sx-0.008*OS:.1f} {sy+0.016*OS:.1f} L {sx-0.008*OS:.1f} {sy+0.024*OS:.1f}" fill="none" stroke="#8A6E1A" stroke-width="3"/>')
by = sy + OFF_ + R_
sh.add(f'<circle cx="{sx:.1f}" cy="{by + 0.0022*OS:.1f}" r="{0.0022*OS:.1f}" fill="none" stroke="#C9A227" stroke-width="{0.0012*OS:.1f}"/>')
dy = by + 0.0065 * OS
sh.add(f'<path d="M {sx:.1f} {dy:.1f} C {sx + 0.0055*OS:.1f} {dy + 0.004*OS:.1f} {sx + 0.0055*OS:.1f} {dy + 0.012*OS:.1f} {sx:.1f} {dy + 0.013*OS:.1f} C {sx - 0.0055*OS:.1f} {dy + 0.012*OS:.1f} {sx - 0.0055*OS:.1f} {dy + 0.004*OS:.1f} {sx:.1f} {dy:.1f} Z" fill="url(#pearl)" stroke="#6E6858" stroke-width=".7"/>')
sh.text(fx, 690 - 0.080 * OS - 14, "0.065 m", 10, "#9A9078", "middle")
sh.text(sx, 690 - 0.080 * OS - 14, "0.014 m + pin", 10, "#9A9078", "middle")

grab(sh, hx - 0.028 * HS * 0.78, hy - 0.004 * HS, "GRAB · pinch", -12, -14, "end")
sh.text(40, 150, "UNBREAKABLE (999) · ARTIFACT · AURUM VOCO: IT SHINES BRIGHTEST IN THE ROOM", 10.5, "#C4542E", "start", extra='letter-spacing="1"')

sh.callout(hx, hy, 640, 360, "BALAS RUBY", "table-cut, 12 mm, 4 claws")
sh.callout(hx + 0.012 * HS * 0.78, hy - 0.017 * HS, 640, 280, "WHITE ENAMEL ROSES", "en ronde bosse, 5 petals")
sh.callout(hx + 0.0215 * HS * 0.78 * 0.7, hy - 0.0215 * HS * 0.7 - 12, 640, 200, "SEED PEARLS", "7 on the rim, 5.5 mm")
sh.callout(hx - 0.012 * HS * 0.78, hy + 0.004 * HS, 60, 560, "GOLD QUATREFOIL", "beaded rim, 2 mm thick", "start")
sh.callout(hx + 6, hy + 0.036 * HS, 640, 600, "PEARL DROP", "on a gold loop, 13 mm")

sh.palette([("#C9A227", "gold (orpiment)"), ("#E8E2D4", "white enamel"), ("#4F6A3A", "green enamel"),
            ("#7A1E3A", "balas ruby"), ("#D9D2C0", "pearl")])
sh.write()
