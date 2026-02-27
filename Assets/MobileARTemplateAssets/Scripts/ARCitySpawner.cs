using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARCitySpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject cityPrefab;

    [SerializeField] private InputActionReference tapAction;

    public float minDistanceFromCamera = 1.5f;

    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;

    private GameObject spawnedCity;

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
        if (plane == null || plane.boundary.Length == 0)
            return;

        // Tính tâm thực của boundary
        Vector2 sum = Vector2.zero;

        foreach (var point in plane.boundary)
            sum += point;

        Vector2 center2D = sum / plane.boundary.Length;

        // Convert local 2D -> world
        Vector3 centerWorld = plane.transform.TransformPoint(
            new Vector3(center2D.x, 0f, center2D.y)
        );

        // Offset nhẹ theo normal
        centerWorld += plane.transform.up * 0.1f;

        spawnedCity = Instantiate(
            cityPrefab,
            centerWorld,
            plane.transform.rotation
        );

        spawnedCity.transform.localScale =
            new Vector3(0.25f, 0.25f, 0.25f);

        // ===== FIX pivot lệch =====
        Renderer[] renderers =
            spawnedCity.GetComponentsInChildren<Renderer>();

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        // Tính offset từ pivot tới center thực
        Vector3 offset = bounds.center - spawnedCity.transform.position;

        // Dời city về đúng center
        spawnedCity.transform.position -= offset;
    }
}