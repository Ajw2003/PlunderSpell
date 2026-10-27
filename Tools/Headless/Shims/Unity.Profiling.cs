// Headless shim for Unity.Profiling.ProfilerMarker: there is no profiler, so Auto() is a no-op scope.
using System;

namespace Unity.Profiling
{
    public readonly struct ProfilerMarker
    {
        public ProfilerMarker(string name) { }
        public AutoScope Auto() => default;
        public void Begin() { }
        public void End() { }

        public readonly struct AutoScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
