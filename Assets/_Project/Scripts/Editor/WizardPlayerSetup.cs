using System.Linq;
using Player;
using StateMachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    // doc-ref 7a1c docs/4-systems/player-animation.md
    /// <summary>
    /// Builds the wizard's Animator Controller from the AnimForge clips and puts the wizard on the
    /// player prefabs in place of the capsule. Safe to run again: it rebuilds the controller and
    /// skips a prefab that already has its wizard. Menu: Plunderspell > Wizard.
    /// </summary>
    public static class WizardPlayerSetup
    {
        public const string ModelPath = ArtBibleModelImporter.PlayerRoot + "Lair/Wizard/Wizard.fbx";
        public const string ControllerPath = ArtBibleModelImporter.PlayerRoot + "Lair/Wizard/Wizard.controller";
        public const string UpperBodyMaskPath = ArtBibleModelImporter.PlayerRoot + "Lair/Wizard/WizardUpperBody.mask";
        public const string ModelChildName = "Wizard";

        public static readonly string[] PlayerPrefabs =
        {
            "Assets/_Project/Prefabs/RaidPlayer.prefab",
            "Assets/_Project/Prefabs/Player.prefab",
        };

        /// <summary>The capsule's centre is the player's origin; its 1.8 m height puts the feet 0.9 m below.</summary>
        public const float FeetBelowOrigin = 0.9f;

        [MenuItem("Plunderspell/Wizard/Build Animator Controller")]
        public static AnimatorController BuildController()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ArtBibleModelImporter.PlayerClipsPath)
                .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__"))
                .ToDictionary(c => c.name);
            AnimationClip Clip(string id)
            {
                if (!clips.TryGetValue(id, out AnimationClip clip))
                    throw new System.InvalidOperationException(
                        $"[Wizard] clip '{id}' not found in {ArtBibleModelImporter.PlayerClipsPath} " +
                        $"(found: {string.Join(", ", clips.Keys)}). Rebuild with Tools/ArtForge/anim_player.py build.");
                return clip;
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Airborne", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Casting", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine body = controller.layers[0].stateMachine;

            // Speeds are the clips' own (anim_player.py): walk 2 m/s, jog 5 m/s, crouch walk 2 m/s.
            AnimatorState move = body.AddState("Move");
            move.motion = SpeedTree(controller, "Move", (Clip("player_idle"), 0f), (Clip("player_walk"), 2f), (Clip("player_jog"), 5f));
            AnimatorState crouch = body.AddState("Crouch");
            crouch.motion = SpeedTree(controller, "Crouch", (Clip("crouch_idle"), 0f), (Clip("crouch_walk"), 2f));
            AnimatorState takeoff = body.AddState("JumpTakeoff");
            takeoff.motion = Clip("jump_takeoff");
            AnimatorState air = body.AddState("JumpAir");
            air.motion = Clip("jump_air");
            AnimatorState land = body.AddState("JumpLand");
            land.motion = Clip("jump_land");
            AnimatorState dead = body.AddState("Dead");
            dead.motion = Clip("death_collapse");
            body.defaultState = move;

            Link(move, crouch, 0.15f).AddCondition(AnimatorConditionMode.If, 0, "Crouch");
            Link(crouch, move, 0.15f).AddCondition(AnimatorConditionMode.IfNot, 0, "Crouch");
            Link(move, takeoff, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Airborne");
            Link(crouch, takeoff, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Airborne");
            Exit(takeoff, air, 0.1f);
            Link(takeoff, land, 0.05f).AddCondition(AnimatorConditionMode.IfNot, 0, "Airborne");
            Link(air, land, 0.05f).AddCondition(AnimatorConditionMode.IfNot, 0, "Airborne");
            Exit(land, move, 0.15f);
            Link(land, takeoff, 0.05f).AddCondition(AnimatorConditionMode.If, 0, "Airborne");

            AnimatorStateTransition die = body.AddAnyStateTransition(dead);
            die.duration = 0.1f;
            die.canTransitionToSelf = false;
            die.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            Link(dead, move, 0.2f).AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");

            // Casting plays over the arms and head only, so the legs keep walking.
            controller.AddLayer(new AnimatorControllerLayer
            {
                name = "Cast",
                defaultWeight = 1f,
                avatarMask = UpperBodyMask(),
                stateMachine = new AnimatorStateMachine { name = "Cast", hideFlags = HideFlags.HideInHierarchy },
            });
            AnimatorControllerLayer[] layers = controller.layers;
            AnimatorStateMachine cast = layers[1].stateMachine;
            AssetDatabase.AddObjectToAsset(cast, controller);
            AnimatorState none = cast.AddState("None");
            AnimatorState hold = cast.AddState("CastHold");
            hold.motion = Clip("cast_hold");
            AnimatorState release = cast.AddState("CastRelease");
            release.motion = Clip("cast_release");
            cast.defaultState = none;
            Link(none, hold, 0.12f).AddCondition(AnimatorConditionMode.If, 0, "Casting");
            Link(hold, release, 0.05f).AddCondition(AnimatorConditionMode.IfNot, 0, "Casting");
            Exit(release, none, 0.2f);
            Link(release, hold, 0.1f).AddCondition(AnimatorConditionMode.If, 0, "Casting");
            controller.layers = layers;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Wizard] built {ControllerPath} from {clips.Count} clips.");
            return controller;
        }

        [MenuItem("Plunderspell/Wizard/Install On Player Prefabs")]
        public static void Install()
        {
            AnimatorController controller = BuildController();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (model == null || avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError($"[Wizard] {ModelPath}: model or valid Humanoid avatar missing (model {model != null}, " +
                               $"avatar {(avatar == null ? "none" : avatar.isValid + "/" + avatar.isHuman)}). Nothing installed.");
                return;
            }

            foreach (string path in PlayerPrefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"[Wizard] {path} not found; skipped.");
                    continue;
                }
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    InstallOn(root, model, avatar, controller);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[Wizard] {path}: wizard installed.");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void InstallOn(GameObject root, GameObject model, Avatar avatar, AnimatorController controller)
        {
            Transform wizard = root.transform.Find(ModelChildName);
            if (wizard == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = ModelChildName;
                wizard = instance.transform;
            }
            wizard.localPosition = new Vector3(0f, -FeetBelowOrigin, 0f);
            wizard.localRotation = Quaternion.identity;
            wizard.localScale = Vector3.one;
            foreach (Collider collider in wizard.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);

            Animator animator = wizard.GetComponent<Animator>();
            if (animator == null) animator = wizard.gameObject.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // The capsule stays for ShowDownPose's fallback and the collider sizes; it just stops drawing.
            Transform visual = root.transform.Find("Visual");
            if (visual != null && visual.TryGetComponent(out MeshRenderer capsule))
                capsule.enabled = false;

            WizardAnimationDriver driver = root.GetComponent<WizardAnimationDriver>();
            if (driver == null) driver = root.AddComponent<WizardAnimationDriver>();
            var so = new SerializedObject(driver);
            so.FindProperty("_animator").objectReferenceValue = animator;
            so.FindProperty("_body").objectReferenceValue = root.GetComponent<PlayerStateMachine>();
            so.FindProperty("_cast").objectReferenceValue = root.GetComponent<Plunderspell.Voice.PushToCastController>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static BlendTree SpeedTree(AnimatorController controller, string name, params (AnimationClip clip, float speed)[] points)
        {
            var tree = new BlendTree
            {
                name = name,
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

        private static AnimatorStateTransition Link(AnimatorState from, AnimatorState to, float seconds)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = seconds;
            return t;
        }

        private static void Exit(AnimatorState from, AnimatorState to, float seconds)
        {
            AnimatorStateTransition t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = 0.95f;
            t.duration = seconds;
        }

        private static AvatarMask UpperBodyMask()
        {
            AssetDatabase.DeleteAsset(UpperBodyMaskPath);
            var mask = new AvatarMask();
            for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
                mask.SetHumanoidBodyPartActive(part, false);
            foreach (AvatarMaskBodyPart part in new[]
                     {
                         AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm,
                         AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers,
                         AvatarMaskBodyPart.LeftHandIK, AvatarMaskBodyPart.RightHandIK,
                     })
                mask.SetHumanoidBodyPartActive(part, true);
            AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
            return mask;
        }
    }
}
