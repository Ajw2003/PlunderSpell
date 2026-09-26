using Code.Scripts.EventSystems;
using Player;
using StateMachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Development convenience only, not shipped gameplay. See docs/systems/core.md - Dev tooling.
public static class TestSceneBuilder
{
    // Judgement calls - tune here rather than hunting through the generated scene.
    // Standard world gravity (Physics.gravity, -Y) is used, so the test surface is a large flat
    // ground plane at the origin rather than a spherical planet. 120 units square is roughly 20
    // seconds of walking edge to edge at PlayerWalkSpeed: enough room to build up speed and test
    // a fall without the ground ending mid-test.
    private const float GroundSize = 120f;
    private const float GroundThickness = 1f;
    private const float PlayerCapsuleHeight = 2f;
    private const float PlayerCapsuleRadius = 0.5f;
    private const float PlayerWalkSpeed = 6f;
    private const float PlayerJumpForce = 7f;
    private const float PlayerGroundCheckRadius = 0.3f;
    private const float PlayerGroundCheckDistance = 1.6f;
    private const float PlayerGroundedHeight = 1f;
    private const float ItemSize = 0.75f;

    private const string ScenePath = "Assets/_Project/Scenes/TestScene.unity";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";

    [MenuItem("Tools/RogueAi/Build Test Scene")]
    public static void BuildTestScene()
    {
        // A fresh scene built from scratch every run, saved over the same path, is what keeps
        // this idempotent - there is never a second planet to accumulate.
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildSceneLight();
        BuildManagers();

        BuildGround();
        GameObject playerPrefab = BuildPlayerPrefab();
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
        BuildTestItem();

        player.transform.position = new Vector3(0f, PlayerGroundedHeight + PlayerCapsuleHeight * 0.5f, 0f);

        EnsureSceneFolderExists();
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log($"RogueAi: test scene built at {ScenePath} with player prefab at {PlayerPrefabPath}.");
    }

    // NewSceneSetup.EmptyScene omits the default light, so without this the ground renders unlit
    // and the scene looks broken before any gameplay is even exercised.
    private static void BuildSceneLight()
    {
        GameObject lightObject = new GameObject("DirectionalLight");
        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // ItemManager drives the grab/carry/throw input and is a scene-owned singleton, so without an
    // instance in the scene ItemManager.Instance stays null and clicking an item does nothing.
    // EventManager backs the input controller's re-enable path for the same reason.
    private static void BuildManagers()
    {
        new GameObject("EventManager").AddComponent<EventManager>();
        new GameObject("ItemManager").AddComponent<ItemManager>();
    }

    // A box rather than a plane: a plane's collider is one-sided and infinitely thin, so anything
    // moving fast enough tunnels straight through it. Standard world gravity (-Y) pulls the
    // player and items straight down onto it, so no GravitySource is needed. The top face sits
    // at y = 0.
    private static void BuildGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "TestGround";
        ground.transform.position = new Vector3(0f, -GroundThickness * 0.5f, 0f);
        ground.transform.localScale = new Vector3(GroundSize, GroundThickness, GroundSize);
    }

    private static GameObject BuildPlayerPrefab()
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";

        CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
        capsule.height = PlayerCapsuleHeight;
        capsule.radius = PlayerCapsuleRadius;

        // Adding PlayerStateMachine pulls in Rigidbody via its [RequireComponent] attribute.
        PlayerStateMachine stateMachine = player.AddComponent<PlayerStateMachine>();
        stateMachine.walkSpeed = PlayerWalkSpeed;
        stateMachine.JumpForce = PlayerJumpForce;

        SetGroundCheckFields(stateMachine);

        // Without this nothing ever calls Move/Jump/Look on the state machine, so the player sits
        // in PlayerIdleState and reads as completely unresponsive.
        player.AddComponent<PlayerInputController>();

        Transform cameraPivot = new GameObject("CameraPivot").transform;
        cameraPivot.SetParent(player.transform, false);
        cameraPivot.localPosition = new Vector3(0f, PlayerCapsuleHeight * 0.5f, 0f);
        stateMachine.CameraTransform = cameraPivot;

        GameObject cameraObject = new GameObject("PlayerCamera");
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<Camera>();
        cameraObject.transform.SetParent(cameraPivot, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.5f, -4f);

        EnsurePrefabFolderExists();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        Object.DestroyImmediate(player);
        return prefab;
    }

    // _groundLayer, _groundCheckRadius, etc. are private [SerializeField] fields per the project's
    // style rules, so they need SerializedObject rather than a direct field write.
    private static void SetGroundCheckFields(PlayerStateMachine stateMachine)
    {
        SerializedObject serialized = new SerializedObject(stateMachine);
        serialized.FindProperty("_groundCheckRadius").floatValue = PlayerGroundCheckRadius;
        serialized.FindProperty("_groundCheckDistance").floatValue = PlayerGroundCheckDistance;
        serialized.FindProperty("_groundedHeight").floatValue = PlayerGroundedHeight;
        serialized.FindProperty("_groundLayer").intValue = LayerMask.GetMask("Default");
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildTestItem()
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = "TestItem";
        item.transform.localScale = Vector3.one * ItemSize;
        // Dropped from a height so the first thing the scene shows is the item falling and
        // tumbling - the quickest read on whether gravity came back correctly.
        item.transform.position = new Vector3(3f, 5f, 0f);

        // Item's [RequireComponent] pulls in a Rigidbody (useGravity = true at runtime);
        // CreatePrimitive already gave the cube a BoxCollider for the grab/throw physics.
        item.AddComponent<Item>();
    }

    private static void EnsureSceneFolderExists()
    {
        EnsureFolderExists("Assets/_Project/Scenes");
    }

    private static void EnsurePrefabFolderExists()
    {
        EnsureFolderExists("Assets/_Project/Prefabs");
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (folderPath == "Assets" || AssetDatabase.IsValidFolder(folderPath)) return;

        string parent = System.IO.Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(folderPath);
        EnsureFolderExists(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
