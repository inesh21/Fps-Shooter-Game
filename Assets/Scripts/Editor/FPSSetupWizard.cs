using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FPSSetupWizard : EditorWindow
{
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private GameObject groundObject;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private GameObject tracerPrefab;
    [SerializeField] private WeaponData weaponData;

    [MenuItem("Tools/FPS/Setup Selected Character")]
    private static void Open() => GetWindow<FPSSetupWizard>("FPS Setup");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Select the character root and the ground Plane. This configures the collision root, centers the visual rig above the floor, and wires the first person camera and weapon.", MessageType.Info);
        playerRoot = (GameObject)EditorGUILayout.ObjectField("Character Root", playerRoot != null ? playerRoot : Selection.activeGameObject, typeof(GameObject), true);
        groundObject = (GameObject)EditorGUILayout.ObjectField("Ground Plane", groundObject, typeof(GameObject), true);
        weaponPrefab = (GameObject)EditorGUILayout.ObjectField("Weapon Prefab (optional)", weaponPrefab, typeof(GameObject), false);
        weaponData = (WeaponData)EditorGUILayout.ObjectField("Weapon Data (optional)", weaponData, typeof(WeaponData), false);
        tracerPrefab = (GameObject)EditorGUILayout.ObjectField("Tracer Prefab (optional)", tracerPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(playerRoot == null))
            if (GUILayout.Button("Configure FPS Character", GUILayout.Height(36))) Configure();
    }

    private void Configure()
    {
        if (playerRoot == null) return;
        if (groundObject == null) groundObject = GameObject.Find("Plane") ?? GameObject.Find("Ground");
        Collider floorCollider = groundObject != null ? groundObject.GetComponent<Collider>() : null;
        if (floorCollider == null)
        {
            EditorUtility.DisplayDialog("Ground collider required", "Assign the Plane or Ground object with a collider, then run setup again.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure FPS Character");
        Undo.RegisterFullObjectHierarchyUndo(playerRoot, "Configure FPS Character");

        Transform root = playerRoot.transform;
        CharacterController controller = GetOrAdd<CharacterController>(playerRoot);
        controller.height = 2f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 1f, 0f);
        controller.stepOffset = 0.3f;
        controller.skinWidth = 0.04f;
        controller.minMoveDistance = 0f;

        float floorY = floorCollider.bounds.max.y;
        Vector3 rootPosition = root.position;
        rootPosition.y = floorY;
        root.position = rootPosition;

        Transform visuals = FindOrCreateChild(root, "Visuals");
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root || child == visuals || child.IsChildOf(visuals)) continue;
            if (child.GetComponent<Camera>() != null || child.name == "PlayerCamera") continue;
            if (child.parent == root) Undo.SetTransformParent(child, visuals, "Move rig under Visuals");
        }

        // Place the imported rig's lowest rendered point on the plane. The
        // CharacterController stays on the clean feet-level PlayerRoot.
        Renderer[] renderers = visuals.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            float lowest = float.PositiveInfinity;
            foreach (Renderer renderer in renderers) lowest = Mathf.Min(lowest, renderer.bounds.min.y);
            Vector3 visualPosition = visuals.position;
            visualPosition.y += floorY - lowest;
            visuals.position = visualPosition;
        }

        PlayerFSM player = GetOrAdd<PlayerFSM>(playerRoot);
        SetReference(player, "controller", controller);

        Transform cameraTransform = FindCamera(root);
        Transform cameraPivot = FindOrCreateChild(root, "CameraPivot");
        cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);
        cameraPivot.localRotation = Quaternion.identity;
        GameObject cameraObject;
        if (cameraTransform == null)
        {
            cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create first person camera");
            cameraTransform = cameraObject.transform;
        }
        else cameraObject = cameraTransform.gameObject;
        Undo.SetTransformParent(cameraTransform, cameraPivot, "Parent camera to pivot");
        cameraTransform.localPosition = Vector3.zero;
        cameraTransform.localRotation = Quaternion.identity;
        cameraObject.tag = "MainCamera";

        CameraFollow cameraFollow = GetOrAdd<CameraFollow>(cameraObject);
        SetReference(cameraFollow, "player", player);

        Transform weaponHolder = FindOrCreateChild(cameraTransform, "WeaponHolder");
        weaponHolder.localPosition = new Vector3(0.28f, -0.25f, 0.45f);
        weaponHolder.localRotation = Quaternion.identity;
        WeaponSwayAndBob oldSway = weaponHolder.GetComponent<WeaponSwayAndBob>();
        if (oldSway != null) Undo.DestroyObjectImmediate(oldSway);
        foreach (WeaponHandler duplicate in weaponHolder.GetComponentsInChildren<WeaponHandler>(true))
            Undo.DestroyObjectImmediate(duplicate);
        Transform swayRoot = FindOrCreateChild(weaponHolder, "Sway");
        Transform recoilRoot = FindDescendant(weaponHolder, "Recoil");
        if (recoilRoot == null) recoilRoot = FindOrCreateChild(swayRoot, "Recoil");
        else if (recoilRoot.parent != swayRoot) Undo.SetTransformParent(recoilRoot, swayRoot, "Place recoil under sway holder");
        WeaponSwayAndBob sway = GetOrAdd<WeaponSwayAndBob>(swayRoot.gameObject);
        RecoilSpring recoil = GetOrAdd<RecoilSpring>(recoilRoot.gameObject);
        SetReference(sway, "player", player);
        SetReference(recoil, "cameraFollow", cameraFollow);

        if (weaponPrefab != null && recoilRoot.childCount == 0)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(weaponPrefab);
            Undo.RegisterCreatedObjectUndo(model, "Add weapon model");
            model.transform.SetParent(recoilRoot, false);
        }

        Transform firePoint = FindDescendant(recoilRoot, "FirePoint");
        if (firePoint == null)
        {
            GameObject fire = new GameObject("FirePoint");
            Undo.RegisterCreatedObjectUndo(fire, "Create FirePoint");
            fire.transform.SetParent(recoilRoot, false);
            fire.transform.localPosition = new Vector3(0f, 0.04f, 0.6f);
            firePoint = fire.transform;
        }

        WeaponHandler handler = GetOrAdd<WeaponHandler>(playerRoot);
        SetReference(handler, "player", player);
        SetReference(handler, "firePoint", firePoint);
        SetReference(handler, "recoilSpring", recoil);
        if (weaponData != null) SetReference(handler, "currentWeapon", weaponData);

        ConfigurePlayerInput(playerRoot);
        ConfigurePooler();

        EditorUtility.SetDirty(playerRoot);
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = playerRoot;
        EditorUtility.DisplayDialog("FPS Character Configured", "PlayerRoot now sits on the ground surface. The visual rig is offset separately, the controller is assigned, and the camera and weapon references are wired. Save the scene.", "OK");
    }

    private void ConfigurePlayerInput(GameObject root)
    {
        UnityEngine.InputSystem.PlayerInput input = root.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (input == null) input = Undo.AddComponent<UnityEngine.InputSystem.PlayerInput>(root);
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/PlayerInput.inputactions");
        if (asset == null) return;
        SerializedObject serialized = new SerializedObject(input);
        serialized.FindProperty("m_Actions").objectReferenceValue = asset;
        serialized.FindProperty("m_DefaultActionMap").stringValue = "Player";
        serialized.FindProperty("m_NotificationBehavior").intValue = (int)UnityEngine.InputSystem.PlayerNotifications.SendMessages;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private void ConfigurePooler()
    {
        if (tracerPrefab == null) return;
        ObjectPooler pooler = FindAnyObjectByType<ObjectPooler>();
        if (pooler == null)
        {
            GameObject poolerObject = new GameObject("ObjectPooler");
            Undo.RegisterCreatedObjectUndo(poolerObject, "Create ObjectPooler");
            pooler = Undo.AddComponent<ObjectPooler>(poolerObject);
        }

        SerializedObject so = new SerializedObject(pooler);
        SerializedProperty list = so.FindProperty("pools");
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            if (element.FindPropertyRelative("tag").stringValue != "Tracer") continue;
            element.FindPropertyRelative("prefab").objectReferenceValue = tracerPrefab;
            element.FindPropertyRelative("size").intValue = 24;
            so.ApplyModifiedProperties();
            return;
        }
        int index = list.arraySize++;
        SerializedProperty pool = list.GetArrayElementAtIndex(index);
        pool.FindPropertyRelative("tag").stringValue = "Tracer";
        pool.FindPropertyRelative("prefab").objectReferenceValue = tracerPrefab;
        pool.FindPropertyRelative("size").intValue = 24;
        so.ApplyModifiedProperties();
    }

    private static Transform FindCamera(Transform root)
    {
        foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            if (camera.transform != root && camera.GetComponent<CameraFollow>() != null) return camera.transform;
        foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            if (camera.transform != root) return camera.transform;
        return null;
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child;
        GameObject created = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void SetReference(Object target, string property, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty field = so.FindProperty(property);
        if (field == null) return;
        field.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
