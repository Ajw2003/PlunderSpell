// #134: gives the 10 weapons and the 5 original loot pieces an authored "GripPoint" child, the way
// EraContentForge does for the era plunder: a fraction of the mesh bounds in the prefab's own space
// (see grip-survey-2026-09-26.cs for those bounds). The same Transform goes on the Item (the raid's
// carry) and the LootPickup. Re-running replaces the grip. Run through `unity command eval_file`.
var fractions = new System.Collections.Generic.Dictionary<string, Vector3>
{
    { "Weapons/ArmingSword", new Vector3(0.5f, 0.5f, 0.10f) },    // the hilt, 0.10 m above the pommel
    { "Weapons/Longsword", new Vector3(0.5f, 0.5f, 0.10f) },      // the hilt, 0.10 m above the pommel
    { "Weapons/BronzeSword", new Vector3(0.5f, 0.5f, 0.12f) },    // the hilt, 0.08 m above the pommel
    { "Weapons/Crossbow", new Vector3(0.5f, 0.25f, 0.5f) },       // the stock, 0.24 m from the butt
    { "Weapons/Matchlock", new Vector3(0.5f, 0.25f, 0.5f) },      // the stock, 0.24 m from the butt
    { "Weapons/FlintlockPistol", new Vector3(0.5f, 0.2f, 0.3f) }, // the handle, by the butt
    { "Weapons/PaviseShield", new Vector3(0.5f, 0.5f, 0.5f) },    // the centre of the back
    { "Weapons/RoundShield", new Vector3(0.5f, 0.5f, 0.5f) },     // the centre boss
    { "Weapons/PlateHelm", new Vector3(0.5f, 0.5f, 0.1f) },       // the lower rim
    { "Weapons/PowderGrenade", new Vector3(0.5f, 0.5f, 0.5f) },   // in the fist
    { "Loot/AncientRelic", new Vector3(0.5f, 0.5f, 0.5f) },       // round the middle
    { "Loot/CopperPot", new Vector3(0.05f, 0.5f, 0.85f) },        // the rim
    { "Loot/GoldenGoblet", new Vector3(0.5f, 0.5f, 0.3f) },       // the stem
    { "Loot/HeavyChest", new Vector3(0.03f, 0.5f, 0.7f) },        // an end handle
    { "Loot/SilverPlate", new Vector3(0.05f, 0.5f, 0.5f) },       // the rim
};
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var sb = new System.Text.StringBuilder();
foreach (var pair in fractions)
{
    string path = "Assets/_Project/Prefabs/" + pair.Key + ".prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    Bounds b = new Bounds(); bool any = false;
    foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
    {
        if (mf.sharedMesh == null) continue;
        var mb = mf.sharedMesh.bounds;
        for (int i = 0; i < 8; i++)
        {
            var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var local = root.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
            if (!any) { b = new Bounds(local, Vector3.zero); any = true; } else b.Encapsulate(local);
        }
    }
    Transform old = root.transform.Find("GripPoint");
    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
    var grip = new GameObject("GripPoint").transform;
    grip.SetParent(root.transform, false);
    grip.localPosition = b.min + Vector3.Scale(b.size, pair.Value);
    var item = root.GetComponent<Item>();
    var itemSo = new UnityEditor.SerializedObject(item);
    itemSo.FindProperty("_gripPoint").objectReferenceValue = grip;
    itemSo.ApplyModifiedPropertiesWithoutUndo();
    var pickup = root.GetComponent<Plunderspell.Loot.LootPickup>();
    if (pickup != null)
    {
        var pickupSo = new UnityEditor.SerializedObject(pickup);
        pickupSo.FindProperty("_gripPoint").objectReferenceValue = grip;
        pickupSo.ApplyModifiedPropertiesWithoutUndo();
    }
    sb.Append(pair.Key + " grip " + grip.localPosition.ToString("F3") + (pickup != null ? "" : " (no pickup)") + "; ");
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
return sb.ToString();
