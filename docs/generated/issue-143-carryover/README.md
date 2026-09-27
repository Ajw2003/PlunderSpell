# #143: state carried over from one raid to the next, 2026-09-26

`snapshot.cs` (run with `unity command eval_file` in Play mode) prints every piece of per-raid state
a player could see carry over: phase, health, mana, status effects, slam and stagger, alarm, the
portal's haul, the held item, the damage view's vignette, numbers and hurt lines, camera shake,
guards, loot and spell bursts.

Before the fix, a raid in which the player was hurt, set alight, raised the hue and cry, spent mana,
held a Silver Ewer, and then died, was followed by this at the start of the next raid:

    hp=68 ... burning=True ... alarm=Calm 0 ... held=none ... vignette=0.06 numbers=6
    hurtLines=-32  RaidPlayer (fire) | trauma=0.69

The player had been revived to 100, and the fire from the last raid had already taken 32. Everything
else (alarm, mana, held item, death flag, loot, guards) reset correctly.

After the fix (`RaidDirector.ClearCarriedOverState`, run when the player is placed for a raid), the
same sequence, and a survivor who set out again while burning, both start with:

    hp=100 ... burning=False stunned=False asleep=False | alarm=Calm 0 | held=none
    vignette=0.00 numbers=0 hurtLines= | trauma=0.00
