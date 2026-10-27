# Writes docs/art/concept/diegetic/pocket-watch.svg (issue #284, model #283). Run from the repo root:
#   python3 Tools/ArtBible/generators/diegetic/pocket_watch.py
# then render: NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs diegetic
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sheet_common import *
import random
defs='''
<linearGradient id="brass" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#EBCB7E"/><stop offset=".45" stop-color="#B58A3C"/><stop offset="1" stop-color="#6E4F1E"/></linearGradient>
<linearGradient id="brassD" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#8A6628"/><stop offset="1" stop-color="#4A3412"/></linearGradient>
<linearGradient id="lidIn" x1="1" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#9A7430"/><stop offset=".6" stop-color="#7A5A22"/><stop offset="1" stop-color="#4E3814"/></linearGradient>
<radialGradient id="dial" cx=".42" cy=".38" r=".75"><stop offset="0" stop-color="#262036"/><stop offset="1" stop-color="#0E0C16"/></radialGradient>
<radialGradient id="glass" cx=".3" cy=".25" r=".9"><stop offset="0" stop-color="#BFD4DA" stop-opacity=".35"/><stop offset=".4" stop-color="#BFD4DA" stop-opacity=".06"/><stop offset="1" stop-color="#BFD4DA" stop-opacity=".12"/></radialGradient>
<radialGradient id="halo" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#7A6AA0" stop-opacity=".28"/><stop offset="1" stop-color="#7A6AA0" stop-opacity="0"/></radialGradient>
<radialGradient id="haloM" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#C4542E" stop-opacity=".3"/><stop offset="1" stop-color="#C4542E" stop-opacity="0"/></radialGradient>
<linearGradient id="steel" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#C3C8CF"/><stop offset=".5" stop-color="#8E939A"/><stop offset="1" stop-color="#4A4E55"/></linearGradient>
<filter id="blur" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="2.6"/></filter>
<filter id="blurS" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="1.2"/></filter>
'''
o=frame("DIEGETIC KIT · HELD · OFF-HAND","The Portal Watch","Ø 52 mm hunter case · ≤ 2k tris · 512²","4 meshes · lid hinge · hands on spindle",defs)
def face(cx,cy,R,lit,col,hand_deg,detail=True,crack=False,glowcol="#7A6AA0"):
    """30 minute ticks; first `lit` ticks lit (clockwise from 12); hand at hand_deg"""
    s=''
    s+=f'<circle cx="{cx}" cy="{cy}" r="{R*.9:.1f}" fill="url(#dial)" stroke="#05040A" stroke-width="{R*.02:.1f}"/>'
    s+=f'<circle cx="{cx}" cy="{cy}" r="{R*.9:.1f}" fill="none" stroke="{col}" stroke-width="{R*.012:.2f}" opacity=".35"/>'
    s+=f'<circle cx="{cx}" cy="{cy}" r="{R*.5:.1f}" fill="none" stroke="#2A2540" stroke-width="{R*.01:.2f}"/>'
    s+=f'<circle cx="{cx}" cy="{cy}" r="{R*.36:.1f}" fill="none" stroke="#2A2540" stroke-width="{R*.01:.2f}" stroke-dasharray="2 3"/>'
    # tracks
    for i in range(30):
        a=math.radians(i*12+1); a2=math.radians(i*12+11)
        r1,r2=R*.6,R*.82
        def pt(r,aa): return (cx+r*math.sin(aa),cy-r*math.cos(aa))
        p1,p2,p3,p4=pt(r1,a),pt(r2,a),pt(r2,a2),pt(r1,a2)
        d=f'M{p1[0]:.1f},{p1[1]:.1f} L{p2[0]:.1f},{p2[1]:.1f} A{r2:.1f},{r2:.1f} 0 0 1 {p3[0]:.1f},{p3[1]:.1f} L{p4[0]:.1f},{p4[1]:.1f} A{r1:.1f},{r1:.1f} 0 0 0 {p1[0]:.1f},{p1[1]:.1f} Z'
        if i<lit:
            s+=f'<path d="{d}" fill="{col}" opacity=".9" filter="url(#blur)"/>'
            s+=f'<path d="{d}" fill="{col}" stroke="#F2EEFF" stroke-width="{R*.006:.2f}" stroke-opacity=".5"/>'
        else:
            s+=f'<path d="{d}" fill="#1C1830" stroke="#2A2540" stroke-width="{R*.006:.2f}"/>'
    # decor center: small portal swirl
    if detail:
        for k,rr in enumerate((R*.2,R*.13,R*.07)):
            s+=f'<circle cx="{cx}" cy="{cy}" r="{rr:.1f}" fill="none" stroke="{col}" stroke-width=".8" opacity="{.5-k*.12:.2f}"/>'
    # hand
    a=math.radians(hand_deg); dx,dy=math.sin(a),-math.cos(a); L=R*.84
    px,py=-dy,dx
    tipx,tipy=cx+dx*L,cy+dy*L
    w=R*.035
    s+=(f'<path d="M{cx-dx*R*.16:.1f},{cy-dy*R*.16:.1f} L{cx+px*w:.1f},{cy+py*w:.1f} L{cx+dx*L*.62+px*w*.5:.1f},{cy+dy*L*.62+py*w*.5:.1f} '
        f'L{tipx:.1f},{tipy:.1f} L{cx+dx*L*.62-px*w*.5:.1f},{cy+dy*L*.62-py*w*.5:.1f} L{cx-px*w:.1f},{cy-py*w:.1f} Z" fill="#D9B764" stroke="#3A2A12" stroke-width="{R*.01:.2f}" stroke-linejoin="round"/>')
    s+=f'<circle cx="{cx}" cy="{cy}" r="{R*.055:.1f}" fill="url(#brass)" stroke="#3A2A12" stroke-width="{R*.01:.2f}"/>'
    # crystal
    s+=f'<circle cx="{cx}" cy="{cy}" r="{R*.9:.1f}" fill="url(#glass)"/>'
    s+=f'<path d="M{cx-R*.7:.1f},{cy-R*.4:.1f} A{R*.8:.1f},{R*.8:.1f} 0 0 1 {cx-R*.1:.1f},{cy-R*.78:.1f}" fill="none" stroke="#FFFFFF" stroke-width="{R*.02:.2f}" opacity=".35" stroke-linecap="round"/>'
    if crack:
        s+=(f'<path d="M{cx+R*.55:.1f},{cy-R*.62:.1f} L{cx+R*.3:.1f},{cy-R*.3:.1f} L{cx+R*.34:.1f},{cy-R*.12:.1f} L{cx+R*.05:.1f},{cy+R*.12:.1f} L{cx-R*.1:.1f},{cy+R*.4:.1f} L{cx-R*.28:.1f},{cy+R*.55:.1f}" fill="none" stroke="#E8F2F4" stroke-width="{max(.6,R*.012):.2f}" opacity=".9" stroke-linejoin="round"/>'
            f'<path d="M{cx+R*.3:.1f},{cy-R*.3:.1f} L{cx+R*.12:.1f},{cy-R*.34:.1f} M{cx+R*.05:.1f},{cy+R*.12:.1f} L{cx+R*.28:.1f},{cy+R*.24:.1f}" fill="none" stroke="#E8F2F4" stroke-width="{max(.5,R*.008):.2f}" opacity=".7"/>')
    return s
