using UnityEngine;

/// <summary>
/// Lower shelf storage with horizontal linear layout
/// Items are arranged in a single horizontal line at fixed Y position
/// </summary>
public class LowerShelf : BaseStorage
{
    private void Awake() => capacity = GameSessionRoot.Instance?.StorageUpgrade?.GetCurrentData("lowerShelf")?.value ?? 4;

    [SerializeField] private float xOffset = 0f;
    [SerializeField] private float yPosition = 0f;
    [SerializeField] private float xInterval = 1f;

    protected override Vector3 CalculatePositionForIndex(int index)
    {
        return new Vector3(
            xOffset + index * xInterval,
            yPosition,
            0f);
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
