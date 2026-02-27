using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARCitySpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject cityPrefab;

    [Header("Input")]
    [SerializeField] private InputActionReference tapAction;

    private ARRaycastManager raycastManager;
    private GameObject spawnedCity;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
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

        SpawnCity(hits[0].pose);
    }

    void SpawnCity(Pose pose)
    {
        Camera cam = Camera.main;

        float minDistance = 1.5f;

        Vector3 spawnPos = pose.position;

        float distance = Vector3.Distance(cam.transform.position, spawnPos);

        if (distance < minDistance)
        {
            Vector3 dir = (spawnPos - cam.transform.position).normalized;
            spawnPos = cam.transform.position + dir * minDistance;
        }

        spawnedCity = Instantiate(cityPrefab, spawnPos, Quaternion.identity);
        spawnedCity.transform.localScale = Vector3.one * 0.1f;
        spawnedCity.transform.position += Vector3.up * 0.05f;
    }

    void AutoScaleToCamera(GameObject obj, Camera cam)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        float size = bounds.size.magnitude;

        float distance =
            Vector3.Distance(cam.transform.position, obj.transform.position);

        // City chiếm khoảng 70% view
        float targetSize = distance * 0.7f;

        float scaleFactor = targetSize / size;

        obj.transform.localScale *= scaleFactor;
    }
}