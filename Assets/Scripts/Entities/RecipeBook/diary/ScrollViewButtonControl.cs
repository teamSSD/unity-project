using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScrollViewButtonControl : MonoBehaviour
{
    [Header("ScrollRect Reference")]
    public ScrollRect scrollRect;

    [Header("Scroll Settings")]
    [Range(0.01f, 1f)]
    public float scrollStep = 0.2f; // 버튼 한 번 클릭 시 이동 비율
    public float scrollDuration = 0.3f; // 부드럽게 이동 시간

    public void ScrollRight()
    {
        float target = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition + scrollStep);
        StartCoroutine(SmoothScroll(target));
    }

    public void ScrollLeft()
    {
        float target = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition - scrollStep);
        StartCoroutine(SmoothScroll(target));
    }

    private IEnumerator SmoothScroll(float target)
    {
        float start = scrollRect.horizontalNormalizedPosition;
        float time = 0f;

        while (time < scrollDuration)
        {
            time += Time.deltaTime;
            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, target, time / scrollDuration);
            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = target;
    }
}
