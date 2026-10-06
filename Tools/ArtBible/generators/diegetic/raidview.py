# Writes docs/art/concept/raidview/{raid-view,alarm-fires}.svg (issue #284). Run from the repo root:
#   python3 Tools/ArtBible/generators/diegetic/raidview.py
# then render: NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs raidview
import math
M="Overpass Mono, monospace"
def T(x,y,s,fill="#9A9078",size=11,anchor="start",ls=0,font=M,w=None,extra=""):
    return f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}" text-anchor="{anchor}" letter-spacing="{ls}"{f" font-weight=\"{w}\"" if w else ""} {extra}>{s}</text>'
def base(h1,title,right=""):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 800" width="1200" height="800">
<defs>
<radialGradient id="bg" cx="35%" cy="55%" r="70%"><stop offset="0" stop-color="#2A2012" stop-opacity=".8"/><stop offset="1" stop-color="#14120E" stop-opacity="0"/></radialGradient>
</defs>
<rect width="1200" height="800" fill="#14120E"/><rect width="1200" height="800" fill="url(#bg)"/>
<rect x="12" y="12" width="1176" height="776" fill="none" stroke="#332D22"/>
{T(40,54,h1,"#635C4C",12,ls=3)}{T(40,96,title,"#DCD2BA",38,font="Eczar, Georgia, serif",w=700)}{right}
'''
def pal(items,y=748):
    s='<g id="palette">'
    for i,(c,n) in enumerate(items):
        x=40+i*190
        s+=f'<rect x="{x}" y="{y}" width="34" height="18" fill="{c}" stroke="#332D22" stroke-width=".6"/>'+T(x+40,y+8,c,size=10)+T(x+40,y+19,n,"#635C4C",10)
    return s+'</g>\n'

# ---------- sheet 1
def stone_walls(p):
    s=''
    # side wall coursing
    for k in range(1,9):
        f=k/9
        yl=f*300; yr=95+f*90
        s+=f'<line x1="0" y1="{yl:.1f}" x2="200" y2="{yr:.1f}" stroke="#0E0C09" stroke-width="1" opacity=".7"/>'
        s+=f'<line x1="540" y1="{yl:.1f}" x2="340" y2="{yr:.1f}" stroke="#0E0C09" stroke-width="1" opacity=".7"/>'
    for x in [30,70,115,160,190]:
        f=x/200; ya=f*95; yb=300-f*115
        s+=f'<line x1="{x}" y1="{ya:.1f}" x2="{x}" y2="{yb:.1f}" stroke="#0E0C09" stroke-width=".8" opacity=".5"/>'
        s+=f'<line x1="{540-x}" y1="{ya:.1f}" x2="{540-x}" y2="{yb:.1f}" stroke="#0E0C09" stroke-width=".8" opacity=".5"/>'
    for r in range(8):
        y=95+r*11.25
        s+=f'<line x1="200" y1="{y:.1f}" x2="340" y2="{y:.1f}" stroke="#0E0C09" stroke-width=".8" opacity=".6"/>'
        for x in range(200+(r%2)*10,340,20): s+=f'<line x1="{x}" y1="{y:.1f}" x2="{x}" y2="{y+11.25:.1f}" stroke="#0E0C09" stroke-width=".7" opacity=".5"/>'
    # floor flags
    for k in range(1,7):
        y=185+(k/7)**1.6*115
        s+=f'<line x1="{200-(y-185)*200/115:.1f}" y1="{y:.1f}" x2="{340+(y-185)*200/115:.1f}" y2="{y:.1f}" stroke="#0E0C09" stroke-width=".9" opacity=".6"/>'
    for xf in [-.5,0,.5]:
        s+=f'<line x1="{270+xf*140}" y1="185" x2="{270+xf*140*4.5:.1f}" y2="300" stroke="#0E0C09" stroke-width=".8" opacity=".5"/>'
    return s
def scene(fid,flame,glowc,alerted,rim):
    s=f'<clipPath id="c{fid}"><rect x="0" y="0" width="540" height="300"/></clipPath><g clip-path="url(#c{fid})">'
    s+='<rect width="540" height="300" fill="#0B0A08"/>'
    s+='<polygon points="0,0 540,0 340,95 200,95" fill="url(#ceil)"/><polygon points="0,300 540,300 340,185 200,185" fill="url(#floor)"/>'
    s+='<polygon points="0,0 200,95 200,185 0,300" fill="url(#wallL)"/><polygon points="540,0 340,95 340,185 540,300" fill="url(#wallR)"/>'
    s+='<rect x="200" y="95" width="140" height="90" fill="url(#back)"/>'
    s+=stone_walls(fid)
    # arched doorway dark far
    s+='<path d="M245 185 V135 a25 25 0 0 1 50 0 V185Z" fill="#05040A" stroke="#0E0C09"/>'
    # torch on left wall
    s+='<polygon points="88,150 102,146 100,184 90,186" fill="#2A2218" stroke="#0E0C09" stroke-width=".8"/><rect x="84" y="140" width="22" height="9" rx="2" fill="#3B3226"/>'
    s+=f'<circle cx="96" cy="124" r="140" fill="url(#{glowc})"/>'
    s+=f'<path d="M96 142 C84 130 90 118 94 104 C96 114 104 118 102 128 C108 124 108 118 106 114 C114 124 110 138 96 142Z" fill="{flame[0]}"/>'
    s+=f'<path d="M96 140 C90 132 93 124 96 116 C99 124 102 130 96 140Z" fill="{flame[1]}"/>'
    # table + goblet
    s+='<polygon points="218,172 322,172 332,180 208,180" fill="#4A3826"/><rect x="214" y="180" width="112" height="5" fill="#2E2218"/><rect x="222" y="185" width="7" height="30" fill="#2E2218"/><rect x="311" y="185" width="7" height="30" fill="#2E2218"/>'
    s+='<ellipse cx="270" cy="173" rx="26" ry="3" fill="#000" opacity=".4"/>'
    gob='<path d="M260 138 H280 C281 150 276 156 272 158 V166 H278 V169 H262 V166 H268 V158 C264 156 259 150 260 138Z"'
    if rim: s+=f'<ellipse cx="270" cy="152" rx="26" ry="30" fill="url(#rimg)"/>'+gob+' fill="#C9A227" stroke="#5FA288" stroke-width="3" stroke-linejoin="round" opacity=".9"/>'
    s+=gob+' fill="url(#gold)" '+('stroke="#5FA288" stroke-width="1.2"' if rim else '')+'/><ellipse cx="270" cy="138.5" rx="10" ry="2" fill="#E8CE60"/>'
    # vignette
    s+='<rect width="540" height="300" fill="url(#vig)"/>'
    return s
def defs1():
    return '''<defs>
