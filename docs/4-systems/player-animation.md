# Player animation: the wizard's body

Every player is the same wizard (plan: `docs/plans/wizard-character.md` on
`claude/wizard-character-plan`, issue #335). This page covers how the wizard's body is built,
animated and driven in game. Issues: #336 (model), #361 (clips), #362 (wiring).

**Status (2026-10-08):** model and clips built and reviewed in Blender. The Unity side is written
but has **not been run in the Editor**: the controller and prefab install happen when someone runs
`Plunderspell > Wizard > Install On Player Prefabs` in Unity.

<!-- ref:7a1c -->

## How it works

| Piece | Where | What |
|---|---|---|
| Model | `Tools/ArtForge/art_forge/blueprints/players_lair.py` → `Assets/Models/ArtBible/Players/Lair/Wizard/` | `figures.Human` 1.80 m, Unity Humanoid bones plus `Hat` (child of `Head`, rigid). One material; `Textures/Wizard_DyeMask.png` is white where the player's colour goes (robe, cape, sleeves, hat, band) |
| Clips | `Tools/ArtForge/anim_forge/library_player.py`, `python3.11 Tools/ArtForge/anim_player.py build` → `Assets/Models/ArtBible/Animations/Humanoid_Player.fbx` | In place, on the reference human, so they retarget through Humanoid. Review sheets: `docs/art/anim/player/` |
| Import | `Assets/_Project/Scripts/Editor/ArtBibleModelImporter.cs` | `Players/` and `Humanoid_Player.fbx` import as Humanoid with the shared bone map; clips are named by their AnimForge id, loop flags from `LoopingPlayerClips` |
| Controller | `Assets/_Project/Scripts/Editor/WizardPlayerSetup.cs` → `Wizard.controller`, `WizardUpperBody.mask` | Base layer: Move (idle 0 / walk 2 / jog 5 m/s blend), Crouch (idle 0 / walk 2), JumpTakeoff → JumpAir → JumpLand, Dead (from any state). Cast layer over head and arms only: CastHold while casting, CastRelease when the key is let go |
| Driver | `Assets/_Project/Scripts/Runtime/Player/WizardAnimationDriver.cs` | Sets `Speed`, `Crouch`, `Airborne`, `Casting`, `Dead`. Turns the model to the camera's yaw |
| Network | `Assets/_Project/Scripts/Runtime/Net/PlayerNetworkOwnership.cs` | The owner writes three synced flags (creeping, casting, airborne); everyone else copies them, plus the existing down flag, into the driver |

Inputs, per machine:

- **Speed** is measured from how far the body moved each frame, so the owner's body and a
  teammate's body (which follows `NetworkTransform`) animate the same way.
- **Crouch** is the creep key (`C`, `PlayerStateMachine.Creeping`), 2 m/s.
- **Airborne** is `!PlayerStateMachine.IsGrounded` on the owner.
- **Casting** is `PushToCastController.IsCasting` (holding `V`). Letting go plays the release.
- **Dead** is `PlayerStateMachine.dead` on the owner and `PlayerNetworkOwnership.IsDown` elsewhere.

## Invariants

- The player's origin is the capsule's centre; the wizard sits `0.9 m` below it
  (`WizardPlayerSetup.FeetBelowOrigin`). The collider and ride height are unchanged.
- The player body never rotates; look input turns the camera. The driver copies the camera's yaw
  onto the model every frame.
- Your own wizard renders as **shadow only** on your machine; everyone else sees all of it.
- **Death (owner's decision, 2026-10-08):** the collapse plays for 1.6 s, then the whole wizard
  is hidden. The hat that stays behind is #339 and is not built yet, so until then nothing marks
  where a player fell. With a driver present, `ShowDownPose` no longer tips the capsule.
- `ArtBibleModelImporter.LoopingPlayerClips` must equal the looping clips in
  `Animations/player_anim_manifest.json`; `WizardAnimationTests` checks it.

## Traps

- Clips play facing the camera's yaw, so walking backwards plays the forward gait backwards
  in space. Strafe and backpedal clips are not authored.
- The dye mask is data and imports linear (`_DyeMask` is in `LinearTextureSuffixes`). Nothing
  tints with it yet; that is #337.
- `WizardPlayerSetup.cs` is excluded from the headless harness, like `PlayerBuilder.cs`: it only
  means anything inside the real Editor.
