// Play mode only. One leg of a walk through the first stair of a kind (#256). ARG is "<StairUp|StairDown> start bx by" to stand the player at a
// point in the module's builder coordinates (x east, y north, metres from the cell centre) on the lobby floor, or
// "<kind> face dx dy" to turn the player to a builder-space direction and hold W. Run walk_release.cs to stop.
string[] all = "__ARG__".Split(' ');
string kind = all[0];
string[] a = new[] { all[1], all[2], all[3] };
var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>();
var stair = default(Plunderspell.Castle.ProceduralCastleData.PlacedModule);
foreach (var m in d.Castle.PlacedModules) if (m.RoomId == kind) { stair = m; break; }
var p = StateMachine.PlayerStateMachine.Local;
float x = float.Parse(a[1], System.Globalization.CultureInfo.InvariantCulture), y = float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture);
if (a[0] == "start")
{
    var at = stair.Position + stair.Rotation * new UnityEngine.Vector3(x, kind == "StairDown" ? 4.5f : 1.2f, y);
    var rb = p.GetComponent<UnityEngine.Rigidbody>();
    rb.isKinematic = false; rb.position = at; p.transform.position = at; rb.linearVelocity = UnityEngine.Vector3.zero;
    UnityEngine.Physics.SyncTransforms();
    return "start " + at.ToString("F2");
}
var dir = stair.Rotation * new UnityEngine.Vector3(x, 0f, y);
float yaw = UnityEngine.Mathf.Atan2(dir.x, dir.z) * UnityEngine.Mathf.Rad2Deg;
p.FaceYaw(yaw);
UnityEngine.Camera.main.transform.localRotation = UnityEngine.Quaternion.Euler(10f, yaw, 0f);
UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,
    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W));
return "facing " + yaw.ToString("F0") + " from " + p.transform.position.ToString("F2");
