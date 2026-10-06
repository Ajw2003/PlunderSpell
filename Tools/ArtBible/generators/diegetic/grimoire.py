# Writes docs/art/concept/diegetic/grimoire.svg (issue #284, model #282). Run from the repo root:
#   python3 Tools/ArtBible/generators/diegetic/grimoire.py
# then render: NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs diegetic
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sheet_common import *
import random
defs='''
<linearGradient id="calf" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#9A6C45"/><stop offset=".5" stop-color="#7A5233"/><stop offset="1" stop-color="#553620"/></linearGradient>
<linearGradient id="calfR" x1="1" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8E6340"/><stop offset=".55" stop-color="#744C2F"/><stop offset="1" stop-color="#4E331E"/></linearGradient>
<linearGradient id="oak" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#6B4E33"/><stop offset="1" stop-color="#3A2A18"/></linearGradient>
<linearGradient id="brass" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#E3C173"/><stop offset=".5" stop-color="#B58A3C"/><stop offset="1" stop-color="#6E4F1E"/></linearGradient>
<linearGradient id="vel" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#E4DAB9"/><stop offset="1" stop-color="#C9BB93"/></linearGradient>
<linearGradient id="gutL" x1="1" y1="0" x2="0" y2="0"><stop offset="0" stop-color="#3B2D14" stop-opacity=".55"/><stop offset=".22" stop-color="#3B2D14" stop-opacity=".12"/><stop offset="1" stop-color="#3B2D14" stop-opacity="0"/></linearGradient>
<linearGradient id="gutR" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#3B2D14" stop-opacity=".55"/><stop offset=".22" stop-color="#3B2D14" stop-opacity=".12"/><stop offset="1" stop-color="#3B2D14" stop-opacity="0"/></linearGradient>
<radialGradient id="halo" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#DCD2BA" stop-opacity=".12"/><stop offset="1" stop-color="#DCD2BA" stop-opacity="0"/></radialGradient>
<clipPath id="flameclip"><path id="flamepath" d="M111,62 C113,80 134,92 134,116 C134,134 124,146 111,146 C98,146 88,134 88,118 C88,104 96,98 100,86 C104,94 107,96 108,84 C109,76 110,70 111,62 Z"/></clipPath>
'''
o=frame("DIEGETIC KIT · HELD · BOTH HANDS","The Grimoire","0.24 × 0.18 × 0.06 m · ≤ 3k tris · 1024²","2 meshes + page · cover hinges on spine",defs)
OY=70
o+='<circle cx="372" cy="560" r="300" fill="url(#halo)"/>\n'
o+=f'<g transform="translate(0,{OY})">'
o+='<ellipse cx="380" cy="545" rx="285" ry="30" fill="#0B0A08" opacity=".75"/>'
# cover thickness
o+='<polygon points="138,606 372,592 372,601 138,615" fill="url(#oak)" stroke="#14120E" stroke-width="1"/>'
o+='<polygon points="372,592 606,606 606,615 372,601" fill="url(#oak)" stroke="#14120E" stroke-width="1" />'
o+='<g transform="matrix(1 -0.0598 0 1 138 606)" ><g transform="translate(0,0)" opacity=".7">'+grain(234,9,5,3,"#1E140A",.7,.8)+'</g></g>'
o+='<g transform="matrix(1 0.0598 0 1 372 592)" opacity=".7">'+grain(234,9,5,4,"#1E140A",.7,.8)+'</g>'
# covers
for name,mat,gid,seed in (("L","matrix(1 -0.0598 0 1 138 380)","calf",11),("R","matrix(1 0.0598 0 1 372 366)","calfR",12)):
    o+=f'<g transform="{mat}"><rect width="234" height="226" fill="url(#{gid})" stroke="#14120E" stroke-width="1.2"/>'
    o+=grain(234,226,26,seed,"#3B2314",.45,1)
    o+=grain(234,226,12,seed+9,"#C69A6C",.22,1.6)  # worn pale patches
    o+=speckle(234,226,45,seed)
    o+='<rect x="3" y="3" width="228" height="220" fill="none" stroke="#C69A6C" stroke-width="1.4" opacity=".25"/>'
    o+='</g>'
