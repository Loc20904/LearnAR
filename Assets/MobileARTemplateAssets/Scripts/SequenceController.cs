using System;
using System.Collections;
using Assets.MobileARTemplateAssets.Scripts;
using UnityEngine;
using UnityEngine.Scripting;


[Preserve]
public class SequenceController : MonoBehaviour
{
    [Header("DANH SÁCH ĐỐI TƯỢNG (ACTORS)")]
    [Tooltip("Kéo các nhân vật/vật thể vào đây theo đúng thứ tự Index (0, 1, 2...)")]
    public GameObject[] actors;
    private Animator[] animators;

    private Vector3[] initialPositions;
    private Quaternion[] initialRotations;

    //private void Awake()
    //{
    //    // Tự động lấy Animator của các actors khi bắt đầu
    //    animators = new Animator[actors.Length];
    //    initialPositions = new Vector3[actors.Length]; // PHẢI CÓ DÒNG NÀY
    //    initialRotations = new Quaternion[actors.Length]; // PHẢI CÓ DÒNG NÀY
    //    for (int i = 0; i < actors.Length; i++)
    //    {
    //        if (actors[i] != null)
    //        {
    //            animators[i] = actors[i].GetComponent<Animator>();
    //            initialPositions[i] = actors[i].transform.localPosition;
    //            initialRotations[i] = actors[i].transform.localRotation;
    //        }
    //    }
    //}

    public void InitializeActors() // Đổi từ Awake thành hàm public
    {
        animators = new Animator[actors.Length];
        initialPositions = new Vector3[actors.Length];
        initialRotations = new Quaternion[actors.Length];

        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i] != null)
            {
                // Quan trọng: Dùng GetComponentInChildren vì Animator thường nằm ở model con
                animators[i] = actors[i].GetComponentInChildren<Animator>();
                initialPositions[i] = actors[i].transform.localPosition;
                initialRotations[i] = actors[i].transform.localRotation;
                Debug.Log($"Actor {i} Animator: {(animators[i] != null ? "Found" : "NULL")}");
            }
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

    /// <summary>
    /// Hàm gọi để reset tất cả nhân vật về vị trí và trạng thái ban đầu
    /// </summary>
    public void ResetAllActors()
    {
        StopAllCoroutines(); // Dừng ngay mọi hành động di chuyển đang dở dang

        for (int i = 0; i < actors.Length; i++)
        {
            if (actors[i] != null)
            {
                // Trả về vị trí và góc xoay gốc
                actors[i].transform.localPosition = initialPositions[i];
                actors[i].transform.localRotation = initialRotations[i];

                // Trả Animator về trạng thái mặc định (về Idle ban đầu)
                if (animators[i] != null)
                {
                    animators[i].Rebind();
                    animators[i].Update(0f);
                }
            }
        }
    }

    private IEnumerator ExecuteSequenceRoutine(VisualAction[] actions, Action onComplete)
    {
        if (actions == null || actions.Length == 0)
        {
            Debug.Log("[SequenceController] Không có actions nào để chạy, gọi onComplete ngay.");
            onComplete?.Invoke();
            yield break;
        }

        bool hasError = false;

        for (int actionIdx = 0; actionIdx < actions.Length; actionIdx++)
        {
            var action = actions[actionIdx];

            // Kiểm tra actorIndex hợp lệ
            if (action.actorIndex < 0 || action.actorIndex >= actors.Length)
            {
                Debug.LogWarning($"[SequenceController] Action {actionIdx}: actorIndex {action.actorIndex} ngoài phạm vi (actors.Length={actors.Length}). Bỏ qua.");
                continue;
            }

            if (actors[action.actorIndex] == null)
            {
                Debug.LogWarning($"[SequenceController] Action {actionIdx}: Actor tại index {action.actorIndex} bị NULL. Bỏ qua.");
                continue;
            }

            Transform targetTransform = actors[action.actorIndex].transform;
            Animator targetAnim = (animators != null && action.actorIndex < animators.Length)
                ? animators[action.actorIndex]
                : null;

            IEnumerator subroutine = null;

            try
            {
                switch (action.type)
                {
                    case ActionType.Move:
                        Vector3 targetPos = targetTransform.position;

                        // Tìm Point theo tên bên trong Prefab này
                        if (!string.IsNullOrEmpty(action.targetPointName))
                        {
                            Transform point = transform.FindDeepChild(action.targetPointName);
                            if (point != null)
                            {
                                targetPos = point.position;
                            }
                            else
                            {
                                Debug.LogError($"[SequenceController] Action {actionIdx}: Không tìm thấy Point '{action.targetPointName}'. Dùng vị trí hiện tại.");
                            }
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
                        else Debug.LogWarning($"[SequenceController] Action {actionIdx}: Animate nhưng Animator là NULL.");
                        break;

                    case ActionType.Wait:
                        subroutine = WaitRoutine(action.duration);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SequenceController] Action {actionIdx} ({action.type}) bị lỗi: {ex.Message}\n{ex.StackTrace}");
                hasError = true;
                continue; // Bỏ qua action lỗi, chạy tiếp action kế
            }

            if (subroutine != null)
            {
                if (action.waitForFinish)
                {
                    yield return StartCoroutine(subroutine);
                }
                else
                {
                    StartCoroutine(subroutine);
                }
            }
        }

        if (hasError)
            Debug.LogWarning("[SequenceController] Sequence hoàn tất nhưng có lỗi ở một số action.");

        // ĐẢM BẢO LUÔN GỌI onComplete dù có lỗi hay không
        Debug.Log("[SequenceController] Sequence hoàn tất, gọi onComplete.");
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
