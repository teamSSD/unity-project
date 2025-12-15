using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class ScanColliderUtil : MonoBehaviour
{
    [Header("Scan Settings")]
    [SerializeField] private ContactFilter2D overlapFilter;

    private Collider2D selfCollider;
    private List<Collider2D> bufferList = new List<Collider2D>(16);

    private void Awake()
    {
        selfCollider = GetComponent<Collider2D>();
    }

    /* null이 반환될 수 있음 */
    public T GetOverlappingWithComponent<T>() where T : Component
    {
        if (selfCollider == null) selfCollider = GetComponent<Collider2D>();
        bufferList.Clear();
        selfCollider.GetContacts(overlapFilter, bufferList);
        for (int i = 0; i < bufferList.Count; i++)
            {
                Collider2D otherCollider = bufferList[i];
                if (otherCollider == null || otherCollider == selfCollider) continue;
                T component = otherCollider.GetComponentInParent<T>();
                if (component != null) return component;
            }
            return default;
    }

    /* null이 반환될 수 있음 */
    public GameObject GetOverlappingWithTag(string tag)
    {
        if (selfCollider == null) selfCollider = GetComponent<Collider2D>();
        bufferList.Clear();
        selfCollider.GetContacts(overlapFilter, bufferList);
        for (int i = 0; i < bufferList.Count; i++)
        {
            Collider2D otherCollider = bufferList[i];
            Debug.Log(otherCollider);
            if (otherCollider != null && otherCollider.gameObject != null)
            {
                if (otherCollider.CompareTag(tag)) return otherCollider.gameObject;
            }
        }
        return null;
    }
}