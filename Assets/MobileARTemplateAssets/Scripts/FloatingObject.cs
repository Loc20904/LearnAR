using UnityEngine;

public class FloatingObject : MonoBehaviour
{
    [Header("Cấu hình di chuyển")]
    public float amplitude = 0.5f; // Độ cao di chuyển (ví dụ: lên 0.5m, xuống 0.5m)
    public float frequency = 1f;   // Tốc độ di chuyển (càng cao càng nhanh)

    private Vector3 startPosition;

    void Start()
    {
        // Lưu lại vị trí bắt đầu để làm mốc
        startPosition = transform.localPosition;
    }

    void Update()
    {
        // Tính toán vị trí Y mới dựa trên hàm Sin
        // Công thức: Y = Vị trí gốc + (Biên độ * Sin(Thời gian * Tần số))
        float newY = startPosition.y + Mathf.Sin(Time.time * frequency) * amplitude;

        // Cập nhật vị trí mới cho Object
        transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);
    }
}