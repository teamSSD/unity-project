using UnityEngine;

/// <summary>
/// 일별 날씨 시스템. 시드 기반 결정적 랜덤으로 재현 가능.
/// 나쁜 날씨 확률 40%.
/// </summary>
public class WeatherSystem : SingletonMonoBehaviour<WeatherSystem>
{
    private const float BadWeatherChance = 0.4f;
    private const int BaseSeed = 7919;

    public bool IsBadWeather { get; private set; }

    public void UpdateWeather(int day)
    {
        var rng = new System.Random(day * BaseSeed + 42);
        IsBadWeather = rng.NextDouble() < BadWeatherChance;
        Debug.Log($"[WeatherSystem] Day {day}: {(IsBadWeather ? "Bad" : "Good")} weather");
    }
}
