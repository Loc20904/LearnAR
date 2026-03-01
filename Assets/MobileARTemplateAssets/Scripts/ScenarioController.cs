using UnityEngine;

public class ScenarioController : MonoBehaviour
{
    public ScenarioData data;
    public SequenceController sequenceController; // Kéo object chứa SequenceController vào đây
    public ScenarioUIManager uiManager;

    private int currentStepIndex = 0;

    public void StartScenario()
    {
        ShowStep(0);
    }

    void ShowStep(int index)
    {
        if (index >= data.steps.Length) return;

        currentStepIndex = index;
        StoryStep step = data.steps[index];

        Debug.Log("Đang ở bước: " + step.dialogueText);

        // 1. Xóa các nút bấm cũ ngay khi bắt đầu bước mới
        if (uiManager != null) uiManager.ClearChoices();

        // 2. Kích hoạt diễn hoạt nhân vật & Chờ hoàn thành để hiện nút bấm mới
        if (sequenceController != null && step.visualActions != null && step.visualActions.Length > 0)
        {
            // Truyền callback (đoạn code bên trong ngoặc nhọn sẽ chỉ chạy khi Animation xong)
            sequenceController.PlayActions(step.visualActions, () =>
            {
                if (uiManager != null) uiManager.DisplayChoices(step.choices);
            });
        }
        else
        {
            // Nếu bước này không có hoạt cảnh nào, hiện UI lên luôn
            if (uiManager != null) uiManager.DisplayChoices(step.choices);
        }

        // 3. Phát âm thanh (Nếu có)
        // if (step.voiceOver != null) AudioSource.PlayClipAtPoint(step.voiceOver, Camera.main.transform.position);
    }

    public void OnChoiceSelected(int choiceIndex)
    {
        Choice selected = data.steps[currentStepIndex].choices[choiceIndex];

        // Logic xử lý đúng sai (Bạn có thể thêm hiệu ứng ở đây)
        if (selected.isCorrect) Debug.Log("Hoan hô! Bé đã chọn đúng: " + selected.feedback);
        else Debug.Log("Cố lên! Hãy thử lại: " + selected.feedback);

        // Chuyển sang bước tiếp theo
        ShowStep(selected.nextStepIndex);
    }
}
