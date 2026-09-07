using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if AFTERTASTE_E2E
/// <summary>
/// 실제 WebGL 입력 테스트가 클릭할 수 있는 UI의 작은 관측 레지스트리.
/// 이 클래스는 버튼을 실행하지 않는다. 실행기는 반환된 화면 영역을 이용해
/// 브라우저의 실제 포인터 입력을 보낸다.
/// </summary>
public static class E2EUiTargetRegistry
{
    [Serializable]
    public sealed class TargetState
    {
        public string id;
        public string kind;
        public bool visible;
        public bool interactable;
        public float x;
        public float y;
        public float width;
        public float height;
    }

    private static readonly Dictionary<string, Button> Buttons = new();

    public static void Register(string id, Button button)
    {
        if (!string.IsNullOrWhiteSpace(id) && button != null) Buttons[id] = button;
    }

    public static void Unregister(string id, Button button)
    {
        if (!string.IsNullOrWhiteSpace(id) && Buttons.TryGetValue(id, out var current) && current == button)
            Buttons.Remove(id);
    }

    public static List<TargetState> Capture()
    {
        var states = new List<TargetState>();
        var stale = new List<string>();
        foreach (var (id, button) in Buttons)
        {
            if (button == null)
            {
                stale.Add(id);
                continue;
            }

            var rect = button.transform as RectTransform;
            if (rect == null) continue;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            float xMin = Mathf.Min(min.x, max.x);
            float xMax = Mathf.Max(min.x, max.x);
            float yMin = Mathf.Min(min.y, max.y);
            float yMax = Mathf.Max(min.y, max.y);

            states.Add(new TargetState
            {
                id = id,
                kind = "ui",
                visible = button.gameObject.activeInHierarchy,
                interactable = button.IsActive() && button.interactable,
                x = xMin / Screen.width,
                y = (Screen.height - yMax) / Screen.height,
                width = (xMax - xMin) / Screen.width,
                height = (yMax - yMin) / Screen.height,
            });
        }

        foreach (var id in stale) Buttons.Remove(id);
        return states;
    }
}
#endif
