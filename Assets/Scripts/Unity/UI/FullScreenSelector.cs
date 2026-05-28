using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FullScreenSelector : MonoBehaviour
{
    private static readonly string[] Labels = { "창 화면", "전체화면" };

    [SerializeField] private TMP_Text label;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private int currentIndex;

    public bool IsFullScreen => currentIndex == 1;

    void Start()
    {
        currentIndex = Screen.fullScreen ? 1 : 0;
        UpdateText();
        prevButton.onClick.AddListener(Toggle);
        nextButton.onClick.AddListener(Toggle);
    }

    public void SetIndex(bool fullScreen)
    {
        currentIndex = fullScreen ? 1 : 0;
        UpdateText();
    }

    public event System.Action OnChanged;

    void Toggle()
    {
        currentIndex = 1 - currentIndex;
        Screen.fullScreen = IsFullScreen;
        UpdateText();
        OnChanged?.Invoke();
    }

    void UpdateText()
    {
        label.text = Labels[currentIndex];
    }
}
