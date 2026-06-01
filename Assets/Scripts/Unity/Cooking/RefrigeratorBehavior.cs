using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class RefrigeratorBehavior : MonoBehaviour
{
    public bool hideWhenMouseOver = true;
    public AudioClip openSound;
    public AudioClip closeSound;

    private SpriteRenderer sr;
    private Camera cam;
    private Collider2D col;
    private bool wasVisible;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;
        col = GetComponent<Collider2D>();
        wasVisible = sr.enabled;
    }

    void Update()
    {
        bool isVisibleNow = JudgeHover();

        if (isVisibleNow != wasVisible)
        {
            if (isVisibleNow == false)
            {
                SoundManager.Instance.Play2DSFX(openSound, 0.4f);
            }
            else
            {
                SoundManager.Instance.Play2DSFX(closeSound, 0.4f);
            }
        }

        sr.enabled = isVisibleNow;
        if (col) col.enabled = isVisibleNow;
        wasVisible = isVisibleNow;
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
