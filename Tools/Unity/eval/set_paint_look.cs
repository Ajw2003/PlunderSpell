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
    C(st + ".AmbientSky", 0.4f, 0.38f, 0.28f);
    C(st + ".AmbientEquator", 0.6f * rouse, 0.55f, 0.32f);
    C(st + ".AmbientGround", 0.3f, 0.24f, 0.15f);
}
so.FindProperty("CelSoftness").floatValue = 0.45f;
so.FindProperty("OutlineStrength").floatValue = 0.6f;
so.FindProperty("PaperGrain").floatValue = 0.6f;
so.FindProperty("SootStrength").floatValue = 0.4f;
C("HighMedieval.Stone", 1.5f, 2.2f, 1.0f);
C("BronzeAge.Stone", 1.3f, 1.75f, 0.45f);
C("LateMedieval.Stone", 1.4f, 1.9f, 0.6f);
C("AgeOfPowder.Stone", 0.8f, 1.2f, 0.8f);
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.EditorUtility.SetDirty(p);
UnityEditor.AssetDatabase.SaveAssets();
return "ok";
