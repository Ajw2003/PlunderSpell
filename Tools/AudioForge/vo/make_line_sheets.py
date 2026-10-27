"""Writes vo/line-sheets/<age>.md: what each guard voice reads, and the exact file name per take.

Generated rather than hand-typed so every file name matches a manifest row; the script refuses to
write if the line table and manifest.csv disagree about which takes exist.

    python3 Tools/AudioForge/vo/make_line_sheets.py
"""

import csv
from pathlib import Path

HERE = Path(__file__).resolve().parent
MANIFEST = HERE.parent / "manifest.csv"

AGES = {
    "bronze": dict(
        title="The Bronze Age (c. 1200 BC)",
        language="Greek-sounding. Built from real Ancient Greek words, but nobody will check: say it like you mean it.",
        voices={
            "levy": "A conscripted farmer with a spear. Bored, put-upon, would rather be home with the goats.",
            "slinger": "Young, cocky, fast talker. Stands on walls and likes it.",
            "champion": "Huge man in bronze plate. Slow, deep, speaks as if every word costs him.",
            "keeper": "A fire priest. Sing-song and hushed. Their **murmur** takes are a chant on one note: "
                      "*\"Hestia... phōs... Hestia...\"*, not the lines below.",
        },
        lines={
            "murmur": ["*(muttering)* Sitos, sitos... panta sitos. *(grain, grain, always grain)*",
                       "*(hum a little tune, 4–5 seconds)*",
                       "*(yawn)* Aaah... nyx makra. *(long night)*",
                       "Wanax... pantote wanax. *(the king... always the king)*"],
            "alert": ["Tis? Tis ekei? *(who? who's there?)*", "Ti touto? *(what's this?)*",
                      "*(quietly)* Akouō ti... *(I hear something)*"],
            "chase": ["KLEPTAI! KLEPTAI! *(thieves!)*", "DEURO! DEURO, PHYLAKES! *(here, guards!)*",
                      "LABETE AUTOUS! *(seize them!)*"],
            "search": ["Pou ei...? *(where are you?)*", "Exelthe, mikre klepta... *(come out, little thief)*"],
            "lost": ["Ouden. ...Mys. *(nothing. a mouse)*", "Oimoi. Wanax ou mathēsetai. *(alas. the king won't hear of it)*"],
            "attack": ["*(effort)* Ha!", "*(effort)* Hyah!", "*(effort)* Hup!"],
            "hurt": ["Aiai!", "Ow— oimoi!", "*(pained grunt)*"],
            "death": ["*(theatrical)* Ōh... wanax...", "*(a yell, cut off)* Aaa—"],
            "asleep": ["*(slow deep snore, 3 breaths)*", "*(snore with a little mumble)*"],
            "grabbed": ["Ti?! Aphes me! *(what?! put me down!)*", "Ou! Ou! OU! *(no! no! NO!)*",
                        "*(indignant, muffled protest)*"],
            "thrown": ["AAAAAAH—", "Ō theoi—! *(oh gods—!)*"],
        }),
    "high": dict(
        title="The High Medieval (c. 1250)",
        language="Middle English, with Norman French from the knight. *Haro!* was the real medieval "
                 "cry that raised the hue and cry.",
        voices={
            "warden": "Old night-watchman with a lantern. Grumbles to himself, creaky, kind of sweet.",
            "crossbowman": "Sharp, suspicious, professional. Says little.",
            "knight": "Well-dressed, well-fed, very sure of himself. Speaks French where the line has it, "
                      "and says English lines with a French accent.",
        },
        lines={
            "murmur": ["*(muttering)* Colde. Evere colde, this wal.", "*(hum a slow chant, 4–5 seconds)*",
                       "*(yawn)* ...Longe nyght.", "Mi lord slepeth, and I walke. As evere."],
            "alert": ["Who goth ther?", "Qui va là?", "*(quietly)* Herke... what was that?"],
            "chase": ["HARO! HARO! THEVES!", "OUT! HARROW! TO ME, TO ME!", "AUX LARRONS! *(at the thieves!)*"],
            "search": ["Com forth, thef...", "I se thee nat... yet."],
            "lost": ["Nought. A rat, par ma foi.", "Wel. Nobody seith a word to mi lord."],
            "attack": ["*(effort)* Hah!", "Have at thee!", "*(effort)* Hyah!"],
            "hurt": ["Ow! By Seint Thomas!", "Aah!", "*(pained grunt)*"],
            "death": ["*(grandly)* Mercy... Jhesu...", "*(winded)* Oof—"],
            "asleep": ["*(slow deep snore, 3 breaths)*", "*(snore with a little mumble)*"],
            "grabbed": ["Unhand me, knave!", "What sorcerie is this?!", "*(indignant, muffled protest)*"],
            "thrown": ["AAAAAAH—", "Mon Dieu—!"],
        }),
    "late": dict(
        title="The Late Medieval (c. 1450)",
        language="Late Middle English and Burgundian French. These guards walk to a schedule and "
                 "hunt in pairs, so they sound drilled.",
        voices={
            "halberdier": "Drilled, counts his steps, a stickler for the rota.",
            "handgunner": "Nervy, smells of smoke, laughs at the wrong moments.",
            "manatarms": "In full Gothic plate. Echoey, calm, menacing. Speak slightly into your hand for the helmet.",
            "pavisier": "Carries a giant shield. Stoic, deep, says everything like an order.",
        },
        lines={
            "murmur": ["*(counting under breath)* ...two and twenti, thre and twenti, turne.",
                       "Hmm. The bellman's late agayn.", "*(hum a marching tune, 4–5 seconds)*",
                       "*(yawn)* Seconde wacche. Alwey the seconde wacche."],
            "alert": ["Halte! Who's there?", "Qui est là?", "*(quietly)* Somthyng stirreth..."],
            "chase": ["À L'ARME! À L'ARME!", "SAINCT GEORGE! THEVES!", "Two by two, lads — TAKE THEM!"],
            "search": ["Come out, come out...", "We hold the gates, thef. Every one."],
            "lost": ["Nothing. Write it in the book.", "...Tell no-one."],
            "attack": ["*(effort)* Hah!", "Montjoie!", "*(effort)* Hyah!"],
            "hurt": ["Aah! Saint Denis!", "Ow!", "*(pained grunt)*"],
            "death": ["*(grandly)* I... am... slayn...", "*(winded)* Oof—"],
            "asleep": ["*(slow deep snore, 3 breaths)*", "*(snore with a little mumble)*"],
            "grabbed": ["Put me DOWN!", "Wicchecraft! WICCHECRAFT!", "*(indignant, muffled protest)*"],
            "thrown": ["AAAAAAH—", "Not agayn—!"],
        }),
    "powder": dict(
        title="The Age of Powder (c. 1620)",
        language="Early Modern English: Shakespeare's era, thee and thou. A palace full of glass, so "
                 "they are twitchy about breakages.",
        voices={
            "guard": "A palace guard. Pompous, bored, proud of his uniform.",
            "musketeer": "Young, jumpy, very keen to fire his musket at something.",
            "cuirassier": "Armoured cavalryman on foot, and resents it. Aristocratic drawl.",
            "petardier": "Demolitions man. Cheerful, slightly deaf, talks too loud.",
        },
        lines={
            "murmur": ["Twelve o' the clock, and all's well... God willing.", "*(muttering)* Zounds, these boots.",
                       "*(hum a courtly dance tune, 4–5 seconds)*", "*(yawn)* A pox on the night watch."],
            "alert": ["Who goes there?", "Stand! Show thyself!", "*(quietly)* Hark — what was that?"],
            "chase": ["THIEVES! THIEVES IN THE GALLERY!", "TO ARMS! BEAT THE DRUM!", "IN THE KING'S NAME, STAND!"],
            "search": ["I know thou'rt here...", "Mind the glass, lads. Mind the glass."],
            "lost": ["'Twas naught. A cat.", "Methinks I'll say nothing of it."],
            "attack": ["Have at you!", "*(effort)* Hah!", "*(effort)* Hyah!"],
            "hurt": ["God's wounds!", "Ah!", "*(pained grunt)*"],
            "death": ["*(grandly)* I am... undone...", "*(winded)* Oof—"],
            "asleep": ["*(slow deep snore, 3 breaths)*", "*(snore with a little mumble)*"],
            "grabbed": ["Unhand me, sir!", "Witchcraft! WITCHCRAFT!", "*(indignant, muffled protest)*"],
            "thrown": ["AAAAAAH—", "Not the mirror—!"],
        }),
}

