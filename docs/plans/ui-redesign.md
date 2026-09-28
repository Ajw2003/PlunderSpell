# UI redesign: bring the game's screens into the pitch's visual world

**Status: planned 2026-09-28, mockup built, implementation in progress.** Asked for by the owner:
"the visual language is taking shape everywhere else but the UI." Visual mockup of every screen:
[`docs/generated/ui-redesign/index.html`](../generated/ui-redesign/index.html).

## The problem

The castle, sky and fire now have a committed look (`docs/plans/night-atmosphere.md`), and the
pitch bible has had a full identity since 2026-09-16: pigment colours, three typefaces, hairline
rules, square corners (`docs/generated/design-system/project/README.md`, `tokens.json`). The
game's UI uses none of it. `UITheme.cs` is a navy-and-orange placeholder, every label is Unity's
built-in `LegacyRuntime.ttf`, every button is a flat orange box with black text, and every screen
is a centred column. The raid HUD (`RaidHudView`, IMGUI) uses the same placeholder palette with
ad-hoc colours on top: a yellow for damage you deal (the colour the identity keeps for gold), a
bright green for a clean cast, blue for mana.

Seen in: `docs/generated/portal-arrival-2026-09-25/05-lair-extracted.png` (the Lair),
`docs/generated/issue-backlog-2026-09-25/raid-hud-after.png` (the raid HUD).

## What is decided (the identity already exists)

The redesign applies the existing design system. It does not invent a new one.

- **Colour means one thing each.** Verdigris: anything you can touch or select. Orpiment: gold and
  value only (debt, banked, haul, loot names). Madder: danger, blood and alarm (health, the hue
  and cry, the critical clock, death). Lapis: the voice (mana, the microphone, a cast being heard).
  Everything else is bone-black, ash and vellum.
- **Three typefaces, one job each.** Eczar for display (titles, the raid clock, spell words, big
  numbers). Spectral for sentences (descriptions, the last-raid line, captions of what you said).
  Overpass Mono for labels, keys and numbers in rows (eyebrows, `[E]`, mana costs, `04:38`
  seconds, debt figures).
- **Square corners, 1 px hairlines, no drop shadows.** Panels are ash with a `line` border; groups
  of panels share a hairline gap.
- **Labels are mono, uppercase, letter-spaced, small**, with a trailing hairline (the "eyebrow").

## Semantic tokens in code

`UITheme` becomes the pigment list plus named roles, so screens say what a colour is for rather
than which colour it is:

| Role | Pigment | Used for |
|---|---|---|
| `Ground` | bone-black `#14120E` | full-screen backgrounds |
| `Surface`, `SurfaceHi` | ash `#1E1A14`, ash-hi `#282318` | panels, hovered panels |
| `Text`, `TextDim`, `TextFaint` | vellum `#DCD2BA`, `#9A9078`, `#635C4C` | primary, secondary, labels and rules |
| `Line`, `LineSoft` | `#332D22`, `#262119` | hairline borders, gaps |
| `Interactive`, `InteractiveLo` | verdigris `#5FA288`, `#2E4C41` | buttons, selection, focus, the active crosshair |
| `Value` | orpiment `#C9A227` | gold, debt, haul, loot names |
| `Danger`, `DangerLo` | madder `#C4542E`, `#5E2A18` | health, alarm, critical clock, death |
| `Voice`, `VoiceLo` | lapis `#7A6AA0`, `#3A3350` | mana, the microphone, what you said |

The alarm is one ramp from quiet to madder: Calm is `TextFaint`, Stirred `DangerLo`, Roused
`Danger`, Hue and cry `Danger` with a slow pulse. The clock turns `Danger` in its last minute, as
now.

## Components

Built in `UIFactory` (uGUI screens) and mirrored in a small IMGUI style set for the raid HUD:

- **Eyebrow**: mono label, 13 px at 1080p, letter-spaced, `TextFaint`, with a hairline to its right.
- **Panel**: `Surface` at 94% opacity, 1 px `Line` border (an outer `Line` image with the
  surface inset by 1 px).
- **Button**, three kinds. *Primary* (one per screen: Set Out, Play Solo, Back to the Lair):
  verdigris fill, bone-black Eczar label. *Secondary*: surface fill, `Line` border, vellum label;
  hover lightens the fill and turns the border verdigris. *Quiet* (Back, Quit): no fill, dim
  label, verdigris on hover. Every button shows a verdigris marker on its left edge while
  hovered or selected, so keyboard and controller focus is visible.
- **Stat**: eyebrow above, value below in Eczar (orpiment when it is money).
- **Bar**: 1 px `Line` frame, flat fill, quarter ticks. Health madder, mana lapis, alarm the ramp.
- **Option row** (settings): label left, value right, with `‹ ›` to step through choices
  (microphone, graphics) or a slider (volumes, gain).
