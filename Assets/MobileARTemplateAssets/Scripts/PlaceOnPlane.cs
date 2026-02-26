using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceOnPlane : MonoBehaviour
{
    public GameObject objectToSpawn;
    private ARRaycastManager _raycastManager;

    [SerializeField] private InputActionReference tapAction;

    GameObject spawnedObject;

    void Awake()
    {
        _raycastManager = GetComponent<ARRaycastManager>();
    }

    void OnEnable() => tapAction.action.Enable();
    void OnDisable() => tapAction.action.Disable();

    void Update()
    {
        if (!tapAction.action.triggered || spawnedObject != null)
            return;

        Camera cam = Camera.main;

        // Spawn cách camera ~1.5m phía trước
        Vector3 spawnPos = cam.transform.position + cam.transform.forward * 1.5f;

        spawnedObject = Instantiate(objectToSpawn, spawnPos, Quaternion.identity);

        // Scale chuẩn city AR
        spawnedObject.transform.localScale = Vector3.one * 0.02f;

        // Xoay city hướng về camera
        Vector3 lookPos = cam.transform.position;
        lookPos.y = spawnedObject.transform.position.y;
        spawnedObject.transform.LookAt(lookPos);
    }
}