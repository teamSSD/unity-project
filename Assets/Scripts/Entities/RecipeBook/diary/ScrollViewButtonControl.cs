using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScrollViewButtonControl : MonoBehaviour
{
    [Header("ScrollRect Reference")]
    public ScrollRect scrollRect;

    [Header("Scroll Settings")]
    [Range(0.01f, 1f)]
    public float scrollStep = 0.2f; // rate of movement with one click of a button
    public float scrollDuration = 0.3f; // smooth move time

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
