# Prompt — build and test Plunderspell (for Fable)

Hand the block below to Fable (`claude-fable-5-1`) as the opening message of a session on this
repository, branched from the current Plunderspell trunk. It is written to be pasted whole; the
notes under [Using this prompt](#using-this-prompt) are for the human, not for Fable.

---

## The prompt

> You are building **Plunderspell**, a four-player voice-cast medieval loot-extraction co-op game,
> inside this existing Unity 6000.3.15f1 repository. Work from the project's own documentation — do
> not invent design.
>
> **Read first, in this order, before writing any code:**
>
> 1. `docs/plunderspell.md` — the design bible. §4 is the spell lexicon and cast/misfire table, §5
>    the physics-and-weight rules, §6 the Ages, §7 the castle shape, §8 the weapon table, §9 the
>    palette, §10 the honest inventory of what is already built, §11 the milestones. Everything from
>    §9 onward is plain fact; §1–§8 is written in a herald's voice but the mechanics in it are real
>    and binding.
> 2. `docs/plans/plunderspell.md` — the engineering plan. Steps 3–6 are your specification for the
>    castle generator, voice layer, Ages and loot. Its "Verification" section is your acceptance
>    criteria.
> 3. `docs/systems/core.md` and `docs/systems/net.md` — the invariants and traps of the code you are
>    building on. The `SingletonBase`/`OnDestroy` trap and Unity's fake-null note in `core.md` will
>    bite you if you skim them.
> 4. The code you are extending: `Assets/_Project/Scripts/Runtime/` — `Core/`, `Player/`, `Items/`,
>    `Enemies/`, `Net/`, and `Scripts/Editor/TestSceneBuilder.cs`.
>
> **Where the project actually stands.** Milestone M0 is done on this branch: the planetary-gravity
> assembly is deleted, world gravity is restored, `DynamicsManager.asset` is back to `(0, -9.81, 0)`.
> Six assemblies exist (Core, Player, Items, Enemies, Net, Editor). There is no `World/`, no `Voice/`,
> no loot layer and no test assembly yet. Verify this yourself rather than trusting the sentence —
> `docs/plans/plunderspell.md` was written before M0 landed and still describes the deletion as
> pending.
>
> ### Scope — build M1 and M2, in that order
>
> **M1 — prove the voice.** This is the riskiest assumption in the project, so it ships first and
> alone. New `Runtime/Voice/` assembly:
>
> - `MicCapture.cs` — `UnityEngine.Microphone` ring buffer, 16 kHz mono, push-to-cast bound to a new
>   `Incant` action added to `Player/Input/PlayerInputs.inputactions`.
> - `IncantationRecogniser.cs` — on-device keyword spotting over the fixed ~40-word lexicon. Prefer
>   Unity Sentis with a small KWS model; Vosk via a native plugin is the documented fallback. No
>   audio ever leaves the machine and no audio ever crosses the wire — only a recognised spell id,
>   over a PurrNet RPC.
> - `SpellLexicon.cs` — a `ScriptableObject` mapping incantation → `SpellStats`, feeding the
>   **existing** `SpellBook.spellStats` field. Do not write a second casting system; `SpellBook`
>   already casts.
> - `MisfireTable.cs` — low-confidence and near-miss recognitions map onto the comedy outcomes in the
>   §4 table (`IGNIS` → your own beard, `FRANGO` → the floor you are standing on, and so on). This is
>   a designed feature, not an error path. Treat it as such.
> - Loudness is an input, not a nuisance: `SOMNUS` must be whispered and `TONITRUS` bellowed, so
>   carry RMS amplitude through the recogniser into the cast.
> - Every incantation must also be bindable to a key. Speaking is how the game is meant to be played,
>   never how it must be played — a player who cannot or will not speak loses nothing but flavour.
>
> While in this file: fix the bug the plan names. `SpellBook.AssignStats()` mutates the **shared
> prefab asset** — `_projectilePrefab`'s `NetworkedProjectile` fields and its `transform.localScale`.
> That leaks between casters and persists into the editor's asset database. Apply those to the
> spawned instance inside `FireProjectile()` instead.
>
> **M2 — the vertical slice.** One castle, one Age (Stratum II, the High Medieval), four spells, four
> players, lair → portal → plunder → hue and cry → extraction → lair.
>
> - New `Runtime/World/` assembly: `CastleGenerator.cs` (concentric wards — curtain wall, outer
>   bailey, inner ward, keep, crypt — not a random room web), `RoomModule.cs` (prefabs with tagged
>   sockets: `door`, `window`, `arrow-loop`, `stair-up`, `stair-down`, `murder-hole`; assembly is
>   socket-matching), `GenerationSeed.cs` (the host rolls one `int` and replicates it before level
>   load; no geometry crosses the wire), `LayoutValidator.cs` (flood-fill proving every extraction
>   portal reaches the keep and the crypt; reject and reroll on failure).
> - `Items/LootValue.cs` — value, bulk in **stone**, fragility. Fragility keys off the impact-damage
>   path `Item.OnCollisionEnter` already computes, so a dropped reliquary breaks for free. Heavy
>   items (the 14-stone altarpiece) need two carriers; a fallen player is 12 stone of cargo.
> - `World/Age.cs` as a `ScriptableObject` — era, date range, room set, loot table, enemy roster,
>   weapon tier, light rig, colour grade. `World/ExtractionPortal.cs` — only what physically crosses
>   the threshold comes home. `Meta/LairState.cs` — persistent lair, JSON to
>   `Application.persistentDataPath`, host-authoritative.
> - Noise propagates as a real event through the building and wakes what it reaches. It is not a
>   scripted trigger volume.
>
> ### Hard constraints — violating any of these fails the work
>
> 1. **`feature/Owen/PCG` is off-limits. Clean room.** Do not check it out, open its files, diff
>    against it, cherry-pick from it, or merge or rebase it into this lineage. The generator is
>    written from the specification in `docs/plans/plunderspell.md` §Step 3 and from castle
>    architecture, nothing else. Read the provenance note in that step before you start, and if you
>    have already read `ProceduralRoom.cs` in some other context, say so rather than proceeding
>    quietly.
> 2. **Determinism is not optional.** Thread an explicitly-passed `System.Random` instance through
>    generation. Never `UnityEngine.Random` — its global static state is shared with VFX and audio,
>    which makes generation order-dependent and silently desynchronises clients.
> 3. **Reuse `Core/`.** `SingletonBase`, `EventManager`/`IEvent`, `BaseStateMachine`/`IState`,
>    `IHealth` already exist and are referenced by every other assembly. Do not re-invent them, and
>    keep anything you add to `Core` small — it is paid for everywhere.
> 4. **Respect the assembly boundaries.** New code goes in a new asmdef (`Voice`, `World`, `Meta`)
>    that references `Core` and only what it genuinely needs. Do not collapse assemblies to make a
>    reference problem go away.
> 5. **Palette discipline.** Verdigris `#5FA288` carries every interactive glow; orpiment `#C9A227`
>    is spent on money and on nothing else; ground lapis `#7A6AA0` is reserved for the voice —
>    incantations, portals, and whatever answers them. Nothing else in the game may be gold.
> 6. **Write the way this codebase writes.** Self-documenting names; comments explain *why*, never
>    what; small single-responsibility methods; `[SerializeField] private` over public fields;
>    `ScriptableObject` tunables over scattered floats. A comment restating the line below it means
>    the line needs renaming.
>
> ### Testing — the part that is usually skipped, and must not be
>
> There is no test assembly in this project yet. Create
> `Assets/_Project/Tests/EditMode/` with an asmdef referencing the assemblies under test plus
> `UnityEngine.TestRunner` / `UnityEditor.TestRunner` (`com.unity.test-framework` 1.6.0 is already a
> dependency), and write the tests as you write the code, not after.
>
> Structure the logic so it is testable without a scene: generation, socket matching, validation,
> lexicon lookup, misfire selection and loot arithmetic should all be plain C# reachable from an
> EditMode test. Keep MonoBehaviours thin around them.
>
> Cover, at minimum:
>
> - **Determinism** — the same seed produces an identical castle. Hash the resulting transform
>   hierarchy, run twice across a fresh domain reload, diff. Then run it again with scene VFX and
>   audio active, which is what catches an accidental `UnityEngine.Random` dependency.
> - **Reachability** — `LayoutValidator` accepts a connected layout and rejects a severed one, and
>   the generator rerolls rather than shipping a castle that soft-locks a run.
> - **Socket matching** — a `door` never joins an `arrow-loop`; every placed module's sockets are
>   either matched or sealed.
> - **Gravity regression** — a crate and a player dropped from height fall along world −Y, land flat,
>   and do not slerp their rotation; a thrown crate still deals velocity-scaled damage through
>   `Item.OnCollisionEnter`. `grep -rn "Gravity" Assets/_Project` must return nothing.
> - **Loot arithmetic** — value, bulk and fragility; two-carrier items refuse a single carrier;
>   dropping a fragile item above its impact threshold destroys it and removes its value.
> - **Extraction** — only what physically crosses the portal threshold is banked, and `LairState`
>   round-trips through JSON to reflect exactly that.
> - **Misfire table** — every lexicon entry has a defined twin, and a below-threshold confidence
>   resolves to that twin deterministically rather than to nothing.
> - **Voice bench** — a scene or harness logging `(recognised id, confidence, latency ms)` per
>   utterance. Targets from the plan: >90% top-1 over the 40-word lexicon, <150 ms from word-end to
>   effect, across four accents. Automated recognition accuracy needs recorded clips, so build the
>   harness to take a folder of wavs; if you have no clips, say so plainly and leave the harness
>   ready rather than reporting a number you did not measure.
>
> **Do not report a test as passing that you did not run.** If the Unity editor is not available in
> your environment, run what you can (plain C# logic through `dotnet test` or an equivalent harness,
> greps, static checks), and for the rest write the test, state exactly which ones are unrun and why,
> and give the human the command to run them. Silence about an unrun test is the failure mode here.
> Manual checks — four players, two editor instances plus a build, invite cold-launch through
> `SteamInviteGateway`, both clients building the same castle from the replicated seed — go in the
> handover as a checklist with expected results.
>
> **Before ship, run the provenance gate:**
> `git log --diff-filter=A --format='%an' -- <path>` across `Assets/_Project/` to confirm nothing in
> the shipping build originated on the excluded branch.
>
> ### How to work
>
> Land M1 and M2 as separate reviewable commits (or a commit per step within them) on the branch you
> were given — not one enormous drop. Update `docs/plans/plunderspell.md`'s Status section and add a
> `docs/systems/` note for each new assembly, matching the existing ones in shape: how it works,
> invariants, traps. When the documentation and the code disagree, the documentation is the intent
> and the code is the bug — unless the documentation is describing work already done differently, in
> which case fix the documentation and say that you did.
>
> Where the design genuinely does not say — a number, a threshold, a curve — choose, state the choice
> and the reasoning in your handover, and move on. Stop and ask only where two readings would send
> the build in materially different directions.
>
> Finish with a handover covering: what you built, what you tested and how, what you could not test
> and why, every judgement call you made, and the manual-verification checklist.

---

## Using this prompt

- **Branch.** Start Fable on a branch forked from this one, which already carries M0. The plan's
  Step 1 (fork from `origin/claude/steam-multiplayer-framework-xia7ch`) is history; do not re-run it.
- **Splitting it.** M1 and M2 are roughly two and six weeks of human work. If a single session is too
  large, cut the prompt at "**M2 — the vertical slice**" and run M2 as a second session with the same
  preamble, constraints and testing sections — those apply to both and must not be dropped.
- **The clean-room constraint is the one to watch.** It is the only instruction here whose violation
  cannot be undone by editing code afterwards. If Fable reports having read anything from
  `feature/Owen/PCG`, the honest move is a fresh session for the generator.
- **Voice model.** No KWS model is vendored in the repo yet. Expect Fable to either add one or build
  the recogniser behind an interface with a stub; either is fine, as long as the handover says which.
