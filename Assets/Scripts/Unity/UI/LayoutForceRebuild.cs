using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class LayoutForceRebuild : MonoBehaviour
{
    void OnEnable()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && gameObject != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        };
    }
#endif
}
