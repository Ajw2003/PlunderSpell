// Usage: edit values below; writes NightAtmosphere profile through SerializedObject (#268).
var path = "Assets/_Project/Settings/Atmosphere/NightAtmosphere.asset";
var p = UnityEditor.AssetDatabase.LoadAssetAtPath<Plunderspell.Atmosphere.NightAtmosphereProfile>(path);
var so = new UnityEditor.SerializedObject(p);
void C(string prop, float r, float g, float b) { so.FindProperty(prop).colorValue = new UnityEngine.Color(r, g, b, 1); }
foreach (var st in new[] { "Calm", "Stirred", "Roused", "HueAndCry" })
{
    float rouse = st == "Roused" ? 1.1f : st == "HueAndCry" ? 1.15f : 1f;
    so.FindProperty(st + ".FogDensity").floatValue = st == "Roused" || st == "HueAndCry" ? 0.013f : 0.012f;
    so.FindProperty(st + ".FireScatter").floatValue = st == "Calm" ? 0.35f : st == "Stirred" ? 0.4f : st == "Roused" ? 0.45f : 0.5f;
    C(st + ".AmbientSky", 0.34f, 0.27f, 0.2f);
    C(st + ".AmbientEquator", 0.5f * rouse, 0.36f, 0.22f);
    C(st + ".AmbientGround", 0.26f, 0.18f, 0.12f);
}
so.FindProperty("CelSoftness").floatValue = 0.3f;
so.FindProperty("OutlineStrength").floatValue = 0.6f;
so.FindProperty("PaperGrain").floatValue = 0.45f;
so.FindProperty("SootStrength").floatValue = 0.4f;
C("BronzeAge.Stone", 0.95f, 0.85f, 0.68f);
C("LateMedieval.Stone", 0.95f, 0.85f, 0.7f);
C("AgeOfPowder.Stone", 0.9f, 0.85f, 0.78f);
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(p);
UnityEditor.AssetDatabase.SaveAssets();
return "ok";
