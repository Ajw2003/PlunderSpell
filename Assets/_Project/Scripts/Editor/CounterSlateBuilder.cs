using Plunderspell.Raid;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>Builds one chalk slate for a Market counter: a dark board in a wooden frame, with chalk-white world text on its face.</summary>
    public static class CounterSlateBuilder
    {
        private const string MaterialDirectory = "Assets/_Project/Prefabs/Market";
        private const float Width = 0.8f;
        private const float Height = 0.6f;
        private static readonly Color Chalk = new Color(0.93f, 0.92f, 0.86f);

        /// <summary>
        /// The slate standing on <paramref name="at"/> (the foot of the board), its face towards the player, who looks along
        /// <paramref name="towardsStall"/>. A child of <paramref name="parent"/>; no colliders, so it never stops a piece or a hand.
        /// </summary>
        public static CounterSlate Build(Transform parent, Vector3 at, Vector3 towardsStall)
        {
            var slate = new GameObject("CounterSlate");
            slate.transform.SetParent(parent, false);
            slate.transform.position = at + Vector3.up * (Height * 0.5f + 0.03f);
            slate.transform.rotation = Quaternion.LookRotation(towardsStall, Vector3.up) * Quaternion.Euler(8f, 0f, 0f); // leans back a little

            AddBox(slate.transform, "Frame", new Vector3(Width + 0.06f, Height + 0.06f, 0.04f), Vector3.zero, MaterialOf("SlateFrame",new Color(0.30f, 0.20f, 0.11f)));
            AddBox(slate.transform, "Board", new Vector3(Width, Height, 0.04f), new Vector3(0f, 0f, -0.01f), MaterialOf("SlateBoard",new Color(0.08f, 0.09f, 0.09f)));

            var words = new GameObject("Words");
            words.transform.SetParent(slate.transform, false);
            words.transform.localPosition = new Vector3(0f, 0f, -0.032f);
            words.transform.localScale = Vector3.one * 0.01f; // the rect is in centimetres, like the ledger pages
            var rect = words.AddComponent<TextMeshPro>();
            rect.rectTransform.sizeDelta = new Vector2(Width * 100f - 6f, Height * 100f - 6f);
            rect.font = Resources.Load<TMP_FontAsset>("UI/Fonts/Spectral-Regular SDF");
            rect.fontSize = 68f;
            rect.color = Chalk;
            rect.alignment = TextAlignmentOptions.Top;
            rect.textWrappingMode = TextWrappingModes.Normal;

            var component = slate.AddComponent<CounterSlate>();
            component.Set(rect);
            return component;
        }

        private static void AddBox(Transform parent, string name, Vector3 size, Vector3 at, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = at;
            box.transform.localScale = size;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        // A saved asset, so the prefab keeps it; one per colour, made once.
        private static Material MaterialOf(string name, Color colour)
        {
            string path = $"{MaterialDirectory}/{name}.mat";
            var made = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (made != null)
                return made;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", colour);
            material.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
