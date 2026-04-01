using UnityEngine;

public class TimeClock : MonoBehaviour
{
    private float gameTimeScale = 180f;

    private float timer = 0f;

    void Update()
    {
        if (StatsSystem.IsTimePaused()) return;

        timer += Time.deltaTime;

        float secondsPerGameMinute = 60f / gameTimeScale;

        while (timer >= secondsPerGameMinute)
        {
            StatsSystem.AddTime(0, 1);
            timer -= secondsPerGameMinute;
        }
    }
}