# ---------- hero
C=(275,445); R=135
cx,cy=C
o+=f'<circle cx="{cx}" cy="{cy}" r="300" fill="url(#halo)" opacity=".5"/>'
o+=f'<ellipse cx="{cx+10}" cy="{cy+R+45}" rx="190" ry="9" fill="#0B0A08" opacity=".6"/>'
# open lid (hinged on left), inside visible
lx=cx-R
o+=f'<ellipse cx="{lx-44+6}" cy="{cy+6}" rx="46" ry="{R}" fill="url(#brassD)" stroke="#14120E" stroke-width="1.2"/>'
o+=f'<ellipse cx="{lx-44}" cy="{cy}" rx="46" ry="{R}" fill="url(#lidIn)" stroke="#14120E" stroke-width="1.2"/>'
for k,(rx,ry) in enumerate(((39,R-8),(30,R-24),(21,R-42))):
    o+=f'<ellipse cx="{lx-44+5}" cy="{cy}" rx="{rx}" ry="{ry}" fill="none" stroke="#3A2A12" stroke-width="1" opacity=".6"/><ellipse cx="{lx-44+5}" cy="{cy}" rx="{rx}" ry="{ry}" fill="none" stroke="#F0D898" stroke-width=".4" opacity=".3" transform="translate(-1,-1)"/>'
