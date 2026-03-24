using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
public class OrderingCustomer : MonoBehaviour
{
    [Header("주입해야할 필드")]
    public GameObject speechBubblePrefab;
    public event Action onExit;
    
    [Header("내부적 속성")]
    private ClickStateUtil clickStateUtil;
    private bool isDisplaying = false;
    private GameObject speechBubble;
    private SpeechBubble speechBubbleScript;
    private MenuSchema menuSchema;
    private CustomerData customerData;

    public void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        clickStateUtil.OnClicked += clickRoutine;
    }

    public void OnDestroy()
    {
        clickStateUtil.OnClicked -= clickRoutine;
        if (speechBubble != null) Destroy(speechBubble);
    }

    public void Inject(MenuSchema menuSchema, CustomerData customerData)
    {
        this.menuSchema = menuSchema;
        this.customerData = customerData;
        gameObject.GetComponent<SpriteRenderer>().sprite = customerData.characterImage;
    }

    public void clickRoutine()
    {
        if (isDisplaying)
        {
            onExit.Invoke();
            return;
        }
        say(string.Format(customerData.orderingMessage, menuSchema.name));
        isDisplaying = true;
    }

    private void say(string message)
    {
        speechBubble = Instantiate(speechBubblePrefab);

        speechBubbleScript = speechBubble.GetComponent<SpeechBubble>();
        speechBubbleScript.setContents(message);
        speechBubble.transform.position = this.transform.position + new Vector3(-2.5f, 4, 0);
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}