using System.Linq;
using Plunderspell.Guards;
using PurrNet;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Gives the Lantern Warden its turn into the Wick (#417): an Animator Controller that plays
    /// <c>warden_turn</c>, the loose-lantern prefab it drops, the Wick as an inactive child, the event receiver and the
    /// turn view, and the tuning switch that lets this guard (and no other) turn. Safe to run again.
    /// Menu: Plunderspell > Fear > Build The Warden's Turn.
    /// </summary>
    public static class WardenTurnForge
    {
        public const string WardenPrefabPath = "Assets/_Project/Prefabs/Enemies/HighMedieval/LanternWarden.prefab";
        public const string ControllerPath = ArtBibleModelImporter.EnemyRoot + "High/LanternWarden/LanternWarden.controller";
        // Outside Prefabs/Enemies: the lantern and the Wick's model are parts of the warden's turn, not roster enemies.
        public const string LanternPrefabPath = "Assets/_Project/Prefabs/Fear/FallenLantern.prefab";
        public const string LanternMaterialPath = "Assets/_Project/Prefabs/Fear/FallenLantern.mat";

        [MenuItem("Plunderspell/Fear/Build The Warden's Turn")]
        public static void Build()
        {
            AnimatorController controller = BuildController();
            GameObject lantern = BuildLooseLantern();
            var wick = AssetDatabase.LoadAssetAtPath<GameObject>(WickPrefabForge.PrefabPath);
            GameObject root = PrefabUtility.LoadPrefabContents(WardenPrefabPath);
            try
            {
                Install(root, controller, wick, lantern);
                PrefabUtility.SaveAsPrefabAsset(root, WardenPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log($"[Warden] the turn is installed on {WardenPrefabPath}.");
        }

        private static AnimatorController BuildController()
        {
            AnimationClip turn = FearClips.Load()["warden_turn"];
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Turn", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle");
            AnimatorState turning = machine.AddState("Turn");
            turning.motion = turn;
            machine.defaultState = idle;
            AnimatorStateTransition start = idle.AddTransition(turning);
            start.hasExitTime = false;
            start.duration = 0.05f;
            start.AddCondition(AnimatorConditionMode.If, 0, "Turn");
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        // A dark iron drum on its side: the placeholder look of the lantern left where it fell.
        private static GameObject BuildLooseLantern()
        {
            var body = new GameObject("FallenLantern");
            try
            {
                GameObject drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                drum.name = "Lantern";
                Object.DestroyImmediate(drum.GetComponent<Collider>());
                drum.transform.SetParent(body.transform, false);
                drum.transform.localScale = new Vector3(0.22f, 0.16f, 0.22f);
                drum.GetComponent<Renderer>().sharedMaterial = DarkIron();
                var collider = body.AddComponent<CapsuleCollider>();
                collider.radius = 0.11f;
                collider.height = 0.32f;
                var physics = body.AddComponent<Rigidbody>();
                physics.mass = 1.5f;
                physics.linearDamping = 3f; // it comes to rest where it fell, not rolls on
                physics.angularDamping = 6f;
                body.AddComponent<NetworkTransform>();
                return PrefabUtility.SaveAsPrefabAsset(body, LanternPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(body);
            }
        }

        private static Material DarkIron()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(LanternMaterialPath);
            if (material != null)
                return material;
            material = new Material(Shader.Find(ArtBibleModelImporter.LitShaderName)) { color = new Color(0.07f, 0.065f, 0.06f) };
            AssetDatabase.CreateAsset(material, LanternMaterialPath);
            return material;
        }

        private static void Install(GameObject root, AnimatorController controller, GameObject wickPrefab, GameObject lantern)
        {
            Animator animator = root.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            Transform wick = root.transform.Find("TheWick");
            if (wick == null)
                wick = ((GameObject)PrefabUtility.InstantiatePrefab(wickPrefab, root.transform)).transform;
            wick.name = "TheWick";
            wick.localPosition = Vector3.zero;
            wick.localRotation = Quaternion.identity;
            wick.gameObject.SetActive(false);

            if (root.GetComponent<FearAnimationEvents>() == null)
                root.AddComponent<FearAnimationEvents>();
            GuardTurnView view = root.GetComponent<GuardTurnView>() ?? root.AddComponent<GuardTurnView>();
            var viewObject = new SerializedObject(view);
            viewObject.FindProperty("_wardenAnimator").objectReferenceValue = animator;
            viewObject.FindProperty("_wick").objectReferenceValue = wick.gameObject;
            viewObject.FindProperty("_lanternRing").objectReferenceValue =
                root.GetComponentsInChildren<Transform>(true).First(part => part.name == "LanternRing");
            viewObject.FindProperty("_looseLanternPrefab").objectReferenceValue = lantern;
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            var guardObject = new SerializedObject(root.GetComponent<Guard>());
            guardObject.FindProperty("_tuning.TrueForm.CanTurn").boolValue = true;
            guardObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
