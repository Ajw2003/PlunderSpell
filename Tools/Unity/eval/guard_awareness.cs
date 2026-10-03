// Play mode, host side only (the server drives the guards). Measures what one calm guard hears and sees
// of the host's own player, for Tools/Unity/guard_awareness_check.sh, which replaces the two
// placeholders below (#238). One action per call; the runner waits between calls.
//   info                 what the player and the guards are (speed, emitter, tuning)
//   place BEHIND|FRONT D [UP]   stand the chosen guard D m from the player (behind its back / in front of
//                        it, facing it), UP m below the player's feet height if UP > 0 means the player is higher
//   footstep STANCE      call FootstepNoiseEmitter.OnFootstep(stance) once (a positive control)
//   walk                 push the player at walking pace for the runner's wait
//   jump                 launch the player upward so it lands
//   loot                 drop the nearest loose loot 1.5 m beside the guard from 0.6 m up
//   read                 counters: state, noticed count, who the guard sees
//   look                 ask the guard to look at once
string action = "__ACTION__";
string arg = "__ARG__";
string arg2 = "__ARG2__";
var inv = System.Globalization.CultureInfo.InvariantCulture;
var player = StateMachine.PlayerStateMachine.Local;
if (player == null) return "no local player";
var guard = System.AppDomain.CurrentDomain.GetData("awGuard") as Plunderspell.Guards.Guard;

if (action == "pick" || guard == null)
{
    guard = null;
    foreach (var g in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.Guard>(UnityEngine.FindObjectsSortMode.None))
    {
        if (g == null || g.IsDead) continue;
        if (guard == null)
        {
            guard = g;
            continue;
        }
        // Every other guard is held still and lifted out of the way, so only the chosen one reacts.
        g.Navigator.Pause();
        g.transform.position += UnityEngine.Vector3.up * 300f;
    }
    if (guard == null) return "no living guard";
    guard.Navigator.Pause();
    System.AppDomain.CurrentDomain.SetData("awGuard", guard);
    System.AppDomain.CurrentDomain.SetData("awAnchor", player.transform.position);
}
var anchor = (UnityEngine.Vector3)System.AppDomain.CurrentDomain.GetData("awAnchor");

if (action == "pick")
    return "picked " + guard.name + " at " + guard.transform.position + ", anchor " + anchor;
var feet = player.transform.position;
var emitter = player.GetComponent<Plunderspell.Acoustics.FootstepNoiseEmitter>();

if (action == "info")
{
    var rb = player.GetComponent<UnityEngine.Rigidbody>();
    return "player " + player.name + " at " + feet + " walkSpeed " + player.walkSpeed
        + " footstepEmitter " + (emitter != null) + " acousticEmitter " + (player.GetComponent<Plunderspell.Acoustics.AcousticEmitter>() != null)
        + " stepAudio " + (player.GetComponent<Plunderspell.Audio.StepAudio>() != null)
        + " guard " + guard.name + " state " + guard.State + " fov " + guard.Tuning.FieldOfView + " sight " + guard.Tuning.SightRange
        + " guards " + UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.Guard>(UnityEngine.FindObjectsSortMode.None).Length
        + " rbMass " + (rb != null ? rb.mass : 0f);
}

