using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 물리 콜라이더를 스캔하여 지정된 컴포넌트나 태그를 가진 객체를 탐색합니다.
/// </summary>
public class ScanColliderUtil : MonoBehaviour
{
    private readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);
    private Collider2D _selfCollider;
    private ContactFilter2D _overlapFilter;

    private void Awake()
    {
        _overlapFilter = ContactFilter2D.noFilter;
        _overlapFilter.useTriggers = true;
    }

    /// <summary>
    /// 지정된 컴포넌트 <typeparamref name="T"/>를 가진 가장 가까운 객체를 반환합니다.
    /// </summary>
    public T GetOverlappingWithComponent<T>() where T : Component
    {
        var candidates = GetValidOverlappingColliders();
        T nearest = default;
        float minDistance = float.MaxValue;

        foreach (var other in candidates)
        {
            if (other.TryGetComponent(out T component))
                UpdateNearest(component, other, ref nearest, ref minDistance);
        }
        return nearest;
    }

    /// <summary>
    /// 지정된 태그를 가진 가장 가까운 GameObject를 반환합니다.
    /// </summary>
    public GameObject GetOverlappingWithTag(string tag)
    {
        var candidates = GetValidOverlappingColliders();
        GameObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (var other in candidates)
        {
            if (other.CompareTag(tag))
                UpdateNearest(other.gameObject, other, ref nearest, ref minDistance);
        }
        return nearest;
    }

    private List<Collider2D> GetValidOverlappingColliders()
    {
        _selfCollider ??= GetComponent<Collider2D>();
        _overlapBuffer.Clear();
        
        _selfCollider.Overlap(_overlapFilter, _overlapBuffer);
        _overlapBuffer.RemoveAll(c => c == null || c == _selfCollider);

        return _overlapBuffer;
    }

    private void UpdateNearest<TResult>(TResult current, Collider2D other, ref TResult nearest, ref float minDist)
    {
        float dist = Vector2.Distance(transform.position, other.transform.position);
        if (dist < minDist)
        {
            minDist = dist;
            nearest = current;
        }
    }
}