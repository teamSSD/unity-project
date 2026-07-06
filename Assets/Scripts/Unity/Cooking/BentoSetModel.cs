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

        // 스폰된 도시락을 소스(BentoSet)의 시각 크기와 일치시킴.
        // Instantiate는 부모 없이 world에 생성하므로 lossyScale을 그대로 localScale로.
        bento.transform.localScale = transform.lossyScale;

        BentoBehavior behavior = bento.GetComponent<BentoBehavior>();
        behavior.defaultPosition = mousePos;

        ClickStateUtil clickUtil = bento.GetComponent<ClickStateUtil>();
        clickUtil.ForceDragStart();
    }
}
