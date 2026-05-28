using UnityEngine;

public class SliceScorer
{
    private float[,] _scoreMatrix;
    private int _totalSlices;
    private int _segments;

    public SliceScorer(int slices, int segments) {
        _totalSlices = slices;
        _segments = segments;
        _scoreMatrix = new float[slices, segments];
    }

    public void RecordSegment(int sliceIdx, int segmentIdx, float currentX, float targetX, float tolerance) {
        float xDiff = Mathf.Abs(currentX - targetX);
        float offsetDiff = Mathf.Max(0, xDiff - tolerance);
        _scoreMatrix[sliceIdx, segmentIdx] = Mathf.Clamp01(1f - (offsetDiff / tolerance));
    }

    public float GetFinalScore() {
        float totalAcc = 0f;
        foreach (var score in _scoreMatrix) totalAcc += score;
        return totalAcc / (_totalSlices * _segments);
    }
}