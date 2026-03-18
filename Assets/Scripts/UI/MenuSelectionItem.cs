using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 개별 메뉴 항목(음식)을 나타내는 UI 스크립트.
/// 음식 이미지, 이름, 조리 도구 아이콘 및 선택 상태(체크박스)를 관리합니다.
/// </summary>
public class MenuSelectionItem : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image foodImage;
    [SerializeField] private Image toolIcon;
    [SerializeField] private GameObject checkbox;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private Button clickButton;

    public FoodData CurrentFood { get; private set; }
    public Action<MenuSelectionItem> OnClicked;

    private void Awake()
    {
        if (clickButton == null) clickButton = GetComponent<Button>();
        if (clickButton != null)
        {
            clickButton.onClick.AddListener(() => OnClicked?.Invoke(this));
        }
    }

    /// <summary>
    /// FoodData SO를 기반으로 음식 이미지와 이름을 설정합니다.
    /// </summary>
    public void SetData(FoodData foodData)
    {
        CurrentFood = foodData;
        if (foodData == null) return;

        if (foodImage != null)
        {
            foodImage.sprite = foodData.image;
            foodImage.preserveAspect = true;
            foodImage.gameObject.SetActive(true);
        }

        if (nameLabel != null)
        {
            nameLabel.text = foodData.ingredientName;
        }

        // 도구 아이콘 초기화 (필요 시 확장)
        if (toolIcon != null) toolIcon.gameObject.SetActive(false);
        
        // 초기 체크박스 상태는 꺼짐
        SetSelection(false);
    }

    /// <summary>
    /// 체크박스 활성화 여부를 결정합니다.
    /// </summary>
    public void SetSelection(bool isSelected)
    {
        if (checkbox != null)
        {
            checkbox.SetActive(isSelected);
        }
    }

    /// <summary>
    /// 필수 조리 도구 아이콘을 설정합니다. (필요 시 외부에서 호출)
    /// </summary>
    public void SetTool(Sprite toolSprite)
    {
        if (toolIcon != null)
        {
            if (toolSprite == null)
            {
                toolIcon.gameObject.SetActive(false);
            }
            else
            {
                toolIcon.gameObject.SetActive(true);
                toolIcon.sprite = toolSprite;
                toolIcon.preserveAspect = true;
            }
        }
    }
}
