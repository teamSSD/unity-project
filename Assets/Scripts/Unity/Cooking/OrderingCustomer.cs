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
    private bool isExiting = false;
    private GameObject speechBubble;
    private SpeechBubble speechBubbleScript;
    private MenuSchema menuSchema;
    public CustomerData customerData;

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
        
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && customerData != null)
        {
            if (customerData.characterImage != null)
            {
                sr.sprite = customerData.characterImage;
            }
            else
            {
                Debug.LogWarning($"[OrderingCustomer] {customerData.name}: characterImage is null! Using default renderer.");
            }
            sr.sortingLayerName = "Customer";
            sr.sortingOrder = 0; 
            
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
            
            Debug.Log($"[OrderingCustomer] Injected {customerData.name}. Sprite: {(sr.sprite != null ? sr.sprite.name : "NULL")}");
        }
        else
        {
            Debug.LogError($"[OrderingCustomer] Missing SpriteRenderer or CustomerData in Inject!");
        }
    }

    public void clickRoutine()
    {
        if (isExiting) return; // 광클 방지: 이미 나가는 중이면 무시

        if (isDisplaying)
        {
            isExiting = true;
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
        speechBubbleScript.PlaceNear(GetComponent<SpriteRenderer>().bounds);
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }
}