o+=f'<path d="M{lx-44-30},{cy-60} q8,40 0,120" fill="none" stroke="#F0D898" stroke-width="2" opacity=".18"/>'
o+=text(lx-44+2,cy+3,"· PLUNDERSPELL ·",7,"#3A2A12","middle",2,MONO,f' transform="rotate(-90 {lx-44+2} {cy})" opacity=".7"')
# case back/thickness
o+=f'<circle cx="{cx+9}" cy="{cy+13}" r="{R}" fill="url(#brassD)" stroke="#14120E" stroke-width="1.2"/>'
o+=f'<circle cx="{cx+5}" cy="{cy+7}" r="{R}" fill="#5C4018" stroke="#14120E" stroke-width="1"/>'
# engine-turned case rim
o+=f'<circle cx="{cx}" cy="{cy}" r="{R}" fill="url(#brass)" stroke="#14120E" stroke-width="1.4"/>'
for k in range(72):
    a=math.radians(k*5)
    o+=f'<path d="M{cx+R*.92*math.sin(a):.1f},{cy-R*.92*math.cos(a):.1f} L{cx+R*.99*math.sin(a):.1f},{cy-R*.99*math.cos(a):.1f}" stroke="#6E4F1E" stroke-width=".9" opacity=".6"/>'
o+=f'<circle cx="{cx}" cy="{cy}" r="{R*.91}" fill="none" stroke="#F0D898" stroke-width="1.2" opacity=".5"/><circle cx="{cx}" cy="{cy}" r="{R*.915}" fill="none" stroke="#3A2A12" stroke-width="1.2"/>'
o+=f'<circle cx="{cx}" cy="{cy}" r="{R*.91}" fill="#C9A24C" stroke="#3A2A12" stroke-width="1"/>'
o+=f'<circle cx="{cx}" cy="{cy}" r="{R*.91}" fill="url(#brassD)" opacity=".35"/>'
# face
o+=f'<circle cx="{cx}" cy="{cy}" r="{R*.88}" fill="url(#halo)"/>'
o+=face(cx,cy,R*.98,20,"#7A6AA0",240,True,False)
# hinge knuckles
for yy in (cy-46,cy-8,cy+30):
    o+=f'<rect x="{lx-6}" y="{yy}" width="14" height="22" rx="5" fill="url(#brass)" stroke="#3A2A12" stroke-width="1"/><path d="M{lx-4},{yy+3} V{yy+19}" stroke="#F0D898" stroke-width=".8" opacity=".6"/>'
