using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(ClickStateUtil))]
public class CookingTool : MonoBehaviour
{
    public CookingToolData cookingToolData;
    ClickStateUtil clickStateUtil;
    void Start()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
    }

    void Update()
    {
        if (clickStateUtil.GetClickState() == ClickState.ClickStart)
        {
            Debug.Log("호건이가~ 좋아하는~ 랜더어엄~ 게임!");
        }
    }
}
