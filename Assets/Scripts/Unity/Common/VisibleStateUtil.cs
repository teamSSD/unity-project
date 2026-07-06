using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(SpriteRenderer))]
public class VisibleStateUtil : MonoBehaviour
{
    [Header("투명도 임계값")]
    [Range(0f, 1f)] public float minAlpha = 0.01f;
    private Camera targetCamera;

    private SpriteRenderer spriteRenderer;
    private Renderer targetRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetRenderer = GetComponent<Renderer>();
        targetCamera = Camera.main;
    }

    public bool getState()
    {
        if (!isRendererActive(spriteRenderer, targetRenderer)) return false;

        refreshCamera(targetCamera);
        if (targetCamera == null) return false;

        return isScreenInside(targetCamera.WorldToViewportPoint(transform.position));
    }

    private bool isRendererActive(SpriteRenderer sr, Renderer rend)
    {
        if (sr != null && sr.enabled && sr.color.a >= minAlpha) return true;
        if (rend != null && rend.enabled) return true;
        return false;
    }

    private bool isScreenInside(Vector3 vp)
    {
        if (vp.z <= 0f) return false;
        return vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
    }

    private Camera refreshCamera(Camera camera)
    {
        if (camera != null) return camera;
        return Camera.main;
    }
}
