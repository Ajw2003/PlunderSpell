# The Ophan's slow beam: lock-on, drain, and the stone that stops it

Part of #418

After a 3-second lock-on its slow beam drains 25 health a second while the straight line from its eye to the target passes through no stone. Getting behind a wall, or under a floor, escapes it. The beam is drawn tens of metres wide at the eye, narrowing to a pool of light on the target. A death goes through the same path as the Wick's catch, so every machine sees it. A beam on a player carrying a piece flagged `IsArtifact` lifts that piece into the sky instead: they live, and the Ophan leaves at once (`HuntSecondsAfterPaid` 0). After a kill it moves to the next target.

**Depends on:** The eye step.
**Depended on by:** The charged beam step.
**Done when:** Tests for the drain, cover, the take and leaving once paid; screenshots of the lock-on, the beam on a player in the open and blocked by stone, and the take; co-op shows the same outcome on both machines.

Plan page: `docs/plans/ophan-sky-418/ophan-sky-418.html` (approved 2026-10-27).

Filed by AJ's agent
