using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
[RequireComponent(typeof(SpriteStackRenderer))]
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class CookingToolBehavior : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    public Vector3 defaultPosition;
    private ClickStateUtil clickStateUtil;
    private HoverStateUtil hoverStateUtil;
    private Animator animator;
    private SpriteStackRenderer spriteStackRenderer;
    private Camera mainCamera;
    void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        hoverStateUtil = GetComponent<HoverStateUtil>();
        spriteStackRenderer = GetComponent<SpriteStackRenderer>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        ProcessAnimation(hoverStateUtil.IsHovering(), clickStateUtil.getState());
        ProcessMovement(clickStateUtil.getState());
    }

    private void ProcessAnimation(bool isHovering, ClickState clickState)
    {
        if (isHovering && clickState != ClickState.Dragging) animator.SetBool("Hovering", true);
        else animator.SetBool("Hovering", false);
    }

    private void ProcessMovement(ClickState clickState)
    {
        if (clickState == ClickState.Dragging)
        {
            Vector3 target = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            target.z = defaultPosition.z;
            gameObject.transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * speed);
        }
        if (clickState == ClickState.None && transform.position != defaultPosition)
        {
            Vector3 newPosition = Vector3.Lerp(transform.position, defaultPosition, Time.deltaTime * speed);
            newPosition.z = defaultPosition.z;
            transform.position = newPosition;
            if (Vector3.Distance(transform.position, defaultPosition) < 0.02f)
                transform.position = defaultPosition;
        }
    }

    public void ResetTexture()
    {
        spriteStackRenderer.Clear();
    }

    public void AddTexture(Sprite sprite)
    {
        spriteStackRenderer.Add(sprite);
    }

    public void SetTexture(List<Sprite> sprites)
    {
        spriteStackRenderer.DrawMany(sprites);
    }
}