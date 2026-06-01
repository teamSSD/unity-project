using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(HoverStateUtil))]
[RequireComponent(typeof(SpriteStackRenderer))]
[DisallowMultipleComponent]
public class CookingToolBehavior : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    public Vector3 defaultPosition;
    public bool isCookable = false;
    private ClickStateUtil clickStateUtil;
    private HoverStateUtil hoverStateUtil;
    private SpriteStackRenderer spriteStackRenderer;
    private Camera mainCamera;
    public bool locked = false;
    void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        hoverStateUtil = GetComponent<HoverStateUtil>();
        spriteStackRenderer = GetComponent<SpriteStackRenderer>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        ProcessMovement(clickStateUtil.getState());
    }

    private void ProcessMovement(ClickState clickState)
    {
        if (locked) return;
        
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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}