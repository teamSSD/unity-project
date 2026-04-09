using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// RecipeBook 열기/닫기 토글 버튼
/// View Mode에서만 동작
/// </summary>
public class RecipeBookToggleButton : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private RecipeBookManager recipeBookManager;

    [Header("Button Text")]
    [SerializeField] private TextMeshProUGUI buttonText;

    private void Start()
    {
        if (toggleButton == null)
        {
            toggleButton = GetComponent<Button>();
        }

        if (recipeBookManager == null)
        {
            recipeBookManager = FindFirstObjectByType<RecipeBookManager>();
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(OnToggleButtonClicked);
        }

        UpdateButtonText();
    }

    private void Update()
    {
        // RecipeBook은 항상 View Mode이므로 버튼은 항상 활성화
        if (toggleButton != null)
        {
            toggleButton.interactable = true;
        }

        UpdateButtonText();
    }

    private void OnToggleButtonClicked()
    {
        if (recipeBookManager == null) return;

        if (RecipeBookManager.IsRecipeBookActive)
        {
            // 레시피북이 열려있으면 닫기
            recipeBookManager.Close();
        }
        else
        {
            // 레시피북이 닫혀있으면 열기
            recipeBookManager.Open();
        }
    }

    private void UpdateButtonText()
    {
        if (buttonText == null) return;

        if (RecipeBookManager.IsRecipeBookActive)
        {
            buttonText.text = "레시피북 닫기";
        }
        else
        {
            buttonText.text = "레시피북 열기";
        }
    }
}
