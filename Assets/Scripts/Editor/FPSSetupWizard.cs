using UnityEngine;
using UnityEditor;

public class FPSSetupWizard : EditorWindow
{
    private GameObject playerPrefab;
    private GameObject weaponModel;
    private GameObject tracerPrefab;
    private WeaponData weaponData;

    [MenuItem("Tools/FPS Setup Wizard")]
    public static void ShowWindow()
    {
        GetWindow<FPSSetupWizard>("FPS Setup Wizard");
    }

    private void OnGUI()
    {
        GUILayout.Label("FPS Project Setup Wizard", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("This wizard will automatically set up your player, camera, and weapon systems.", MessageType.Info);
        GUILayout.Space(10);

        GUILayout.Label("Optional References:", EditorStyles.boldLabel);
        weaponModel = (GameObject)EditorGUILayout.ObjectField("Weapon Model (Optional)", weaponModel, typeof(GameObject), false);
        tracerPrefab = (GameObject)EditorGUILayout.ObjectField("Tracer Prefab (Optional)", tracerPrefab, typeof(GameObject), false);
        weaponData = (WeaponData)EditorGUILayout.ObjectField("Weapon Data (Optional)", weaponData, typeof(WeaponData), false);

        GUILayout.Space(20);

        if (GUILayout.Button("Setup Complete FPS System", GUILayout.Height(40)))
        {
            SetupFPSSystem();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Create Default Weapon Data", GUILayout.Height(30)))
        {
            CreateDefaultWeaponData();
        }

        if (GUILayout.Button("Create Simple Tracer Prefab", GUILayout.Height(30)))
        {
            CreateSimpleTracerPrefab();
        }
    }

    private void SetupFPSSystem()
    {
        // 1. Create or find Player
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = new GameObject("Player");
            Debug.Log("Created Player GameObject");
        }

        // Add CharacterController
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0, 1, 0);
            controller.radius = 0.5f;
            controller.height = 2f;
            Debug.Log("Added CharacterController to Player");
        }

        // Add PlayerFSM
        PlayerFSM playerFSM = player.GetComponent<PlayerFSM>();
        if (playerFSM == null)
        {
            playerFSM = player.AddComponent<PlayerFSM>();
            Debug.Log("Added PlayerFSM to Player");
        }

        // Add visual capsule if no children
        if (player.transform.childCount == 0 || player.transform.Find("PlayerModel") == null)
        {
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "PlayerModel";
            model.transform.SetParent(player.transform);
            model.transform.localPosition = new Vector3(0, 1, 0);
            model.transform.localRotation = Quaternion.identity;
            DestroyImmediate(model.GetComponent<Collider>()); // Remove duplicate collider
            Debug.Log("Created Player visual model");
        }

        // 2. Setup Camera Hierarchy
        GameObject cameraHolder = player.transform.Find("CameraHolder")?.gameObject;
        if (cameraHolder == null)
        {
            cameraHolder = new GameObject("CameraHolder");
            cameraHolder.transform.SetParent(player.transform);
            cameraHolder.transform.localPosition = Vector3.zero;
            Debug.Log("Created CameraHolder");
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            Debug.Log("Created Main Camera");
        }

        mainCamera.transform.SetParent(cameraHolder.transform);
        mainCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
        mainCamera.transform.localRotation = Quaternion.identity;

        // Add CameraFollow
        CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
        if (cameraFollow == null)
        {
            cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
            Debug.Log("Added CameraFollow to Main Camera");
        }