- **Backdrop**: bone-black with two slow candle-glow gradients (one warm, one faintly verdigris),
  the moodboard's lair at 2 am. Generated at runtime as a texture, no asset to author.

## Screens

Layouts are for 1920x1080 (the canvas reference resolution) and scale with it.

- **Main menu.** Left-aligned, not centred: a ring sigil, the wordmark `PLUNDER` in vellum over
  `SPELL` in verdigris (Eczar ExtraBold), the pitch's hook as one line of Spectral, then the four
  actions as a list (Play Solo primary). Co-op status and the version sit on a hairline footer.
- **Lair.** A ledger across the top (Owed, Banked, Last raid) in orpiment figures. Below it, the
  four Ages as four cards in a row, each with its stratum numeral, name, date (`c. 1200 BC`), and
  the pitch's one-line character. The selected Age has a verdigris border and a "Setting out" tag.
  A company panel on the right (session status, Invite Friend, the friends list). Footer: Back to
  Menu on the left, Set Out on the right.
- **Raid HUD.** Top-left: eyebrow with the phase and alarm name, the clock in Eczar, the alarm bar
  split in four named segments. Top-right: the ledger in mono (`OWED 1,050 · BANKED 0`), the haul
  line in orpiment once anything stands in the portal. Bottom-left: health (madder) and mana
  (lapis) bars with mono figures, and `[ESC] Menu`. Bottom-right: the spellbook, eight words in
  Eczar with key numerals and lapis mana costs, a word dimmed when you cannot afford it; while the
  cast key is held its header turns lapis and reads LISTENING. Bottom-centre: the microphone meter
  with whisper and shout marks, and the caption of what you said in Spectral italic. Centre: the
  crosshair turns verdigris over something you can use; the prompt under it is mono with the key in
  a box (`[E] LIFT Gilt Reliquary`).
- **Damage feedback.** Numbers you deal: vellum. Damage to you: madder. Friendly fire: umber-red.
  Loot shattering: flint. The screen-edge wound flash: madder, not pure red.
- **Pause.** The world stays visible (the raid does not pause). A panel on the right titled "The
  raid goes on", with Resume, Settings, Quit to Main Menu.
- **Settings.** One panel, three groups under eyebrows: Sound (three volume sliders), Voice
  (microphone choice, gain), Graphics (quality level). Back as a quiet button.
- **Game over.** Extracted: eyebrow `EXTRACTED`, "Home with" and the coin figure in orpiment
  Eczar. Died: `YOU DIED` in madder Eczar, and "The castle keeps everything you didn't carry out."
  in Spectral. Back to the Lair as the primary button.

## How it is built

1. **Fonts.** Eczar SemiBold and ExtraBold, Spectral Light, Regular and Italic, Overpass Mono
   Medium and SemiBold, all SIL Open Font License, in `Assets/_Project/Resources/UI/Fonts/` with
   their licences. Eczar and Overpass Mono are published only as variable fonts, which Unity's
   legacy text draws at their default weight, so static weights were cut with fontTools. Loaded by
   `UIFonts` through `Resources.Load<Font>`; a missing font logs one warning and falls back to
   `LegacyRuntime.ttf`, so the game never shows no text.
2. **Tokens.** `UITheme` rewritten to the roles above; every screen and HUD view moved to them.
3. **Components.** `UIFactory` gains the eyebrow, panel, three button kinds, stat, bar and option
   row; `UITextures` makes the backdrop glow and 1 px textures once and caches them.
4. **Screens.** Each screen's `OnBuild` re-laid out as above. Behaviour, state wiring and object
   names used by tests stay as they are.
5. **Raid HUD.** `RaidHudView`, `CrosshairView` and `DamageFeedbackView` restyled in place, still
   IMGUI (moving the HUD to a canvas is a separate, larger change). `RaidHudModel`'s strings stay,
   since `HudAndInteractionTests` and `ExtractionHaulTests` check them; the view splits them for
   layout.

Staying on legacy `Text` rather than TextMeshPro: TMP needs font assets generated in the Editor,
and every screen here is built from code. TMP is the better renderer at large sizes and is the
natural next step once someone is in the Editor.

## Verification

- `UIScreenshotPlayModeTests` captures every screen to `UI_Verification_Screenshots/`; compare
  those against the mockup.
- `HudAndInteractionTests`, `ExtractionHaulTests`, `PlayableLoopTests`, `BackdropCameraTests`
  must still pass.
- A person plays menu → Lair → raid → death → Lair → raid → extract, at 1920x1080 and at a small
  window, and checks nothing overlaps.

## Not in this change

The Lair as a 3D place (#30, #31), menu art (#16), an icon set (#34, #40), Steam branding (#58),
and moving the raid HUD off IMGUI. The redesign gives those a visual language to follow.
