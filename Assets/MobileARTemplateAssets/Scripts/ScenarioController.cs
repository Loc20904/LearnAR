using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ScenarioController : MonoBehaviour
{
    public ScenarioData data;
    public SequenceController sequenceController;
    public ScenarioUIManager uiManager;
    private AudioSource audioSource;

    private int currentStepIndex = 0;
    private Coroutine stepCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void StartScenario()
    {
        if (data == null)
        {
            Debug.LogError("FATAL ERROR: Bien 'data' (ScenarioData) dang bi NULL tren Prefab!");
            return;
        }

        if (data.steps == null || data.steps.Length == 0)
        {
            Debug.LogError("FATAL ERROR: 'data' co ton tai nhung mang 'steps' bi TRONG!");
            return;
        }

        ShowStep(0);
    }
    void ShowStep(int index)
    {
        if (index >= data.steps.Length) return;

        // Nếu đang chạy bước cũ mà bé bấm nhanh quá, dừng bước cũ lại để tránh chồng chéo âm thanh
        if (stepCoroutine != null) StopCoroutine(stepCoroutine);

        stepCoroutine = StartCoroutine(ExecuteStepRoutine(index));
    }

    private IEnumerator ExecuteStepRoutine(int index)
    {
        currentStepIndex = index;
        StoryStep step = data.steps[index];

        // 1. Dọn dẹp UI cũ
        if (uiManager != null) uiManager.ClearChoices();

        // 2. PHÁT INTRO VOICE (Lời dẫn đầu bước)
        if (step.introVoice != null)
        {
            PlayVoice(step.introVoice);
            // Nếu bạn muốn chờ đọc xong Intro mới chạy hoạt cảnh:
            //while (audioSource.isPlaying) yield return null;
        }

        // 3. CHẠY HOẠT CẢNH (Visual Actions)
        bool sequenceFinished = false;
        if (sequenceController != null && step.visualActions != null && step.visualActions.Length > 0)
        {
            sequenceController.PlayActions(step.visualActions, () =>
            {
                sequenceFinished = true;
            });

            // Chờ hoạt cảnh xong
            while (!sequenceFinished) yield return null;
        }

        // 4. PHÁT OUTRO VOICE VÀ CHỜ KẾT THÚC
        if (step.outroVoice != null)
        {
            PlayVoice(step.outroVoice);

            // CHỜ CHO ĐẾN KHI VOICE PHÁT XONG HẾT
            // (Vòng lặp này sẽ chạy liên tục cho đến khi isPlaying = false)
            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }

        // 5. SAU KHI VOICE HẾT MỚI HIỆN NÚT BẤM
        if (uiManager != null)
        {
            uiManager.DisplayChoices(step.choices);
        }
    }

    void PlayVoice(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    public void OnChoiceSelected(int choiceIndex)
    {
        Choice selected = data.steps[currentStepIndex].choices[choiceIndex];

        if (selected.nextStepIndex == 0)
        {
            if (sequenceController != null) sequenceController.ResetAllActors();
            ShowStep(0);
            return;
        }

        // Chuyển sang bước tiếp theo
        ShowStep(selected.nextStepIndex);
    }
}