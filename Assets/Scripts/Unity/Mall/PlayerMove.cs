using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("SFX")]
    [SerializeField] private AudioClip walkSfx;
    [SerializeField] private float stepInterval = 0.22f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    float moveInput;
    private float _stepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); 
    }

    void Update()
    {
        // A,D 또는 ←,→ 키 입력(-1(왼쪽), 0, 1(오른쪽))
        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput < 0)
            sr.flipX = true;
        else if (moveInput > 0)
            sr.flipX = false;

        if (moveInput != 0)
        {
            _stepTimer += Time.deltaTime;
            if (_stepTimer >= stepInterval)
            {
                _stepTimer = 0f;
                SoundManager.Instance?.Play2DSFX(walkSfx);
            }
        }
        else
        {
            _stepTimer = stepInterval;
        }

        // X축 Clamp
        //float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        //transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    void FixedUpdate()
    {
        // 물리 이동 (프레임별로 부드럽게)
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    void OnDisable()
    {
        moveInput = 0;
        if (rb != null)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