DIRECTION = {
    "murmur": "patrolling, nothing's wrong, half to yourself", "alert": "heard something, not sure",
    "chase": "seen you, shouting for help. LOUD", "search": "lost sight of you, prowling",
    "lost": "giving up, back to the patrol", "attack": "swinging a weapon: short effort sounds",
    "hurt": "hit, short", "death": "knocked out for good: short, a bit comic, never gory",
    "asleep": "put to sleep by a spell", "grabbed": "a wizard has picked you up by magic",
    "thrown": "and now thrown across the room",
}


def main():
    manifest = {r["name"]: int(r["variants"]) for r in csv.DictReader(MANIFEST.open()) if r["name"].startswith("vo_")}
    out_dir = HERE / "line-sheets"
    out_dir.mkdir(exist_ok=True)
    total = 0
    for age, spec in AGES.items():
        for voice in spec["voices"]:
            for line, takes in spec["lines"].items():
                name = f"vo_{age}_{voice}_{line}"
                if manifest.get(name) != len(takes):
                    raise SystemExit(f"{name}: sheet has {len(takes)} take(s), manifest has {manifest.get(name)}")
        md = [f"# Line sheet — {spec['title']}", "",
              "*Generated by `Tools/AudioForge/vo/make_line_sheets.py`; edit the lines there, not here.*", "",
              f"**Language:** {spec['language']}", "",
              "Recording tips and how to send files: [../README.md](../README.md).", "",
              "## The voices", "", "| Voice | Character |", "|---|---|"]
        md += [f"| **{v}** | {note} |" for v, note in spec["voices"].items()]
        md += ["", "## The lines", "",
               "Each voice reads every line below. Replace `<voice>` in the file name with the voice's name, "
               f"e.g. `vo_{age}_{next(iter(spec['voices']))}_chase_01.wav`. Words in *(brackets)* are "
               "directions or translations: don't say them.", ""]
        for line, takes in spec["lines"].items():
            md += [f"### {line} — {DIRECTION[line]}", "", "| File | Say |", "|---|---|"]
            for i, text in enumerate(takes, 1):
                md.append(f"| `vo_{age}_<voice>_{line}_{i:02d}.wav` | {text} |")
                total += len(spec["voices"])
            md.append("")
        md += ["## Checklist per voice", "",
               f"{sum(len(t) for t in spec['lines'].values())} files per voice; "
               f"{sum(len(t) for t in spec['lines'].values()) * len(spec['voices'])} for this Age.", ""]
        (out_dir / f"{age}.md").write_text("\n".join(md), encoding="utf-8")
    print(f"wrote 4 line sheets covering {total} takes; manifest has {sum(manifest.values())} vo files "
          f"({sum(v for k, v in manifest.items() if k.startswith('vo_hound'))} of them hound, from the library)")


if __name__ == "__main__":
    main()
