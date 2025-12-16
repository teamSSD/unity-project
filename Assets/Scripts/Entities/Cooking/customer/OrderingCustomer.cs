using System;
using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
public class OrderingCustomer : MonoBehaviour
{
    [Header("주입해야할 필드")]
    public GameObject speechBubblePrefab;
    public Canvas canvas;
    public bool readyToOrder;
    public MenuSchema menuSchema;
    public Action onExit;
    
    [Header("내부적 속성")]
    private ClickStateUtil clickStateUtil;
    private bool isDisplaying = false;
    private GameObject speechBubble;
    SpeechBubble speechBubbleScript;

    public void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        clickStateUtil.OnClicked += clickRoutine;
    }

    public void OnDestroy()
    {
        clickStateUtil.OnClicked -= clickRoutine;
        if (speechBubble != null) Destroy(speechBubble);
        onExit.Invoke();
    }

    public void Update()
    {
        if (speechBubble != null)
        {
            speechBubble.transform.position = Camera.main.WorldToScreenPoint(this.transform.position + new Vector3(-2.5f, 4, 0));
        }
    }

    public void clickRoutine()
    {
        if (!readyToOrder) return;
        if (isDisplaying)
        {
            Destroy(gameObject);
            return;
        }
        speechBubble = Instantiate(speechBubblePrefab, canvas.transform);

        speechBubbleScript = speechBubble.GetComponent<SpeechBubble>();
        speechBubbleScript.setContents("사장님, 제가 오늘 이거 먹으려고 아침부터 빌드업 해왔거든요? 고민 없이 " + menuSchema.mainMenu.ingredientName + " (으)로 직진할게요.");
        speechBubble.transform.position = Camera.main.WorldToScreenPoint(this.transform.position + new Vector3(-2.5f, 4, 0));

        isDisplaying = true;
    }
}