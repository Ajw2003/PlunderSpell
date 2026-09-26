// Play mode only. Leaves the Lair for the castle, as the Set Out button does.
Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.Playing);
var d = UnityEngine.Object.FindFirstObjectByType<RogueAi.Raid.RaidDirector>();
return "phase " + d.Phase + " seed " + d.Seed + " arrival module " + d.ArrivalModuleIndex + " at " + d.ArrivalPoint;
