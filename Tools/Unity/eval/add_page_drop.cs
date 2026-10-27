// Adds the dropped page (#450): DroppedPageView, PageDropper and PagePicker to the LairRoom prefab's LedgerPages, wired to its keeper. Run once with eval.sh --file.
var path = "Assets/_Project/Prefabs/Lair/LairRoom.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
var pages = root.transform.Find("LedgerPages").gameObject;
if (pages.GetComponent<Plunderspell.Raid.DroppedPageView>() != null) { UnityEditor.PrefabUtility.UnloadPrefabContents(root); return "already there"; }
var keeper = pages.GetComponent<Plunderspell.Raid.DemandPageKeeper>();
var view = pages.AddComponent<Plunderspell.Raid.DroppedPageView>();
var dropper = pages.AddComponent<Plunderspell.Raid.PageDropper>();
var picker = pages.AddComponent<Plunderspell.Raid.PagePicker>();
foreach (var part in new UnityEngine.Component[] { view, dropper, picker })
{
    var so = new UnityEditor.SerializedObject(part);
    so.FindProperty("_keeper").objectReferenceValue = keeper;
    if (part == picker) so.FindProperty("_view").objectReferenceValue = view;
    so.ApplyModifiedPropertiesWithoutUndo();
}
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
UnityEditor.PrefabUtility.UnloadPrefabContents(root);
return "added";
