// Play mode only, on the Editor, for Tools/Unity/raid_length_check.sh (#433). The script replaces the two placeholders.
//   read         game state, the zone's raid length and time left, the watch caption, and game time (Time.time)
//   watch        raise the pocket watch as holding T does (the key reader is switched off so it cannot lower it)
//   slow <scale> set Time.timeScale (1 restores)
string action = "__ACTION__";
string arg = "__ARG__";

if (action == "slow")
{
    UnityEngine.Time.timeScale = float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture);
    return "time scale " + UnityEngine.Time.timeScale;
}
if (action == "watch")
{
    var keys = UnityEngine.Object.FindFirstObjectByType<Plunderspell.UI.HudHoldKeys>();
    if (keys != null) keys.enabled = false;
    Code.Scripts.EventSystems.EventManager.Instance.Publish(new Plunderspell.UI.WatchRaised(true));
    return "watch raised";
}

var zone = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Extraction.ExtractionZone>();
var watch = UnityEngine.Object.FindFirstObjectByType<Plunderspell.UI.WatchView>();
string caption = watch != null && zone != null ? watch.CaptionFor(zone.TimeRemaining) : "?";
return "state " + Plunderspell.Core.GameServices.GameState.CurrentState
    + " length " + (zone != null ? zone.RaidLength.ToString("F1") : "?")
    + " left " + (zone != null ? zone.TimeRemaining.ToString("F1") : "?")
    + " done " + (zone != null && zone.ExtractionComplete)
    + " ticks " + (zone != null ? Plunderspell.UI.WatchView.LitTicks(zone.TimeRemaining, zone.RaidLength).ToString() : "?")
    + " caption '" + caption + "' gametime " + UnityEngine.Time.time.ToString("F1");
