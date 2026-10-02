# Deprecated code (kept as reference, not live)

Owner decision 2026-10-01 (#230): keep dead code, mark it `[System.Obsolete]`, list it here. Obsolete
warnings from dead code referencing dead code are expected.

A type is listed only if (1) no code outside its own file/folder references it (grep of `Assets/` and
`Tools/`, Editor, tests and evals included), (2) its script GUID is in no `.prefab`, `.unity` or `.asset`
under `Assets/`, and (3) nothing creates it (`AddComponent`, `Type.GetType`, `[RuntimeInitializeOnLoadMethod]`: none found).

| Type (file under `Assets/_Project/Scripts/Runtime/`) | Why it is dead (evidence) | To bring it back |
|---|---|---|
| `MonsterStateMachine` (`Enemies/MonsterStateMachine.cs`) | GUID `c4146c26...` in 0 prefabs/scenes/assets; referenced only by `Enemies/States/*` and in comments in `Items/Item.cs:107`. Implements `ICarryableCreature`, which stays live (used by `Items/Item.cs:111,191`, resolves to nothing at runtime now). | New navigation from `docs/plans/bespoke-navigation.md`: it drives a `NavMeshAgent`. Note `Items/Item.cs:853` reads a `NavMeshAgent` (`_agent`) only for monsters. |
| `MonsterState`, `MonsterAttackState`, `MonsterDeadState`, `MonsterIdleState`, `MonsterPatrolState`, `MonsterPickedUpState`, `MonsterPursueState` (`Enemies/States/*.cs`) | Not MonoBehaviours (plain `IState` classes); 0 references outside `Enemies/`; only `MonsterStateMachine` uses them. | Same as above. |
| `HealthBar` (`Core/UI/HealthBar.cs`) | GUID `c938ccb0...` in 0 prefabs/scenes/assets; 0 code references (`grep -w HealthBar`) outside its file; no `AddComponent`. | A prefab/scene using it, or `AddComponent<HealthBar>` from a HUD. |
| `PlayerInteractState` (`Player/States/PlayerInteractState.cs`) | Empty `PlayerState` subclass; 0 references anywhere else in `Assets/` or `Tools/`. | `PlayerStateMachine` must construct it. |

## Considered and left live
- `ShotRelay`: GUID in 2 scene/asset files. `DamageFeedbackView`: created via `AddComponent` (`RaidHud/DamageFeedbackView.cs:100`).
- `AudioGapProbe`: attached from evals by `AddComponent` (its own doc comment); tooling, not dead.
- `ICarryableCreature`: interface used by live `Items/Item.cs`.
- Remaining zero-reference hits (input event structs, generated `PlayerInputs` nested types, `ItemCategory`, `NetworkTypes`, `MisfireEffectBase`, `SpkModel`, `VoiceRecognizerJson`): nested/base/generated/serialized types used within their own file, not judged dead.
- `Guards/`: being rewritten (#206), not touched.
