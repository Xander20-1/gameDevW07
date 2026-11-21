#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;

public static class SetupLectureScene
{
    [MenuItem("Tools/Fix Animation Looping")]
    public static void FixAllAnimationsLooping()
    {
        Debug.Log("[AnimFix] Fixing looping on animation clips...");

        string[] animPaths = new[]
        {
            "Assets/Asset-Meet7/Animation-humanoid/DefaultAnims/Walk.fbx",
            "Assets/Asset-Meet7/Animation-humanoid/DefaultAnims/Idle.fbx",
            "Assets/Asset-Meet7/Animation-humanoid/RevolverAnims/RevolverAimIdle.fbx",
            "Assets/Asset-Meet7/Animation-humanoid/humanoid_walkForward_faceForward.fbx",
            "Assets/Asset-Meet7/Animation-humanoid/humanoid_idle.fbx"
        };

        foreach (var path in animPaths)
        {
            if (!File.Exists(path)) continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        clips[i].loopTime = true;
                        clips[i].wrapMode = WrapMode.Loop;
                    }
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    Debug.Log($"[AnimFix] Successfully set loopTime = true for: {path}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Also update the animator controller to use the looping clips
        string controllerPath = "Assets/Animations/HeroLectureAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null)
        {
            AnimationClip walkClip = FindAnimationClip(
                "Assets/Asset-Meet7/Animation-humanoid/humanoid_walkForward_faceForward.fbx",
                "Assets/Asset-Meet7/Animation-humanoid/DefaultAnims/Walk.fbx"
            );
            AnimationClip idleClip = FindAnimationClip(
                "Assets/Asset-Meet7/Animation-humanoid/humanoid_idle.fbx",
                "Assets/Asset-Meet7/Animation-humanoid/DefaultAnims/Idle.fbx"
            );

            Debug.Log($"[AnimFix] walkClip: {walkClip?.name}, isLooping: {walkClip?.isLooping}");
            Debug.Log($"[AnimFix] idleClip: {idleClip?.name}, isLooping: {idleClip?.isLooping}");

            var baseSm = controller.layers[0].stateMachine;
            foreach (var s in baseSm.states)
            {
                if (s.state.name == "Walk" && walkClip != null)
                {
                    s.state.motion = walkClip;
                }
                else if (s.state.name == "Idle" && idleClip != null)
                {
                    s.state.motion = idleClip;
                }
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        Debug.Log("[AnimFix] Completed fixing animation looping!");
    }

    [MenuItem("Tools/Setup Lecture Scene (Meet 7)")]
    public static void Execute()
    {
        Debug.Log("[LectureScene] Starting Setup for Meet 7 Lecture Scene...");

        // Ensure not in Play Mode
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        // 0. Fix animation looping first
        FixAllAnimationsLooping();

        // 1. Ensure Tag 'item' and 'walkable'
        EnsureTagsAndLayers();

        // 2. Upgrade Materials to URP (Fix Pink/Magenta Shaders)
        UpgradeMaterialsToURP();

        // 3. Create or verify hero_aim_controller.mask
        AvatarMask aimMask = CreateOrGetAimAvatarMask();

        // 4. Create or verify laser_material.mat
        Material laserMat = CreateOrGetLaserMaterial();

        // 5. Create or verify HeroLectureAnimator.controller
        AnimatorController animController = CreateOrGetLectureAnimatorController(aimMask);

        // 6. Build the dedicated LectureScene
        BuildLectureScene(animController, laserMat);

        Debug.Log("[LectureScene] Lecture Scene Setup Successfully Completed!");
    }

    [MenuItem("Tools/Upgrade Asset-Meet7 Materials to URP")]
    public static void UpgradeMaterialsToURP()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogWarning("Universal Render Pipeline/Lit shader not found!");
            return;
        }

        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/Asset-Meet7" });
        foreach (var guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (mat.name == "laser_material") continue;

            // Jika memakai shader Standard lama (pink di URP)
            if (mat.shader.name == "Standard" || mat.shader.name.StartsWith("Legacy") || mat.shader.name == "Hidden/InternalErrorShader")
            {
                Color baseColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
                float smoothness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;

                mat.shader = urpLit;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                if (mat.HasProperty("_BaseMap") && mainTex != null) mat.SetTexture("_BaseMap", mainTex);
                if (mat.HasProperty("_BumpMap") && bumpMap != null) mat.SetTexture("_BumpMap", bumpMap);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

                // Neon lights
                if (mat.name.Contains("BLUE_LIGHT"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_BaseColor", new Color(0.2f, 0.7f, 1.0f));
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", new Color(0.2f, 0.7f, 1.0f) * 3f);
                }
                else if (mat.name.Contains("WHITE_LIGHT"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_BaseColor", Color.white);
                    if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.white * 3f);
                }

                EditorUtility.SetDirty(mat);
            }
        }
        AssetDatabase.SaveAssets();
    }

    private static void EnsureTagsAndLayers()
    {
        var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAsset != null && tagManagerAsset.Length > 0)
        {
            SerializedObject tagManager = new SerializedObject(tagManagerAsset[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            string[] neededTags = { "item", "walkable" };
            foreach (var tag in neededTags)
            {
                bool hasTag = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        hasTag = true;
                        break;
                    }
                }
                if (!hasTag)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                }
            }
            tagManager.ApplyModifiedProperties();
        }
    }

