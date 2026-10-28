// Ported from ufo-3-rag-dahl FractureSystem (aj); see docs/plans/ophan-redo-plan.html.
using UnityEngine;
using System.Runtime.InteropServices;

namespace Plunderspell.Fracture
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PieceData
    {
        public Vector3 position;     // 12 bytes
        public int isStatic;         // 4 bytes  (Total: 16)
        public Quaternion rotation;  // 16 bytes (Total: 32)
        public Vector3 velocity;     // 12 bytes
        public float radius;         // 4 bytes  (Total: 48)
        public Vector3 angularVelocity; // 12 bytes
        public float lifeTime;       // 4 bytes  (Total: 64)
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexData
    {
        public Vector3 localPosition; // 12 bytes
        public uint pieceIndex;       // 4 bytes  (Total: 16)
        public Vector3 localNormal;   // 12 bytes
        public float pad0;            // 4 bytes  (Total: 32)
        public Vector2 uv;            // 8 bytes
        public Vector2 pad1;          // 8 bytes  (Total: 48)
    }
}
