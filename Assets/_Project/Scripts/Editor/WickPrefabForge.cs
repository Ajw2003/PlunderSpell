using System.Collections.Generic;
using System.Linq;
using Plunderspell.Guards;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the Wick (#417): its Animator Controller from the fear clips, and its prefab (the model, an
    /// Animator on the Humanoid avatar, and the animation-event receiver). Built the way the wizard's
    /// animator is (<see cref="WizardPlayerSetup"/>). Safe to run again: it rebuilds both.
    /// Menu: Plunderspell > Fear > Build The Wick.
    /// </summary>
    public static class WickPrefabForge
    {
        public const string ModelPath = ArtBibleModelImporter.FearRoot + "TheWick/TheWick.fbx";
        public const string ControllerPath = ArtBibleModelImporter.FearRoot + "TheWick/TheWick.controller";
        public const string PrefabPath = "Assets/_Project/Prefabs/Fear/TheWick.prefab";

        /// <summary>Agent speeds the walk and run clips were authored for (handoff, Part A clip table).</summary>
        public const float StalkSpeed = 1.4f;
        public const float HuntSpeed = 4.2f;

        [MenuItem("Plunderspell/Fear/Build The Wick")]
        public static void Build()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (model == null || avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError($"[Wick] {ModelPath}: model or valid Humanoid avatar missing. Nothing built.");
                return;
            }

            AnimatorController controller = BuildController(FearClips.Load());
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = "TheWick";
                Animator animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                if (instance.GetComponent<FearAnimationEvents>() == null)
                    instance.AddComponent<FearAnimationEvents>();
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
            Debug.Log($"[Wick] built {ControllerPath} and {PrefabPath}.");
        }

        private static AnimatorController BuildController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Catch", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            // The Wick rears up out of the warden when its model appears, then settles into its walk.
            AnimatorState emerge = machine.AddState("Emerge");
            emerge.motion = clips["wick_emerge"];
            AnimatorState move = machine.AddState("Move");
            move.motion = SpeedTree(controller, (clips["wick_idle"], 0f), (clips["wick_stalk"], StalkSpeed),
                (clips["wick_hunt"], HuntSpeed));
            AnimatorState grab = machine.AddState("Catch");
            grab.motion = clips["wick_catch"];
            machine.defaultState = emerge;

            ExitAfterPlaying(emerge, move, 0.15f);
            ExitAfterPlaying(grab, move, 0.15f);
            AnimatorStateTransition seize = machine.AddAnyStateTransition(grab);
            seize.duration = 0.1f;
            seize.canTransitionToSelf = false;
            seize.AddCondition(AnimatorConditionMode.If, 0, "Catch");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static BlendTree SpeedTree(AnimatorController controller, params (AnimationClip clip, float speed)[] points)
        {
            var tree = new BlendTree
            {
                name = "Move",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy,
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            foreach ((AnimationClip clip, float speed) in points)
                tree.AddChild(clip, speed);
            return tree;
        }

        private static void ExitAfterPlaying(AnimatorState from, AnimatorState to, float seconds)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.95f;
            transition.duration = seconds;
        }
    }
}
