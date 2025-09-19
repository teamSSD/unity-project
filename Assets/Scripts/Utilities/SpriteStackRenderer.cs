using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class SpriteStackRenderer : MonoBehaviour
{
    [Header("Root (생략 시 이 오브젝트 하위에 생성)")]
    [SerializeField] private GameObject rootGO;
    private Transform root;

    [Header("Layout")]
    [SerializeField] private Vector2 start = new Vector2(0f, 0.1f);
    [SerializeField] private Vector2 step  = new Vector2(0.08f, 0.04f);
    [SerializeField] private float iconScale = 2f;
    [SerializeField] private int baseSortingOrder = 10;

    [Header("Sorting")]
    [SerializeField] private bool inheritSortingFromThis = true;
    [SerializeField] private SpriteRenderer sortingReference;

    private void Awake()
    {
        if (rootGO != null) root = rootGO.transform;
        if (root == null)
        {
            var go = new GameObject("SpriteStackRoot");
            go.transform.SetParent(transform, false);
            root = go.transform;
        }

        if (inheritSortingFromThis && sortingReference == null)
            sortingReference = GetComponent<SpriteRenderer>();
    }

    public void Clear()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    public void DrawSingle(Sprite sprite, string name = "Icon")
    {
        Clear();
        if (sprite == null) return;
        CreateIcon(sprite, 0, name);
    }

    public void DrawMany(IEnumerable<Sprite> sprites)
    {
        Clear();
        if (sprites == null) return;

        int visibleIndex = 0;
        foreach (var s in sprites.Where(x => x != null))
        {
            CreateIcon(s, visibleIndex, $"Icon_{visibleIndex}");
            visibleIndex++;
        }
    }

    private void CreateIcon(Sprite sprite, int index, string goName)
    {
        var iconGO = new GameObject(goName);
        iconGO.transform.SetParent(root, false);

        var sr = iconGO.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        if (sortingReference != null)
        {
            sr.sortingLayerID   = sortingReference.sortingLayerID;
            sr.sortingLayerName = sortingReference.sortingLayerName;
        }
        sr.sortingOrder = baseSortingOrder + index;

        sr.transform.localPosition = new Vector3(
            start.x + step.x * index,
            start.y + step.y * index,
            -0.001f * index
        );
        sr.transform.localScale = Vector3.one * iconScale;
    }
}
