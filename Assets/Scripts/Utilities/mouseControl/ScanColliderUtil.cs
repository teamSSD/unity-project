using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public class ScanColliderUtil : MonoBehaviour
{
    [Header("Scan Settings")]
    [SerializeField] private ContactFilter2D overlapFilter;

    private Collider2D selfCollider;
    private static readonly Collider2D[] buffer = new Collider2D[16];

    private void Awake()
    {
        selfCollider = GetComponent<Collider2D>();
    }

    /* null이 반환될 수 있음 */
    public T GetOverlappingWithComponent<T>() where T : Component
    {
        if (selfCollider == null) selfCollider = GetComponent<Collider2D>();
        int hitCount = selfCollider.Overlap(overlapFilter, buffer);
        return buffer
            .Take(hitCount)
            .Select(c => c?.GetComponentInParent<T>())
            .FirstOrDefault(t => t != null);
    }

    /* null이 반환될 수 있음 */
    public GameObject GetOverlappingWithTag(string tag)
    {
        if (selfCollider == null) selfCollider = GetComponent<Collider2D>();
        int hitCount = selfCollider.Overlap(overlapFilter, buffer);
        return buffer
            .Take(hitCount)
            .Select(c => c != null ? c.gameObject : null)
            .FirstOrDefault(go => go != null && go.CompareTag(tag));
    }
}