o+=f'<circle cx="{lx+1}" cy="{cy-46+11}" r="2" fill="#3A2A12"/>'
# crown & bow
top=cy-R
o+=f'<rect x="{cx-17}" y="{top-24}" width="34" height="26" rx="4" fill="url(#brass)" stroke="#3A2A12" stroke-width="1.2"/>'
for k in range(8): o+=f'<path d="M{cx-14+k*4},{top-22} V{top}" stroke="#6E4F1E" stroke-width="1"/>'
o+=f'<rect x="{cx-11}" y="{top-34}" width="22" height="12" rx="3" fill="url(#brass)" stroke="#3A2A12" stroke-width="1.2"/>'
bowc=(cx,top-58)
o+=f'<ellipse cx="{bowc[0]}" cy="{bowc[1]}" rx="25" ry="25" fill="none" stroke="#3A2A12" stroke-width="10"/><ellipse cx="{bowc[0]}" cy="{bowc[1]}" rx="25" ry="25" fill="none" stroke="url(#brass)" stroke-width="7"/><path d="M{cx-22},{bowc[1]-10} A25,25 0 0 1 {cx-6},{bowc[1]-24}" fill="none" stroke="#F8E2A0" stroke-width="1.4" opacity=".7"/>'
# chain: bezier from top of bow
P=[(cx,bowc[1]-25),(cx-10,bowc[1]-95),(cx+140,bowc[1]-100),(cx+165,bowc[1]-20)]
P2=[(cx+165,bowc[1]-20),(cx+178,bowc[1]+40),(cx+185,bowc[1]+95),(cx+168,bowc[1]+150)]
def bez(p,t):
    a=(1-t)**3;b=3*(1-t)**2*t;c=3*(1-t)*t*t;d=t**3
    return (a*p[0][0]+b*p[1][0]+c*p[2][0]+d*p[3][0],a*p[0][1]+b*p[1][1]+c*p[2][1]+d*p[3][1])
pts=[]
for pp in (P,P2):
    for i in range(0,31): pts.append(bez(pp,i/30))
# resample by arc length
acc=[0]
for i in range(1,len(pts)): acc.append(acc[-1]+math.dist(pts[i],pts[i-1]))
total=acc[-1]; n=int(total/9); links=[]
for k in range(n+1):
    d=k*total/n
    j=max(i for i in range(len(acc)) if acc[i]<=d); j=min(j,len(pts)-2)
    t=(d-acc[j])/(acc[j+1]-acc[j]+1e-9)
    x=pts[j][0]+(pts[j+1][0]-pts[j][0])*t; y=pts[j][1]+(pts[j+1][1]-pts[j][1])*t
    ang=math.degrees(math.atan2(pts[j+1][1]-pts[j][1],pts[j+1][0]-pts[j][0]))
    links.append((x,y,ang))
for k,(x,y,ang) in enumerate(links):
    rot=ang+(90 if k%2 else 0)
    ry=2.2 if k%2 else 4.6
    o+=f'<ellipse cx="{x:.1f}" cy="{y:.1f}" rx="5.6" ry="{ry}" transform="rotate({rot:.0f} {x:.1f} {y:.1f})" fill="none" stroke="#3A3D42" stroke-width="3.2"/>'
    o+=f'<ellipse cx="{x:.1f}" cy="{y:.1f}" rx="5.6" ry="{ry}" transform="rotate({rot:.0f} {x:.1f} {y:.1f})" fill="none" stroke="url(#steel)" stroke-width="1.8"/>'
