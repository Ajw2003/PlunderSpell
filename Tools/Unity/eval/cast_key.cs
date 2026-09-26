// Play mode only. Taps a spell's number key with the mic open, exactly as holding V and pressing it
// does, then reports the local player's state. Edit KEY and SHIFT (a misfire) before running.
// The keyboard chant means the cast fires 1.5 s later; run probe_player.cs after to see it land.
const UnityEngine.KeyCode KEY = UnityEngine.KeyCode.Alpha5;
const bool SHIFT = false;
var voice = Plunderspell.Voice.VoiceServiceLocator.Current;
var keys = Plunderspell.Voice.VoiceServiceLocator.Keyboard;
if (voice == null || keys == null) return "no voice service";
Plunderspell.Core.GameServices.PlayerStats.RefillMana();
voice.StartListening();
keys.SimulateKeyPress(KEY, SHIFT);
voice.StopListening();
var caster = Plunderspell.Spells.SpellCastingSystem.Local;
var body = caster != null ? caster.GetComponentInParent<StateMachine.PlayerStateMachine>() : null;
return "pressed " + KEY + (SHIFT ? " (misfire)" : "") + "; chanting " + (caster != null ? caster.ChantingWord : "?") +
       "; at " + (body != null ? body.transform.position.ToString("F2") : "?");
