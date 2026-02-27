using UnityEngine;
using System.Collections;

public class BoySimulation : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float firstMoveDistance = 0.42f;
    [SerializeField] private float secondMoveDistance = 0.06f;
    [SerializeField] private float moveSpeed = 0.1f;

    [Header("Rotation")]
    [SerializeField] private float rotateAngle = 90f;
    [SerializeField] private float rotateSpeed = 120f;

    private Animator anim;
    private bool hasFallen = false;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();

        if (anim != null)
            anim.applyRootMotion = false;

        StartCoroutine(MainRoutine());
    }

    IEnumerator MainRoutine()
    {
        yield return Walk(firstMoveDistance);
        if (hasFallen) yield break;

        yield return RotateRight(rotateAngle);
        if (hasFallen) yield break;

        yield return Walk(secondMoveDistance);
    }

    IEnumerator Walk(float distance)
    {
        SetWalking(true);

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + transform.forward * distance;

        while (Vector3.Distance(transform.position, targetPos) > 0.005f)
        {
            if (hasFallen) yield break;

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPos;
        SetWalking(false);
    }

    IEnumerator RotateRight(float angle)
    {
        float rotated = 0f;

        while (rotated < angle)
        {
            if (hasFallen) yield break;

            float step = rotateSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up * step);
            rotated += step;

            yield return null;
        }
    }

    void SetWalking(bool value)
    {
        if (anim != null)
            anim.SetBool("isWalking", value);
    }

    public void Fall()
    {
        if (hasFallen) return;

        hasFallen = true;
        SetWalking(false);

        if (anim != null)
            anim.SetTrigger("fall");
    }
}