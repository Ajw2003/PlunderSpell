// #127: creates Assets/_Project/Resources/SpellBurst.mat, the spell burst's material. URP Unlit,
// transparent alpha blend, double-sided, no depth write, so a burst reads as light and its
// _BaseColor alpha fade shows. Run once through `unity command eval_file` in Edit mode.
const string path = "Assets/_Project/Resources/SpellBurst.mat";
var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
if (mat == null)
{
    mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
    UnityEditor.AssetDatabase.CreateAsset(mat, path);
}
mat.shader = Shader.Find("Universal Render Pipeline/Unlit");
mat.SetFloat("_Surface", 1f);   // Transparent
mat.SetFloat("_Blend", 0f);     // Alpha
mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
mat.SetFloat("_ZWrite", 0f);
mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.6f));
mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
mat.SetOverrideTag("RenderType", "Transparent");
mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
UnityEditor.EditorUtility.SetDirty(mat);
UnityEditor.AssetDatabase.SaveAssets();
return mat.shader.name + " queue " + mat.renderQueue + " transparent " + mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
