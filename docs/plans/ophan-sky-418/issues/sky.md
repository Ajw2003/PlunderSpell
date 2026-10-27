# The Ophan appears in the sky, with its hum and screen shake

Part of #418

A prefab of the Ophan of Hours at 150 times its model (about 1.4 km tall, 1.65 km across), with its Animator on `TheOphanOfHours_Rig`. `HuntWindowSeconds` (60 s, `OphanDemandTuning`) before the portal closes it appears 900 m off the castle, high in the sky, always facing the castle with its eye on the players, and plays its 4-second arrival. Its warning (#434) plays on every machine: a loud, low drone made in code as a stand-in until real audio exists, and the existing camera shake. The player camera's far clip plane rises from 1000 m to 3000 m so it is drawn whole. A test switch forces a hunted raid until the demand (#419) exists. It is the same on every machine.

**Depends on:** #433 (the tuning asset, merged into staging).
**Depended on by:** Every other step below.
**Done when:** In a solo Editor raid it appears at 60 s left, whole, eye toward the castle, with the hum and the shake; a test checks the arrival time; a co-op check shows it on both machines; screenshots are looked at.

Plan page: `docs/plans/ophan-sky-418/ophan-sky-418.html` (approved 2026-10-27).

Filed by AJ's agent
