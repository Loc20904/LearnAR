using UnityEngine;
using System.Collections;

public class BoySimulation : MonoBehaviour
{
    Animator anim;

    float moveDistance = 5f;
    float moveSpeed = 2f;

    void Start()
    {
        anim = GetComponent<Animator>();
        StartCoroutine(SimulationRoutine());
    }

    IEnumerator SimulationRoutine()
    {
        // 1. Đi bộ 5m
        anim.SetBool("isWalking", true);

        float moved = 0f;

        while (moved < moveDistance)
        {
            float step = moveSpeed * Time.deltaTime;
            transform.Translate(Vector3.forward * step);
            moved += step;
            yield return null;
        }

        anim.SetBool("isWalking", false);

        // 2. Chờ 1 giây
        yield return new WaitForSeconds(1f);

        // 3. Ngã
        anim.SetTrigger("fall");

        // 4. Đợi animation ngã + đứng dậy hoàn tất
        yield return new WaitForSeconds(3f);

        // 5. Trở về idle (Animator tự xử lý nếu bạn setup đúng)
    }
}