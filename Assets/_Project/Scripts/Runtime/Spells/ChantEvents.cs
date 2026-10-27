using EventSystems;

namespace Plunderspell.Spells
{
    /// <summary>A chant (a keyboard cast that takes time) started, advanced or ended (#302). Published on each change
    /// while chanting, and once with <see cref="Chanting"/> false when it stops.</summary>
    public readonly struct ChantProgressChanged : IEvent
    {
        public readonly bool Chanting;
        public readonly string Word;
        public readonly float Progress;
        public ChantProgressChanged(bool chanting, string word, float progress)
        {
            Chanting = chanting; Word = word; Progress = progress;
        }
    }
}
