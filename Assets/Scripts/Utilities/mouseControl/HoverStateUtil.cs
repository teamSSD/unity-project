using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class HoverStateUtil : MonoBehaviour
{
    private Camera cam;
    private Collider2D col2d;
    private bool isHovering;
    public Action OnHovering;
    public Action OnNone;

    void Start()
    {
        cam = Camera.main;
        col2d = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (col2d == null) col2d = GetComponent<Collider2D>();
        Vector3 mouse = Input.mousePosition;
        mouse.z = Mathf.Abs(cam.transform.position.z);
        Vector2 p = cam.ScreenToWorldPoint(mouse);

        Collider2D[] hits = Physics2D.OverlapPointAll(p);
        isHovering = false;

        foreach (var h in hits)
        {
            if (h == col2d)
            {
                isHovering = true;
                break;
            }
        }

        Invoke();
    }

    public bool IsHovering() => isHovering;

    public void Invoke() {
        try
        {
            if (isHovering) OnHovering?.Invoke();
            else OnNone?.Invoke();
        }
        catch (Exception e) { Debug.LogException(e); }
    }
}
