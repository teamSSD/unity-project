using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
public class DeliveryOrderingCustomer : MonoBehaviour
{
    [Header("주입해야할 필드")]
    public GameObject speechBubblePrefab;
    public MenuSchema menuSchema;
    public event Action onExit;

    [Header("내부적 속성")]
    //private ClickStateUtil clickStateUtil;
    private bool isDisplaying = false;
    private GameObject speechBubble;
    SpeechBubble speechBubbleScript;

    /*
    public void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        clickStateUtil.OnClicked += clickRoutine;
    }
    */
    public void OnDestroy()
    {
        //clickStateUtil.OnClicked -= clickRoutine;
        if (speechBubble != null) Destroy(speechBubble);
    }
    void OnMouseDown()
    {
        clickRoutine();
    }
    public void clickRoutine()
    {
        if (isDisplaying)
        {
            Destroy(gameObject);
            onExit.Invoke();
            return;
        }
        say("배달주문이요! " + menuSchema.name + " 하나 부탁드릴게요");

        isDisplaying = true;
    }

    private void say(string message)
    {
        speechBubble = Instantiate(speechBubblePrefab);

        speechBubbleScript = speechBubble.GetComponent<SpeechBubble>();
        speechBubbleScript.setContents(message);
        speechBubble.transform.position = this.transform.position + new Vector3(-2.5f, 4, 0);
    }
}