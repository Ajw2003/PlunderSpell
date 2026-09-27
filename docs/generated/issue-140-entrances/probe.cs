// #140: after a raid has been built, reports where the team arrived and whether the player can
// walk from there into the castle: a NavMesh path from the player to the crypt, and whether a door
// plug stands in the archway of the room inward of a strip arrival. Run in Play mode.
var director = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>();
var castle = director.Castle;
var module = castle.PlacedModules[director.ArrivalModuleIndex];
var player = StateMachine.PlayerStateMachine.Local.transform.position;
var crypt = castle.PlacedModules[castle.CryptStartIndex].Position;
UnityEngine.AI.NavMeshHit from, to;
bool a = UnityEngine.AI.NavMesh.SamplePosition(player, out from, 2f, UnityEngine.AI.NavMesh.AllAreas);
bool b = UnityEngine.AI.NavMesh.SamplePosition(crypt, out to, 3f, UnityEngine.AI.NavMesh.AllAreas);
var path = new UnityEngine.AI.NavMeshPath();
bool found = a && b && UnityEngine.AI.NavMesh.CalculatePath(from.position, to.position, UnityEngine.AI.NavMesh.AllAreas, path);
string plug = "n/a";
if (module.Zone == Plunderspell.Castle.CastleZone.CurtainWall)
{
    var inward = Plunderspell.Castle.CastleEntrancePlanner.InwardCell(module.GridPosition);
    var outward = module.GridPosition - inward;
    string name = "_" + inward.x + "_" + inward.y + "_" + outward.x + "_" + outward.y;
    plug = "none";
    foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        if (t.name.StartsWith("DoorPlug_") && t.name.EndsWith(name)) plug = t.name;
}
return "seed " + director.Seed + ": arrived " + module.Zone + " " + module.RoomId + " at " + module.GridPosition
    + " | entrances " + castle.EntranceCells.Count + " | plug in front: " + plug
    + " | path to crypt: " + (found ? path.status.ToString() : "none") + (found ? " (" + path.corners.Length + " corners)" : "");
