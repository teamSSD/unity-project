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
       
       int totalPrice = 0;
       totalPrice += mainMenu.Price;
       sideMenus.ForEach(menu => totalPrice += menu.Price);

       int matchCount = 0;
       if (menuSchema.mainMenu.id == mainMenu.foodData.id) matchCount++;
       List<string> ids = sideMenus.Select(menu => menu.foodData.id).ToList();
       menuSchema.sideMenus.ForEach(menu =>
       {
           if (ids.Contains(menu.id)) matchCount++;
       });

       if (matchCount == menuSchema.sideMenus.Count + 1)
        {
            StatsSystem.Instance.AddMoney(totalPrice);
            say(customerData.satisfiedMessage);
            return;
        }
        StatsSystem.Instance.AddMoney((int) (totalPrice * 0.7f));
        say(customerData.unsatisfiedMessage);
    }

    private void ApplySpriteSettings()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && customerData != null)
        {
            sr.sprite = customerData.characterImage;
            sr.sortingLayerName = "Customer";
            sr.sortingOrder = 0;
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
        speechBubbleScript.PlaceNear(GetComponent<SpriteRenderer>().bounds);
        Destroy(speechBubble, 3f);

        SoundManager.Instance.Play2DSFX(takeSoundEffect, 0.3f);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