        // Use reflection to set serialized private fields
        SerializedObject so = new SerializedObject(cameraFollow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.FindProperty("smoothSpeed").floatValue = 5f;
        so.FindProperty("offset").vector3Value = new Vector3(0, 6, -8);
        so.FindProperty("rotationSpeed").floatValue = 3f;
        so.FindProperty("minDistance").floatValue = 0.5f;
        so.ApplyModifiedProperties();
        Debug.Log("Configured CameraFollow component");

        // 3. Setup Weapon Hierarchy
        GameObject weaponHolder = mainCamera.transform.Find("WeaponHolder")?.gameObject;
        if (weaponHolder == null)
        {
            weaponHolder = new GameObject("WeaponHolder");
            weaponHolder.transform.SetParent(mainCamera.transform);
            weaponHolder.transform.localPosition = new Vector3(0.3f, -0.3f, 0.5f);
            weaponHolder.transform.localRotation = Quaternion.identity;
            Debug.Log("Created WeaponHolder");
        }

        // Add RecoilSpring
        RecoilSpring recoilSpring = weaponHolder.GetComponent<RecoilSpring>();
        if (recoilSpring == null)
        {
            recoilSpring = weaponHolder.AddComponent<RecoilSpring>();
            Debug.Log("Added RecoilSpring");
        }

        SerializedObject recoilSO = new SerializedObject(recoilSpring);
        recoilSO.FindProperty("stiffness").floatValue = 150f;
        recoilSO.FindProperty("damping").floatValue = 12f;
        recoilSO.ApplyModifiedProperties();

        // Add WeaponSwayAndBob
        WeaponSwayAndBob swayBob = weaponHolder.GetComponent<WeaponSwayAndBob>();
        if (swayBob == null)
        {
            swayBob = weaponHolder.AddComponent<WeaponSwayAndBob>();
            Debug.Log("Added WeaponSwayAndBob");
        }

        SerializedObject swaySO = new SerializedObject(swayBob);
        swaySO.FindProperty("player").objectReferenceValue = playerFSM;
        swaySO.ApplyModifiedProperties();

        // Add WeaponHandler
        WeaponHandler weaponHandler = weaponHolder.GetComponent<WeaponHandler>();
        if (weaponHandler == null)
        {
            weaponHandler = weaponHolder.AddComponent<WeaponHandler>();
            Debug.Log("Added WeaponHandler");
        }

        // Create FirePoint
        GameObject firePoint = weaponHolder.transform.Find("FirePoint")?.gameObject;
        if (firePoint == null)
        {
            firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(weaponHolder.transform);
            firePoint.transform.localPosition = new Vector3(0, 0, 0.5f);
            firePoint.transform.localRotation = Quaternion.identity;
            Debug.Log("Created FirePoint");
        }

        // Add weapon model if provided
        if (weaponModel != null && firePoint.transform.childCount == 0)
        {
            GameObject weaponInstance = Instantiate(weaponModel, firePoint.transform);
            weaponInstance.name = "WeaponModel";
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;
            Debug.Log("Added weapon model");
        }

        // Configure WeaponHandler
        SerializedObject handlerSO = new SerializedObject(weaponHandler);
        handlerSO.FindProperty("player").objectReferenceValue = playerFSM;
        handlerSO.FindProperty("recoilSpring").objectReferenceValue = recoilSpring;
        handlerSO.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
        if (weaponData != null)
        {
            handlerSO.FindProperty("currentWeapon").objectReferenceValue = weaponData;
        }
        handlerSO.ApplyModifiedProperties();
        Debug.Log("Configured WeaponHandler");

        // 4. Setup ObjectPooler
        ObjectPooler pooler = FindObjectOfType<ObjectPooler>();
        if (pooler == null)
        {
            GameObject poolerObj = new GameObject("ObjectPooler");
            pooler = poolerObj.AddComponent<ObjectPooler>();
            Debug.Log("Created ObjectPooler");
        }

        if (tracerPrefab != null)
        {
            SerializedObject poolerSO = new SerializedObject(pooler);
            SerializedProperty poolsList = poolerSO.FindProperty("pools");

            // Check if Tracer pool already exists
            bool hasTracerPool = false;
            for (int i = 0; i < poolsList.arraySize; i++)
            {
                if (poolsList.GetArrayElementAtIndex(i).FindPropertyRelative("tag").stringValue == "Tracer")
                {
                    hasTracerPool = true;
                    break;
                }
            }

            if (!hasTracerPool)
            {
                poolsList.InsertArrayElementAtIndex(poolsList.arraySize);
                SerializedProperty newPool = poolsList.GetArrayElementAtIndex(poolsList.arraySize - 1);
                newPool.FindPropertyRelative("tag").stringValue = "Tracer";
                newPool.FindPropertyRelative("prefab").objectReferenceValue = tracerPrefab;
                newPool.FindPropertyRelative("size").intValue = 20;
                poolerSO.ApplyModifiedProperties();
                Debug.Log("Added Tracer pool to ObjectPooler");
            }
        }

        // 5. Setup Ground if missing
        if (GameObject.Find("Ground") == null)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);
            Debug.Log("Created Ground plane");
        }

        EditorUtility.DisplayDialog("Setup Complete!",
            "FPS system has been set up successfully!\n\n" +
            "Next steps:\n" +
            "1. Create a WeaponData asset (Tools → Create Default Weapon Data)\n" +
            "2. Assign it to WeaponHandler\n" +
            "3. Press Play and test with WASD, Shift, Mouse, Space, Left Click, R",
            "OK");

        Debug.Log("=== FPS Setup Complete ===");
    }

    private void CreateDefaultWeaponData()
    {
        WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
        data.weaponName = "Assault Rifle";
        data.isAutomatic = true;
        data.fireRate = 600f;
        data.damage = 25f;
        data.range = 100f;
        data.magazineSize = 30;
        data.reloadTime = 1.8f;
        data.recoilRotation = new Vector3(4f, 1.5f, 1f);
        data.recoilKickback = new Vector3(0.02f, 0.02f, 0.1f);
        data.baseSpread = 0.5f;
        data.maxSpread = 3f;

        string path = "Assets/WeaponData_AssaultRifle.asset";
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = data;

        Debug.Log($"Created default WeaponData at {path}");
        EditorUtility.DisplayDialog("Weapon Data Created",
            $"Default Assault Rifle data created at:\n{path}\n\nNow assign it to WeaponHandler's 'Current Weapon' field!",
            "OK");
    }

    private void CreateSimpleTracerPrefab()
    {
        // Create tracer object
        GameObject tracer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tracer.name = "BulletTracer";

        // Scale to thin line
        tracer.transform.localScale = new Vector3(0.01f, 0.5f, 0.01f);

        // Remove collider
        DestroyImmediate(tracer.GetComponent<Collider>());

        // Create bright material
        Material tracerMat = new Material(Shader.Find("Standard"));
        tracerMat.color = new Color(1f, 0.8f, 0f); // Bright yellow
        tracerMat.SetFloat("_Metallic", 0f);
        tracerMat.EnableKeyword("_EMISSION");
        tracerMat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0f) * 2f);
        tracer.GetComponent<Renderer>().material = tracerMat;

        // Add BulletTracer script
        BulletTracer tracerScript = tracer.AddComponent<BulletTracer>();
        SerializedObject tracerSO = new SerializedObject(tracerScript);
        tracerSO.FindProperty("speed").floatValue = 100f;
        tracerSO.ApplyModifiedProperties();

        // Save as prefab
        string path = "Assets/BulletTracerPrefab.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tracer, path);
        DestroyImmediate(tracer);

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = prefab;

        Debug.Log($"Created tracer prefab at {path}");
        EditorUtility.DisplayDialog("Tracer Prefab Created",
            $"Bullet tracer prefab created at:\n{path}\n\nDrag this to the wizard's 'Tracer Prefab' field and run setup again!",
            "OK");

        tracerPrefab = prefab;
    }
}
