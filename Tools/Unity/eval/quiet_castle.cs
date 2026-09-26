// Play mode only. Removes the garrison and heals the player, so fixed-camera captures are not
// interrupted by a crossbowman. Captures only; never part of play.
var guards = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.GuardSpawner>();
if (guards != null) guards.Clear();
return "garrison cleared";
