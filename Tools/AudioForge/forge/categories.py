"""Which part of the mix each sound belongs to, and how loud and long that part should be.

Shared by build.py (sets each file's level) and audit.py (checks it). `level` is the RMS window in
dBFS for a one-shot of that category; `target` is where the build aims inside it before the
gameplay-noise adjustment, so a hover tick lands far below a musket shot (docs/plans/audio.md §1.1:
what you hear matches what the guards hear).
"""

# Per-category expectations. level: target RMS dBFS window for the category's role in the mix,
# so a hover tick is not as loud as an explosion. max_s: longest a one-shot of that kind should run.
CATEGORY = {
    "ui": dict(target=-28, level=(-34, -22), max_s=0.6, organic=True),
    # UI-bus sounds that are events, not menu clicks (Lair, results, loot lost): menu-quiet, any length.
    "cue": dict(target=-25, level=(-33, -18), max_s=None, organic=False),
    "foley": dict(target=-25, level=(-32, -18), max_s=0.8, organic=True),
    "physics": dict(target=-19, level=(-26, -12), max_s=3.0, organic=True),
    "weapons": dict(target=-17, level=(-26, -8), max_s=4.0, organic=True),
    "spells": dict(target=-19, level=(-26, -12), max_s=3.0, organic=False),
    "creatures": dict(target=-21, level=(-28, -14), max_s=3.5, organic=True),
    "world": dict(target=-18, level=(-26, -10), max_s=6.5, organic=True),
    "ambience": dict(target=-26, level=(-34, -20), max_s=None, organic=True),
    "music": dict(target=-20, level=(-28, -16), max_s=None, organic=False),
}


def category(row):
    folder, name = row["folder"], row["name"]
    if name.startswith(("mus_", "sting_")):
        return "music"
    if folder.startswith("Ambience") or name.startswith("amb_"):
        return "ambience"
    if folder == "UI":
        return "ui"
    if row["bus"] == "UI":
        return "cue"
    if folder in ("Foley",) or folder.startswith("SFX/Status") or name.startswith("foley_"):
        return "foley"
    if folder == "Physics" or folder in ("SFX/Grab", "SFX/Loot"):
        return "physics"
    if folder == "SFX/Weapons":
        return "weapons"
    if folder.startswith(("SFX/Spells", "SFX/Voice", "SFX/Portal")):
        return "spells"
    if folder.startswith(("VO", "SFX/Enemies")):
        return "creatures"
    return "world"



# Louder gameplay noise, louder file: the rule that lets players learn by ear what wakes guards.
NOISE_OFFSET_DB = {"none": -3.0, "low": -1.5, "mid": 0.0, "high": 2.0, "max": 4.0}


def target_rms_db(row):
    spec = CATEGORY[category(row)]
    lo, hi = spec["level"]
    return max(lo + 1, min(hi - 1, spec["target"] + NOISE_OFFSET_DB.get(row["noise"], 0.0)))
