using UnityEngine;

/// <summary>
/// Upper shelf storage with horizontal linear layout
/// Items are arranged in a single horizontal line at fixed Y position
/// </summary>
public class UpperShelf : BaseStorage
{
    private void Awake() => capacity = 3;

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
}
