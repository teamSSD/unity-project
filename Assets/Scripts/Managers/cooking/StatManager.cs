using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CustomerManager))]
public class StatManager : MonoBehaviour
{
    public event Action onTimeEnd = () => {};

    [Header("Time Settings")]
    [SerializeField] private int startHour = 11;
    [SerializeField] private int startMinute = 0;
    [SerializeField] private int endHour = 15;
    [SerializeField] private int endMinute = 0;
    [SerializeField] private float timeAdvanceInterval = 1f; // Real seconds per game minute

    [Header("Audio")]
    [SerializeField] private AudioClip tickingSfx;

    private CustomerManager customerManager;
    private float time = 0;
    private bool isPaused = false;

    void OnEnable()
    {
        StatsSystem.Initialize();

        customerManager = this.GetComponent<CustomerManager>();

        StatsSystem.OnStaminaExhausted += OnStaminaExhausted;

        StatsSystem.SetTime(startHour, startMinute);
        StatsSystem.RegisterBreakPoint(endHour, endMinute, OnTimeEnd);
    }

    void OnDisable()
    {
        StatsSystem.OnStaminaExhausted -= OnStaminaExhausted;
    }

    void Update()
    {
        if (isPaused) return;

        time += Time.deltaTime;
        while (time >= timeAdvanceInterval)
        {
            time -= timeAdvanceInterval;
            StatsSystem.AddTime(0, 1);
            if (tickingSfx != null)
            {
                SoundManager.Instance.Play2DSFX(tickingSfx, 0.5f);
            }
        }
    }

    private void OnStaminaExhausted()
    {
        Debug.Log("[StatManager] Stamina exhausted");
    }

    private void OnTimeEnd()
    {
        Debug.Log("[StatManager] Time ended");
        onTimeEnd.Invoke();
    }

    /// <summary>
    /// Pause time progression
    /// </summary>
    public void PauseTime()
    {
        isPaused = true;
        Debug.Log("[StatManager] Time paused");
    }

    /// <summary>
    /// Resume time progression
    /// </summary>
    public void ResumeTime()
    {
        isPaused = false;
        Debug.Log("[StatManager] Time resumed");
    }

    /// <summary>
    /// Check if time is currently paused
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
}