# page block strips
o+='<polygon points="150,598 372,580 372,586 150,604" fill="#B9AC86" stroke="#14120E" stroke-width=".6"/>'
o+='<polygon points="372,580 594,598 594,604 372,586" fill="#A89B76" stroke="#14120E" stroke-width=".6"/>'
for k in range(1,5):
    t=k*1.2
    o+=f'<path d="M150,{598+t:.1f} L372,{580+t:.1f}" stroke="#7E7050" stroke-width=".5"/><path d="M372,{580+t:.1f} L594,{598+t:.1f}" stroke="#6E6244" stroke-width=".5"/>'
# left outer edge of page block
o+='<polygon points="144,392 150,390 150,598 144,602" fill="#A89B76" stroke="#14120E" stroke-width=".5"/>'
o+='<polygon points="594,390 600,392 600,602 594,598" fill="#A89B76" stroke="#14120E" stroke-width=".5"/>'
# left page (blank)
o+='<g transform="matrix(1 -0.081 0 1 150 390)"><rect width="222" height="208" fill="url(#vel)" stroke="#14120E" stroke-width=".8"/>'
o+=speckle(222,208,40,21,"#8E7A4A")
o+='<rect x="26" y="26" width="170" height="156" fill="none" stroke="#8B7A58" stroke-width=".9" stroke-dasharray="4 3" opacity=".7"/>'
o+='<rect x="30" y="30" width="162" height="148" fill="none" stroke="#8B7A58" stroke-width=".4" opacity=".4"/>'
o+=text(111,106,"BLANK UNTIL LEARNED",8.5,"#8B7A58","middle",1.5,MONO,' opacity=".85"')
o+=text(111,120,"no word yet",7.5,"#8B7A58","middle",0,"Spectral, Georgia, serif",' font-style="italic" opacity=".6"')
o+='<rect width="222" height="208" fill="url(#gutL)"/></g>'
# right page (spell)
o+='<g transform="matrix(1 0.081 0 1 372 372)"><rect width="222" height="208" fill="url(#vel)" stroke="#14120E" stroke-width=".8"/>'
o+=speckle(222,208,40,22,"#8E7A4A")
o+='<rect x="14" y="14" width="194" height="180" fill="none" stroke="#8B7A58" stroke-width=".8" opacity=".55"/>'
o+=text(111,56,"IGNIS",42,"#7A6AA0","middle",5,"Eczar, Georgia, serif",' font-weight="700" stroke="#5C4E86" stroke-width=".6"')
o+='<path d="M60,66 H162" stroke="#7A6AA0" stroke-width=".8" opacity=".7"/><path d="M80,69 H142" stroke="#7A6AA0" stroke-width=".5" opacity=".6"/>'
# flame woodcut
o+='<path d="M111,62 C113,80 134,92 134,116 C134,134 124,146 111,146 C98,146 88,134 88,118 C88,104 96,98 100,86 C104,94 107,96 108,84 C109,76 110,70 111,62 Z" fill="#E7D9AE" opacity=".6"/>'
o+='<g clip-path="url(#flameclip)" stroke="#3A2A18" stroke-width=".9" opacity=".8">'
for i in range(0,45):
    y=64+i*2.0; o+=f'<path d="M80,{y+8:.1f} L142,{y-6:.1f}"/>'
