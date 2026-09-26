# Core

Shared infrastructure every other module sits on: the singleton base, the event bus, and the state
machine contract. Ported from `RogueLikeSlop@ThirdPerson`.

`Plunderspell.Foundation` references nothing. Every other assembly references it, so anything added here is
paid for everywhere — keep it small.

## How it works

- **`SingletonBase<T>`** — one scene-owned instance per type, claimed in `Awake`. A second instance
  destroys itself. `PersistBetweenScenes` (default true) controls `DontDestroyOnLoad`.
- **`EventManager`** — a typed publish/subscribe bus. Subscribers are held by `WeakReference`, so a
  destroyed listener does not keep its subscriber alive or throw on publish. Publishing is
  reentrancy-safe: unsubscribes that happen *during* a publish are queued and applied afterwards
  rather than mutating the list mid-iteration.
- **`BaseStateMachine` / `IState` / `PlayerState`** — `Enter`/`Update`/`Exit`/`FixedUpdate`. States
  are plain C# objects constructed once in `Awake`, not MonoBehaviours, which keeps their logic
  testable without a scene.

## Invariants

- **`SingletonBase.Instance` never creates anything.** An absent instance means no scene owner has
  been set up yet; that is information the caller needs, not a problem to paper over by spawning a
  GameObject.
- **A subclass overriding `OnDestroy` must call `base.OnDestroy()`.** See Traps.

## Spell-target interfaces

`Core/Interfaces/ISpellTargets.cs` declares `IBreakable`, `ILevitatable`, `ISleepable`,
`IStunnable`, `IIgnitable`, `IOpenable` and `IHandOpenable`. They live in Core for a structural
reason: `Plunderspell.Spells` must be able to affect loot, doors and players, all of which sit downstream
of it, so a direct reference would cycle. A spell only ever sees the interface, found by an overlap
query. Anything that should be affectable implements one — or adds `StatusEffectReceiver`, which
implements four of them.

## Dev tooling

- **`Tools/Plunderspell/Build Playable Raid Scene`**
  (`Assets/_Project/Scripts/Editor/RaidSceneBuilder.cs`) builds a complete playable raid from code:
  rooms with doorways, a placeholder haul, a garrison, the extraction zone, the HUD and a player who
  can walk, grab and cast. Everything it generates is placeholder — it is a harness for playing the
  game, not the art pass. Re-running always starts from an empty scene, so it never leaves a second
  castle behind. See `docs/4-systems/raid.md`.

- **`Tools/Headless/verify.sh`** compiles every gameplay and editor assembly and runs the whole test
  suite without Unity. See `Tools/Headless/README.md`.

- **`Tools/Plunderspell/Build Test Scene`** (`Assets/_Project/Scripts/Editor/TestSceneBuilder.cs`) builds a
  throwaway player prefab, flat ground and grabbable item from code and saves them as
  `Assets/_Project/Scenes/TestScene.unity` / `Assets/_Project/Prefabs/Player.prefab`. It exists
  because nothing in the project has ever been run - there was no player prefab and no test scene -
  and the project decision was a reviewable code path over prefab YAML authored blind. Re-running
  always starts from a fresh empty scene rather than adding to what is already there, so it never
  leaves a duplicate ground behind. Tuning (ground size, spawn height) lives in named consts at the
  top of the file. Not shipped gameplay.

  Because it regenerates both assets wholesale, it is also the supported way to repair them after a
  component is deleted from the project - fix the builder and re-run it rather than hand-editing
  the prefab or scene YAML.

## Traps

- **Unity's fake null and `?.`** — a destroyed `UnityEngine.Object` is not a real C# null. It
  compares equal to null through Unity's overloaded `==`, but `?.` bypasses that overload entirely
  and happily dereferences it. Since call sites are written `EventManager.Instance?.Publish(...)`,
  the getter collapses a destroyed instance to a genuine null (`_instance != null ? _instance : null`)
  so `?.` behaves. Removing that ternary makes every `?.` call site a latent null-ref after teardown.

- **`OnDestroy` is a hiding trap, not an overriding one.** `SingletonBase.OnDestroy` is `protected
  virtual` and clears `_instance`. A subclass that declares `private void OnDestroy()` *hides* it
  rather than overriding it — Unity calls only the most-derived one, the base never runs, and
  `_instance` is left pointing at a destroyed object. The compiler warns (CS0114) but does not error.
  `EventManager` gets this right with `protected override void OnDestroy()` + `base.OnDestroy()`;
  any future singleton must do the same.
