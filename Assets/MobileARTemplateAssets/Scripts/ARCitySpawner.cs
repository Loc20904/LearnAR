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

        // 1. Tính tâm mặt phẳng (Giữ nguyên code cũ của bạn)
        Vector2 sum = Vector2.zero;
        foreach (var point in plane.boundary) sum += point;
        Vector2 center2D = sum / plane.boundary.Length;
        Vector3 centerWorld = plane.transform.TransformPoint(new Vector3(center2D.x, 0f, center2D.y));

        // 2. Instantiate thành phố
        spawnedCity = Instantiate(cityPrefab, centerWorld, plane.transform.rotation);
        spawnedCity.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        // 3. ===== FIX CHÌM SÀN (Căn chỉnh lại cao độ Y) =====
        Renderer[] renderers = spawnedCity.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            // Tính toán tổng thể kích thước khối của thành phố
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);

            // Tìm khoảng cách từ cao độ mặt sàn (centerWorld.y) đến điểm thấp nhất của thành phố (bounds.min.y)
            float yOffset = centerWorld.y - bounds.min.y;

            // Đẩy toàn bộ thành phố lên một khoảng đúng bằng phần bị chìm
            spawnedCity.transform.position += new Vector3(0, yOffset, 0);

            // Thêm một khoảng nhỏ (0.01m) để tránh hiện tượng nhấp nháy (Z-fighting) với lưới mặt phẳng
            spawnedCity.transform.position += plane.transform.up * 0.01f;
        }

        ScenarioController controller = spawnedCity.GetComponent<ScenarioController>();
        if (controller != null)
        {
            // GÁN DỮ LIỆU Ở ĐÂY (Đây là bước bạn đang thiếu)
            controller.data = scenarioDataToAssign;

            // Gán UI và khởi tạo như bạn đã làm
            controller.uiManager = uiManagerInScene;
            if (uiManagerInScene != null) uiManagerInScene.controller = controller;

            if (controller.sequenceController != null)
            {
                controller.sequenceController.InitializeActors();
            }

            // Sau khi gán xong xuôi mới cho chạy
            controller.StartScenario();
        }
    }
}