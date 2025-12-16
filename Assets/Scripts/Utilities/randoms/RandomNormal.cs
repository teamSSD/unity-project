using UnityEngine;

public static class RandomNormal
{
    public static float Get(float mean, float stdDev)
    {
        float u1 = 1.0f - Random.value; 
        float u2 = Random.value;

        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);

        return mean + stdDev * randStdNormal;
    }
}