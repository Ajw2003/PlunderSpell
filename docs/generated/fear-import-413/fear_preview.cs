// Renders the imported Wick and Ophan side by side (and the Ophan at ophan_arrive's first frame) to PNGs (#413).
// Run: bash Tools/Unity/eval.sh --file docs/generated/fear-import-413/fear_preview.cs
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
UnityEngine.GameObject Spawn(string path, UnityEngine.Vector3 at)
{
    var go = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path));
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
    go.transform.position = at;
    go.transform.rotation = UnityEngine.Quaternion.identity;
    return go;
}
UnityEngine.AnimationClip Clip(string path, string name) =>
    UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.AnimationClip>().First(c => c.name == name);
string ophanClips = "Assets/Models/ArtBible/Animations/TheOphanOfHours_Generic.fbx";
var wick = Spawn("Assets/Models/ArtBible/Enemies/Fear/TheWick/TheWick.fbx", new UnityEngine.Vector3(-4, 0, 0));
var ophan = Spawn("Assets/Models/ArtBible/Enemies/Fear/TheOphanOfHours/TheOphanOfHours.fbx", new UnityEngine.Vector3(5, 0, 0));
// The Ophan clips bind from its armature node down, so they play on TheOphanOfHours_Rig, not the model root.
var ophanRig = ophan.transform.Find("TheOphanOfHours_Rig").gameObject;

var light = new UnityEngine.GameObject("Light").AddComponent<UnityEngine.Light>();
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject, scene);
light.type = UnityEngine.LightType.Directional; light.intensity = 1.6f;
light.transform.rotation = UnityEngine.Quaternion.Euler(35, 150, 0);
var cam = new UnityEngine.GameObject("Cam").AddComponent<UnityEngine.Camera>();
UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam.gameObject, scene);
cam.scene = scene; cam.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
cam.backgroundColor = new UnityEngine.Color(0.32f, 0.34f, 0.38f); cam.fieldOfView = 40;
cam.transform.position = new UnityEngine.Vector3(0, 5, -26);
cam.transform.LookAt(new UnityEngine.Vector3(0, 4.5f, 0));

string Render(string file)
{
    var rt = new UnityEngine.RenderTexture(1600, 1000, 24);
    cam.targetTexture = rt; cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1600, 1000, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 1000), 0, 0); tex.Apply();
    UnityEngine.RenderTexture.active = null; cam.targetTexture = null;
    System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
    return file;
}
// A preview scene does not re-skin on render, so this shows each model in its rest pose. The clips were
// checked by reading bones after sampling instead (ophan_arrive frame 0: Core scale 0.02).
Render("C:/Users/aj/AppData/Local/Temp/claude/C--Users-aj-Desktop-GameDev-PlunderSpell/5a0322b8-145e-4a12-97ec-a075a16fb90d/scratchpad/warmup.png");
var r1 = Render("docs/generated/fear-import-413/wick-and-ophan.png");
UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
return r1;
