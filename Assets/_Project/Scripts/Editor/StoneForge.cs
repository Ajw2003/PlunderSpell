using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the stone a melee guard throws at a player it cannot reach (#237), the same way
    /// <see cref="ProjectileForge"/> builds the bolt: a code-built sphere with a gravity-free Rigidbody and
    /// a <c>NetworkedProjectile</c>. It sits in Resources because the guard loads it as its default stone.
    /// Like the bolt it is not a PurrNet network prefab: <c>NetworkedProjectile</c> is a plain
    /// MonoBehaviour and every peer shows the shot from the replicated attack signal.
    /// </summary>
    public static class StoneForge
    {
        private const string PrefabPath = "Assets/_Project/Resources/GuardStone.prefab";
        private const string MaterialPath = "Assets/_Project/Data/Generated/StoneMaterial.mat";
        private const float Radius = 0.1f;

        [MenuItem("Tools/Plunderspell/Forge Guard Stone Prefab")]
        public static void ForgeStonePrefab()
        {
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            stone.name = "GuardStone";
            stone.transform.localScale = Vector3.one * (Radius * 2f);

            // Gravity off, as for the bolt: a stone flies where it was aimed, up onto a table or ledge too.
            var body = stone.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            stone.GetComponent<Renderer>().sharedMaterial = MakeStoneMaterial();
            stone.AddComponent<NetworkedProjectile>();

            AssetDatabase.DeleteAsset(PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(stone, PrefabPath);
            Object.DestroyImmediate(stone);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Projectile] Forged {PrefabPath}.");
        }

        private static Material MakeStoneMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "StoneMaterial", color = new Color(0.45f, 0.43f, 0.4f) };
            Directory.CreateDirectory("Assets/_Project/Data/Generated");
            AssetDatabase.DeleteAsset(MaterialPath);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
