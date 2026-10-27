// Play mode only, after set_out_seed.cs. Stands the local player on the ground-floor lobby at the head of the
// down-stair's ramp, facing down it, and holds W through the Input System so the real controller walks them
// down (#254). Run walk_release.cs after a few seconds, then probe_player.cs: y near -3.0 means the crypt floor.
var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>();
var stair = default(Plunderspell.Castle.ProceduralCastleData.PlacedModule);
bool found = false;
foreach (var m in d.Castle.PlacedModules)
    if (m.RoomId == Plunderspell.Castle.ProceduralCastleGenerator.StairDownId) { stair = m; found = true; }
if (!found) return "no down-stair in this castle";
var p = StateMachine.PlayerStateMachine.Local ?? UnityEngine.Object.FindFirstObjectByType<StateMachine.PlayerStateMachine>();
if (p == null) return "no player";
// Ramp (CastleStairPlaceholderForge): local x 3.9 at the lobby floor (local y 3.6) down to x -3.3, z -3.6..-1.6.
var rot = stair.Rotation;
var start = stair.Position + rot * new UnityEngine.Vector3(4.6f, 3.7f, -2.6f);
var down = rot * UnityEngine.Vector3.left;
var rb = p.GetComponent<UnityEngine.Rigidbody>();
rb.isKinematic = false;
rb.position = start; p.transform.position = start;
rb.linearVelocity = UnityEngine.Vector3.zero;
float yaw = UnityEngine.Mathf.Atan2(down.x, down.z) * UnityEngine.Mathf.Rad2Deg;
p.FaceYaw(yaw);
var cam = UnityEngine.Camera.main;
if (cam != null) cam.transform.localRotation = UnityEngine.Quaternion.Euler(20f, yaw, 0f);
UnityEngine.Physics.SyncTransforms();
UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current,
    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W));
return "stair root " + stair.Position + " start " + start.ToString("F2") + " yaw " + yaw.ToString("F0") + "; holding W";