<linearGradient id="ceil" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#0A0908"/><stop offset="1" stop-color="#1A1610"/></linearGradient>
<linearGradient id="floor" x1="0" y1="1" x2="0" y2="0"><stop offset="0" stop-color="#14110C"/><stop offset="1" stop-color="#2A241A"/></linearGradient>
<linearGradient id="wallL" x1="1" y1="0" x2="0" y2="0"><stop offset="0" stop-color="#3A332A"/><stop offset="1" stop-color="#1E1A14"/></linearGradient>
<linearGradient id="wallR" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#2C271F"/><stop offset="1" stop-color="#14120E"/></linearGradient>
<linearGradient id="back" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2B261E"/><stop offset="1" stop-color="#3A3328"/></linearGradient>
<linearGradient id="gold" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#F0D875"/><stop offset=".5" stop-color="#C9A227"/><stop offset="1" stop-color="#7A5F12"/></linearGradient>
<radialGradient id="glowA" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#E0A04A" stop-opacity=".55"/><stop offset=".5" stop-color="#E0A04A" stop-opacity=".16"/><stop offset="1" stop-color="#E0A04A" stop-opacity="0"/></radialGradient>
<radialGradient id="glowR" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#C4542E" stop-opacity=".6"/><stop offset=".5" stop-color="#C4542E" stop-opacity=".2"/><stop offset="1" stop-color="#C4542E" stop-opacity="0"/></radialGradient>
<radialGradient id="rimg" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#5FA288" stop-opacity=".35"/><stop offset="1" stop-color="#5FA288" stop-opacity="0"/></radialGradient>
<radialGradient id="vig" cx=".5" cy=".5" r=".75"><stop offset=".55" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity=".6"/></radialGradient>
<radialGradient id="lap" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#B6A8E0" stop-opacity=".9"/><stop offset=".4" stop-color="#7A6AA0" stop-opacity=".5"/><stop offset="1" stop-color="#7A6AA0" stop-opacity="0"/></radialGradient>
<linearGradient id="brass" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#E2BE6A"/><stop offset=".5" stop-color="#A9812F"/><stop offset="1" stop-color="#5E4515"/></linearGradient>
<linearGradient id="skin" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#7A5E4A"/><stop offset="1" stop-color="#3C2C22"/></linearGradient>
<linearGradient id="sleeve" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#241E2E"/><stop offset="1" stop-color="#14101C"/></linearGradient>
</defs>'''
def hud():
    s=''
    s+=T(12,20,"RAIDING · ALERTED","#DCD2BA",10,ls=1)
    for i in range(4): s+=f'<rect x="{12+i*26}" y="26" width="23" height="7" fill="{"#C4542E" if i<3 else "none"}" stroke="#C4542E" stroke-width="1"/>'
    s+=T(528,20,"OWED 1,200 · BANKED 340","#C9A227",10,"end")+T(528,34,"HAUL 3 PIECES · 410","#C9A227",10,"end")
    s+='<g stroke="#DCD2BA" stroke-width="1.6" style="filter:drop-shadow(0 0 1px #000)"><line x1="262" y1="150" x2="278" y2="150"/><line x1="270" y1="142" x2="270" y2="158"/></g>'
    s+=T(270,200,"E — PICK UP GOLDEN GOBLET","#DCD2BA",9.5,"middle")
    s+='<rect x="458" y="100" width="72" height="92" fill="#14120E" fill-opacity=".8" stroke="#635C4C"/>'
    for i,n in enumerate(["IGNIS","GLACIES","VENTUS","LEVO"]): s+=T(466,120+i*20,n,"#7A6AA0",10)
    s+='<rect x="200" y="276" width="140" height="8" fill="#14120E" stroke="#7A6AA0"/><rect x="200" y="276" width="70" height="8" fill="#7A6AA0"/>'
    s+=T(200,271,"whisper","#9A9078",8.5)+T(340,271,"shout","#9A9078",8.5,"end")
    for n,(x,y) in {1:(158,27),2:(358,28),3:(398,196),4:(442,110),5:(180,284),6:(30,262)}.items():
        pass
    return s
def badge(n,x,y,c="#DCD2BA"):
    return f'<circle cx="{x}" cy="{y}" r="8" fill="#14120E" stroke="{c}"/>'+T(x,y+3.5,n,c,10,"middle",w=600)
def hands():
    s=''
    # left forearm + watch
    s+='<path d="M0 232 L60 250 L92 300 L0 300Z" fill="url(#sleeve)"/>'
    s+='<path d="M52 262 C72 252 96 262 112 284 L118 300 L70 300Z" fill="url(#skin)"/>'
    s+='<circle cx="96" cy="256" r="70" fill="url(#lap)" opacity=".35"/>'
    s+='<circle cx="96" cy="262" r="44" fill="url(#brass)" stroke="#3A2A0C" stroke-width="1.5"/><circle cx="96" cy="262" r="38" fill="#14101C" stroke="#E2BE6A" stroke-width=".8"/>'
    s+='<circle cx="96" cy="262" r="28" fill="none" stroke="#2A2440" stroke-width="5"/>'
    s+='<circle cx="96" cy="262" r="28" fill="none" stroke="#9A8AD0" stroke-width="5" stroke-linecap="round" stroke-dasharray="117.3 58.7" transform="rotate(-90 96 262)"/>'
    s+='<circle cx="96" cy="262" r="28" fill="none" stroke="#fff" stroke-opacity=".25" stroke-width="1.5" stroke-dasharray="117.3 58.7" transform="rotate(-90 96 262)"/>'
    s+='<line x1="96" y1="262" x2="96" y2="244" stroke="#DCD2BA" stroke-width="1.4"/><line x1="96" y1="262" x2="108" y2="268" stroke="#DCD2BA" stroke-width="1.8"/><circle cx="96" cy="262" r="2.5" fill="#E2BE6A"/>'
    s+='<path d="M60 236 L72 224 L82 228 L74 242Z" fill="url(#brass)" stroke="#3A2A0C"/>'  # open lid hint
    # right hand
    s+='<path d="M540 250 L470 270 L440 300 L540 300Z" fill="url(#sleeve)"/>'
    s+='<ellipse cx="440" cy="272" rx="46" ry="16" fill="url(#skin)"/>'
    for i in range(4): s+=f'<ellipse cx="{404+i*10}" cy="{262-(i%3)*2}" rx="4" ry="12" fill="#6A5242" transform="rotate({-20+i*6} {404+i*10} 262)"/>'
    s+='<ellipse cx="456" cy="262" rx="14" ry="6" fill="#6A5242" transform="rotate(-25 456 262)"/>'
    s+='<circle cx="436" cy="258" r="42" fill="url(#lap)"/><circle cx="436" cy="262" r="8" fill="#E3DAFF"/>'
    s+='<ellipse cx="436" cy="262" rx="52" ry="18" fill="none" stroke="#7A6AA0" stroke-width=".8" stroke-dasharray="2 3"/>'
    for i in range(12):
        a=i/12*2*math.pi; x=436+52*math.cos(a); y=262+18*math.sin(a)
        g=["M-3 -3 L3 3 M-3 3 L3 -3","M0 -4 V4 M-3 -1 H3","M-3 3 L0 -4 L3 3","M-3 -3 H3 V3","M-3 0 H3 M0 -3 V3 M-2 -2 L2 2"][i%5]
        s+=f'<path d="{g}" transform="translate({x:.1f} {y:.1f})" stroke="#B6A8E0" stroke-width="1.3" fill="none" stroke-linecap="round" opacity="{.95 if i<8 else .35}"/>'
    for i,ch in enumerate("IGNIS"):
        s+=T(406+i*15,228-i%2*6,ch,"#B6A8E0",15,"middle",font="Eczar, Georgia, serif",w=700,extra=f'opacity="{.9-i*.12:.2f}"')
    s+='<g fill="#B6A8E0" opacity=".7"><circle cx="440" cy="205" r="1.4"/><circle cx="414" cy="212" r="1"/><circle cx="462" cy="214" r="1.2"/></g>'
    return s
sheet=base("DIEGETIC KIT · THE RAID, BEFORE AND AFTER","One frame, no HUD",T(1160,54,"same corridor, same torch, same goblet","#9A9078",12,"end")+T(1160,76,"17 HUD elements → the world, the hands, the Lair","#9A9078",12,"end"))
sheet+=defs1()
FY=130
for fx,lab,sub,rim,fl,gl,hudon in [(40,"TODAY","flat overlay, text on screen",False,("#E0A04A","#FFE9A8"),"glowA",True),(620,"PROPOSED","the world and the hands say it",True,("#C4542E","#FFB07A"),"glowR",False)]:
    fid='L' if hudon else 'R'
    sheet+=T(fx,FY-12,lab,"#DCD2BA",13,ls=4,w=600)+T(fx+540,FY-12,sub,"#635C4C",11,"end")
    sheet+=f'<g transform="translate({fx} {FY})">'+scene(fid,fl,gl,not hudon,rim)
    if hudon:
        sheet+=hud()
    else:
        sheet+=hands()+'<g stroke="#DCD2BA" stroke-width="1.2" opacity=".9"><line x1="262" y1="150" x2="278" y2="150"/><line x1="270" y1="142" x2="270" y2="158"/></g>'
    sheet+='</g></g>'  # close clip group
    sheet+=f'<rect x="{fx}" y="{FY}" width="540" height="300" fill="none" stroke="#635C4C" stroke-width="1.2"/><rect x="{fx-4}" y="{FY-4}" width="548" height="308" fill="none" stroke="#332D22"/>'
# badges
b={1:(40+160,130+27),2:(40+360,130+48),3:(40+300,130+170),4:(40+444,130+196),5:(40+345,130+280),6:(40+16,130+280)}
bn={1:(620+82,130+100),2:None,3:(620+300,130+160),4:None,5:(620+470,130+240),6:(620+60,130+232)}
for n,(x,y) in b.items(): sheet+=badge(n,x,y,"#C9A227" if n==2 else "#DCD2BA")
for n,p in bn.items():
    if p: sheet+=badge(n,*p,"#5FA288")
old=["Alarm bar and phase text","Debt, banked, haul numbers","Interaction prompt","Spell list panel","Mic loudness meter","Raid timer"]
new=["Torch colour: the fires go red","Leave the raid: Lair ledger, strongbox, portal pile","Verdigris rim-light on the goblet","Grimoire: hold Tab, spell word written large","Palm glow: lapis light, size is loudness","Pocket watch: hold T, ring of light empties"]
for i in range(6):
    y=468+i*30
    sheet+=badge(i+1,52,y-3,"#9A9078")+T(70,y,old[i],"#9A9078",11.5)
    sheet+=badge(i+1,632,y-3,"#5FA288")+T(650,y,new[i],"#DCD2BA",11.5)
sheet+='<line x1="600" y1="450" x2="600" y2="640" stroke="#332D22"/>'
sheet+='<rect x="40" y="664" width="1120" height="1" fill="#332D22"/>'
sheet+=T(40,690,"STAYS FLAT: crosshair · damage feedback","#C4542E",12,ls=2)
sheet+=T(40,708,"Nothing else is drawn on the screen in the proposed frame. Letters rising from the palm are the cast line, in the world.","#635C4C",10.5)
sheet+=pal([("#DCD2BA","vellum"),("#5FA288","verdigris"),("#C9A227","orpiment"),("#C4542E","madder"),("#7A6AA0","lapis"),("#1E1A14","ash")])
sheet+='</svg>'
open("docs/art/concept/raidview/raid-view.svg","w").write(sheet)

# ---------- sheet 2
s=base("DIEGETIC KIT · THE ALARM, READ FROM THE FIRES","Four fires",T(1160,54,"same brazier, four alarm levels","#9A9078",12,"end")+T(1160,76,"tint blends continuously with the level","#9A9078",12,"end"))
s+='''<defs>
<linearGradient id="stone" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2E2920"/><stop offset="1" stop-color="#1A1610"/></linearGradient>
<linearGradient id="iron" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#4A443A"/><stop offset="1" stop-color="#1E1A16"/></linearGradient>
<radialGradient id="gA" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#E0A04A" stop-opacity=".6"/><stop offset=".5" stop-color="#E0A04A" stop-opacity=".18"/><stop offset="1" stop-color="#E0A04A" stop-opacity="0"/></radialGradient>
<radialGradient id="gR" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#C4542E" stop-opacity=".7"/><stop offset=".5" stop-color="#C4542E" stop-opacity=".25"/><stop offset="1" stop-color="#C4542E" stop-opacity="0"/></radialGradient>
<radialGradient id="gF" cx=".5" cy=".5" r=".5"><stop offset="0" stop-color="#E8452A" stop-opacity=".8"/><stop offset=".5" stop-color="#C4542E" stop-opacity=".3"/><stop offset="1" stop-color="#C4542E" stop-opacity="0"/></radialGradient>
<linearGradient id="bar" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#E0A04A"/><stop offset=".5" stop-color="#D07A3A"/><stop offset="1" stop-color="#C4542E"/></linearGradient>
</defs>'''
W=262; X0=40; GAP=24; Y0=130; H=400
def course(x,y,w,h,seed):
    r=''
    for j in range(int(h/25)+1):
        yy=y+j*25
        r+=f'<line x1="{x}" y1="{yy}" x2="{x+w}" y2="{yy}" stroke="#0E0C09" stroke-width="1" opacity=".7"/>'
        for xx in range(int(x)+((j+seed)%2)*35,int(x+w),70): r+=f'<line x1="{xx}" y1="{yy}" x2="{xx}" y2="{min(yy+25,y+h)}" stroke="#0E0C09" stroke-width=".8" opacity=".6"/>'
    return r
def brazier(cx,by):
    return (f'<path d="M{cx-34} {by-40} L{cx-30} {by-40} L{cx-18} {by} L{cx+18} {by} L{cx+30} {by-40} L{cx+34} {by-40} L{cx+24} {by+6} L{cx-24} {by+6}Z" fill="url(#iron)" stroke="#0E0C09"/>'
      f'<rect x="{cx-36}" y="{by-44}" width="72" height="5" rx="2" fill="#5A5246"/><path d="M{cx-14} {by+6} V{by+34} M{cx+14} {by+6} V{by+34}" stroke="#3A342A" stroke-width="4"/><rect x="{cx-22}" y="{by+32}" width="44" height="6" fill="#3A342A"/>')
def flame(cx,by,kind):
    b=by-42
    if kind=="calm": cols=("#E0A04A","#FFE2A0"); hh=70; lean=0
    if kind=="susp": cols=("#D8923E","#F5C878"); hh=58; lean=18
    if kind=="alert": cols=("#C4542E","#F0905A"); hh=88; lean=0
    if kind=="cry": cols=("#E8452A","#FFB070"); hh=135; lean=-6
    w=24 if kind!="cry" else 30
    o=f'<path d="M{cx-w} {b} C{cx-w-6} {b-hh*.4} {cx-8+lean*.5} {b-hh*.6} {cx+lean} {b-hh} C{cx+8+lean*.5} {b-hh*.55} {cx+w+6} {b-hh*.4} {cx+w} {b}Z" fill="{cols[0]}"/>'
    o+=f'<path d="M{cx-w*.55} {b} C{cx-w*.7} {b-hh*.35} {cx-3+lean*.4} {b-hh*.45} {cx+lean*.7} {b-hh*.68} C{cx+4+lean*.4} {b-hh*.4} {cx+w*.7} {b-hh*.3} {cx+w*.55} {b}Z" fill="{cols[1]}"/>'
    if kind=="susp":
        o+=f'<path d="M{cx+w*.6} {b} C{cx+w+10} {b-hh*.3} {cx+lean+24} {b-hh*.6} {cx+lean+30} {b-hh*.8} C{cx+lean+10} {b-hh*.7} {cx+lean} {b-hh*.6} {cx+lean-2} {b-hh*.5}Z" fill="{cols[0]}" opacity=".85"/>'
    return o
panels=[("CALM","calm","gA",.0,"steady amber, warm round glow",210),("SUSPICIOUS","susp","gA",.0,"flames lean and gutter, glow smaller, flickers",170),("ALERTED","alert","gR",.0,"madder flame and madder light on the stone",250),("HUE AND CRY","cry","gF",.0,"tall red flare, embers, the chapel bell tolls",285)]
import random; random.seed(4)
for i,(name,k,g,_,desc,gr) in enumerate(panels):
    x=X0+i*(W+GAP); cx=x+W/2; by=Y0+250
    s+=f'<clipPath id="p{i}"><rect x="{x}" y="{Y0}" width="{W}" height="{H}"/></clipPath><g clip-path="url(#p{i})">'
    s+=f'<rect x="{x}" y="{Y0}" width="{W}" height="{H}" fill="url(#stone)"/>'+course(x,Y0,W,H,i)
    cy=by-90
    s+=f'<circle cx="{cx}" cy="{cy}" r="{gr}" fill="url(#{g})"/>'
    if k=="susp":
        s+=f'<circle cx="{cx}" cy="{cy}" r="{gr*.8}" fill="none" stroke="#E0A04A" stroke-opacity=".25" stroke-dasharray="6 8"/>'
    s+=f'<rect x="{x}" y="{Y0}" width="{W}" height="{H}" fill="none"/>'
    s+=brazier(cx,by)+flame(cx,by,k)
    if k=="susp":
        for dx in (-40,-52,-30): s+=f'<path d="M{cx+dx} {by-80+dx*0.3} q-8 -6 -2 -14 q6 -8 -2 -16" fill="none" stroke="#F5C878" stroke-opacity=".7" stroke-width="1.4" stroke-linecap="round"/>'
        s+=f'<path d="M{cx+46} {by-100} q8 -6 2 -14 M{cx+58} {by-88} q8 -6 2 -14" fill="none" stroke="#F5C878" stroke-opacity=".7" stroke-width="1.4" stroke-linecap="round"/>'
    if k=="cry":
        for _ in range(26):
            ex=cx+random.uniform(-70,70); ey=by-80-random.uniform(20,200); r=random.uniform(1,2.4)
            s+=f'<circle cx="{ex:.1f}" cy="{ey:.1f}" r="{r:.1f}" fill="{random.choice(["#FFB070","#E8452A","#F0905A"])}"/>'
        # bell
        bx=x+W-44; byy=Y0+58
        s+=f'<path d="M{bx-12} {byy+16} C{bx-12} {byy+4} {bx-6} {byy-6} {bx} {byy-6} C{bx+6} {byy-6} {bx+12} {byy+4} {bx+12} {byy+16} H{bx+16} V{byy+20} H{bx-16} V{byy+16}Z" fill="#C9A227" stroke="#14120E"/><circle cx="{bx}" cy="{byy+24}" r="3" fill="#8A6C14"/><rect x="{bx-1.5}" y="{byy-12}" width="3" height="6" fill="#C9A227"/>'
        for r_ in (22,32): s+=f'<path d="M{bx-r_} {byy+4} a{r_} {r_} 0 0 1 0 -20 M{bx+r_} {byy+4} a{r_} {r_} 0 0 0 0 -20" fill="none" stroke="#DCD2BA" stroke-opacity=".6" stroke-width="1.3" transform="translate(0 6)"/>'
        s+=T(bx,byy+52,"BELL TOLLS","#DCD2BA",9.5,"middle",ls=1.5)
    s+='</g>'
    s+=f'<rect x="{x}" y="{Y0}" width="{W}" height="{H}" fill="none" stroke="#635C4C"/>'
    s+=T(cx,Y0+H+24,name,"#DCD2BA",13,"middle",ls=3,w=600)+T(cx,Y0+H+41,desc,"#9A9078",9.5,"middle")
by=Y0+H+66
s+=f'<rect x="40" y="{by}" width="1120" height="16" fill="url(#bar)" stroke="#332D22"/>'
s+=T(40,by+34,"ALARM LEVEL 0 → 100 · tint blends continuously","#DCD2BA",12,ls=1)
s+=T(1160,by+34,"0 CALM · · · 100 HUE AND CRY","#635C4C",10.5,"end")
s+=T(40,by+54,"every zone gets at least one alarm light (generator rule)","#9A9078",11)
s+=T(1160,by+54,"crackle grows louder with the level","#9A9078",11,"end")
s+=pal([("#E0A04A","calm amber"),("#D8923E","gutter amber"),("#C4542E","madder flame"),("#E8452A","flare red"),("#FFB070","ember"),("#2E2920","stone")])
s+='</svg>'
open("docs/art/concept/raidview/alarm-fires.svg","w").write(s)
