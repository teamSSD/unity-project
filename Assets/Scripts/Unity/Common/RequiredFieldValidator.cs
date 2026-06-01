#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;

/// <summary>
/// MonoBehaviour.OnValidate에서 호출. [Required] + SerializeField 필드가 null이면 경고.
/// 표준 패턴:
///   private void OnValidate() => RequiredFieldValidator.Validate(this);
/// </summary>
public static class RequiredFieldValidator
{
    private const BindingFlags FieldFlags =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

    public static void Validate(MonoBehaviour mb)
    {
        if (mb == null || Application.isPlaying) return;
        var type = mb.GetType();
        while (type != null && type != typeof(MonoBehaviour))
        {
            foreach (var f in type.GetFields(FieldFlags))
            {
                if (f.GetCustomAttribute<RequiredAttribute>() == null) continue;
                object value = f.GetValue(mb);
                bool isNull = value == null || (value is Object obj && obj == null);
                if (isNull)
                    Debug.LogWarning($"[{type.Name}] {f.Name} not assigned (Required)", mb);
            }
            type = type.BaseType;
        }
    }
}
#endif
