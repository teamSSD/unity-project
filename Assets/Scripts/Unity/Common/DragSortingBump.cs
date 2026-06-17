using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(ClickStateUtil))]
public class DragSortingBump : MonoBehaviour
{
    [Tooltip("드래그 중 사용할 소팅 레이어 이름")]
    public string dragSortingLayer = "UI";
    [Tooltip("드래그 중 사용할 소팅 오더")]
    public int dragSortingOrder = 999;

    private ClickStateUtil clickStateUtil;

    // SortingGroup 경로 — 루트에 SortingGroup이 있으면 그것만 bump (자식 Sprite/Mesh 모두 묶임)
    private SortingGroup sortingGroup;
    private int origGroupLayerID;
    private int origGroupOrder;

    // Fallback — SortingGroup 없을 때 자식 SpriteRenderer 직접 bump (TMP MeshRenderer는 못 잡으니 SortingGroup 권장)
    private SpriteRenderer[] renderers;
    private int[] origRendererLayerIDs;
    private int[] origRendererOrders;

    private void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();
        sortingGroup = GetComponent<SortingGroup>();
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
        int layerID = SortingLayer.NameToID(dragSortingLayer);

        if (sortingGroup != null)
        {
            origGroupLayerID = sortingGroup.sortingLayerID;
            origGroupOrder = sortingGroup.sortingOrder;
            sortingGroup.sortingLayerID = layerID;
            sortingGroup.sortingOrder = dragSortingOrder;
            return;
        }

        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        origRendererLayerIDs = new int[renderers.Length];
        origRendererOrders = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            origRendererLayerIDs[i] = renderers[i].sortingLayerID;
            origRendererOrders[i] = renderers[i].sortingOrder;
            renderers[i].sortingLayerID = layerID;
            renderers[i].sortingOrder = dragSortingOrder + i;
        }
    }

    private void OnDragEnd()
    {
        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerID = origGroupLayerID;
            sortingGroup.sortingOrder = origGroupOrder;
            return;
        }

        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerID = origRendererLayerIDs[i];
            renderers[i].sortingOrder = origRendererOrders[i];
        }
    }
}
