using UnityEngine;

public class TimeClock : MonoBehaviour
{
    private float timer = 0f;

    void Update()
    {
        if (StatsSystem.IsTimePaused()) return;

        timer += Time.deltaTime;

        if (timer >= 1f)
        {
            StatsSystem.AddTime(0, 1);
            timer -= 1f;
        }
    }
}
