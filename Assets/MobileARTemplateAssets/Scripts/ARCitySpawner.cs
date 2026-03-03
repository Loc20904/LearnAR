using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARCitySpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject cityPrefab;
    public ScenarioUIManager uiManagerInScene;

    [SerializeField] private InputActionReference tapAction;

    public float minDistanceFromCamera = 1.5f;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;

    private GameObject spawnedCity;
    public ScenarioData scenarioDataToAssign;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();

        // ScenarioLoader handles the full loading chain:
        //   1. Inspector-assigned SO (Editor)  2. JSON TextAsset  3. Hardcoded fallback.
        // Result is always non-null, so SetupAfterSpawn never injects null data.
        scenarioDataToAssign = ScenarioLoader.Load(scenarioDataToAssign);
    }

    void OnEnable() => tapAction.action.Enable();
    void OnDisable() => tapAction.action.Disable();

    void Update()
    {
        if (!tapAction.action.triggered || spawnedCity != null)
            return;

        Vector2 screenCenter =
            new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (!raycastManager.Raycast(screenCenter, hits,
            TrackableType.PlaneWithinBounds))
            return;

        SpawnAtPlaneCenter(hits[0]);
    }

    void SpawnAtPlaneCenter(ARRaycastHit hit)
    {
        ARPlane plane = planeManager.GetPlane(hit.trackableId);
        if (plane == null || plane.boundary.Length == 0) return;

        // 1. Tính tâm mặt phẳng
        Vector2 sum = Vector2.zero;
        foreach (var point in plane.boundary) sum += point;
        Vector2 center2D = sum / plane.boundary.Length;
        Vector3 centerWorld = plane.transform.TransformPoint(new Vector3(center2D.x, 0f, center2D.y));

        // 2. Instantiate thành phố
        spawnedCity = Instantiate(cityPrefab, centerWorld, plane.transform.rotation);
        spawnedCity.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // 3. FIX CHÌM SÀN
        Renderer[] renderers = spawnedCity.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
            float yOffset = centerWorld.y - bounds.min.y;
            spawnedCity.transform.position += new Vector3(0, yOffset, 0);
            spawnedCity.transform.position += plane.transform.up * 0.01f;
        }

        Debug.Log("[ARCitySpawner] City đã spawn xong. Bắt đầu setup...");

        // 4. Chờ 1 frame rồi mới setup (đảm bảo mọi Awake/Start đã chạy xong)
        StartCoroutine(SetupAfterSpawn());
    }

    private IEnumerator SetupAfterSpawn()
    {
        // Chờ 2 frame để tất cả Awake() và Start() trên prefab chạy xong
        yield return null;
        yield return null;

        ScenarioController controller = spawnedCity.GetComponent<ScenarioController>();
        if (controller == null)
        {
            Debug.LogError("[ARCitySpawner] Không tìm thấy ScenarioController trên prefab city!");
            yield break;
        }

        // GÁN DỮ LIỆU
        controller.data = scenarioDataToAssign;
        Debug.Log($"[ARCitySpawner] ScenarioData gán: {(scenarioDataToAssign != null ? scenarioDataToAssign.scenarioTitle : "NULL!")}");

        if (scenarioDataToAssign != null && scenarioDataToAssign.steps != null)
            Debug.Log($"[ARCitySpawner] Số steps: {scenarioDataToAssign.steps.Length}");
        else
            Debug.LogError("[ARCitySpawner] scenarioDataToAssign hoặc steps bị NULL!");

        // Gán UI
        controller.uiManager = uiManagerInScene;
        Debug.Log($"[ARCitySpawner] uiManager: {(uiManagerInScene != null ? "OK" : "NULL!")}");

        if (uiManagerInScene != null) uiManagerInScene.controller = controller;

        // Initialize Actors
        if (controller.sequenceController != null)
        {
            Debug.Log("[ARCitySpawner] sequenceController: OK. Đang InitializeActors...");
            controller.sequenceController.InitializeActors();
        }
        else
        {
            Debug.LogError("[ARCitySpawner] sequenceController trên prefab bị NULL!");
        }

        // ====== THAY ĐỔI CHÍNH: HIỆN NÚT START THAY VÌ TỰ ĐỘNG CHẠY ======
        Debug.Log("[ARCitySpawner] Setup hoàn tất. Hiện nút Start...");

        if (uiManagerInScene != null)
        {
            uiManagerInScene.ShowStartButton();
        }
        else
        {
            // Fallback: nếu không có UI, tự động start
            Debug.LogWarning("[ARCitySpawner] uiManagerInScene NULL, tự động StartScenario...");
            controller.StartScenario();
        }
    }
}