using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 5f; // 이동 속도
    public float minX = 3f;     // 맵 왼쪽 경계
    public float maxX = 12f;      // 맵 오른쪽 경계

    Rigidbody2D rb;
    SpriteRenderer sr;
    float moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>(); 
    }

    void Update()
    {
        // A,D 또는 ←,→ 키 입력(-1(왼쪽), 0, 1(오른쪽))
        moveInput = Input.GetAxisRaw("Horizontal"); 

        if (moveInput < 0) //왼쪽 이동이면 스프라이트 뒤집기
            sr.flipX = true;
        else if (moveInput > 0)
            sr.flipX = false;

        // X축 Clamp
        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    void FixedUpdate()
    {
        // 물리 이동 (프레임별로 부드럽게)
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }
}
