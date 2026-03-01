using UnityEngine;

// Định nghĩa các loại hành động có thể cấu hình
public enum ActionType { Move, Rotate, Animate, Wait }

[System.Serializable]
public class VisualAction
{
    public ActionType type;
    public int actorIndex;       // 0: Nhân vật 1, 1: Nhân vật 2... trong danh sách actors
    public string parameter;     // Tên Animation (State name) hoặc Trigger
    public string targetPointName; // Điểm đến tuyệt đối (Ví dụ: một empty object đặt sẵn trong scene)
    public Vector3 targetOffset; // Di chuyển tương đối (Ví dụ: forward 2 đơn vị)
    public float duration;       // Thời gian thực hiện hành động
    public bool waitForFinish = true; // Có chờ hành động này xong mới chạy tiếp không?
}

[System.Serializable]
public class StoryStep
{
    public string stepName;
    [TextArea(3, 10)]
    public string dialogueText;
    public Sprite illustration;
    public AudioClip voiceOver;

    [Header("Visual Actions")]
    public VisualAction[] visualActions; // Danh sách các hành động diễn ra tại bước này

    [Header("Choices")]
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public string choiceText;
    public bool isCorrect;
    public string feedback;
    public int nextStepIndex;
}

[CreateAssetMenu(fileName = "NewScenario", menuName = "AR Game/Scenario")]
public class ScenarioData : ScriptableObject
{
    public string scenarioTitle;
    public StoryStep[] steps;
}
