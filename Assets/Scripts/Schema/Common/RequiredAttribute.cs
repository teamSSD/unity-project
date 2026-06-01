using System;

/// <summary>
/// SerializeField가 null이면 OnValidate 시 경고. RequiredFieldValidator와 짝.
/// 사용: [Required] [SerializeField] private GameObject prefab;
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class RequiredAttribute : Attribute { }
