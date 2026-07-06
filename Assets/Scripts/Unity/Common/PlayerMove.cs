using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 5f;

    [System.Serializable]
    public struct SceneSpeed { public string sceneName; public float speed; }

    [Tooltip("씬 이름별 이동 속도 override. 매칭 없으면 moveSpeed default 유지.")]
    [SerializeField] private SceneSpeed[] sceneSpeeds;

    [Header("SFX")]
    [SerializeField] private AudioClip walkSfx;
    [SerializeField] private float stepInterval = 0.22f;

    private float defaultMoveSpeed;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator animator;
    float moveInput;
    private float _stepTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        defaultMoveSpeed = moveSpeed;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySpeedForScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplySpeedForScene(scene.name);

    private void ApplySpeedForScene(string sceneName)
    {
        if (sceneSpeeds != null)
        {
            foreach (var s in sceneSpeeds)
            {
                if (s.sceneName == sceneName) { moveSpeed = s.speed; return; }
            }
        }
        moveSpeed = defaultMoveSpeed;
    }

    void Update()
    {
        // UI가 잠긴 상태(PhaseSelection/Shop/Modal 등)에서는 이동 입력 차단.
        moveInput = UILockManager.IsLocked ? 0f : Input.GetAxisRaw("Horizontal");

        if (moveInput < 0)
            sr.flipX = true;
        else if (moveInput > 0)
            sr.flipX = false;

        animator.SetBool(IsMovingHash, moveInput != 0);

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
        if (animator != null) animator.SetBool(IsMovingHash, false);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
