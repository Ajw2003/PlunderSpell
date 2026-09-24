# PlunderSpell - Execution Priority Queue

This document orders the open backlog issues based on the core directive: **"Get each individual feature completed first to see if the game is fun mechanically before doing any more artwork."**

The backlog is broken down into phases. Work should be completed sequentially from Phase 0 through Phase 5.

## Phase 0: Make the shipped build actually playable
*Added 2026-09-22, after the user played the real standalone build produced for #53 and found the raid
itself broken in ways no automated check (headless or otherwise) had caught. This supersedes Phase 1
as the top of the queue: everything in Phase 1 was verified against code and the headless suite, not
against an actual play session, and it turns out those are different claims. Fix these before
anything else — including before re-attempting #55, which cannot succeed while these are broken.*

*   **#102** EPIC: the shipped standalone build is not actually playable (ties the three below together)
*   **#100** Player.prefab is missing SpellCastingSystem/AcousticEmitter/FootstepNoiseEmitter — casting doesn't work in the shipped raid
*   **#14** No usable health or damage model — reopened, the fix landed on an unmerged branch (PR #97) and was never on the branch that ships
*   **#101** Extraction and return-to-Lair don't work when actually played in the built RaidScene

## Phase 1: Core Game Loop, Mechanics & Controls
*These items must be fixed immediately. Without them, the game literally cannot be played, tested, or evaluated for mechanical fun. As of 2026-09-22 this phase's own issues are closed except #24 (superseded by Phase 0's #102, see above) and #55 (blocked on Phase 0).*

*   **#24** EPIC: build the game loop end to end so there is something to actually play
*   **#5** PCG: rooms do not connect and the floor plan does not read as a castle
*   **#19** Modules float and snap inconsistently - grid spacing must be exact
*   **#6** Player is too tall for the rooms (scale mismatch between character and architecture)
*   **#25** Player spawn can be inside or flush against castle geometry
*   **#20** Loot is flung across the map by physics at spawn
*   **#15** No carryable objects placed in the level - picking anything up is not discoverable
*   **#7** No crosshair - cannot tell where you are aiming
*   **#8** Mouse is not locked or hidden during play
*   **#9** Player can still move and look around while the main menu is open
*   **#37** No melee combat system exists - reach/heft/noise stats aren't a swing mechanic yet
*   **#39** Ranged weapons have no aim or fire implementation
*   **#14** No usable health or damage model - nothing is distinguishable in play
*   **#18** No way to test combat
*   **#53** No standalone Unity player build has ever been produced
*   **#55** M2's acceptance criterion has never been run - a real four-player raid start to finish

## Phase 2: Expanded Systems & Mechanical Depth
*Once the basic loop works, these items add the actual mechanical depth that makes the game interesting to playtest.*

*   **#26** EPIC: The Mystical Market pillar does not exist
*   **#27** Build the market hub - a place, a shop UI, and persistent "stays bought" state
*   **#29** Market mark-up and "spend the leftover after the debt" are unimplemented rules
*   **#33** Debt and hoard have no in-fiction representation - only HUD numbers
*   **#43** GildedColossus (vault boss) has no unique behaviour, telegraph, or arena design
*   **#44** Door/socket types (murder-hole, arrow-loop) are layout tags only, not functional hazards
*   **#45** Drawbridge has no operable mechanism despite existing as a modelled room
*   **#35** Era-specific hazards from the pitch are entirely unimplemented
*   **#21** No portal - no asset and no way to travel to or from the lair
*   **#46** Portal has no closing-countdown feedback - "the way home begins to narrow" is invisible
*   **#47** No loudness meter - the whisper/normal/shout dial the pitch calls "the whole trick" is invisible
*   **#49** Recognised phrase has no on-screen caption
*   **#48** Casting is invisible to teammates - "friends hear the word half a heartbeat before it resolves" isn't represented
*   **#50** M1's acceptance criterion has never been measured - real microphone, multi-accent recognition

## Phase 3: Settings, Config, and Data Authoring
*Getting the data right and ensuring accessibility before piling on the polish.*

*   **#57** "Every word can also be bound to a key" has no real settings-exposed keyboard-casting mode
*   **#56** Settings menu completeness (audio mix, keybinds, mic device) is unverified
*   **#41** Bestiary thematic divergence: half the enemy roster reads as fantasy monsters, not the pitch's household
*   **#38** Weapon roster doesn't match the pitch's twelve named weapons
*   **#28** Author wares and pricing for all four market stalls
*   **#36** Era-specific loot catalogue is unauthored - five generic pieces stand in for the pitch's four eras
*   **#54** No performance budget or profiling pass exists for authored-art raids

## Phase 4: Game Feel & Animation
*The bridge between raw mechanics and visual art. Adding weight and impact to the mechanics so they actually "feel" fun to play.*

*   **#51** No camera shake, hit-stop, or impact juice anywhere
*   **#52** Throwing and swinging have no wind-up or follow-through animation weight
*   **#12** No visual feedback for taking or dealing damage
*   **#13** No visual feedback for spells
*   **#11** No animations for the player or for doors
*   **#10** Enemies have no animations

## Phase 5: Art, Audio & Polish (Deferred)
*Strictly visual and auditory upgrades. These are intentionally deferred to the very end as requested.*

*   **#30** EPIC: The Lair is a menu screen, not the physical place the pitch describes
*   **#31** Build a 3D Lair space to replace the menu screen
*   **#34** EPIC: No icon set exists anywhere - spells, weapons, loot, eras, status effects
*   **#40** Weapon and loot icons for UI do not exist
*   **#16** Main menu has no art
*   **#17** Only one era (High Medieval) has art and models
*   **#23** Castles look bland
*   **#32** Lair lighting and mood pass - "one candle, one fire, falling into black"
*   **#59** The pitch's six reference lighting frames have no concept art or lighting targets
*   **#58** Steam lobby/invite flow has no art or branding pass
*   **#22** No VFX or SFX anywhere - spells, attacks, UI and casting failures are all silent
*   **#42** Guards are silent - no patrol murmur, alert bark, or chase shout
