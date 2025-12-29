using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CustomerManager))]
public class StatManager : MonoBehaviour
{
    public event Action onTimeEnd = () => {};
    private CustomerManager customerManager;
    private float time = 0;
    private float threshold = 1.5f;
    void OnEnable()
    {
        StatsSystem.Initialize();

        customerManager = this.GetComponent<CustomerManager>();

        StatsSystem.OnStaminaExhausted += OnStaminaExhausted;

        StatsSystem.SetTime(11, 00);
        StatsSystem.RegisterBreakPoint(15, 00, OnTimeEnd);

        customerManager.isOpen = true;
    }

    void OnDisable()
    {
        StatsSystem.OnStaminaExhausted -= OnStaminaExhausted;
    }

    void Update()
    {
        time += Time.deltaTime;
        while (time >= threshold)
        {
            time -= threshold;
            StatsSystem.AddTime(0, 1);
        }
    }

    private void OnStaminaExhausted()
    {
        Debug.Log("쥬금");
    }

    private void OnTimeEnd()
    {
        customerManager.isOpen = false;
        Debug.Log("시간 다됨");
        onTimeEnd.Invoke();
    }
}
