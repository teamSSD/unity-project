using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 런타임 스프라이트/텍스처 진단. Managers 씬에 붙여두면 자동으로 씬 전환 + 주기 스캔.
/// - 씬 진입 시 SpriteRenderer 전수 스냅샷 (개수/누락/카메라 가시성).
/// - 주기 스캔: null sprite, null material, 활성인데 안 보이는 렌더러 검출.
/// - 재현 시 콘솔 로그(GameObject 경로)로 어디 스프라이트가 사라졌는지 즉시 확인 가능.
/// </summary>
public class TextureDiagnostics : MonoBehaviour
{
    [Header("Scan Options")]
    [SerializeField, Tooltip("주기 스캔 간격 (초). 0 이하면 주기 스캔 안 함.")]
    private float scanIntervalSec = 3f;

    [SerializeField, Tooltip("씬 진입 후 얼마 지나서 첫 스캔할지 (초). 로딩 완료 대기용.")]
    private float sceneLoadDelaySec = 1f;

    [SerializeField, Tooltip("씬 진입 직후 전체 개수/누락 요약 로그 남길지.")]
    private bool logOnSceneLoad = true;

    [Header("Detection")]
    [SerializeField, Tooltip("메인 카메라 frustum 안인데 SpriteRenderer.isVisible=false 인 것도 로그.")]
    private bool detectInvisibleInFrustum = true;

    private float _timer;
    private bool _pendingFirstScan;
    private float _firstScanAt;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _pendingFirstScan = true;
        _firstScanAt = Time.unscaledTime + sceneLoadDelaySec;
        if (logOnSceneLoad)
        {
            // 다음 프레임 정도에 요약 (씬 완전 활성 대기)
            _timer = scanIntervalSec; // 곧바로 첫 주기 스캔 트리거
        }
    }

    private void Update()
    {
        if (_pendingFirstScan && Time.unscaledTime >= _firstScanAt)
        {
            _pendingFirstScan = false;
            Scan(initial: true);
            _timer = 0f;
            return;
        }
        if (scanIntervalSec <= 0f) return;
        _timer += Time.unscaledDeltaTime;
        if (_timer < scanIntervalSec) return;
        _timer = 0f;
        Scan(initial: false);
    }

    private void Scan(bool initial)
    {
        var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int total = renderers.Length;
        int nullSprite = 0;
        int nullMaterial = 0;
        int disabledRenderer = 0;
        int invisibleInFrustum = 0;
        var anomalies = new List<string>();
        var cam = Camera.main;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            if (!r.gameObject.activeInHierarchy) continue;

            if (!r.enabled) { disabledRenderer++; continue; }

            if (r.sprite == null)
            {
                nullSprite++;
                anomalies.Add($"  NULL_SPRITE  {GetPath(r.gameObject)}");
                continue;
            }
            if (r.sharedMaterial == null)
            {
                nullMaterial++;
                anomalies.Add($"  NULL_MATERIAL {GetPath(r.gameObject)}");
                continue;
            }

            if (detectInvisibleInFrustum && cam != null && IsInFrustum(cam, r) && !r.isVisible)
            {
                invisibleInFrustum++;
                anomalies.Add($"  INVISIBLE_IN_FRUSTUM {GetPath(r.gameObject)} sortLayer={r.sortingLayerName} order={r.sortingOrder}");
            }
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (initial || anomalies.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[TextureDiag] scene={sceneName} total={total} " +
                          $"nullSprite={nullSprite} nullMat={nullMaterial} " +
                          $"disabled={disabledRenderer} invisibleInFrustum={invisibleInFrustum}");
            if (anomalies.Count > 0)
            {
                sb.AppendLine("이상 감지:");
                foreach (var a in anomalies) sb.AppendLine(a);
                Debug.LogWarning(sb.ToString());
            }
            else
            {
                Debug.Log(sb.ToString());
            }
        }
    }

    private static bool IsInFrustum(Camera cam, SpriteRenderer r)
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, r.bounds);
    }

    private static string GetPath(GameObject go)
    {
        var t = go.transform;
        string path = go.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return $"{go.scene.name}::{path}";
    }
}
