using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ClickStateUtil))]
[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public class BentoSetModel : MonoBehaviour
{
    public GameObject prefab;

    private ClickStateUtil clickStateUtil;

    private void Awake()
    {
        clickStateUtil = GetComponent<ClickStateUtil>();

        clickStateUtil.OnDragStart += SpawnBento;
    }

    private void OnDestroy()
    {
        clickStateUtil.OnDragStart -= SpawnBento;
    }

    public void SpawnBento()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        GameObject bento = Instantiate(prefab, mousePos, Quaternion.identity);

        BentoBehavior behavior = bento.GetComponent<BentoBehavior>();
        behavior.defaultPosition = mousePos;

        ClickStateUtil clickUtil = bento.GetComponent<ClickStateUtil>();
        clickUtil.ForceDragStart();
    }
}
