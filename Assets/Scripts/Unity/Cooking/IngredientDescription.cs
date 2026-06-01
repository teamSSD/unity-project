using TMPro;
using UnityEngine;

public class IngredientDescription : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    public void SetTexts(string name, string count, string description)
    {
        nameText.text = "이름 : " + name;
        countText.text = "수량 : " + count;
        descriptionText.text = "설명 : " + description;
    }

    public void UpdateCount(string count)
    {
        countText.text = "수량 : " + count;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}