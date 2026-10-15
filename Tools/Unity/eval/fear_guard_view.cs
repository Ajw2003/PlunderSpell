// Play mode, host only. One step of Tools/Unity/coop_fear_guards_check.sh (#420): stands the host's player
// in a ground-floor room of the castle, looking at the biggest group of ground guards, and reports them.
//   frame   put the player 5 m from the ground guard with the most other ground guards within 12 m, facing it
//   count   one line: ground guards, upper-floor guards, ground guards within 14 m of the player
string action = "__ACTION__";
string arg = "__ARG__";

const System.Reflection.BindingFlags All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static
    | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

System.Type T(string name)
{
    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
    {
        var type = assembly.GetType(name);
        if (type != null) return type;
    }
    throw new System.Exception("no type " + name);
}

object Get(object target, string name)
{
    var type = target as System.Type ?? target.GetType();
    object instance = target is System.Type ? null : target;
    for (var t = type; t != null; t = t.BaseType)
    {
        var property = t.GetProperty(name, All);
        if (property != null) return property.GetValue(instance);
        var field = t.GetField(name, All);
        if (field != null) return field.GetValue(instance);
    }
    throw new System.Exception("no member " + name + " on " + type.Name);
}

void Set(object target, string name, object value)
{
    for (var t = target.GetType(); t != null; t = t.BaseType)
    {
        var field = t.GetField(name, All);
        if (field != null) { field.SetValue(target, value); return; }
    }
    throw new System.Exception("no field " + name);
}

var spawner = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.GuardSpawner>();
var ground = new System.Collections.Generic.List<UnityEngine.Transform>();
int upper = 0;
foreach (var guard in spawner.Spawned)
{
    if (guard == null) continue;
    if (guard.transform.position.y > 3f || guard.transform.position.y < -1f) upper++;
    else ground.Add(guard.transform);
}
var player = Get(T("StateMachine.PlayerStateMachine"), "Local") as UnityEngine.Component;

if (action == "count")
{
    int near = 0;
    foreach (var guard in ground)
        if ((guard.position - player.transform.position).magnitude < 14f) near++;
    return "ground " + ground.Count + " other-floors " + upper + " within14m " + near;
}

// A guard is seen from a point when the first thing a ray from the eye meets is that guard.
bool Sees(UnityEngine.Vector3 eye, UnityEngine.Transform guard)
{
    var chest = guard.position + UnityEngine.Vector3.up;
    var toGuard = chest - eye;
    if (toGuard.magnitude > 14f) return false;
    return UnityEngine.Physics.Raycast(eye, toGuard.normalized, out var hit, toGuard.magnitude + 0.5f, ~0,
        UnityEngine.QueryTriggerInteraction.Ignore) && hit.transform.IsChildOf(guard);
}

// The standing spot, among 16 round each ground guard at 3 and 5 m, that sees the most guards.
UnityEngine.Vector3 stand = default;
var seen = new System.Collections.Generic.List<UnityEngine.Transform>();
foreach (var guard in ground)
    for (int ring = 0; ring < 2; ring++)
        for (int step = 0; step < 16; step++)
        {
            var direction = UnityEngine.Quaternion.Euler(0f, step * 22.5f, 0f) * UnityEngine.Vector3.forward;
            var candidate = guard.position + direction * (3f + 2f * ring);
            var visible = new System.Collections.Generic.List<UnityEngine.Transform>();
            foreach (var other in ground)
                if (Sees(candidate + UnityEngine.Vector3.up * 1.6f, other)) visible.Add(other);
            if (visible.Count > seen.Count) { seen = visible; stand = candidate; }
        }
if (seen.Count == 0) return "no spot sees a ground guard";

var view = (UnityEngine.Transform)Get(player, "CameraTransform");
var body = player.GetComponent<UnityEngine.Rigidbody>();
body.position = stand;
player.transform.position = stand;
body.linearVelocity = UnityEngine.Vector3.zero;
var middle = UnityEngine.Vector3.zero;
foreach (var guard in seen) middle += guard.position;
middle /= seen.Count;
var to = middle + UnityEngine.Vector3.up - (stand + UnityEngine.Vector3.up * 1.6f);
float yaw = UnityEngine.Mathf.Atan2(to.x, to.z) * UnityEngine.Mathf.Rad2Deg;
Set(player, "_yaw", yaw);
Set(player, "_xRotation", 0f);
view.localRotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
return "framed: " + seen.Count + " of " + ground.Count + " ground guards in line of sight, player at " + stand.ToString("F1");
