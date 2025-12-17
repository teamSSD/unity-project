using UnityEngine;
using System.Collections.Generic;
using System;

public class WaitingCustomer : MonoBehaviour
{
    public GameObject TakingCustomerPrefab;
    public GameObject Receipt;
    public bool stopTimer = false;
    private float timer = 0;
    private float timeLimit = 180;

    void Update()
    {
        if (!stopTimer) timer += Time.deltaTime;
        if (timer > timeLimit) OnExit();
    }

    void OnExit()
    {
        Destroy(Receipt);
        GameObject generated = Instantiate(TakingCustomerPrefab);
        generated.GetComponent<TakingCustomer>().exit();

        Destroy(this);
    }
}