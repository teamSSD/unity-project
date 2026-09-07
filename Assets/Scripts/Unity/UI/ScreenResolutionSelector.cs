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
        // WebGL: SetResolution 후 canvas render target/DOM은 즉시 바뀌지만 EventSystem/GraphicRaycaster의
        // 좌표 매핑 캐시가 다음 프레임까지 이전 값을 사용 → 클릭 좌표 어긋남. 강제 refresh.
        StartCoroutine(RefreshInputAfterResize());
    }

    private System.Collections.IEnumerator RefreshInputAfterResize()
    {
        // WebGL은 canvas render target/DOM 크기 변경이 여러 프레임에 걸쳐 propagate됨.
        // 한 프레임만 대기하면 EventSystem/GraphicRaycaster/Camera.pixelRect 갱신이 늦음.
        for (int i = 0; i < 3; i++) yield return null;
        Canvas.ForceUpdateCanvases();
        // Camera pixelRect도 강제 갱신 (raycast 좌표 매핑 원천).
        var cam = Camera.main;
        if (cam != null) { cam.enabled = false; cam.enabled = true; }
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null) { es.enabled = false; es.enabled = true; }
        // 갱신 후 한 프레임 더 대기해서 재초기화 반영 확인.
        yield return null;
        Canvas.ForceUpdateCanvases();
    }

void UpdateText()
    {
        var (w, h) = Resolutions[currentIndex];
        resolutionText.text = $"{w}x{h}";
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
