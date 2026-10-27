# The Ophan's eye follows its target, and its rings spin by state

Part of #418

It targets whoever carries the most worth (the sell value of the piece they carry, the number the portal's tally counts), then the next down the list. Its `Eye` bone turns every frame to follow that player; its three rings (`GreatWheel`, `InnerWheel`, `TiltWheel`) spin at a speed set by its state: fastest while charging the charged beam, half that during the lock-on, a slow turn while watching, spinning down to a stop as it leaves. The tells: for the lock-on the eye narrows and pulses, the pulse quickening over 3 s; for the charge the eye swells to half again its size and brightens steadily to full blast. Target, state and tells are the same on every machine.

**Depends on:** The sky step.
**Depended on by:** The slow beam step and the charged beam step.
**Done when:** Tests for target order and state; a solo raid with screenshots of the eye on its target and each tell; the same target and state on both machines in co-op.

Plan page: `docs/plans/ophan-sky-418/ophan-sky-418.html` (approved 2026-10-27).

Filed by AJ's agent
