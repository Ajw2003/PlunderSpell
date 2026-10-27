using System.Linq;
using Plunderspell.Guards;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the Ophan of Hours' look (#439): its Animator Controller from its own clips, and a prefab of the model with
    /// the Animator on the TheOphanOfHours_Rig child, where its clips bind (on the top object they miss every bone). The
    /// prefab is only the look; <c>Plunderspell.Raid.OphanOfHours</c> places it in the sky and drives it.
    /// Safe to run again: it rebuilds both. Menu: Plunderspell > Fear > Build The Ophan.
    /// </summary>
    public static class OphanPrefabForge
    {
        public const string ModelPath = ArtBibleModelImporter.FearRoot + "TheOphanOfHours/TheOphanOfHours.fbx";
        public const string ClipsPath = "Assets/Models/ArtBible/Animations/TheOphanOfHours_Generic.fbx";
        public const string ControllerPath = ArtBibleModelImporter.FearRoot + "TheOphanOfHours/TheOphanOfHours.controller";
        public const string PrefabPath = "Assets/_Project/Resources/Fear/TheOphanOfHours.prefab";
        public const string RigName = "TheOphanOfHours_Rig";

        [MenuItem("Plunderspell/Fear/Build The Ophan")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var clips = AssetDatabase.LoadAllAssetsAtPath(ClipsPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__")).ToDictionary(clip => clip.name);
            if (model == null || !clips.ContainsKey("ophan_arrive") || !clips.ContainsKey("ophan_hover_turn"))
            {
                Debug.LogError($"[Ophan] {ModelPath} or its clips in {ClipsPath} are missing. Nothing built.");
                return;
            }

            AnimatorController controller = BuildController(clips["ophan_arrive"], clips["ophan_hover_turn"]);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = "TheOphanOfHours";
                // The importer puts an Animator on the top object; the clips address bones from the rig down.
                Animator top = instance.GetComponent<Animator>();
                if (top != null)
                    Object.DestroyImmediate(top, true);
                Transform rig = instance.transform.Find(RigName);
                // Not ??: a missing component comes back as Unity's fake null, which ?? keeps.
                Animator animator = rig.GetComponent<Animator>();
                if (animator == null)
                    animator = rig.gameObject.AddComponent<Animator>();
                animator.avatar = null;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                // It is a kilometre away and always on screen: never cull it.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (rig.GetComponent<FearAnimationEvents>() == null)
                    rig.gameObject.AddComponent<FearAnimationEvents>();
                foreach (var skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                    skinned.updateWhenOffscreen = true;
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
            Debug.Log($"[Ophan] built {ControllerPath} and {PrefabPath}.");
        }

        private static AnimatorController BuildController(AnimationClip arrive, AnimationClip hover)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            // It unfolds out of a point of light, then hangs there turning.
            AnimatorState arriving = machine.AddState("Arrive");
            arriving.motion = arrive;
            AnimatorState hanging = machine.AddState("Hover");
            hanging.motion = hover;
            machine.defaultState = arriving;
            AnimatorStateTransition settle = arriving.AddTransition(hanging);
            settle.hasExitTime = true;
            settle.exitTime = 0.95f;
            settle.duration = 0.3f;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }
    }
}
