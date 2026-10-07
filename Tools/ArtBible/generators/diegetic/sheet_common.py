# Shared drawing helpers for the diegetic concept sheets (grimoire.py, pocket_watch.py).
import random, math
MONO="Overpass Mono, monospace"
def f(v): return ("%.1f"%v).rstrip('0').rstrip('.') if isinstance(v,float) else str(v)
def text(x,y,s,size=12,fill="#DCD2BA",anchor="start",ls=None,font=MONO,extra=""):
    l=f' letter-spacing="{ls}"' if ls else ''
    s=s.replace("&","&amp;").replace(">","&gt;").replace("<","&lt;")
    return f'<text x="{f(x)}" y="{f(y)}" font-family="{font}" font-size="{size}" fill="{fill}" text-anchor="{anchor}"{l}{extra}>{s}</text>'
def callout(side,dot,tx,ty,main,sub):
    dx,dy=dot
    if side=='l':
        p=f'M{f(dx)},{f(dy)} L{tx+20},{ty-4} L{tx+6},{ty-4}'; a='end'
    else:
        p=f'M{f(dx)},{f(dy)} L{tx-20},{ty-4} L{tx-6},{ty-4}'; a='start'
    return (f'<path d="{p}" fill="none" stroke="#9A9078" stroke-width=".8"/>'
      f'<circle cx="{f(dx)}" cy="{f(dy)}" r="2.6" fill="#DCD2BA" stroke="#14120E" stroke-width="1"/>'
      +text(tx,ty,main,12,"#DCD2BA",a)+text(tx,ty+14,sub,10,"#9A9078",a))
def grab(x,y,label,lx,ly,anchor="start"):
    return (f'<circle cx="{x}" cy="{y}" r="7" fill="none" stroke="#5FA288" stroke-width="2"/><circle cx="{x}" cy="{y}" r="2.2" fill="#5FA288"/>'
      +text(lx,ly,label,10,"#5FA288",anchor,1.5))
def frame(header,title,spec1,spec2,defs):
    return (f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 800" width="1200" height="800">
<defs>
<pattern id="grid" width="55" height="55" patternUnits="userSpaceOnUse" x="0" y="690"><path d="M55 0 H0 V55" fill="none" stroke="#262119" stroke-width="1"/></pattern>
<radialGradient id="glow" cx="30%" cy="60%" r="60%"><stop offset="0" stop-color="#2A2012" stop-opacity=".9"/><stop offset="1" stop-color="#14120E" stop-opacity="0"/></radialGradient>
{defs}
</defs>
<rect width="1200" height="800" fill="#14120E"/>
<rect width="1200" height="800" fill="url(#glow)"/>
<rect x="0" y="120" width="1200" height="570" fill="url(#grid)"/>
<line x1="40" y1="690" x2="1160" y2="690" stroke="#635C4C" stroke-width="1.5"/>
<rect x="12" y="12" width="1176" height="776" fill="none" stroke="#332D22" stroke-width="1"/>
'''+text(40,54,header,12,"#635C4C","start",3)+'\n'
 +text(40,96,title,38,"#DCD2BA","start",None,"Eczar, Georgia, serif",' font-weight="700"')+'\n'
 +text(1160,54,spec1,12,"#9A9078","end")+'\n'+text(1160,76,spec2,12,"#9A9078","end")+'\n')
def palette(items):
    o='<g id="palette" font-family="Overpass Mono, monospace" font-size="10" fill="#9A9078">'
    for i,(h,n) in enumerate(items):
        x=40+i*185
        o+=f'<rect x="{x}" y="748" width="34" height="18" fill="{h}" stroke="#332D22" stroke-width=".6"/><text x="{x+40}" y="756">{h}</text><text x="{x+40}" y="767" fill="#635C4C">{n}</text>'
    return o+'</g>\n'
def vlabel(x,y,s): return text(x,y,s,11,"#635C4C","middle",3)
def grain(w,h,n,seed,col="#3B2B1B",op=.55,sw=1.2):
    r=random.Random(seed); o=''
    for _ in range(n):
        x=r.uniform(0,w*.7); y=r.uniform(2,h-2); l=r.uniform(w*.12,w*.5); k=r.uniform(-2,2)
        o+=f'<path d="M{x:.0f},{y:.0f} q{l/2:.0f},{k:.1f} {l:.0f},{r.uniform(-1,1):.1f}" fill="none" stroke="{col}" stroke-width="{sw}" opacity="{op}"/>'
    return o
def speckle(w,h,n,seed,col="#2A1D10"):
    r=random.Random(seed)
    return ''.join(f'<circle cx="{r.uniform(0,w):.0f}" cy="{r.uniform(0,h):.0f}" r="{r.uniform(.4,1.1):.1f}" fill="{col}" opacity=".5"/>' for _ in range(n))
