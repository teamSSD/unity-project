using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class WaitingCustomer : MonoBehaviour
{
    [SerializeField] private GameObject gaugePrefab;
    public event Action onExit = () => {};
    private int managerTimerId = -1;
    private GameObject gaugeUI;
    private GaugeUI guageScript;
    private bool injected = false;

    public void inject(Canvas worldCanvas, CustomerData customerData, float timerScale = 1f, float timerCanvasY = 0f)
    {
        // Awake에서 world-instantiate된 gauge를 폐기하고 canvas 자식으로 재생성.
        // 그래야 RectTransform이 canvas 안에서 정상 layout됨.
        if (gaugeUI != null) Destroy(gaugeUI);
        gaugeUI = Instantiate(gaugePrefab, worldCanvas.transform);
        guageScript = gaugeUI.GetComponent<GaugeUI>();

        var timerRt = (RectTransform)gaugeUI.transform;
        timerRt.localScale = new Vector3(timerScale, timerScale, 1f);

        // npc world x → canvas anchoredPosition x로 매핑, y는 timerCanvasY 고정.
        var camera = worldCanvas.worldCamera;
        var canvasRt = (RectTransform)worldCanvas.transform;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(camera, transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screenPos, camera, out Vector2 canvasPos);
        timerRt.anchoredPosition = new Vector2(canvasPos.x, timerCanvasY);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
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
        
        injected = true;
    }

    void Awake()
    {
        gaugeUI = Instantiate(gaugePrefab);
        guageScript = gaugeUI.GetComponent<GaugeUI>();
    }

    void Start()
    {
        var time = TimeManager.Instance;
        if (time != null)
        {
            managerTimerId = time.StartCustomerTimer(
                onTick: (elapsed, duration) => {
                    if (guageScript != null)
                        guageScript.SetProgress(elapsed, duration);
                },
                onComplete: () => {
                    OnExit();
                }
            );
        }
    }

    public void StopTimer()
    {
        if (managerTimerId != -1 && TimeManager.Instance != null)
        {
            TimeManager.Instance.CancelTimer(managerTimerId);
            managerTimerId = -1;
        }
    }

    public void OnExit()
    {
        onExit.Invoke();
    }

    public void DestroyObject()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (managerTimerId != -1 && TimeManager.Instance != null)
        {
            TimeManager.Instance.CancelTimer(managerTimerId);
        }

        if (gaugeUI != null)
            Destroy(gaugeUI);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
