using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    [SerializeField] private Image foodIcon;
    [SerializeField] private TextMeshProUGUI quantityLabel;
    [SerializeField] private Image selectionOverlay;
    [SerializeField] private Button button;

    public FoodData CurrentFood { get; private set; }
    public System.Action<InventorySlot> OnClicked;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button?.onClick.AddListener(() => OnClicked?.Invoke(this));
    }

    public void SetData(FoodData food, int totalQty)
    {
        CurrentFood = food;

        if (foodIcon != null)
        {
            foodIcon.sprite = food.image;
            foodIcon.preserveAspect = true;
            foodIcon.gameObject.SetActive(true);
        }

        if (quantityLabel != null)
        {
            quantityLabel.text = totalQty.ToString();
            quantityLabel.gameObject.SetActive(true);
        }

        SetSelected(false);
    }

    public void SetEmpty()
    {
        CurrentFood = null;
        if (foodIcon != null)      foodIcon.gameObject.SetActive(false);
        if (quantityLabel != null) quantityLabel.gameObject.SetActive(false);
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionOverlay != null)
            selectionOverlay.enabled = isSelected;
    }
}
