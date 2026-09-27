// PlayerInputController.cs itself is excluded from the headless build (it binds to the generated
// Input System action wrapper, also excluded -- see Plunderspell.Headless.csproj). But three Editor
// scene builders reference the type only as `AddComponent<PlayerInputController>()`, and
// LootSettleAndInputGatingTests exercises the pure gate predicate the real file exposes for exactly
// this reason (docs/6-decisions/Decisions.md, "Issue 9's gate belongs on the raid's player"). AcceptsInputIn is
// copied verbatim from the real file rather than re-derived, so this test still asserts the actual
// rule and not a shim's guess at it -- keep the two in sync if the real predicate ever changes.
using UnityEngine;

namespace Player
{
    public class PlayerInputController : MonoBehaviour
    {
        public static bool AcceptsInputIn(Plunderspell.Core.GameState state) =>
            state == Plunderspell.Core.GameState.Playing;
    }
}
