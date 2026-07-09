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

    public void inject(Canvas worldCanvas, CustomerData customerData, float timerScale = 1f, float timerCanvasY = 0f, float timerCanvasXOffset = 0f)
    {
        // Awake에서 world-instantiate된 gauge를 canvas 자식으로 이전.
        // worldPositionStays=true → SetParent가 localScale을 canvas lossyScale 역수로 자동 보정
        // (SizeDelta 1×1 픽셀이지만 이 자동 scale이 곱해져 시각적으로 큼).
        // timerScale은 그 위에 곱하는 배율.
        Vector3 worldPos = transform.position;
        gaugeUI.transform.SetParent(worldCanvas.transform, worldPositionStays: true);
        gaugeUI.transform.position = worldPos; // canvas SS-Camera는 world position 자동 변환
        gaugeUI.transform.localScale *= timerScale;

        // y는 캔버스 좌표 timerCanvasY로 고정, x는 npc→canvas 자동값 + XOffset.
        var timerRt = (RectTransform)gaugeUI.transform;
        timerRt.anchoredPosition = new Vector2(timerRt.anchoredPosition.x + timerCanvasXOffset, timerCanvasY);

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
