// Reports what happened to the guard setup.cs chose.
var d = System.AppDomain.CurrentDomain;
var g = (Plunderspell.Guards.CastleGuard)d.GetData("guard");
if (g == null) return "guard gone (destroyed)";
var st = g.GetComponent<Plunderspell.Status.StatusEffectReceiver>();
var stats = Plunderspell.Core.GameServices.PlayerStats;
float moved = Vector3.Distance(g.transform.position, (Vector3)d.GetData("g0pos"));
return "hp " + (float)d.GetData("g0hp") + " -> " + g.CurrentHealth.ToString("F1") + ", moved " + moved.ToString("F1") + " m, state " + g.State
    + (st != null ? ", burning " + st.IsBurning + " stunned " + st.IsStunned + " asleep " + st.IsAsleep + " levitating " + st.IsLevitating : "")
    + ", airborne " + g.IsAirborne + ", mana " + stats.Mana + "/" + stats.MaxMana;
