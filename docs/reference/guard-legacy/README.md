# The guard before the FSM rewrite (reference copy)

Frozen copies of the guard as it was at commit 13206fc6 (2026-10-01), just before the guard was
rebuilt from scratch around a state machine (#203). They are `.txt` files so Unity does not
compile them next to the new `CastleGuard`.

Use them to compare behaviour while the new guard is built and while dropped features are added
back. The same code is also tagged in git as `guard-legacy-2026-10-01`.

| File | What it held |
|---|---|
| `CastleGuard.cs.txt` | All the old behaviour in 1315 lines: senses, movement, attack, Levo, networking, test seams |
| `GuardBrain.cs.txt` | The pure decision functions, including `NextState` (the transition switch) |
| `GuardAlertState.cs.txt` | The replicated state enum |
| `GuardAttackSignal.cs.txt` | The attack counter packed into one SyncVar |
