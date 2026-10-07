"""Builds index.html, the wizards proposal page, with the plates from plates.py inlined."""
import pathlib
from plates import plate_wizard, plate_lobby, plate_distance, plate_fallen, plate_things

CSS = """
:root {
  --ground: #14120E; --surface: #1E1A14; --line: #332D22;
  --text: #DCD2BA; --dim: #9A9078; --faint: #635C4C;
  --verdigris: #5FA288; --orpiment: #C9A227; --madder: #C4542E; --lapis: #9A8BC4;
  --display: "Eczar", Georgia, serif; --body: "Spectral", Georgia, serif;
  --mono: "Overpass Mono", ui-monospace, Menlo, monospace;
  color-scheme: dark;
}
@media (prefers-color-scheme: light) { :root:not([data-theme="dark"]) {
  --ground: #EFE9DA; --surface: #E5DDC9; --line: #CDBF9F;
  --text: #241F17; --dim: #5B5241; --faint: #84796290;
  --verdigris: #2F6E58; --orpiment: #8A6A0E; --madder: #A63E1C; --lapis: #5A4A86;
  color-scheme: light; } }
:root[data-theme="light"] {
  --ground: #EFE9DA; --surface: #E5DDC9; --line: #CDBF9F;
  --text: #241F17; --dim: #5B5241; --faint: #84796290;
  --verdigris: #2F6E58; --orpiment: #8A6A0E; --madder: #A63E1C; --lapis: #5A4A86;
  color-scheme: light; }
:root[data-theme="dark"] {
  --ground: #14120E; --surface: #1E1A14; --line: #332D22;
  --text: #DCD2BA; --dim: #9A9078; --faint: #635C4C;
  --verdigris: #5FA288; --orpiment: #C9A227; --madder: #C4542E; --lapis: #9A8BC4;
  color-scheme: dark; }

* { box-sizing: border-box; }
body { margin: 0; background: var(--ground); color: var(--text); font: 400 1.08rem/1.6 var(--body);
  padding-inline: 16px; padding-block: 0 4rem; }
main { max-width: 68ch; margin-inline: auto; display: grid; grid-template-columns: minmax(0, 1fr); gap: 1.1rem; }
.plate { margin: .6rem 0 1rem; display: grid; gap: .4rem; justify-self: center;
  width: min(1100px, calc(100vw - 32px)); }
.plate svg { width: 100%; height: auto; border: 1px solid var(--line); display: block; }
.plate figcaption { font: 400 .78rem/1.45 var(--mono); color: var(--dim); }

header { padding-block: 3.2rem 1rem; display: grid; gap: .6rem; }
.eyebrow { font: 600 .74rem/1 var(--mono); letter-spacing: .22em; text-transform: uppercase; color: var(--faint); }
h1 { font: 700 clamp(2.1rem, 6vw, 3.4rem)/1.05 var(--display); margin: 0; text-wrap: balance; }
h2 { font: 700 1.7rem/1.15 var(--display); margin: 2.2rem 0 0; text-wrap: balance;
  padding-top: 1.2rem; border-top: 1px solid var(--line); }
h3 { font: 600 1.15rem/1.3 var(--display); margin: .6rem 0 0; }
p, li { margin: 0; }
ul, ol { margin: 0; padding-left: 1.3rem; display: grid; gap: .35rem; }
.lede { font-size: 1.2rem; }
.status { font: 400 .8rem/1.5 var(--mono); color: var(--dim); }
.status b { color: var(--madder); font-weight: 600; }

.tablewrap { overflow-x: auto; border: 1px solid var(--line); }
table { border-collapse: collapse; width: 100%; font-size: .93rem; min-width: 0; }
th, td { text-align: left; vertical-align: top; padding: .5rem .65rem; border-bottom: 1px solid var(--line); }
th { font: 600 .7rem/1.3 var(--mono); letter-spacing: .12em; text-transform: uppercase; color: var(--faint);
  background: var(--surface); }
tr:last-child td { border-bottom: 0; }
td.who { font: 700 1.15rem/1.2 var(--display); white-space: nowrap; }
.dot { display: inline-block; width: .85em; height: .85em; vertical-align: -.08em; margin-right: .45em;
  border: 1px solid var(--line); }

.chips { display: grid; gap: .5rem; }
.chips div { display: grid; gap: .35rem; }
.chips span span { white-space: nowrap; }
.chips b { font: 600 .7rem/1.3 var(--mono); letter-spacing: .12em; text-transform: uppercase; color: var(--faint); }
.chips span { display: flex; flex-wrap: wrap; gap: .4rem .9rem; font: 400 .8rem/1.4 var(--mono); color: var(--dim); }
.chips i { display: inline-block; width: 1.6rem; height: 1rem; vertical-align: -.18em; margin-right: .35rem;
  border: 1px solid var(--line); }

.decisions { display: grid; gap: .7rem; padding-left: 1.4rem; }
.decisions li::marker { font: 700 1rem var(--display); color: var(--madder); }
footer { margin-top: 2.5rem; font: 400 .78rem/1.6 var(--mono); color: var(--faint); border-top: 1px solid var(--line); padding-top: 1rem; }
"""

