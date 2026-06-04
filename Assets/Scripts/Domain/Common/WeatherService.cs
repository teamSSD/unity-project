using UnityEngine;

namespace Game.Domain.Common
{
    /// <summary>
    /// 일별 날씨 결정 서비스 (POCO). WeatherSystem facade 후속.
    /// 시드 기반 결정적 랜덤 (Day 단위 재현 가능). 나쁜 날씨 확률 40%.
    /// </summary>
    public class WeatherService
    {
        private const float BadWeatherChance = 0.4f;

        public bool IsBadWeather { get; private set; }

        public void UpdateWeather(int day)
        {
            GameRandom.InitDay(day);
            IsBadWeather = GameRandom.Value(GameRandom.Immutable) < BadWeatherChance;
            Debug.Log($"[WeatherService] Day {day}: {(IsBadWeather ? "Bad" : "Good")} weather");
        }
    }
}
