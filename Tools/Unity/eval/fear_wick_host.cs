// Play mode, host (Editor) only. Actions of Tools/Unity/fear_wick_check.sh (#417); the script fills in the action.
//   spawn    clear the garrison, end the arrival grace and stand one Lantern Warden 4 m in front of the host's
//            player with its back to both players, so it sees nobody yet
//   provoke  turn the warden round to face the host's player, within its sight: it confirms a sighting
//   look     put the host's view on the warden
string action = "__ACTION__";
var spawner = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.GuardSpawner>();
if (spawner != null) spawner.Clear();
var me = StateMachine.PlayerStateMachine.Local;
if (me == null) return "no local player";

Plunderspell.Guards.Guard warden = null;
foreach (var guard in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.Guard>(UnityEngine.FindObjectsSortMode.None))
    if (guard.name.StartsWith("LanternWarden")) warden = guard;

if (action == "spawn")
{
    Plunderspell.Guards.GuardArrivalGrace.End();
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/_Project/Prefabs/Enemies/HighMedieval/LanternWarden.prefab");
    var forward = me.transform.forward; forward.y = 0f; forward.Normalize();
    var spot = me.transform.position + forward * 4f;
    // PurrNet's proxy, since code compiled by the eval is not rewritten to spawn on the network by itself.
    var made = (UnityEngine.GameObject)PurrNet.UnityProxy.Instantiate(prefab, spot, UnityEngine.Quaternion.LookRotation(forward));
    made.GetComponent<Plunderspell.Guards.Guard>().Configure(Plunderspell.Alarm.EnemyDirector.Current);
    // Held still (stunned) until provoked, so the "before" shot is a warden that has not noticed anyone.
    made.GetComponent<Plunderspell.Status.StatusEffectReceiver>().Stun(60f);
    return "warden at " + spot.ToString("F2") + ", facing away, held still";
}
if (warden == null) return "no warden";
var toward = me.transform.position - warden.transform.position; toward.y = 0f;
if (action == "provoke")
{
    warden.Status.ClearAll();
    warden.transform.rotation = UnityEngine.Quaternion.LookRotation(toward);
    warden.Sight.LookNext();
    return "warden now faces the host's player, " + toward.magnitude.ToString("F1") + " m away; sees "
        + (warden.Sight.Visible != null ? warden.Sight.Visible.name : "nobody") + ", intruders known "
        + Plunderspell.Alarm.EnemyDirector.IntrudersOf(warden.Link.Director).Count + ", director " + (warden.Link.Director != null);
}
if (action == "look")
{
    me.FaceYaw(UnityEngine.Quaternion.LookRotation(-toward).eulerAngles.y);
    return "host looks at the warden";
}
return "unknown action " + action;
