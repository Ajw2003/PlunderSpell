// Play mode only. Starts a solo session and sets out on a High Medieval raid, the same path the
// main menu's Play Solo and the Lair's Set Out buttons take. Run through `unity command eval_file`.
var session = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Net.CoopSession>();
if (session == null) return "no CoopSession in the scene";
session.PlaySolo();
var lair = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Lair.LairHubManager>();
if (lair != null) lair.SelectEra(Plunderspell.Inventory.HistoricalEra.HighMedieval);
Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.Lair);
return "solo started; state " + Plunderspell.Core.GameServices.GameState.CurrentState;
