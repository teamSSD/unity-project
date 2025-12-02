using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class StatMock : MonoBehaviour
{
    private float timer1;
    private float timer2;
    private float nextTime;

    void Start()
    {
        StatsSystem.SetTime(09, 00);
        StatsSystem.ResumeTime();
        StatsSystem.SetStamina(100);
        StatsSystem.SetMoney(0);
        nextTime = Random.Range(0.3f, 3f);
        StatsSystem.RegisterBreakPoint(12, 0, () => { StatsSystem.PauseTime(); });
    }

    void Update()
    {
        timer1 += Time.deltaTime;
        if (timer1 >= 0.4f)
        {
            StatsSystem.SubStamina(1);
            timer1 -= 0.4f;
        }

        timer2 += Time.deltaTime;
        if (timer2 >= nextTime)
        {
            int reward = Random.Range(1, 10001);
            StatsSystem.AddMoney(reward);
            timer2 = 0f;
            nextTime = Random.Range(0.3f, 3f);
        }
    }
}
