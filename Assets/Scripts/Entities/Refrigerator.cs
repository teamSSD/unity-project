using UnityEngine;

[RequireComponent(typeof(HoverStateUtil))]
[RequireComponent(typeof(SpriteRenderer))]
public class Refrigerator : MonoBehaviour
{
    private HoverStateUtil hover;
    private SpriteRenderer sr;

    void Awake()
    {
        hover = GetComponent<HoverStateUtil>();
        sr    = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        sr.enabled = !hover.IsHovering();
    }
}