if (action == "place")
{
    float d = float.Parse(arg2, inv);
    float up = arg.Contains(":") ? float.Parse(arg.Split(':')[1], inv) : 0f;
    string mode = arg.Split(':')[0];
    // The player goes back to the spot it was at when the guard was picked (UP m higher, held there
    // if UP > 0), and the guard is put on the floor under the anchor, d m in front of it or behind it.
    var rb = player.GetComponent<UnityEngine.Rigidbody>();
    rb.isKinematic = false;
    rb.linearVelocity = UnityEngine.Vector3.zero;
    player.transform.position = anchor + UnityEngine.Vector3.up * up;
    rb.position = player.transform.position;
    rb.isKinematic = up > 0f;
    var flat = UnityEngine.Vector3.forward;
    // BEHIND: the guard stands behind the player (a little to the side, where a walk will pass) and
    // faces away from it. FRONT: the guard stands in front and faces the player.
    var dir = mode == "FRONT" ? flat : -flat;
    var side = mode == "WALK" ? UnityEngine.Vector3.right * 2.5f : UnityEngine.Vector3.zero;
    if (mode == "WALK") dir = -flat;
    guard.transform.position = new UnityEngine.Vector3(anchor.x, anchor.y - 1.19f, anchor.z) + dir * d + side;
    guard.transform.rotation = UnityEngine.Quaternion.LookRotation(mode == "FRONT" ? -dir : dir, UnityEngine.Vector3.up);
    UnityEngine.Physics.SyncTransforms();
    System.AppDomain.CurrentDomain.SetData("awJump", null);
    guard.Leads.Clear();
    guard.ChangeState(guard.States.Patrol);
    System.AppDomain.CurrentDomain.SetData("awNoticed0", guard.Hearing.NoticedCount);
    return "placed " + mode + " " + d + " m up " + up + " guard at " + guard.transform.position + " player at " + player.transform.position;
}

if (action == "footstep")
{
    if (emitter == null) return "no footstep emitter";
    emitter.OnFootstep((Plunderspell.Acoustics.MoveStance)System.Enum.Parse(typeof(Plunderspell.Acoustics.MoveStance), arg));
    return "stepped " + arg;
}

if (action == "walk")
{
    var rb = player.GetComponent<UnityEngine.Rigidbody>();
    // Walks the player sideways at walkSpeed for ARG seconds, a little each editor frame, so StepAudio
    // sees the same per-frame travel as a real walk. It stops by itself.
    double walkEnd = UnityEngine.Time.realtimeSinceStartupAsDouble + double.Parse(arg, inv);
    var from = player.transform.position;
    UnityEditor.EditorApplication.CallbackFunction walkTick = null;
    walkTick = () =>
    {
        if (player == null || UnityEngine.Time.realtimeSinceStartupAsDouble > walkEnd)
        {
            UnityEditor.EditorApplication.update -= walkTick;
            return;
        }
        var step = UnityEngine.Vector3.right * player.walkSpeed * UnityEngine.Time.deltaTime;
        player.transform.position += step;
        rb.position = player.transform.position;
    };
    UnityEditor.EditorApplication.update += walkTick;
    return "walking " + arg + " s at " + player.walkSpeed + " m/s from " + from;
}

if (action == "jump")
{
    // Launches the player upward and records how high it went and how many airborne frames the guard saw it.
    var rb = player.GetComponent<UnityEngine.Rigidbody>();
    float baseY = player.transform.position.y;
    var stats = new float[3];   // peak height, airborne frames, frames seen
    System.AppDomain.CurrentDomain.SetData("awJump", stats);
    double jumpEnd = UnityEngine.Time.realtimeSinceStartupAsDouble + 2.5;
    UnityEditor.EditorApplication.CallbackFunction jumpTick = null;
    jumpTick = () =>
    {
        if (player == null || UnityEngine.Time.realtimeSinceStartupAsDouble > jumpEnd)
        {
            UnityEditor.EditorApplication.update -= jumpTick;
            return;
        }
        float h = player.transform.position.y - baseY;
        if (h > stats[0]) stats[0] = h;
        if (h > 0.3f)
        {
            stats[1] += 1f;
            if (guard.Sight.Visible != null) stats[2] += 1f;
        }
    };
    UnityEditor.EditorApplication.update += jumpTick;
    rb.linearVelocity = new UnityEngine.Vector3(0f, 7f, 0f);
    return "jumped from " + baseY;
}

