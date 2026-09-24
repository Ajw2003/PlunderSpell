namespace Plunderspell.Core
{
    /// <summary>
    /// Where the player's microphone choice is saved. Shared by the Settings screen (which writes it)
    /// and the voice service (which reads it), neither of which can reference the other.
    /// </summary>
    public static class AudioInputSettings
    {
        /// <summary>PlayerPrefs key holding the chosen microphone's device name; empty means automatic.</summary>
        public const string MicrophoneKey = "Settings.Microphone";
    }
}
