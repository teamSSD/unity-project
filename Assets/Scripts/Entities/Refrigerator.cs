using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Refrigerator : MonoBehaviour
{
    public bool hideWhenMouseOver = true;

    private SpriteRenderer sr;
    private Camera cam;
    private Collider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        bool visible = JudgeHover();

        sr.enabled = visible;
        if (col) col.enabled = visible;
    }
    
    private bool JudgeHover()
    {
        Vector3 mp = Input.mousePosition;
        mp.z = cam.WorldToScreenPoint(transform.position).z;
        Vector3 world = cam.ScreenToWorldPoint(mp);

        bool mouseOver = sr.bounds.Contains(world);

        return hideWhenMouseOver ? !mouseOver : true;
    }
}
