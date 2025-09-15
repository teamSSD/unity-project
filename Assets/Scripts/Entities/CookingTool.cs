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

        clickStateUtil.OnClicked += ClickRoutine;
    }

    private void ClickRoutine() {
        Debug.Log("호건이가~ 좋아하는~ 랜더어엄~ 게임!");
    }
}
