using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Places the AssetPipeline's FBX models in Unity at positions written in Blender's Z-up coordinates,
    /// as the Lair and Market reference renders place them.
    ///
    /// Unity(X, Y, Z) = Blender(-X, Z, -Y): measured on the imported Lair cellar, whose hearth (Blender
    /// x +3.5) lands at Unity x -3.5. Tools/AssetPipeline/README.md's castle rule ("X passes straight
    /// through") does not hold for these models.
    /// </summary>
    public static class BlenderPlacement
    {
        public static Vector3 ToUnity(Vector3 blender) => new Vector3(-blender.x, blender.z, -blender.y);

        /// <summary>
        /// Blender XYZ Euler (applied X, then Y, then Z) to a Unity rotation. The axis map is a mirror,
        /// so each turn keeps its angle about the mapped axis but reverses sense: X stays X,
        /// Y becomes Unity Z, and a turn about Blender Z is the opposite turn about Unity Y.
        /// </summary>
        public static Quaternion ToUnityRotation(Vector3 blenderDegrees) =>
            Quaternion.AngleAxis(-blenderDegrees.z, Vector3.up)
            * Quaternion.AngleAxis(blenderDegrees.y, Vector3.forward)
            * Quaternion.AngleAxis(blenderDegrees.x, Vector3.right);

        /// <summary>
        /// Instantiates <paramref name="fbxPath"/> under a slot that carries the placement, so the model keeps
        /// whatever root rotation its import gave it. Returns null, with an error, when the model is missing.
        /// </summary>
        public static GameObject PlaceModel(Transform parent, string fbxPath, Vector3 blenderPosition,
            Vector3 blenderDegrees, bool withCollider)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (asset == null)
            {
                Debug.LogError($"[BlenderPlacement] No model at {fbxPath}; it is missing from {parent.name}.");
                return null;
            }

            var slot = new GameObject(System.IO.Path.GetFileNameWithoutExtension(fbxPath));
            slot.transform.SetParent(parent, false);
            slot.transform.localPosition = ToUnity(blenderPosition);
            slot.transform.localRotation = ToUnityRotation(blenderDegrees);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, slot.transform);
            if (withCollider)
                foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            return slot;
        }

        public static Light AddPointLight(Transform parent, string name, Vector3 blenderPosition, Color color,
            float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = ToUnity(blenderPosition);
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            return light;
        }
    }
}
