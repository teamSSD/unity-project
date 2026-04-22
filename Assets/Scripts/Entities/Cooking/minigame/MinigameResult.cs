using System;
using System.Collections.Generic;
using UnityEngine;

public class MinigameResult : MonoBehaviour
{
    private static readonly int AppearHash = Animator.StringToHash("Appear");

    public List<MinigameResultEntity> minigameResults;
    SpriteRenderer spriteRenderer;
    Animator animator;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        minigameResults.Sort(); // score, asc
    }

    public void SetScore(float score)
    {
        foreach (MinigameResultEntity entry in minigameResults)
        {
            if (score * 100 <= entry.score)
            {
                spriteRenderer.sprite = entry.sprite;
                break;
            }
        }
        animator.SetTrigger(AppearHash);
    }

    //0 fail, 0 - 50 poor, 50-70 ok, 

    [System.Serializable]
    public class MinigameResultEntity : IComparable<MinigameResultEntity>
    {
        public float score;
        public Sprite sprite;

        public int CompareTo(MinigameResultEntity other)
        {
            if (other == null) return 1;
            return score.CompareTo(other.score);
        }
    }
}
