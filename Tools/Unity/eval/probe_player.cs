// Play mode only. The local player's position, speed and movement-spell state, and mana.
var caster = Plunderspell.Spells.SpellCastingSystem.Local;
if (caster == null) return "no local caster";
var body = caster.GetComponentInParent<StateMachine.PlayerStateMachine>();
if (body == null) return "no player body";
return "at " + body.transform.position.ToString("F2") + " v " + body._rb.linearVelocity.ToString("F1") +
       " grounded " + body.IsGrounded + " slamArmed " + body.IsSlamArmed +
       " slamming " + body.IsSlamming + " staggered " + body.IsStaggered +
       " mana " + Plunderspell.Core.GameServices.PlayerStats.Mana;
