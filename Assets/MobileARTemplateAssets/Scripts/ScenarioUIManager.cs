using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScenarioUIManager : MonoBehaviour
{
    public ScenarioController controller; // Kéo ScenarioController vào đây
    public GameObject choiceButtonPrefab; // Prefab của nút bấm
    public Transform buttonContainer;     // Nơi chứa các nút

    public void DisplayChoices(Choice[] choices)
    {
        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        for (int i = 0; i < choices.Length; i++)
        {
            int index = i;
            GameObject btn = Instantiate(choiceButtonPrefab, buttonContainer);

            // 2. Đổi Text thành TextMeshProUGUI hoặc TMP_Text
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