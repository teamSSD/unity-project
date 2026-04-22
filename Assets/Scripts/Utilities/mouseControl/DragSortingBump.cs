using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
public class DragSortingBump : MonoBehaviour
{
    [Tooltip("드래그 중 사용할 소팅 레이어 이름")]
    public string dragSortingLayer = "UI";
    [Tooltip("드래그 중 사용할 소팅 오더")]
    public int dragSortingOrder = 999;

    private ClickStateUtil clickStateUtil;
    private SpriteRenderer[] renderers;
    private int[] originalLayerIDs;
    private int[] originalOrders;

    private void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
    }

    private void OnEnable()
    {
        clickStateUtil.OnDragStart += OnDragStart;
        clickStateUtil.OnDragEnd += OnDragEnd;
    }

    private void OnDisable()
    {
        clickStateUtil.OnDragStart -= OnDragStart;
        clickStateUtil.OnDragEnd -= OnDragEnd;
    }

    private void OnDragStart()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalLayerIDs = new int[renderers.Length];
        originalOrders = new int[renderers.Length];

        int layerID = SortingLayer.NameToID(dragSortingLayer);
        for (int i = 0; i < renderers.Length; i++)
        {
            originalLayerIDs[i] = renderers[i].sortingLayerID;
            originalOrders[i] = renderers[i].sortingOrder;
            renderers[i].sortingLayerID = layerID;
            renderers[i].sortingOrder = dragSortingOrder + i;
        }
    }

    private void OnDragEnd()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerID = originalLayerIDs[i];
            renderers[i].sortingOrder = originalOrders[i];
        }
    }
}