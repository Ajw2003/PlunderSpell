// Run through coop_eval.sh host with COOP_EVAL_FILE set to this file: presses and releases one key on the Editor's
// keyboard (__ARG__: f7, f8, f9 or f10), as a player would, for the Ophan Gym check (#455). Editor only: a player
// build's eval cannot compile Input System calls.
string action = "__ACTION__";
string arg = "__ARG__";
switch (action)
{
    case "press":
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return "no keyboard";
        var key = (UnityEngine.InputSystem.Key)System.Enum.Parse(typeof(UnityEngine.InputSystem.Key), arg, true);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
        UnityEngine.InputSystem.InputSystem.Update();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        UnityEngine.InputSystem.InputSystem.Update();
        return "pressed " + key;
    }

}
return "unknown action " + action;
