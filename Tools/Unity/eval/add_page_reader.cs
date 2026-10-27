// Adds DemandPageReader (#449) to the LairRoom prefab's LedgerPages, wired to its keeper. Run once with eval.sh --file.
var path = "Assets/_Project/Prefabs/Lair/LairRoom.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
var pages = root.transform.Find("LedgerPages").gameObject;
if (pages.GetComponent<Plunderspell.Raid.DemandPageReader>() != null) { UnityEditor.PrefabUtility.UnloadPrefabContents(root); return "already there"; }
var reader = pages.AddComponent<Plunderspell.Raid.DemandPageReader>();
var so = new UnityEditor.SerializedObject(reader);
so.FindProperty("_keeper").objectReferenceValue = pages.GetComponent<Plunderspell.Raid.DemandPageKeeper>();
so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path);
UnityEditor.PrefabUtility.UnloadPrefabContents(root);
return "added";
