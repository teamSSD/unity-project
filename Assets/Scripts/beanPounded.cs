using UnityEngine;

public class Bean : MonoBehaviour
{
    public int scoreValue = 1;
    public int playerScore = 0;

    public Sprite defaultSprite;
    public Sprite score5Sprite;
    public Sprite score10Sprite;

    private SpriteRenderer spriteRenderer;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("stick"))
        {
            playerScore += scoreValue;
            Debug.Log("점수 올라감! 현재 점수: " + playerScore);

            Vector3 originalScale = transform.localScale; // 현재 스케일 저장

            if (playerScore >= 10)
            {
                spriteRenderer.sprite = score10Sprite;
            }
            else if (playerScore >= 5)
            {
                spriteRenderer.sprite = score5Sprite;
            }
            else
            {
                spriteRenderer.sprite = defaultSprite;
            }

            transform.localScale = originalScale; // 스케일 다시 적용
        }
    }
}
