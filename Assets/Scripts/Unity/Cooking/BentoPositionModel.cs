using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BentoPositionModel : MonoBehaviour
{
    public bool isSet { get; set; }

    [Header("Highlight Blink")]
    [SerializeField] private float blinkSpeed = 2.5f;     // 깜빡임 속도 (Hz)
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 1.0f;

    [Header("Highlight 모양 (sprite 미할당 시 점선 outline 자동 생성)")]
    [SerializeField] private Color fallbackColor = Color.black;
    [SerializeField] private int borderThicknessPx = 4;
    [SerializeField] private int dashLengthPx = 12;
    [SerializeField] private string sortingLayerName = "Surface";
    [SerializeField] private int sortingOrder = 100;

    private SpriteRenderer spriteRenderer;
    private Coroutine blinkRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        EnsureFallbackSprite();
        spriteRenderer.sortingLayerName = sortingLayerName;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.enabled = false;
    }

    private void Start()
    {
#if AFTERTASTE_E2E
        // 빈 도시락 슬롯은 화면 좌표만 노출한다. 배치는 매크로의 실제 mouse drag와
        // BentoModel의 충돌 판정이 결정한다.
        E2EWorldTargetRegistry.Register($"cooking.bento-slot.{GetInstanceID()}", transform);
#endif
    }

    private void OnDestroy()
    {
#if AFTERTASTE_E2E
        E2EWorldTargetRegistry.Unregister($"cooking.bento-slot.{GetInstanceID()}", transform);
#endif
    }

    private void EnsureFallbackSprite()
    {
        if (spriteRenderer.sprite != null) return;

        // BoxCollider2D 사이즈 기준 점선 outline texture 동적 생성. 디자인 sprite 만들면 인스펙터에서 교체.
        var col = GetComponent<BoxCollider2D>();
        Vector2 sizeUnits = col != null ? col.size : new Vector2(3f, 1f);
        const int ppu = 100;
        int w = Mathf.Max(8, Mathf.RoundToInt(sizeUnits.x * ppu));
        int h = Mathf.Max(8, Mathf.RoundToInt(sizeUnits.y * ppu));
        int thick = Mathf.Max(1, borderThicknessPx);
        int dash = Mathf.Max(2, dashLengthPx);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool top = y >= h - thick;
                bool bottom = y < thick;
                bool left = x < thick;
                bool right = x >= w - thick;
                bool onBorder = top || bottom || left || right;
                if (!onBorder) { tex.SetPixel(x, y, Color.clear); continue; }
                int idx = (top || bottom) ? x : y;
                bool isDot = (idx / dash) % 2 == 0;
                tex.SetPixel(x, y, isDot ? Color.white : Color.clear);
            }
        }
        tex.Apply();

        spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
        spriteRenderer.color = fallbackColor;
        spriteRenderer.drawMode = SpriteDrawMode.Simple;
    }

    public void ShowHighlight()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null) return;
        spriteRenderer.enabled = true;
        if (blinkRoutine == null) blinkRoutine = StartCoroutine(BlinkLoop());
    }

    public void HideHighlight()
    {
        if (spriteRenderer == null) return;
        if (blinkRoutine != null) { StopCoroutine(blinkRoutine); blinkRoutine = null; }
        spriteRenderer.enabled = false;
    }

    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            float t = Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f);
            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
            spriteRenderer.color = c;
            yield return null;
        }
    }
}
