"""One-off doc edit for the Velox/Saltus change (2026-09-26): tier-4 spells, raid and voice docs.

Kept as a record of exactly what changed in the docs. Re-running it fails on its own asserts,
because the old text it replaces is gone.
"""
import os

os.chdir(os.path.join(os.path.dirname(__file__), '..', '..', '..'))


def sub(p, old, new):
    s = open(p, encoding='utf-8').read()
    assert s.count(old) == 1, (p, old[:70], s.count(old))
    open(p, 'w', encoding='utf-8', newline='').write(s.replace(old, new))


p = 'docs/4-systems/spells.md'
sub(p, '| `Burst` | Frango, Levo, AurumVoco, Tonitrus, Somnus, CadaverSurge, Porta |',
    '| `Burst` | Frango, Levo, AurumVoco, Velox, Somnus, Saltus, Porta |')
sub(p, '| 1–8 | Ignis, Frango, Levo, Aurum Voco, Tonitrus, Somnus, Cadaver Surge, Porta | |',
    '| 1–8 | Ignis, Frango, Levo, Aurum Voco, Velox, Somnus, Saltus, Porta | |')
sub(p, 'Single-target spells (Ignis, Levo, Porta, Cadaver Surge) take the target',
    'Single-target spells (Ignis, Levo, Porta) take the target')
sub(p, 'Area spells (Tonitrus,\nSomnus) burst at the crosshair',
    'Area spells (Somnus)\nburst at the crosshair')
sub(p, '''`Assets/_Project/Data/Spells/`: Porta 10, Levo 12, Ignis 15, Frango 20, Somnus 20, Cadaver Surge
  20, Tonitrus 25, Aurum Voco 30.''', '''`Assets/_Project/Data/Spells/`: Porta 10, Velox 10, Levo 12, Ignis 15, Saltus 15, Frango 20,
  Somnus 20, Aurum Voco 30.''')
sub(p, '''## Invariants
''', '''### Velox and Saltus

Added 2026-09-26, at the owner's call; they replace Tonitrus (a thunderclap stun) and Cadaver Surge
(raise a corpse), which are deleted. Plan: `docs/plans/dodge-and-leap-spells.md`.

- **Velox** (key 5, 10 mana) is the dodge: a dash at `VeloxDashSpeed` (14 m/s) for
  `VeloxDashSeconds` (0.25 s), about 3.5 m, along where you are steering, or where you look when
  you are not. There is no dodge key any more (it was Ctrl, which also whispers).
- **Saltus** (key 7, 15 mana) is a high jump, only from the ground: `SaltusLaunchSpeed` (14 m/s,
  times the volume power) straight up, about 4 m, since the player falls at 2.5 g. Cast in the air
  it fizzles and costs nothing.
- **The slam.** After a Saltus launch, jump in the air drives the body straight down at
  `SaltusSlamSpeed` (22 m/s). On landing, every living thing within `SlamRadius` (3 m) except the
  caster takes up to `SlamDamage` (30), half at the edge, scaled by landing speed, and is shoved
  `SlamKnockback` (2.5 m); the landing makes an `Explosion` noise of `SlamNoiseStrength` 1 over
  `SlamNoiseRadius` (14 m). Jump in the air without a Saltus launch does nothing, as before.
- **Misfires.** A misfired Velox (VELOS, VELO) is a full dash in a random direction. A misfired
  Saltus (SALTAS, SULTUS) is a hop at `MisfireSaltusHop` (a quarter) of a launch, and the legs lock
  for `MisfireSaltusStaggerSeconds` (1 s); it never arms the slam, and is paid for even in mid-air.
- **Where it runs.** These are `ICasterMovementSpell`s: `SpellCastingSystem.Cast` checks
  `CanMove` before spending mana and calls `MoveCaster` on the caster's machine, where the body is
  simulated, through `ISpellMovable` (the player's `PlayerStateMachine`). The server's `Execute`
  only makes the cast noise. The slam's landing is reported by `PlayerStateMachine.SlamLanded`, and
  `SpellCastingSystem` resolves its damage on the server (`ServerSlam` → `SaltusEffect.ResolveSlam`).
- **Speech.** The small English model hears VELOX as "the locks" and SALTUS as "salt is"; the
  `HeardAs` lists on `Spell_VELOX.asset` and `Spell_SALTUS.asset` start from what it heard on the
  synthetic clips. Zira's mispronounced SULTUS is also heard as "salt is", so it casts Saltus;
  David's is heard as "salt soldiers" and misfires. A real voice has not been tried.
- Tests: `MovementSpellTests` (a real player body on a floor), `SpellEffectTests` (the slam's
  damage, falloff, noise, and that a caster with no body fizzles).

## Invariants
''')
sub(p, '''- **Consequence is server-side.** `SpellCastingSystem.ServerCast` runs the effect; the
  `[ObserversRpc]` that follows is presentation only. Running effects in the observers RPC would
  have four clients each applying the same damage.''', '''- **Consequence is server-side.** `SpellCastingSystem.ServerCast` runs the effect; the
  `[ObserversRpc]` that follows is presentation only. Running effects in the observers RPC would
  have four clients each applying the same damage. The one exception is moving the caster's own
  body (Velox, Saltus), which happens on the caster's machine because that is where the body is
  simulated; the damage a slam does still resolves on the server.''')
sub(p, '''## Traps
''', '''## Traps

- **The dodge used to go nowhere.** `PlayerDodgeState` gave one impulse and ended once the body
  was slower than 1 m/s, which the very first physics step always was, before the impulse had been
  applied. It now holds the dash speed for the dash's length. Found by `MovementSpellTests` when
  Velox reused it; the old dodge key had the same fault.
''')

p = 'docs/4-systems/raid.md'
sub(p, 'down by Somnus, Tonitrus and Ignis through `StatusEffectReceiver`.',
    'down by Somnus, Frango and Ignis through `StatusEffectReceiver`.')
sub(p, 'which is what gives Somnus and Tonitrus their point.',
    'which is what gives Somnus and Frango their point.')
print('ok')
