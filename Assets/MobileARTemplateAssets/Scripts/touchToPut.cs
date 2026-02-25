using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceOnPlane : MonoBehaviour
{
    public GameObject objectToSpawn; // Kéo Prefab hiệp sĩ/Boss vào đây
    private ARRaycastManager _raycastManager;

    // Đây là ô để kéo Action từ file Input Actions vào
    [SerializeField] private InputActionReference tapAction;

    void Awake() => _raycastManager = GetComponent<ARRaycastManager>();

    void OnEnable() => tapAction.action.Enable();
    void OnDisable() => tapAction.action.Disable();

    void Update()
    {
        if (tapAction.action.triggered)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();
            List<ARRaycastHit> hits = new List<ARRaycastHit>();

            if (_raycastManager.Raycast(screenPos, hits, TrackableType.PlaneWithinBounds))
            {
                Pose hitPose = hits[0].pose;

                // 1. Tạo object
                GameObject spawnedObject = Instantiate(objectToSpawn, hitPose.position, hitPose.rotation);

                // 2. Tính toán hướng nhìn về phía Camera
                // Chúng ta lấy vị trí Camera nhưng giữ nguyên độ cao (y) của Object để nó không bị ngửa lên trời
                Vector3 lookPos = Camera.main.transform.position;
                lookPos.y = spawnedObject.transform.position.y;

                // 3. Xoay object về phía Camera
                spawnedObject.transform.LookAt(lookPos);

                // 4. (Tùy chọn) Xoay thêm 180 độ nếu model vẫn quay lưng
                // Đôi khi model gốc bị ngược trục Z, bạn thêm dòng này nếu cần:
                // spawnedObject.transform.Rotate(0, 180f, 0);
            }
        }
    }
}