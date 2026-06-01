using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI contents;
    [SerializeField] private RectTransform tail;

    public void setContents(string text)
    {
        contents.text = text;
    }

    public void PlaceNear(Bounds targetBounds)
    {
        Camera cam = Camera.main;

        RectTransform rt = GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        Vector2 worldSize = new Vector2(
            rt.rect.width * rt.lossyScale.x,
            rt.rect.height * rt.lossyScale.y
        );

        bool placeRight = cam.WorldToViewportPoint(targetBounds.center).x < 0.5f;

        tail.localScale = new Vector3(placeRight ? 1f : -1f, 1f, 1f);

        float tailX = placeRight ? -rt.rect.width * 0.40f : rt.rect.width * 0.40f;
        float tailY = -rt.rect.height * 0.5f - tail.rect.height * 0.5f + 52f;
        tail.anchoredPosition = new Vector2(tailX, tailY);

        transform.position = WorldUIPositioner.Calculate(cam, targetBounds, worldSize);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
