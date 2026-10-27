// Builds the Lair room prefab and renders it from the Blender reference camera
// (Tools/AssetPipeline/render_lair_scene.py: eye (2.4, -4.4, 1.65), target (-0.6, 1.8, 1.3), 17 mm lens),
// so docs/art/models/lair/lair-unity.png can be compared with lair-assembled.png.
// Run: bash Tools/Unity/eval.sh --file Tools/Unity/eval/lair_room_capture.cs
Plunderspell.EditorTools.LairRoomForge.BuildLairRoom();
var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(Plunderspell.EditorTools.LairRoomForge.PrefabPath);
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
var room = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
var camGo = new UnityEngine.GameObject("LairCaptureCamera");
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, scene);
var cam = camGo.AddComponent<UnityEngine.Camera>();
cam.transform.position = Plunderspell.EditorTools.BlenderPlacement.ToUnity(new UnityEngine.Vector3(2.4f, -4.4f, 1.65f));
cam.transform.LookAt(Plunderspell.EditorTools.BlenderPlacement.ToUnity(new UnityEngine.Vector3(-0.6f, 1.8f, 1.3f)));
cam.fieldOfView = 61.5f;   // 17 mm on a 36 mm sensor at 16:9, vertical
cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
cam.backgroundColor = UnityEngine.Color.black;
var rt = new UnityEngine.RenderTexture(1280, 720, 24);
cam.targetTexture = rt;
cam.Render();   // the first pass can come out black while shaders compile
cam.Render();
UnityEngine.RenderTexture.active = rt;
var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0);
tex.Apply();
UnityEngine.RenderTexture.active = null;
string path = "docs/art/models/lair/lair-unity.png";
System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
var bounds = new UnityEngine.Bounds(room.transform.position, UnityEngine.Vector3.zero);
foreach (var r in room.GetComponentsInChildren<UnityEngine.Renderer>()) bounds.Encapsulate(r.bounds);
int colliders = room.GetComponentsInChildren<UnityEngine.Collider>().Length;
int renderers = room.GetComponentsInChildren<UnityEngine.Renderer>().Length;
return $"wrote {path}; renderers {renderers}, colliders {colliders}, bounds centre {bounds.center} size {bounds.size}";
