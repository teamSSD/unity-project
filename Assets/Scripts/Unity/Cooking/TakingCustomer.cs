using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class TakingCustomer : MonoBehaviour
{
    public GameObject speechBubblePrefab;
    public CustomerData customerData;
    [SerializeField] private AudioClip takeSoundEffect;

    public void exit()
    {
        ApplySpriteSettings();
        say(customerData.escapeMessage);
    }

    public void take(MenuSchema menuSchema, FoodSchema mainMenu, List<FoodSchema> sideMenus)
    {
        ApplySpriteSettings();

        // 보상 지급은 CustomerLifecycle.OnOrderDelivered가 담당 (기획 공식 적용).
        // 여기는 만족도에 따른 시각적 메시지만 처리.
        int matchCount = 0;
        if (menuSchema.mainMenu.id == mainMenu.foodData.id) matchCount++;
        List<string> ids = sideMenus.Select(menu => menu.foodData.id).ToList();
        menuSchema.sideMenus.ForEach(menu =>
        {
            if (ids.Contains(menu.id)) matchCount++;
        });

        bool perfect = matchCount == menuSchema.sideMenus.Count + 1;
        say(perfect ? customerData.satisfiedMessage : customerData.unsatisfiedMessage);
    }

    private void ApplySpriteSettings()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && customerData != null)
        {
            sr.sprite = customerData.characterImage;
            sr.sortingLayerName = "Customer";
            sr.sortingOrder = 1; // 가져가는 손님은 다른 손님 위로
        }

        var pc = GetComponent<PolygonCollider2D>();
        if (pc != null && sr.sprite != null)
        {
            // 새로운 스프라이트 외곽선에 맞게 콜라이더 재생성
            int pathCount = sr.sprite.GetPhysicsShapeCount();
            pc.pathCount = pathCount;
            List<Vector2> pathPoints = new List<Vector2>();
            for (int i = 0; i < pathCount; i++)
            {
                pathPoints.Clear();
                sr.sprite.GetPhysicsShape(i, pathPoints);
                pc.SetPath(i, pathPoints);
            }
        }
    }

    private void say(string message)
    {
        GameObject speechBubble = Instantiate(speechBubblePrefab);
        SpeechBubble speechBubbleScript = speechBubble.GetComponent<SpeechBubble>();
        speechBubbleScript.setContents(message);
        // Bubble offset이 손님 왼쪽 어깨 기준으로 캘리브됨 → 항상 손님 왼쪽에 강제.
        speechBubbleScript.PlaceNear(GetComponent<SpriteRenderer>().bounds, forceBubbleOnLeftOfCustomer: true);
        Destroy(speechBubble, 3f);

        SoundManager.Instance.Play2DSFX(takeSoundEffect, 0.3f);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
