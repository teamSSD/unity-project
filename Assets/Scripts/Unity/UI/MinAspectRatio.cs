using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class MinAspectRatio : MonoBehaviour, ILayoutSelfController
{
    [SerializeField] private float minHeightRatio = 0.6f;

    private RectTransform _rt;
    private RectTransform Rt => _rt ??= GetComponent<RectTransform>();

    public void SetLayoutHorizontal() { }

    public void SetLayoutVertical()
    {
        float minH = Rt.rect.width * minHeightRatio;
        if (Rt.rect.height < minH)
            Rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minH);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