if (action == "loot")
{
    Plunderspell.Loot.LootPickup best = null;
    float bestD = float.MaxValue;
    foreach (var l in UnityEngine.Object.FindObjectsByType<Plunderspell.Loot.LootPickup>(UnityEngine.FindObjectsSortMode.None))
    {
        var lbody = l.GetComponent<UnityEngine.Rigidbody>();
        // HEAVY picks the heaviest piece in the castle, anything else the nearest to the guard.
        float dd = arg == "HEAVY" ? -(lbody != null ? lbody.mass : 0f) : (l.transform.position - guard.transform.position).sqrMagnitude;
        if (dd < bestD) { bestD = dd; best = l; }
    }
    if (best == null) return "no loose loot";
    var body = best.GetComponent<UnityEngine.Rigidbody>();
    float lootDistance = arg2.Length > 0 && arg2[0] != '_' ? float.Parse(arg2, inv) : 1.5f;
    best.transform.position = guard.transform.position + guard.transform.right * lootDistance + UnityEngine.Vector3.up * 0.6f;
    if (body != null) { body.linearVelocity = UnityEngine.Vector3.zero; body.WakeUp(); }
    return "dropped " + best.name + " mass " + (body != null ? body.mass : 0f) + " beside the guard";
}

if (action == "diag")
{
    // What StepAudio on the local player thinks it is doing: its private counters, by reflection.
    var stepAudio = player.GetComponent<Plunderspell.Audio.StepAudio>();
    var all = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    string text = "";
    foreach (var name in new[] { "_noise", "_wasGrounded", "_idleSeconds", "_travelled", "_windowSeconds", "_fallSpeed" })
        text += name + "=" + typeof(Plunderspell.Audio.StepAudio).GetField(name, all).GetValue(stepAudio) + " ";
    // What stands between the player and the guard's body, as the noise search would see it.
    var gcol = guard.GetComponentInChildren<UnityEngine.Collider>();
    string between = "";
    if (gcol != null)
    {
        var target = gcol.bounds.center;
        var rayDir = target - player.transform.position;
        foreach (var h in UnityEngine.Physics.RaycastAll(player.transform.position, rayDir.normalized, rayDir.magnitude, 1, UnityEngine.QueryTriggerInteraction.Ignore))
            between += h.collider.name + "(rb " + (h.collider.attachedRigidbody != null) + ") ";
        between = "toGuardCenter " + target + " walls " + Plunderspell.Acoustics.NoiseBroadcaster.CountWalls(player.transform.position, target, 1, guard) + " hits " + between;
    }
    return text + "isGrounded " + player.IsGrounded + " dead " + player.dead + " " + between;
}

if (action == "look")
{
    guard.Sight.LookNext();
    return "look queued";
}

if (action == "read")
{
    int n0 = (int)System.AppDomain.CurrentDomain.GetData("awNoticed0");
    float dist = UnityEngine.Vector3.Distance(guard.transform.position, player.transform.position);
    var eye = guard.transform.position + UnityEngine.Vector3.up * guard.Tuning.EyeHeight;
    var aim = player.transform.position + UnityEngine.Vector3.up * guard.Tuning.TargetAimHeight;
    bool cone = Plunderspell.Guards.GuardBrain.CanSee(eye, guard.transform.forward, aim, guard.Tuning.SightRange,
        guard.Tuning.FieldOfView, true);
    var jump = System.AppDomain.CurrentDomain.GetData("awJump") as float[];
    string jumpText = jump == null ? "" : " jumpPeak " + jump[0].ToString("F2") + " airFrames " + jump[1] + " seenFrames " + jump[2];
    return "player " + player.transform.position + jumpText + " state " + guard.State + " noticedSincePlace " + (guard.Hearing.NoticedCount - n0)
        + " coneOnly " + cone + " sees " + (guard.Sight.Visible != null ? guard.Sight.Visible.name : "nobody") + " distance " + dist.ToString("F1")
        + " playerY " + player.transform.position.y.ToString("F2") + " guardY " + guard.transform.position.y.ToString("F2");
}

return "unknown action " + action;