page = f"""<title>Plunderspell Wizards Proposal</title>
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Eczar:wght@600;700&family=Overpass+Mono:wght@400;600&family=Spectral:ital,wght@0,400;0,600;1,400&display=swap">
<style>{CSS}</style>
<main>
<header>
  <div class="eyebrow">Plunderspell · proposal · the wizards</div>
  <h1>One wizard, your colour, your name on your hat</h1>
  <p class="lede">Every player wears the same outfit. You pick any colour you like for it, as long as nobody in the raid already wears that exact colour. Your name runs around your hat band. When you die, your hat is all that's left of you, and your friends carry it home.</p>
  <p class="status"><b>Proposal</b> · 2026-10-07 · nothing is built · your four decisions are written in</p>
</header>

<figure class="plate" aria-label="Front view of the wizard in blue: a tall bent hat with the name Hollowmere on its band, a short shoulder cape over a long robe, a belt carrying a small book, a brass watch and a coin pouch, one bare hand and one gloved hand.">
{plate_wizard()}
<figcaption>Concept · the wizard, front view, for a player called Hollowmere.</figcaption></figure>

<h2>Who you play</h2>
<ul>
  <li>A hedge-wizard and a thief, in debt, raiding castles through a portal. A tall hat, a long robe, a short shoulder cape, a belt and soft boots for sneaking.</li>
  <li>What the game already gives you has a place on the body: the grimoire at the hip, the portal watch on a chain, the coin pouch on the belt.</li>
  <li>Two different hands. The bare one casts, and glows when you speak. The gloved one grabs and carries loot.</li>
  <li>Everyone wears the same outfit. That means one figure to build and animate, and any new movement works for every player at once.</li>
</ul>

<h2>Set in the lobby</h2>
<figure class="plate" aria-label="Four wizards in blue, wine, green and bone, each with their colour's red, green and blue values. Below left, a colour picker with three sliders set to 142, 52, 70, its button greyed out because nyx_77 already wears exactly that colour. Below right, a choice between using your username or a different name, with a long username shrunk to fit a hat band.">
{plate_lobby()}
<figcaption>Concept · a party of four, the colour picker, and the name on the hat.</figcaption></figure>
<h3>Your colour</h3>
<ul>
  <li>Three sliders, red, green and blue, each from 0 to 255. Any mix is allowed.</li>
  <li>No two players in a raid can wear exactly the same colour. Whoever picks it first keeps it; anyone else who lands on it is told who has it and can't wear it until they move a slider.</li>
  <li>The hat, cape, robe and sleeves take your colour. The belt, boots and leather stay the same for everyone.</li>
</ul>
<h3>The name on your hat</h3>
<ul>
  <li>Two clear choices: <b>use my username</b>, or <b>choose a different name for the game</b>.</li>
  <li>Long names are never cut. They shrink to fit the band, so a short name stitches large and a long one small.</li>
  <li>The thread is pale on dark colours and dark on pale ones, so the name always shows.</li>
</ul>

<h2>Who is who</h2>
<figure class="plate" aria-label="Three panels. Far: four wizards down a corridor, told apart by colour. Facing you: a close-up of a wine-red wizard with nyx_77 on the hat band. Behind: a green wizard from the back with Pip on the hat band.">
{plate_distance()}
<figcaption>Concept · colour from afar, the name up close.</figcaption></figure>
<ul>
  <li>The name is only on the hat. The band runs all the way round, so it reads from the front, the side and behind.</li>
  <li>Colour reads first, from further away. The name settles it up close, and it works for players who can't tell some colours apart.</li>
</ul>

<h2>When a wizard falls</h2>
<figure class="plate" aria-label="Two panels. Left: a wine-red hat lying on the floor where its wizard fell, nyx_77 on its band, a faint ring of ash around it. Right: the green wizard Pip, seen from behind, walking toward a violet portal with nyx_77's hat in the gloved hand.">
{plate_fallen()}
<figcaption>Concept · the hat is what remains.</figcaption></figure>
<ul>
  <li>The body is gone. The hat drops where the wizard fell, still in their colour and with their name on the band, so nobody wonders who it was.</li>
  <li>A friend picks it up with the gloved hand and carries it through the portal, which brings that wizard back in the Lair.</li>
  <li>A hat is light and fits in one hand, so carrying a fallen friend home no longer means dragging a body.</li>
</ul>

<h2>Your name and colour on your things</h2>
<figure class="plate" aria-label="Four panels: a strongbox with a blue wax seal and a brass plate reading Hollowmere; a ledger page with four columns headed Hollowmere, nyx_77, Pip and GrumboldTheUnwashed, each in its own colour of ink; a coin pouch with a blue drawstring and Hollowmere stitched on; two first-person hands in blue sleeves.">
{plate_things()}
<figcaption>Concept · shown for Hollowmere.</figcaption></figure>
<ul>
  <li>Your strongbox in the Lair has a wax seal in your colour and your name on a brass plate.</li>
  <li>Your column in the ledger is headed with your name, in your colour of ink.</li>
  <li>Your coin pouch has a drawstring in your colour and your name stitched on.</li>
  <li>In first person you only see your own hands, so your sleeves show your colour.</li>
</ul>

<h2>Risks</h2>
<ul>
  <li><b>Look-alike colours.</b> Only an exact match is blocked, so two colours one step apart are allowed but look the same. If that confuses players, the name on the hat is the backup.</li>
  <li><b>Colours that mean something.</b> Players can pick the game's own signal colours: violet for spells, orange-red for danger, the blue-green glow on things you can use, and gold for loot. A violet wizard may read like a spell.</li>
  <li><b>Small names.</b> A very long name shrinks until it's hard to read. That's why the option to choose a shorter name sits right beside it.</li>
  <li><b>A hat in the dark.</b> A fallen hat is small and could be lost on a dark floor. It needs a playtest in the darkest rooms.</li>
  <li><b>Rude names</b> are now on someone's hat for the whole raid, not just in a list.</li>
</ul>

<h2>Decided</h2>
<ol class="decisions">
  <li><b>Shared colours.</b> First come, first served: whoever picks a colour first keeps it.</li>
  <li><b>Where the name goes.</b> Around the hat band only, and the hat is what remains of a wizard when they die.</li>
  <li><b>Long or awkward names.</b> They shrink to fit, and players are clearly offered a different name for the game.</li>
  <li><b>Colours.</b> A free red, green and blue picker; no two players in a raid can wear exactly the same values.</li>
</ol>

<footer>Builds on the HUD proposal: the grimoire, the portal watch, the ledger, the strongboxes and carrying friends home · concept plates drawn for this page</footer>
</main>"""


out = pathlib.Path(__file__).parent.parent / "proposal.html"
out.write_text(page)
print("wrote", out, len(page), "bytes")
