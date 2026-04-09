using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Diary의 시간대별 액션 토글 (Work/Rest/Shopping)
/// SelectPrefab.prefab에서 사용
/// </summary>
public class ActionToggle : MonoBehaviour
{
    public int actionType;
    public Toggle toggle;
    public TextMeshProUGUI descriptionLabel;
    public TextMeshProUGUI nameLabel;
    public Image iconImage;
    public GameObject checkmark;
    public Color doneColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
    public Color availableColor = Color.white;
}
