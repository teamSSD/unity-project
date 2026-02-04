using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TakingCustomer : MonoBehaviour
{
    public GameObject speechBubblePrefab;
    public CustomerData customerData;
    [SerializeField] private AudioClip takeSoundEffect;

    public void exit()
    {
        gameObject.GetComponent<SpriteRenderer>().sprite = customerData.characterImage;
        say(customerData.escapeMessage);
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
            say(customerData.satisfiedMessage);
            return;
        }
        StatsSystem.AddMoney((int) (totalPrice * 0.7f));
        say(customerData.unsatisfiedMessage);
    }

    private void say(string message)
    {
        GameObject speechBubble = Instantiate(speechBubblePrefab);
        speechBubble.transform.parent = this.gameObject.transform;

        SpeechBubble speechBubbleScript = speechBubble.GetComponent<SpeechBubble>();
        speechBubbleScript.setContents(message);
        speechBubble.transform.position = this.transform.position + new Vector3(-3f, 4, 0);
        
        SoundManager.Instance.Play2DSFX(takeSoundEffect, 0.3f);
    }
}