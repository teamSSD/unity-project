using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class TakingCustomer : MonoBehaviour
{
    public GameObject speechBubblePrefab;

    public void exit()
    {
        say("사장님, 기다림도 미학이라지만 오늘은 제 인내심 배터리가 방전 직전이라 이만 물러나겠습니다. 더 기다리다간 제가 젠틀함을 잃을 것 같아서요. 오늘은 서로 타이밍이 안 맞았던 걸로 하죠. 주문은 취소해 주세요.");
    }

    public void take(MenuSchema menuSchema, FoodSchema mainMenu, List<FoodSchema> sideMenus)
    {
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
            StatsSystem.AddMoney(totalPrice);
            say("와, 도시락을 건네받자마자 느낌 딱 오는데요? 냄새가 너무 치명적이라 집에 가는 엘리베이터에서 테러 좀 하겠네요. 식기 전에 얼른 '모셔' 가겠습니다. 완벽했어요, 감사합니다.");
            return;
        }
        StatsSystem.AddMoney((int) (totalPrice * 0.7f));
        say("일단 받아는 가는데, 솔직히 첫인상이 제가 기대했던 바이브는 아니네요. 부디 맛이 이 찝찝함을 날려버릴 '반전 드라마'이길 바라며 들고 갑니다. 수고하세요.");
    }

    private void say(string message)
    {
        GameObject speechBubble = Instantiate(speechBubblePrefab);
        speechBubble.transform.parent = this.gameObject.transform;

        SpeechBubble speechBubbleScript = speechBubble.GetComponent<SpeechBubble>();
        speechBubbleScript.setContents(message);
        speechBubble.transform.position = this.transform.position + new Vector3(-3f, 4, 0);
        Destroy(gameObject, 3f);
    }
}