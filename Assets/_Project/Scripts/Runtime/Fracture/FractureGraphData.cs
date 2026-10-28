// Ported from ufo-3-rag-dahl FractureSystem (aj); see docs/plans/ophan-redo-plan.html.
using UnityEngine;

namespace Plunderspell.Fracture
{
    [System.Serializable]
    public struct PieceMapping
    {
        public int submeshIndex;
        public int pieceIndex;
    }

    [System.Serializable]
    public struct IntArray
    {
        public int[] array;
    }

    [CreateAssetMenu(fileName = "FractureGraphData", menuName = "Fracture System/Fracture Graph Data")]
    public class FractureGraphData : ScriptableObject
    {
        public PieceMapping[] pieceMappings;
        public IntArray[] triangleNeighbors;
        public bool[] isSupportTriangle;
    }
}
