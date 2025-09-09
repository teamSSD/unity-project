using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cooker : MonoBehaviour
{
    public CookerData cookerData;
    private ClickStateUtil clickStateUtil;

    void Start()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
    }

    void Update()
    {
        
    }
}