    private static AvatarMask CreateOrGetAimAvatarMask()
    {
        string dir = "Assets/Animations";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        string maskPath = "Assets/Animations/hero_aim_controller.mask";
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            mask.name = "hero_aim_controller";
            AssetDatabase.CreateAsset(mask, maskPath);
        }

        // Sesuai Slide 7: Body & Legs disabled, Head & Arms enabled
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);

        EditorUtility.SetDirty(mask);
        AssetDatabase.SaveAssets();
        return mask;
    }

    private static Material CreateOrGetLaserMaterial()
    {
        string matPath = "Assets/Asset-Meet7/material/laser_material.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        if (mat == null)
        {
            mat = new Material(shader);
            mat.name = "laser_material";
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = shader;
        }

        Color laserColor = new Color(1.0f, 0.1f, 0.35f, 1.0f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", laserColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", laserColor);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static AnimatorController CreateOrGetLectureAnimatorController(AvatarMask mask)
    {
        string controllerPath = "Assets/Animations/HeroLectureAnimator.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        // Parameters
        EnsureParameter(controller, "stat_jalan", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "stat_aim", AnimatorControllerParameterType.Bool);

        // Animation clips (Prioritaskan humanoid_walk dan humanoid_idle yang looping sempurna)
        AnimationClip walkClip = FindAnimationClip(
            "Assets/Asset-Meet7/Animation-humanoid/humanoid_walkForward_faceForward.fbx",
            "Assets/Asset-Meet7/Animation-humanoid/DefaultAnims/Walk.fbx"
        );
        AnimationClip idleClip = FindAnimationClip(
            "Assets/Asset-Meet7/Animation-humanoid/humanoid_idle.fbx",
            "Assets/Asset-Meet7/Animation-humanoid/DefaultAnims/Idle.fbx"
        );
        AnimationClip revolverAimClip = FindAnimationClip(
            "Assets/Asset-Meet7/Animation-humanoid/RevolverAnims/RevolverAimIdle.fbx"
        );

        // Base Layer Setup
        var baseSm = controller.layers[0].stateMachine;
        ClearStateMachine(baseSm);

        var idleState = baseSm.AddState("Idle");
        idleState.motion = idleClip;
        baseSm.defaultState = idleState;

        var walkState = baseSm.AddState("Walk");
        walkState.motion = walkClip;

        // Transitions Idle <-> Walk (Slide 4)
        var toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.2f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "stat_jalan");

        var toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.2f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "stat_jalan");

        // LayerAim Setup (Slide 7, 8, 9)
        int aimLayerIndex = -1;
        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (controller.layers[i].name == "LayerAim")
            {
                aimLayerIndex = i;
                break;
            }
        }

        if (aimLayerIndex == -1)
        {
            controller.AddLayer("LayerAim");
            aimLayerIndex = controller.layers.Length - 1;
        }

        var layers = controller.layers;
        layers[aimLayerIndex].avatarMask = mask;
        layers[aimLayerIndex].defaultWeight = 1.0f;
        layers[aimLayerIndex].blendingMode = AnimatorLayerBlendingMode.Override;
        layers[aimLayerIndex].iKPass = true;
        controller.layers = layers;

        var aimSm = controller.layers[aimLayerIndex].stateMachine;
        ClearStateMachine(aimSm);

        var tidakAdaAnimState = aimSm.AddState("tidak_ada_anim");
        tidakAdaAnimState.motion = null;
        aimSm.defaultState = tidakAdaAnimState;

        var revolverState = aimSm.AddState("RevolverAimIdle");
        revolverState.motion = revolverAimClip;

        var toAim = tidakAdaAnimState.AddTransition(revolverState);
        toAim.hasExitTime = false;
        toAim.duration = 0.2f;
        toAim.AddCondition(AnimatorConditionMode.If, 0, "stat_aim");

        var fromAim = revolverState.AddTransition(tidakAdaAnimState);
        fromAim.hasExitTime = false;
        fromAim.duration = 0.2f;
        fromAim.AddCondition(AnimatorConditionMode.IfNot, 0, "stat_aim");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }

    private static void ClearStateMachine(AnimatorStateMachine sm)
    {
        while (sm.states.Length > 0)
        {
            sm.RemoveState(sm.states[0].state);
        }
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

    private static void BuildLectureScene(AnimatorController controller, Material laserMat)
    {
        string scenePath = "Assets/Scenes/LectureScene.unity";

        // Create new empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. SciFi Environment (Free_RoomA)
        string roomPrefabPath = "Assets/Asset-Meet7/Envi-Free LowPoly SciFi Pack/Prefabs/Rooms/Free_RoomA.prefab";
        GameObject roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(roomPrefabPath);
        if (roomPrefab != null)
        {
            GameObject room = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
            room.name = "Free_RoomA";
            room.transform.position = Vector3.zero;
            room.transform.rotation = Quaternion.identity;

            // Ensure colliders on all mesh children
            foreach (var mf in room.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null)
                {
                    if (mf.name.StartsWith("Floor") || mf.name.StartsWith("Wall") || mf.name.StartsWith("Gate") || mf.name.StartsWith("Column"))
                    {
                        mf.gameObject.AddComponent<MeshCollider>();
                    }
                }
            }
        }

        // Floor collider covering the entire corridor area
        GameObject floorObj = new GameObject("Level_Floor_Collider");
        floorObj.transform.position = new Vector3(-10f, -0.05f, -10f);
        var floorCollider = floorObj.AddComponent<BoxCollider>();
        floorCollider.size = new Vector3(30f, 0.1f, 30f);

        // 2. Directional Light
        GameObject lightObj = new GameObject("Directional Light");
        var dirLight = lightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.intensity = 1.3f;
        dirLight.color = new Color(0.96f, 0.98f, 1.0f);
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 3. Hero Character (SkelMesh_Bodyguard_03)
        Vector3 heroSpawnPos = new Vector3(-10f, 0f, -4.5f);
        Quaternion heroSpawnRot = Quaternion.Euler(0f, 0f, 0f);

        string heroPath = "Assets/Asset-Meet7/Char-BodyGuards/Meshes/SkelMesh_Bodyguard_03.fbx";
        if (!File.Exists(heroPath))
        {
            heroPath = "Assets/Asset-Meet7/Char-BodyGuards/Meshes/SkelMesh_Bodyguard_01.fbx";
        }
        GameObject heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(heroPath);
        GameObject hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
        hero.name = "hero";
        hero.transform.position = heroSpawnPos;
        hero.transform.rotation = heroSpawnRot;

        // Animator
        var anim = hero.GetComponent<Animator>();
        if (anim == null) anim = hero.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;

        // Rigidbody & Capsule Collider (Slide 3)
        var rb = hero.GetComponent<Rigidbody>();
        if (rb == null) rb = hero.AddComponent<Rigidbody>();
        rb.mass = 1f;
        rb.isKinematic = true;

        var cap = hero.GetComponent<CapsuleCollider>();
        if (cap == null) cap = hero.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0, 0.9f, 0);
        cap.height = 1.8f;
        cap.radius = 0.3f;

        // char_point_cam di kepala hero (Slide 6)
        GameObject camPoint = new GameObject("char_point_cam");
        camPoint.transform.SetParent(hero.transform, false);
        camPoint.transform.localPosition = new Vector3(0, 1.6f, 0);

        // Sc_hero
        var heroScript = hero.AddComponent<Sc_hero>();
        heroScript.kecepatan = 3f;

        // Challenge 1: Pasang Handgun ke tangan karakter (RightHand)
        Transform rightHand = FindDeepChild(hero.transform, "RightHand");
        if (rightHand != null)
        {
            string gunPath = "Assets/Asset-Meet7/Envi-ModernGuns_Handgun/Prefabs/Handgun_M1911A_Steel.prefab";
            GameObject gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gunPath);
            if (gunPrefab != null)
            {
                GameObject gun = (GameObject)PrefabUtility.InstantiatePrefab(gunPrefab);
                gun.name = "Handgun_M1911A_Steel";
                gun.transform.SetParent(rightHand, false);

                gun.transform.localPosition = new Vector3(0.045f, 0.085f, 0.02f);
                gun.transform.localRotation = Quaternion.Euler(-15f, 85f, -90f);

                // BarrelLocation & Laser (Slide 11 & Challenge 2)
                Transform barrel = FindDeepChild(gun.transform, "BarrelLocation");
                if (barrel == null)
                {
                    GameObject barrelObj = new GameObject("BarrelLocation");
                    barrelObj.transform.SetParent(gun.transform, false);
                    barrelObj.transform.localPosition = new Vector3(0, 0.04f, 0.15f);
                    barrelObj.transform.localRotation = Quaternion.identity;
                    barrel = barrelObj.transform;
                }

                GameObject laserObj = new GameObject("Laser");
                laserObj.transform.SetParent(barrel, false);
                laserObj.transform.localPosition = Vector3.zero;
                laserObj.transform.localRotation = Quaternion.identity;

                var lr = laserObj.AddComponent<LineRenderer>();
                if (laserMat != null) lr.material = laserMat;
                lr.startWidth = 0.015f;
                lr.endWidth = 0.015f;
                lr.positionCount = 2;
                lr.useWorldSpace = true;
                lr.SetPosition(0, Vector3.zero);
                lr.SetPosition(1, Vector3.forward * 5f);

                // Sc_laser dengan Challenge 2 (onlyOnAim = true)
                var laserScript = laserObj.AddComponent<Sc_laser>();
                laserScript.onlyOnAim = true;
            }
        }

        // 4. Main Camera & Sc_camera (Slide 6)
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        var cam = camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = heroSpawnPos + new Vector3(0, 1.6f, -3.2f);
        camObj.transform.LookAt(heroSpawnPos + new Vector3(0, 1.6f, 0));
        camObj.AddComponent<Sc_camera>();

        // 5. Item Magazine Target (Slide 14 & 15)
        string magPath = "Assets/Asset-Meet7/Envi-ModernGuns_Handgun/Prefabs/M1911A_Magazine_Steel.prefab";
        GameObject magPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(magPath);
        if (magPrefab != null)
        {
            GameObject itemMag = (GameObject)PrefabUtility.InstantiatePrefab(magPrefab);
            itemMag.name = "item_magazine";
            itemMag.tag = "item";
            itemMag.transform.position = heroSpawnPos + new Vector3(1.2f, 0.35f, 2.5f);
            itemMag.transform.rotation = Quaternion.Euler(-15f, 30f, 10f);

            var sphere = itemMag.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 2.5f;

            var pointLight = itemMag.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(0.2f, 0.6f, 1.0f);
            pointLight.intensity = 2.5f;
            pointLight.range = 4.0f;

            itemMag.AddComponent<ItemFloatingAnim>();
        }

        // Save new scene
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log("[LectureScene] Saved scene at: " + scenePath);

        // Add to Build Settings if not already
        AddSceneToBuildSettings(scenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, newScenes, scenes.Length);
        newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
#endif