o+='</g>'
o+='<path d="M111,62 C113,80 134,92 134,116 C134,134 124,146 111,146 C98,146 88,134 88,118 C88,104 96,98 100,86 C104,94 107,96 108,84 C109,76 110,70 111,62 Z" fill="none" stroke="#2B2013" stroke-width="1.8" stroke-linejoin="round"/>'
o+='<path d="M111,98 C116,108 124,114 123,126 C122,136 117,141 111,141 C104,141 99,136 99,127 C99,118 106,114 108,108 C109,106 110,103 111,98 Z" fill="#E7D9AE" stroke="#2B2013" stroke-width="1.3"/>'
o+='<path d="M111,118 C114,124 116,128 115,133 C114,137 112,138 111,138 C109,138 107,136 107,133 C107,128 110,124 111,118 Z" fill="#2B2013"/>'
o+='<path d="M96,148 H128 M90,152 H132 M100,156 H122" stroke="#2B2013" stroke-width="1.6" stroke-linecap="round"/>'
o+='<path d="M80,102 l6,-5 M142,100 l-6,-6 M78,120 l6,0 M144,120 l-6,0" stroke="#2B2013" stroke-width="1" stroke-linecap="round"/>'
# faint script
r=random.Random(5)
for y in (166,176):
    x=30; d=''
    while x<186:
        wl=r.randint(14,34); d+=f'M{x},{y} '
        for _ in range(wl//4): d+=f'q2,{-r.uniform(2,4.5):.1f} 4,0 '
        x+=wl+7
    o+=f'<path d="{d}" fill="none" stroke="#5A4A30" stroke-width=".8" opacity=".45"/>'
# margin notes
o+=text(206,186,"last casts",5.5,"#8B7A58","end",1,MONO,' opacity=".9"')
o+=text(206,194,"ignis ✓",6.5,"#5A4A30","end",0,"Spectral, Georgia, serif",' font-style="italic"')
o+=text(206,202,"glacies ✗ misfire",6.5,"#C4542E","end",0,"Spectral, Georgia, serif",' font-style="italic"')
o+='<rect width="222" height="208" fill="url(#gutR)"/></g>'
# gutter line
o+='<path d="M372,372 L372,580" stroke="#2B2013" stroke-width="1.6" opacity=".8"/>'
# ribbon
o+='<path d="M372,578 C380,590 376,600 384,614 L392,610 C384,598 386,588 380,578 Z" fill="#8E3A24" stroke="#14120E" stroke-width=".6"/>'
# brass corners
def corner(p,u,v,s=36):
    px,py=p; ux,uy=u; vx,vy=v
    a=(px+ux*s,py+uy*s); b=(px+vx*s,py+vy*s); c=(px+(ux+vx)*s*.3,py+(uy+vy)*s*.3)
    d=f'M{px},{py} L{a[0]:.1f},{a[1]:.1f} Q{c[0]:.1f},{c[1]:.1f} {b[0]:.1f},{b[1]:.1f} Z'
    rx=px+(ux+vx)*s*.22; ry=py+(uy+vy)*s*.22
    return (f'<path d="{d}" fill="url(#brass)" stroke="#3A2A12" stroke-width="1" stroke-linejoin="round"/>'
      f'<path d="M{px+ux*5+vx*5:.1f},{py+uy*5+vy*5:.1f} L{a[0]-ux*6+vx*4:.1f},{a[1]-uy*6+vy*4:.1f}" stroke="#F0D898" stroke-width=".8" opacity=".6"/>'
      f'<circle cx="{rx:.1f}" cy="{ry:.1f}" r="2.1" fill="#E3C173" stroke="#3A2A12" stroke-width=".6"/><circle cx="{rx-.5:.1f}" cy="{ry-.5:.1f}" r=".7" fill="#FFF3C8"/>')
o+=corner((138,380),(.998,-.06),(0,1))+corner((138,606),(.997,-.077),(0,-1))
o+=corner((606,380),(-.998,-.06),(0,1))+corner((606,606),(-.997,-.077),(0,-1))
# clasp (strap dangling from fore-edge, open)
o+='<path d="M606,476 L648,480 Q658,495 648,512 L606,508 Z" fill="url(#brass)" stroke="#3A2A12" stroke-width="1.1" stroke-linejoin="round"/>'
o+='<path d="M610,481 L644,484" stroke="#F0D898" stroke-width=".8" opacity=".6"/><ellipse cx="642" cy="496" rx="4.5" ry="5.5" fill="#2B2013" stroke="#3A2A12" stroke-width=".8"/>'
o+='<circle cx="618" cy="486" r="2" fill="#E3C173" stroke="#3A2A12" stroke-width=".5"/><circle cx="618" cy="502" r="2" fill="#E3C173" stroke="#3A2A12" stroke-width=".5"/>'
o+='<rect x="598" y="472" width="12" height="40" rx="3" fill="url(#brass)" stroke="#3A2A12" stroke-width="1"/>'
o+='<path d="M600,476 H608 M600,492 H608 M600,508 H608" stroke="#3A2A12" stroke-width=".7"/>'
# catch plate on left cover edge
o+='<rect x="130" y="484" width="9" height="24" rx="2" fill="url(#brass)" stroke="#3A2A12" stroke-width=".8"/>'
o+='</g>\n'
# callouts
o+=callout('l',(150,OY+388),300,150,"Brass corner pieces","filed brass, riveted through the board")
o+=callout('l',(144,OY+440),300,210,"Oak boards in worn calfskin","rubbed pale at the edges")
o+=callout('l',(300,OY+352+68+ -0),300,270,"Vellum pages","cream, shaded toward the gutter")
o+=callout('r',(483,OY+442),680,150,"Lapis ink","the spell word, big and unmistakable")
o+=callout('r',(535,OY+575),680,210,"Margin notes","last casts, small, in the owner's hand")
o+=callout('r',(630,OY+495),680,270,"Brass clasp","strap swings free when open")
o+=grab(144,OY+500,"GRAB",130,OY+504,"end")+grab(598,OY+560,"GRAB",612,OY+564)
o+=text(680,360,"Tab to raise · scroll turns pages · walk 60%, no carrying",10.5,"#9A9078")
o+=text(680,378,"casting stays by voice, so the book can stay open",10,"#635C4C")
# closed front view  (1 m = 600 px: 144 x 108)
cx,cy=700,582
o+=f'<ellipse cx="{cx+72}" cy="690" rx="90" ry="7" fill="#0B0A08" opacity=".7"/>'
o+=f'<g transform="translate({cx},{cy})"><rect width="144" height="108" fill="url(#calf)" stroke="#14120E" stroke-width="1.2"/>'
o+=grain(144,108,18,31,"#3B2314",.45,.9)+grain(144,108,8,32,"#C69A6C",.2,1.4)+speckle(144,108,25,33)
o+='<rect x="12" y="10" width="124" height="88" fill="none" stroke="#3B2314" stroke-width="1.2" opacity=".7"/><rect x="16" y="14" width="116" height="80" fill="none" stroke="#C69A6C" stroke-width=".5" opacity=".4"/>'
o+='<path d="M74,30 L96,54 L74,78 L52,54 Z" fill="none" stroke="#3B2314" stroke-width="1.1" opacity=".7"/><circle cx="74" cy="54" r="5" fill="none" stroke="#7A6AA0" stroke-width="1.2" opacity=".8"/>'
o+='<rect x="0" y="0" width="13" height="108" fill="#4B3220" opacity=".8"/>'
for yy in (18,45,72,98): o+=f'<path d="M0,{yy} H13" stroke="#C69A6C" stroke-width="1.4" opacity=".5"/>'
o+='<rect x="0" y="108" width="144" height="0"/>'
o+='<g><rect x="92" y="46" width="48" height="16" rx="2" fill="url(#brass)" stroke="#3A2A12" stroke-width="1"/><path d="M96,49 H136" stroke="#F0D898" stroke-width=".7" opacity=".6"/><circle cx="102" cy="54" r="5" fill="#E3C173" stroke="#3A2A12" stroke-width=".8"/><circle cx="102" cy="54" r="1.6" fill="#3A2A12"/></g>'
o+='</g>'
# corners on closed
def cc(p,u,v): return corner(p,u,v,22)
o+=cc((cx,cy),(1,0),(0,1))+cc((cx+144,cy),(-1,0),(0,1))+cc((cx,cy+108),(1,0),(0,-1))+cc((cx+144,cy+108),(-1,0),(0,-1))
o+=vlabel(372,730,"HERO 3/4 · OPEN · 1 m = 950 px")+vlabel(772,730,"CLOSED · FRONT")+vlabel(1020,730,"SIDE · HINGE")
# side/hinge view: spine at x=950? slab 144 wide, 36 thick
sx,sy=1000,654
o+=f'<ellipse cx="{sx+72}" cy="690" rx="90" ry="6" fill="#0B0A08" opacity=".7"/>'
o+=f'<rect x="{sx}" y="{sy+0}" width="144" height="36" fill="url(#oak)" stroke="#14120E" stroke-width="1"/>'
o+=f'<rect x="{sx+4}" y="{sy+4}" width="136" height="28" fill="url(#vel)" stroke="#8B7A58" stroke-width=".5"/>'
for k in range(1,6): o+=f'<path d="M{sx+4},{sy+4+k*4.5} H{sx+140}" stroke="#8B7A58" stroke-width=".4"/>'
o+=f'<rect x="{sx}" y="{sy}" width="144" height="4" fill="url(#calf)" stroke="#14120E" stroke-width=".6"/><rect x="{sx}" y="{sy+32}" width="144" height="4" fill="url(#calf)" stroke="#14120E" stroke-width=".6"/>'
o+=f'<rect x="{sx-8}" y="{sy}" width="9" height="36" rx="4" fill="url(#calf)" stroke="#14120E" stroke-width="1"/>'
# raised cover at 115 deg about hinge (sx,sy+2)
hx,hy=sx,sy+2; a=math.radians(115)
ex,ey=hx+144*math.cos(a),hy-144*math.sin(a)
nx,ny=math.sin(a)*4,math.cos(a)*4
o+=f'<polygon points="{hx},{hy} {ex:.1f},{ey:.1f} {ex+nx:.1f},{ey+ny:.1f} {hx+nx:.1f},{hy+ny:.1f}" fill="url(#calf)" stroke="#14120E" stroke-width="1"/>'
o+=f'<path d="M{hx+144},{hy} A144,144 0 0 0 {ex:.1f},{ey:.1f}" fill="none" stroke="#5FA288" stroke-width="1.4" stroke-dasharray="5 4"/>'
o+=f'<path d="M{ex:.1f},{ey:.1f} A144,144 0 0 0 {hx-144},{hy}" fill="none" stroke="#5FA288" stroke-width="1" stroke-dasharray="2 5" opacity=".6"/>'
o+=f'<path d="M{hx-144},{hy} l6,-7 m-6,7 l8,2" stroke="#5FA288" stroke-width="1.2" fill="none"/>'
o+=f'<circle cx="{hx}" cy="{hy}" r="6" fill="none" stroke="#5FA288" stroke-width="2"/><circle cx="{hx}" cy="{hy}" r="2" fill="#5FA288"/>'
o+=text(sx,494,"COVER PIVOT ON SPINE — opens 180°",10.5,"#5FA288","middle",1)
o+=text(sx+10,694-0,"",8)
o+=palette([("#7A5233","calfskin"),("#5A4026","oak"),("#B58A3C","brass"),("#D8CBA8","vellum"),("#7A6AA0","lapis ink"),("#C4542E","madder ink")])
o+='</svg>\n'
open('docs/art/concept/diegetic/grimoire.svg','w').write(o)
print(len(o))
