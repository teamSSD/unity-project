using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenResolutionSelector : MonoBehaviour
{
    private static readonly (int w, int h)[] Resolutions =
    {
        (1280, 720), (1600, 900), (1920, 1080), (2560, 1440)
    };

    [SerializeField] private TMP_Text resolutionText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    private int currentIndex = 2;

    public int CurrentIndex => currentIndex;

    public void SetIndex(int idx)
    {
        currentIndex = Mathf.Clamp(idx, 0, Resolutions.Length - 1);
        UpdateText();
    }

    void Start()
    {
        for (int i = 0; i < Resolutions.Length; i++)
        {
            if (Screen.width == Resolutions[i].w && Screen.height == Resolutions[i].h)
            {
                currentIndex = i;
                break;
            }
        }
        UpdateText();
        prevButton.onClick.AddListener(Prev);
        nextButton.onClick.AddListener(Next);
    }

    void Prev()
    {
        currentIndex = (currentIndex - 1 + Resolutions.Length) % Resolutions.Length;
        Apply();
    }

    void Next()
    {
        currentIndex = (currentIndex + 1) % Resolutions.Length;
        Apply();
    }

    public event System.Action OnChanged;

    void Apply()
    {
        var (w, h) = Resolutions[currentIndex];
        Screen.SetResolution(w, h, Screen.fullScreen);
        UpdateText();
        OnChanged?.Invoke();
    }

void UpdateText()
    {
        var (w, h) = Resolutions[currentIndex];
        resolutionText.text = $"{w}x{h}";
    }
}
