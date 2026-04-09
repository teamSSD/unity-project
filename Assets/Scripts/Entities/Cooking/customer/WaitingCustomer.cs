using UnityEngine;
using System.Collections.Generic;
using System;

public class WaitingCustomer : MonoBehaviour
{
    [SerializeField] private GameObject gaugePrefab;
    public event Action onExit = () => {};
    private int managerTimerId = -1;
    private GameObject gaugeUI;
    private GaugeUI guageScript;
    private bool injected = false;

    public void inject(Canvas worldCanvas, CustomerData customerData)
    {
        Vector3 offset = new Vector3(0, 2.3f, 0);

        gaugeUI.transform.SetParent(worldCanvas.transform);
        gaugeUI.transform.position = this.gameObject.transform.position + offset;

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
        if (TimeManager.Instance != null)
        {
            managerTimerId = TimeManager.Instance.StartCustomerTimer(
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
}