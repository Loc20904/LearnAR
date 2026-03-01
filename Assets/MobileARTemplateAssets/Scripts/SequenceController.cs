using System;
using System.Collections;
using Assets.MobileARTemplateAssets.Scripts;
using UnityEngine;

public class SequenceController : MonoBehaviour
{
    [Header("DANH SÁCH ĐỐI TƯỢNG (ACTORS)")]
    [Tooltip("Kéo các nhân vật/vật thể vào đây theo đúng thứ tự Index (0, 1, 2...)")]
    public GameObject[] actors;

    private Animator[] animators;

    private void Awake()
    {
        // Tự động lấy Animator của các actors khi bắt đầu
        animators = new Animator[actors.Length];
        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i] != null) animators[i] = actors[i].GetComponent<Animator>();
        }
    }

    /// <summary>
    /// Thêm tham số Action onComplete
    /// </summary>
    public void PlayActions(VisualAction[] actions, Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(ExecuteSequenceRoutine(actions, onComplete));
    }

    private IEnumerator ExecuteSequenceRoutine(VisualAction[] actions, Action onComplete)
    {
        if (actions == null || actions.Length == 0) yield break;

        foreach (var action in actions)
        {
            if (action.actorIndex >= actors.Length || actors[action.actorIndex] == null) continue;

            Transform targetTransform = actors[action.actorIndex].transform;
            Animator targetAnim = animators[action.actorIndex];

            IEnumerator subroutine = null;

            switch (action.type)
            {
                case ActionType.Move:
                    Vector3 targetPos = targetTransform.position;

                    // Tìm Point theo tên bên trong Prefab này
                    if (!string.IsNullOrEmpty(action.targetPointName))
                    {
                        // Tìm trong các con của Prefab (bao gồm cả các PointMove bạn đã đặt)
                        Transform point = transform.FindDeepChild(action.targetPointName);
                        if (point != null) targetPos = point.position;
                        else Debug.LogWarning("Không tìm thấy Point: " + action.targetPointName);
                    }
                    else
                    {
                        targetPos += (targetTransform.rotation * action.targetOffset);
                    }
                    if (targetAnim != null)
                        subroutine = MoveObject(targetTransform, targetPos, action.duration, targetAnim);
                    else
                        subroutine = MoveObject(targetTransform, targetPos, action.duration);
                    break;

                case ActionType.Rotate:
                    Quaternion targetRot = targetTransform.rotation * Quaternion.Euler(action.targetOffset);
                    subroutine = RotateObject(targetTransform, targetRot, action.duration);
                    break;

                case ActionType.Animate:
                    if (targetAnim != null) targetAnim.Play(action.parameter);
                    break;

                case ActionType.Wait:
                    subroutine = WaitRoutine(action.duration);
                    break;
            }

            if (subroutine != null)
            {
                if (action.waitForFinish)
                {
                    yield return StartCoroutine(subroutine);
                    // Khi Move xong thì tự động về Idle (nếu có animator)
                    //if (action.type == ActionType.Move && targetAnim != null) targetAnim.Play("Idle");
                }
                else
                {
                    StartCoroutine(subroutine);
                }
            }
        }
        onComplete?.Invoke();
    }

    private IEnumerator WaitRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    private IEnumerator MoveObject(Transform obj, Vector3 targetPosition, float duration, Animator anim)
    {
        if (anim != null)
        {
            // Thử bật cả hai cách để chắc chắn (tùy vào Animator của bạn)
            anim.SetBool("isWalking", true);
            //anim.Play("walk"); // Nếu bạn dùng State Name
        }

        Vector3 startPosition = obj.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            obj.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        obj.position = targetPosition;

        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            //anim.Play("Idle");
        }
    }
    private IEnumerator MoveObject(Transform obj, Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = obj.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            obj.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        obj.position = targetPosition;
    }

    private IEnumerator RotateObject(Transform obj, Quaternion targetRotation, float duration)
    {
        Quaternion startRotation = obj.rotation;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            obj.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        obj.rotation = targetRotation;
    }
}