ex,ey=links[-1][0],links[-1][1]
o+=f'<rect x="{ex-12:.1f}" y="{ey+2:.1f}" width="24" height="7" rx="3" fill="url(#steel)" stroke="#2A2D32" stroke-width="1"/>'
chain_dot=links[len(links)//4][:2]
# lid label dot positions
o+=callout('r',(cx+20,top-30),540,150,"Crown and bow","the chain runs through the bow")
o+=callout('r',chain_dot,540,206,"Chain","steel, short, dangling")
o+=callout('r',(cx+R-5,cy+8),540,262,"Brass hunter case","Ø 52 mm, engine-turned rim")
a=math.radians(100)
o+=callout('r',(cx+R*.7*math.sin(a),cy-R*.7*math.cos(a)),540,318,"Portal-light ring","30 ticks, one per raid minute")
# bottom callouts
o+='<path d="M80,545 L80,648 L94,648" fill="none" stroke="#9A9078" stroke-width=".8"/><circle cx="80" cy="545" r="2.6" fill="#DCD2BA" stroke="#14120E" stroke-width="1"/>'
o+=text(100,652,"Hinged lid",12)+text(100,666,"pin hinge",10,"#9A9078")
hx_,hy_=cx+math.sin(math.radians(240))*50,cy-math.cos(math.radians(240))*50
o+=f'<path d="M{hx_:.1f},{hy_:.1f} L{hx_:.1f},648 L250,648" fill="none" stroke="#9A9078" stroke-width=".8"/><circle cx="{hx_:.1f}" cy="{hy_:.1f}" r="2.6" fill="#DCD2BA" stroke="#14120E" stroke-width="1"/>'
o+=text(256,652,"The hand",12)+text(256,666,"stops where the light stops",10,"#9A9078")
# grab marker on case rim
o+=grab(cx+int(R*.72),cy+int(R*.72)+0,"GRAB",cx+int(R*.72)+12,cy+int(R*.72)+24)
# ---------- states panel
o+=text(760,150,"THE RING, THREE TIMES",11,"#635C4C","start",3)
for (fx,lit,col,hd,crack,lab,lc,gh) in ((810,20,"#7A6AA0",240,False,"20:00 LEFT","#DCD2BA","halo"),(950,5,"#9A8CD0",60,False,"5:00 · CHIME","#DCD2BA","halo"),(1090,1,"#C4542E",9,True,"0:45 · CRITICAL","#C4542E","haloM")):
    fy=225
    o+=f'<circle cx="{fx}" cy="{fy}" r="62" fill="url(#{gh})"/>'
    o+=f'<circle cx="{fx+2}" cy="{fy+3}" r="54" fill="url(#brassD)" stroke="#14120E" stroke-width="1"/>'
    o+=f'<circle cx="{fx}" cy="{fy}" r="52" fill="url(#brass)" stroke="#14120E" stroke-width="1"/><circle cx="{fx}" cy="{fy}" r="47" fill="none" stroke="#3A2A12" stroke-width="1"/>'
    o+=face(fx,fy,50,lit,col,hd,False,crack)
    o+=text(fx,fy+82,lab,11,lc,"middle",1)
o+=f'<path d="M1090,162 q10,-6 20,0 M1086,154 q14,-10 28,0" fill="none" stroke="#C4542E" stroke-width="1.2" opacity=".8" transform="translate(-12,0)"/>'
o+=text(1090,322,"ticks aloud",10.5,"#C4542E","middle",1)
o+=text(540,400,"T to raise · lid opens · chimes at 5 min and 1 min",10.5,"#9A9078")
o+=text(540,418,"empties anticlockwise; the hand rides the end of the light",10,"#635C4C")
o+=text(540,436,"never shows clock time",10,"#635C4C")
# ---------- ortho: front closed (1 mm = 2 px -> Ø104)
fx,fy=790,638
o+=f'<ellipse cx="{fx}" cy="690" rx="60" ry="5" fill="#0B0A08" opacity=".7"/>'
o+=f'<rect x="{fx-6}" y="{fy-52-14}" width="12" height="16" rx="2" fill="url(#brass)" stroke="#3A2A12" stroke-width="1"/><rect x="{fx-9}" y="{fy-52-9}" width="18" height="10" rx="2" fill="url(#brass)" stroke="#3A2A12" stroke-width="1"/>'
o+=f'<ellipse cx="{fx}" cy="{fy-52-24}" rx="12" ry="11" fill="none" stroke="#3A2A12" stroke-width="5.5"/><ellipse cx="{fx}" cy="{fy-52-24}" rx="12" ry="11" fill="none" stroke="url(#brass)" stroke-width="3.5"/>'
o+=f'<circle cx="{fx+2}" cy="{fy+2}" r="52" fill="url(#brassD)" stroke="#14120E" stroke-width="1"/><circle cx="{fx}" cy="{fy}" r="52" fill="url(#brass)" stroke="#14120E" stroke-width="1.2"/>'
o+=f'<circle cx="{fx}" cy="{fy}" r="44" fill="none" stroke="#3A2A12" stroke-width="1"/><circle cx="{fx}" cy="{fy}" r="40" fill="none" stroke="#F0D898" stroke-width=".6" opacity=".5"/>'
# closed hunter lid: engraved star
for k in range(12):
    a=math.radians(k*30); o+=f'<path d="M{fx},{fy} L{fx+36*math.sin(a):.1f},{fy-36*math.cos(a):.1f}" stroke="#6E4F1E" stroke-width=".8" opacity=".55"/>'
o+=f'<circle cx="{fx}" cy="{fy}" r="12" fill="url(#brassD)" stroke="#3A2A12" stroke-width="1"/><circle cx="{fx}" cy="{fy}" r="5" fill="#7A6AA0" opacity=".7"/>'
o+=f'<path d="M{fx-30},{fy-34} A42,42 0 0 1 {fx+4},{fy-42}" fill="none" stroke="#F8E2A0" stroke-width="1.6" opacity=".6" stroke-linecap="round"/>'
# side / hinge: slab 104 wide, 28 thick; hinge at left
sx,sy=980,662
o+=f'<ellipse cx="{sx+52}" cy="690" rx="70" ry="5" fill="#0B0A08" opacity=".7"/>'
o+=f'<rect x="{sx}" y="{sy+8}" width="104" height="20" rx="3" fill="url(#brass)" stroke="#14120E" stroke-width="1.1"/>'
o+=f'<rect x="{sx+4}" y="{sy+8}" width="96" height="3" fill="#7A9BA6" opacity=".8"/>'
o+=f'<path d="M{sx+4},{sy+22} H{sx+100}" stroke="#6E4F1E" stroke-width=".8"/>'
hx,hy=sx,sy+6; a=math.radians(112); L=104
ex,ey=hx+L*math.cos(a),hy-L*math.sin(a); nx,ny=math.sin(a)*7,math.cos(a)*7
o+=f'<polygon points="{hx},{hy} {ex:.1f},{ey:.1f} {ex+nx:.1f},{ey+ny:.1f} {hx+nx:.1f},{hy+ny:.1f}" fill="url(#brass)" stroke="#14120E" stroke-width="1.1"/>'
o+=f'<path d="M{hx+L},{hy} A{L},{L} 0 0 0 {ex:.1f},{ey:.1f}" fill="none" stroke="#5FA288" stroke-width="1.4" stroke-dasharray="5 4"/>'
o+=f'<path d="M{ex:.1f},{ey:.1f} A{L},{L} 0 0 0 {hx-L},{hy}" fill="none" stroke="#5FA288" stroke-width="1" stroke-dasharray="2 5" opacity=".6"/>'
o+=f'<circle cx="{hx}" cy="{hy}" r="6" fill="none" stroke="#5FA288" stroke-width="2"/><circle cx="{hx}" cy="{hy}" r="2" fill="#5FA288"/>'
o+=text(sx,540,"LID PIVOT",10.5,"#5FA288","middle",2)
o+=vlabel(275,730,"HERO 3/4 · LID OPEN · 1 mm = 5.2 px") if False else vlabel(275,730,"HERO 3/4 · LID OPEN")
o+=vlabel(790,730,"FRONT · CLOSED")+vlabel(1020,730,"SIDE · HINGE")
o+=text(1020,745,"",8)
o+=text(1160,708,"orthos 1 mm = 2 px",10,"#635C4C","end")
o+=palette([("#B58A3C","brass"),("#6E4F1E","brass shadow"),("#BFD4DA","crystal"),("#8F7FCF","lapis light"),("#C4542E","madder"),("#8E939A","chain steel")])
o+='</svg>\n'
open('docs/art/concept/diegetic/pocket-watch.svg','w').write(o)
print(len(o))
