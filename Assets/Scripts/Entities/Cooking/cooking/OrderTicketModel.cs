using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FoodBehavior))]
[RequireComponent(typeof(ScanColliderUtil))]
[RequireComponent(typeof(ClickStateUtil))]
[DisallowMultipleComponent]
public class OrderTicketModel : MonoBehaviour
{
    public FoodBehavior BehaviorInstance { get; private set; }
    public event Action<OrderTicketModel> onDestroy;
    ScanColliderUtil scanColliderUtil;
    ClickStateUtil clickStateUtil;
    void Awake()
    {
        BehaviorInstance = GetComponent<FoodBehavior>();
        scanColliderUtil = GetComponent<ScanColliderUtil>();
        clickStateUtil = GetComponent<ClickStateUtil>();

        clickStateUtil.OnDragEnd += AddToBento;
    }

    void OnDestroy()
    {
        onDestroy?.Invoke(this);
        clickStateUtil.OnDragEnd -= AddToBento;
    }
    public void AddToBento()
    {
        BentoModel collision = scanColliderUtil.GetOverlappingWithComponent<BentoModel>();
        if (collision != null)
        {
            Destroy(this.gameObject);
            Destroy(collision.gameObject);
        }
    }
}
