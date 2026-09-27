// #143: a snapshot of every piece of per-raid state a player could see carry over from one raid
// to the next. Run at the start of a raid (Play mode); compare against a fresh first raid.
var sb = new System.Text.StringBuilder();
var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>();
var p = StateMachine.PlayerStateMachine.Local;
var stats = Plunderspell.Core.GameServices.PlayerStats;
var alarm = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Alarm.AlarmFSMManager>();
var zone = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Extraction.ExtractionZone>();
var dmg = Plunderspell.UI.DamageFeedbackView.Instance;
var shake = Plunderspell.UI.CameraShakeDirector.Instance;
sb.Append("phase=" + d.Phase + " seed=" + d.Seed + " state=" + Plunderspell.Core.GameServices.GameState.CurrentState);
sb.Append(" | hp=" + (p != null ? p.CurrentHealth.ToString("F0") : "-") + " dead=" + (p != null && p.dead) + " statsHp=" + stats.Health + " mana=" + stats.Mana + "/" + stats.MaxMana);
sb.Append(" | slamArmed=" + (p != null && p.IsSlamArmed) + " staggered=" + (p != null && p.IsStaggered));
var status = p != null ? p.GetComponent<Plunderspell.Status.StatusEffectReceiver>() : null;
if (status != null) sb.Append(" burning=" + status.IsBurning + " stunned=" + status.IsStunned + " asleep=" + status.IsAsleep);
sb.Append(" | alarm=" + (alarm != null ? alarm.State + " " + alarm.AlarmLevel.ToString("F0") : "-"));
sb.Append(" | zonePieces=" + (zone != null ? zone.PiecesInZone.ToString() : "-"));
sb.Append(" | held=" + (ItemManager.Instance != null && ItemManager.Instance.CarriedItem != null ? ItemManager.Instance.CarriedItem.name : "none"));
sb.Append(" | vignette=" + (dmg != null ? dmg.Vignette.ToString("F2") : "-") + " numbers=" + (dmg != null ? dmg.LiveNumberCount : -1) + " hurtLines=" + (dmg != null ? string.Join("/", dmg.HurtLines) : ""));
sb.Append(" | trauma=" + (shake != null ? shake.Trauma.ToString("F2") : "-"));
int guards = 0, alertGuards = 0, dead = 0;
foreach (var g in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.CastleGuard>(FindObjectsSortMode.None))
{ guards++; if (g.IsDead) dead++; else if (g.State != Plunderspell.Guards.GuardAlertState.Patrolling) alertGuards++; }
sb.Append(" | guards=" + guards + " notPatrolling=" + alertGuards + " dead=" + dead);
int loot = UnityEngine.Object.FindObjectsByType<Plunderspell.Loot.LootValue>(FindObjectsSortMode.None).Length;
sb.Append(" | loot=" + loot + " bursts=" + UnityEngine.Object.FindObjectsByType<Plunderspell.Spells.Vfx.SpellBurst>(FindObjectsSortMode.None).Length);
sb.Append(" | castles=" + UnityEngine.Object.FindObjectsByType<Plunderspell.Castle.CastleRoomModule>(FindObjectsSortMode.None).Length);
return sb.ToString();
