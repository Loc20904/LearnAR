using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Scripting;

[Preserve]
public class ScenarioUIManager : MonoBehaviour
{
    public ScenarioController controller;
    public GameObject choiceButtonPrefab; // Prefab của nút bấm choice
    public Transform buttonContainer;     // Nơi chứa các nút choice

    [Header("Start Button (Tự động tạo nếu để trống)")]
    public Button startButton; // Có thể kéo từ Inspector hoặc để null để tự tạo

    private GameObject autoCreatedStartButton; // Reference để quản lý nút tự tạo

    /// <summary>
    /// Hiện nút Start để người dùng bấm bắt đầu scenario
    /// </summary>
    public void ShowStartButton()
    {
        Debug.Log("[ScenarioUIManager] ShowStartButton được gọi.");

        // Nếu đã có Start Button từ Inspector → chỉ cần bật lên
        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
            Debug.Log("[ScenarioUIManager] Dùng Start Button từ Inspector.");
            return;
        }

        // Nếu chưa có → TỰ ĐỘNG TẠO bằng code (không cần drag-drop)
        Debug.Log("[ScenarioUIManager] Tạo Start Button bằng code...");

        // Tìm Canvas trong scene
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ScenarioUIManager] Không tìm thấy Canvas! Không thể tạo Start Button.");
            // Fallback: Tự động start luôn
            OnStartButtonClicked();
            return;
        }

        // Tạo nút
        autoCreatedStartButton = new GameObject("StartButton");
        autoCreatedStartButton.transform.SetParent(canvas.transform, false);

        // RectTransform - đặt ở giữa dưới màn hình
        RectTransform rect = autoCreatedStartButton.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.15f);
        rect.anchorMax = new Vector2(0.5f, 0.15f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400, 120);
        rect.anchoredPosition = Vector2.zero;

        // Background Image
        Image bg = autoCreatedStartButton.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.7f, 0.3f, 0.95f); // Xanh lá đẹp

        // Text con
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(autoCreatedStartButton.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Dùng TextMeshProUGUI nếu có, nếu không dùng Text thường
        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "▶ BẮT ĐẦU";
        tmpText.fontSize = 42;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;
        tmpText.fontStyle = FontStyles.Bold;

        // Button component
        startButton = autoCreatedStartButton.AddComponent<Button>();
        startButton.targetGraphic = bg;

        // Color transition cho đẹp
        ColorBlock colors = startButton.colors;
        colors.normalColor = new Color(0.2f, 0.7f, 0.3f, 0.95f);
        colors.highlightedColor = new Color(0.3f, 0.8f, 0.4f, 1f);
        colors.pressedColor = new Color(0.1f, 0.5f, 0.2f, 1f);
        startButton.colors = colors;

        startButton.onClick.AddListener(OnStartButtonClicked);

        Debug.Log("[ScenarioUIManager] Start Button đã tạo xong và hiển thị.");
    }

    /// <summary>
    /// Khi bấm nút Start
    /// </summary>
    void OnStartButtonClicked()
    {
        Debug.Log("[ScenarioUIManager] Nút Start được bấm!");

        // Ẩn nút Start
        if (startButton != null)
            startButton.gameObject.SetActive(false);

        // Gọi StartScenario
        if (controller != null)
        {
            Debug.Log("[ScenarioUIManager] Gọi controller.StartScenario()...");
            controller.StartScenario();
        }
        else
        {
            Debug.LogError("[ScenarioUIManager] controller bị NULL! Không thể Start.");
        }
    }

    /// <summary>
    /// Hiện các nút choice cho step hiện tại
    /// </summary>
    public void DisplayChoices(Choice[] choices)
    {
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            GameObject btn = Instantiate(choiceButtonPrefab, buttonContainer);

            btn.GetComponentInChildren<TMP_Text>().text = choices[index].choiceText;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                controller.OnChoiceSelected(index);
            });
        }
    }

    public void ClearChoices()
    {
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);
    }
}