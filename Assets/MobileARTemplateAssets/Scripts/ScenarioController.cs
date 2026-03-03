using System.Collections;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
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

        Debug.Log($"[ScenarioController] Bắt đầu Step {index}: {step.stepName}");

        // 1. Dọn dẹp UI cũ
        if (uiManager != null) uiManager.ClearChoices();

        // 2. PHÁT INTRO VOICE (Lời dẫn đầu bước)
        if (step.introVoice != null)
        {
            PlayVoice(step.introVoice);
        }

        // 3. CHẠY HOẠT CẢNH (Visual Actions)
        bool sequenceFinished = false;
        if (sequenceController != null && step.visualActions != null && step.visualActions.Length > 0)
        {
            Debug.Log($"[ScenarioController] Step {index}: Bắt đầu chạy {step.visualActions.Length} visual actions...");

            sequenceController.PlayActions(step.visualActions, () =>
            {
                sequenceFinished = true;
                Debug.Log($"[ScenarioController] Step {index}: Visual actions HOÀN TẤT.");
            });

            // Chờ hoạt cảnh xong VỚI TIMEOUT 60 GIÂY để tránh treo mãi mãi
            float timeout = 60f;
            float elapsed = 0f;
            while (!sequenceFinished && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!sequenceFinished)
            {
                Debug.LogError($"[ScenarioController] Step {index}: TIMEOUT! Visual actions chạy quá 60 giây. Bỏ qua để tiếp tục.");
                sequenceFinished = true;
            }
        }
        else
        {
            Debug.Log($"[ScenarioController] Step {index}: Không có visual actions hoặc sequenceController null.");
        }

        // 4. PHÁT OUTRO VOICE VÀ CHỜ KẾT THÚC
        if (step.outroVoice != null)
        {
            PlayVoice(step.outroVoice);

            // CHỜ CHO ĐẾN KHI VOICE PHÁT XONG HẾT (với timeout 30 giây)
            float audioTimeout = 30f;
            float audioElapsed = 0f;
            while (audioSource != null && audioSource.isPlaying && audioElapsed < audioTimeout)
            {
                audioElapsed += Time.deltaTime;
                yield return null;
            }

            if (audioElapsed >= audioTimeout)
            {
                Debug.LogWarning($"[ScenarioController] Step {index}: Audio timeout sau 30 giây.");
            }
        }

        // 5. SAU KHI VOICE HẾT MỚI HIỆN NÚT BẤM
        Debug.Log($"[ScenarioController] Step {index}: Hiện choices.");
        if (uiManager != null)
        {
            uiManager.DisplayChoices(step.choices);
        }
        else
        {
            Debug.LogError($"[ScenarioController] Step {index}: uiManager bị NULL! Không thể hiện choices.");
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