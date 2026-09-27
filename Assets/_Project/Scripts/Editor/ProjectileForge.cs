using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the one projectile prefab the whole game fires — guard turrets and spells alike.
    /// Code-built for the same reason the rest of the scene tooling is: no authored art exists yet.
    ///
    /// See docs/4-systems/raid.md, "Guards that can actually hurt you".
    /// </summary>
    public static class ProjectileForge
    {
        private const string k_PrefabDirectory = "Assets/_Project/Prefabs/Projectiles";
        private const string k_PrefabPath = k_PrefabDirectory + "/Bolt.prefab";

        private const float k_Radius = 0.12f;

        [MenuItem("Tools/Plunderspell/Forge Projectile Prefab")]
        public static void ForgeProjectilePrefab()
        {
            Directory.CreateDirectory(k_PrefabDirectory);

            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bolt.name = "Bolt";
            bolt.transform.localScale = Vector3.one * (k_Radius * 2f);

            var collider = bolt.GetComponent<SphereCollider>();
            collider.isTrigger = false;

            // Gravity off: a bolt flies where it was aimed. With gravity on, anything fired at a
            // guard across a room lands on the floor between them.
            var body = bolt.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            bolt.GetComponent<Renderer>().sharedMaterial = MakeBoltMaterial();

            var light = new GameObject("Glow");
            light.transform.SetParent(bolt.transform, false);
            Light glow = light.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 4f;
            glow.intensity = 2f;
            glow.color = new Color(0.55f, 0.8f, 1f);

            bolt.AddComponent<NetworkedProjectile>();
            bolt.AddComponent<ProjectileTint>();

            AssetDatabase.DeleteAsset(k_PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(bolt, k_PrefabPath);
            Object.DestroyImmediate(bolt);

            AssetDatabase.SaveAssets();
            Debug.Log($"[Projectile] Forged {k_PrefabPath}.");
        }

        private static Material MakeBoltMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "BoltMaterial" };
            material.color = new Color(0.55f, 0.8f, 1f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.55f, 0.8f, 1f) * 2.5f);

            Directory.CreateDirectory("Assets/_Project/Data/Generated");
            AssetDatabase.CreateAsset(material, "Assets/_Project/Data/Generated/BoltMaterial.mat");
            return material;
        }
    }
}
