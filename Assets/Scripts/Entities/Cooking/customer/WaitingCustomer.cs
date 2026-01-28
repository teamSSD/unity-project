using UnityEngine;
using System.Collections.Generic;
using System;

public class WaitingCustomer : MonoBehaviour
{
    [SerializeField] private GameObject gaugePrefab;
    public event Action onExit = () => {};
    public bool stopTimer = false;
    private float timer = 0;
    private float timeLimit = 180; // 180
    private GameObject gaugeUI;
    private GaugeUI guageScript;
    private bool injected = false;

    public void inject(Canvas worldCanvas)
    {
        Vector3 offset = new Vector3(0, 2.3f, 0);

        gaugeUI.transform.SetParent(worldCanvas.transform);
        gaugeUI.transform.position = this.gameObject.transform.position + offset;
        injected = true;
    }

    void Awake()
    {
        gaugeUI = Instantiate(gaugePrefab);
        guageScript = gaugeUI.GetComponent<GaugeUI>();
    }

    void Update()
    {
        if (!injected)
        {
            Debug.Log("WaitingCustomer didn't injected.");
        }
        guageScript.SetProgress(timer, timeLimit);
        if (!stopTimer) timer += Time.deltaTime;
        if (timer > timeLimit)
        {
            OnExit();
            stopTimer = true;
            timeLimit = timer + 1;
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
        Destroy(gaugeUI);
    }
}