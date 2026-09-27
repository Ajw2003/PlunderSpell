// Play mode only. Sets out on a fixed seed (777) so captures from run to run show the same castle.
var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>();
d.SetFixedSeed(777);
Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.Playing);
var gate = d.Castle.PlacedModules[d.Castle.ExtractionExitIndex];
return "phase " + d.Phase + " seed " + d.Seed + " arrival " + d.ArrivalPoint + " gate " + gate.Position + " grid " + gate.GridPosition;
