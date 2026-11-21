#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public static class SetupFootIKLab
{
    [MenuItem("Tools/Debug Hero Pivot")]
    public static void DebugHeroPivot()
    {
        GameObject hero = GameObject.Find("Hero");
        if (hero == null) return;
        Debug.Log($"[HeroPivot] Hero pos={hero.transform.position}, rot={hero.transform.eulerAngles}");
        foreach (Transform t in hero.transform)
        {
            Debug.Log($"[HeroPivot] Child {t.name}: localPos={t.localPosition}, pos={t.position}");
        }
        var renderers = hero.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (var r in renderers)
        {
            Debug.Log($"[HeroPivot] SkinnedMesh {r.name}: bounds.center={r.bounds.center}, bounds.extents={r.bounds.extents}");
        }
    }

    [MenuItem("Tools/Debug Walk Animation")]
    public static void DebugWalk()
    {
        var all1 = AssetDatabase.LoadAllAssetsAtPath("Assets/Asset-Meet7/Animation-humanoid/humanoid_walkForward_faceForward.fbx");
        foreach (var a in all1)
        {
            if (a is AnimationClip c)
            {
                Debug.Log($"[AnimDebug] humanoid_walk: {c.name}, isLooping: {c.isLooping}, length: {c.length}, wrapMode: {c.wrapMode}");
            }
        }

        var all2 = AssetDatabase.LoadAllAssetsAtPath("Assets/Asset-Meet7/Animation-humanoid/DefaultAnims/Walk.fbx");
        foreach (var a in all2)
        {
            if (a is AnimationClip c)
            {
                Debug.Log($"[AnimDebug] DefaultAnims/Walk: {c.name}, isLooping: {c.isLooping}, length: {c.length}, wrapMode: {c.wrapMode}");
            }
        }
    }

    [MenuItem("Tools/Setup Foot IK Lab")]
    public static void ExecuteSetup()
    {
        Debug.Log("[FootIK] Starting Foot IK Lab Setup...");

        // 1. Ensure tag 'walkable' and layer 'Player'
        EnsureTagAndLayer();

        // 2. Setup Animator Controller
        AnimatorController controller = CreateOrGetAnimatorController();

        // 3. Setup Scene
        SetupScene(controller);

        Debug.Log("[FootIK] Setup Completed Successfully!");
    }

    private static void EnsureTagAndLayer()
    {
        var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAssets != null && tagManagerAssets.Length > 0)
        {
            SerializedObject tagManager = new SerializedObject(tagManagerAssets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            bool hasTag = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == "walkable")
                {
                    hasTag = true;
                    break;
                }
            }
            if (!hasTag)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "walkable";
            }

            SerializedProperty layersProp = tagManager.FindProperty("layers");
            bool hasLayer = false;
            int emptyIndex = -1;
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                string s = layersProp.GetArrayElementAtIndex(i).stringValue;
                if (s == "Player")
                {
                    hasLayer = true;
                    break;
                }
                if (string.IsNullOrEmpty(s) && emptyIndex == -1)
                {
                    emptyIndex = i;
                }
            }
            if (!hasLayer && emptyIndex != -1)
            {
                layersProp.GetArrayElementAtIndex(emptyIndex).stringValue = "Player";
            }
            tagManager.ApplyModifiedProperties();
        }
    }

    private static AnimatorController CreateOrGetAnimatorController()
    {
        string dir = "Assets/Animations";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        string controllerPath = "Assets/Animations/HeroAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Add isWalk parameter if not existing
        bool hasParam = false;
        foreach (var p in controller.parameters)
        {
            if (p.name == "isWalk")
            {
                hasParam = true;
                break;
            }
        }
        if (!hasParam)
        {
            controller.AddParameter("isWalk", AnimatorControllerParameterType.Bool);
        }

        // Enable IK Pass on Base Layer
        var layers = controller.layers;
        if (layers.Length > 0)
        {
            layers[0].iKPass = true;
            controller.layers = layers;
        }

        // Load animations
        AnimationClip idleClip = FindAnimationClip(
            "Assets/Asset-Meet7/Animation-humanoid/humanoid_idle.fbx",
            "Assets/Asset-Meet7/Animation-humanoid/DefaultAnims/Idle.fbx"
        );
        AnimationClip walkClip = FindAnimationClip(
            "Assets/Asset-Meet7/Animation-humanoid/humanoid_walkForward_faceForward.fbx",
            "Assets/Asset-Meet7/Animation-humanoid/DefaultAnims/Walk.fbx"
        );

        var rootStateMachine = controller.layers[0].stateMachine;

        // Clean existing states or reuse
        ChildAnimatorState idleState = default;
        ChildAnimatorState walkState = default;
        bool hasIdle = false;
        bool hasWalk = false;

        foreach (var s in rootStateMachine.states)
        {
            if (s.state.name == "Idle") { idleState = s; hasIdle = true; }
            if (s.state.name == "Walk") { walkState = s; hasWalk = true; }
        }

        if (!hasIdle)
        {
            var st = rootStateMachine.AddState("Idle");
            st.motion = idleClip;
            idleState.state = st;
            rootStateMachine.defaultState = st;
        }
        else
        {
            idleState.state.motion = idleClip;
        }

        if (!hasWalk)
        {
            var st = rootStateMachine.AddState("Walk");
            st.motion = walkClip;
            walkState.state = st;
        }
        else
        {
            walkState.state.motion = walkClip;
        }

        // Transitions
        idleState.state.transitions = new AnimatorStateTransition[0];
        walkState.state.transitions = new AnimatorStateTransition[0];

        // Idle -> Walk
        var toWalk = idleState.state.AddTransition(walkState.state);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.25f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalk");

        // Walk -> Idle
        var toIdle = walkState.state.AddTransition(idleState.state);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.25f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalk");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        return controller;
    }

    private static AnimationClip FindAnimationClip(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
        }
        return null;
    }

    private static void SetupScene(AnimatorController controller)
    {
        // Remove existing test objects from root
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in rootObjects)
        {
            if (go == null) continue;
            if (go.name.Contains("humanoid_dying") || go.name == "Hero" || go.name == "Ground" || go.name.StartsWith("batu"))
            {
                Object.DestroyImmediate(go);
            }
        }

        // Materials
        Material keramikMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Asset-Meet7/material/keramik.mat");
        Material rockMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Asset-Meet7/material/rock.mat");

        // Ground
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -0.5f, 0);
        ground.transform.localScale = new Vector3(12, 1, 12);
        if (keramikMat != null)
        {
            ground.GetComponent<MeshRenderer>().sharedMaterial = keramikMat;
        }

        // Batu / Stepping stones (walkable tag, isTrigger checked)
        // batu1: placed right under Left Foot to clearly demonstrate Foot IK
        CreateRock("batu1", new Vector3(-0.2f, 0.12f, 0f), new Vector3(0.6f, 0.24f, 0.6f), new Vector3(0, 15, 0), rockMat);
        // Additional stepping stones
        CreateRock("batu2", new Vector3(0.3f, 0.25f, 0.6f), new Vector3(0.6f, 0.5f, 0.6f), new Vector3(0, -15, 0), rockMat);
        CreateRock("batu3", new Vector3(-0.25f, 0.4f, 1.2f), new Vector3(0.6f, 0.8f, 0.6f), new Vector3(0, 10, 0), rockMat);
        CreateRock("batu4", new Vector3(0.3f, 0.55f, 1.8f), new Vector3(0.6f, 1.1f, 0.6f), new Vector3(0, -10, 0), rockMat);

        // Character Hero
        GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Asset-Meet7/Char-BodyGuards/Meshes/SkelMesh_Bodyguard_01.fbx");
        if (heroPrefab == null)
        {
            Debug.LogError("Hero model SkelMesh_Bodyguard_01.fbx not found!");
            return;
        }

        GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
        hero.name = "Hero";
        hero.transform.position = Vector3.zero;
        hero.transform.rotation = Quaternion.identity;

        // Set layer to Player
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer != -1)
        {
            hero.layer = playerLayer;
            foreach (Transform child in hero.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = playerLayer;
            }
        }

        // Animator
        Animator anim = hero.GetComponent<Animator>();
        if (anim == null) anim = hero.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;

        // Rigidbody
        Rigidbody rb = hero.GetComponent<Rigidbody>();
        if (rb == null) rb = hero.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.useGravity = true;
        rb.isKinematic = true;

        // Capsule Collider (slightly raised as requested in step 5)
        CapsuleCollider cap = hero.GetComponent<CapsuleCollider>();
        if (cap == null) cap = hero.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0, 1.05f, 0);
        cap.height = 1.8f;
        cap.radius = 0.3f;

        // Sc_hero2 Foot IK script
        Sc_hero2 footIK = hero.GetComponent<Sc_hero2>();
        if (footIK == null) footIK = hero.AddComponent<Sc_hero2>();
        footIK.distanceToGround = 0.08f;
        if (playerLayer != -1)
        {
            footIK.layermask = ~(1 << playerLayer);
        }
        else
        {
            footIK.layermask = ~0;
        }

        // Camera setup
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 1.3f, -2.5f);
            mainCam.transform.rotation = Quaternion.Euler(12f, 0, 0);
        }

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }

    private static GameObject CreateRock(string name, Vector3 pos, Vector3 scale, Vector3 rot, Material mat)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rock.name = name;
        rock.tag = "walkable";
        rock.transform.position = pos;
        rock.transform.localScale = scale;
        rock.transform.rotation = Quaternion.Euler(rot);

        BoxCollider box = rock.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.isTrigger = true;
        }

        if (mat != null)
        {
            rock.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        return rock;
    }
}
#endif
