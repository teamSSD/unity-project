using NUnit.Framework;
using UnityEngine;

public class WeatherSystemTest
{
    private WeatherSystem weather;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject("WeatherSystem");
        weather = go.AddComponent<WeatherSystem>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(weather.gameObject);
    }

    [Test]
    public void UpdateWeather_Deterministic_SameDay_SameResult()
    {
        weather.UpdateWeather(42);
        bool first = weather.IsBadWeather;

        weather.UpdateWeather(42);
        bool second = weather.IsBadWeather;

        Assert.AreEqual(first, second);
    }

    [Test]
    public void UpdateWeather_DifferentDays_CanDiffer()
    {
        // 100일 돌려서 Good과 Bad가 모두 나오는지 확인
        bool sawGood = false;
        bool sawBad = false;

        for (int day = 0; day < 100; day++)
        {
            weather.UpdateWeather(day);
            if (weather.IsBadWeather) sawBad = true;
            else sawGood = true;

            if (sawGood && sawBad) break;
        }

        Assert.IsTrue(sawGood, "100일 중 좋은 날씨가 한 번도 없음");
        Assert.IsTrue(sawBad, "100일 중 나쁜 날씨가 한 번도 없음");
    }

    [Test]
    public void UpdateWeather_BadWeatherRate_Approximately40Percent()
    {
        int badCount = 0;
        int total = 1000;

        for (int day = 0; day < total; day++)
        {
            weather.UpdateWeather(day);
            if (weather.IsBadWeather) badCount++;
        }

        float rate = (float)badCount / total;
        // 40% ± 10% 허용
        Assert.Greater(rate, 0.30f, $"나쁜 날씨 비율이 너무 낮음: {rate:P1}");
        Assert.Less(rate, 0.50f, $"나쁜 날씨 비율이 너무 높음: {rate:P1}");
    }
}
