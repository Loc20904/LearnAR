using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 0.076f;
    [SerializeField] private float moveDistance = 1.5f;

    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private LayerMask boyLayer;

    private Vector3 startPos;
    private Vector3 targetPos;
    private bool stopped = false;

    void Start()
    {
        startPos = transform.position;
        targetPos = startPos + transform.forward * moveDistance;
    }

    void Update()
    {
        if (stopped) return;

        DetectBoy();

        if (!stopped)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
        }
    }

    void DetectBoy()
    {
        float scaledRadius = detectionRadius * transform.lossyScale.x;

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            scaledRadius,
            boyLayer
        );

        foreach (Collider hit in hits)
        {
            BoySimulation boy = hit.GetComponentInParent<BoySimulation>();

            if (boy != null)
            {
                stopped = true;

                boy.Fall();   // ép Boy ngã

                Debug.Log("Boy detected! Car stopped & Boy fell.");
            }
        }
    }

    void OnDrawGizmos()
    {
        float scaledRadius = detectionRadius * transform.lossyScale.x;

        // ===== Detection Sphere =====
        Gizmos.color = stopped ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scaledRadius);

        // ===== Fill nhẹ để nhìn rõ hơn =====
        Color fillColor = stopped ?
            new Color(1f, 0f, 0f, 0.1f) :
            new Color(1f, 1f, 0f, 0.1f);

        Gizmos.color = fillColor;
        Gizmos.DrawSphere(transform.position, scaledRadius);

        // ===== Hướng di chuyển =====
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position,
                        transform.position + transform.forward * 0.5f);

        // ===== Đường đi =====
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPos, targetPos);

            Gizmos.DrawSphere(targetPos, 0.03f);
        }
    }
}