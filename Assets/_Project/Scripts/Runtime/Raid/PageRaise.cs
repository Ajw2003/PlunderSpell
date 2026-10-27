using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>The rules for raising the torn page in front of the view (#449): only the holder raises it, and it eases.</summary>
    public static class PageRaise
    {
        public const float EaseSeconds = 0.25f;

        /// <summary>1 when the page should be up, else 0.</summary>
        public static float Target(bool holdsPage, bool readPressed) => holdsPage && readPressed ? 1f : 0f;

        /// <summary>Moves the raise (0 lowered, 1 raised) toward its target, taking <see cref="EaseSeconds"/> for the whole way.</summary>
        public static float Step(float raise, float target, float deltaTime) => Mathf.MoveTowards(raise, target, deltaTime / EaseSeconds);
    }
}
