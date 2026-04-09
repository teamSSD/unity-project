using UnityEngine;

/// <summary>
/// Refrigerator storage with grid layout (multiple rows)
/// Items are arranged in a grid: 3 items per row, with vertical spacing
/// </summary>
public class Refrigerator : BaseStorage
{
    private void Awake() => capacity = 7;

    [SerializeField] private float xOffset = 0f;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float xInterval = 1f;
    [SerializeField] private float yInterval = 1f;
    [SerializeField] private int linePerEntity = 3;

    protected override Vector3 CalculatePositionForIndex(int index)
    {
        return new Vector3(
            xOffset + (index % linePerEntity) * xInterval,
            yOffset + (index / linePerEntity) * yInterval,
            0f);
    }
}
