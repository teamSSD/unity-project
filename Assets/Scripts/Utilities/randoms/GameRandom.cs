using System;
using UnityEngine;

/// <summary>
/// 게임 전역 시드 기반 랜덤.
/// Immutable: 같은 날 = 같은 결과 (상점 상품, 작물, 날씨 등)
/// Variable: 매 세션 다르지만 시드로 재현 가능 (손님, 대화, 미니게임 등)
/// </summary>
public static class GameRandom
{
    private const int BaseSeed = 7919;

    /// <summary>불변 시드 — 세이브별 고유. 같은 세이브 + 같은 날 = 같은 결과.</summary>
    public static System.Random Immutable { get; private set; }

    /// <summary>가변 시드 — 매 세션 다르지만 시드로 재현 가능.</summary>
    public static System.Random Variable { get; private set; }

    public static int CurrentDay { get; private set; } = -1;
    public static int ImmutableSeed { get; private set; }
    public static int SessionSeed { get; private set; }

    /// <summary>
    /// 게임 시작 시 1회 호출.
    /// immutableSeed: 세이브에 저장됨 (새 게임 시 생성, 이어하기 시 복원)
    /// sessionSeed: 매 플레이마다 달라도 됨
    /// </summary>
    public static void InitSession(int immutableSeed, int sessionSeed)
    {
        ImmutableSeed = immutableSeed;
        SessionSeed = sessionSeed;
    }

    /// <summary>
    /// 매 PassDay 또는 씬 진입 시 호출.
    /// </summary>
    public static void InitDay(int day)
    {
        CurrentDay = day;
        Immutable = new System.Random(day * BaseSeed + ImmutableSeed);
        Variable = new System.Random(day * BaseSeed + SessionSeed);
    }

    // ── 유틸리티: System.Random 확장 ──

    public static int Range(System.Random rng, int min, int maxExclusive)
    {
        return rng.Next(min, maxExclusive);
    }

    public static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    public static float Value(System.Random rng)
    {
        return (float)rng.NextDouble();
    }

    /// <summary>정규분포 (Box-Muller)</summary>
    public static float Normal(System.Random rng, float mean, float stdDev)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = rng.NextDouble();
        double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * (float)z;
    }

    /// <summary>정규분포 범위 클램프</summary>
    public static float NormalRange(System.Random rng, float min, float max)
    {
        float mean = (min + max) / 2f;
        float stdDev = (max - min) / 6f;
        return Mathf.Clamp(Normal(rng, mean, stdDev), min, max);
    }

    /// <summary>리스트에서 1개 뽑기</summary>
    public static T Pick<T>(System.Random rng, System.Collections.Generic.List<T> list)
    {
        if (list == null || list.Count == 0) return default;
        return list[rng.Next(list.Count)];
    }

    /// <summary>리스트에서 n개 뽑기 (Fisher-Yates)</summary>
    public static System.Collections.Generic.List<T> Pick<T>(System.Random rng, System.Collections.Generic.List<T> list, int n)
    {
        if (list == null || list.Count == 0 || n <= 0)
            return new System.Collections.Generic.List<T>();

        int count = Mathf.Min(n, list.Count);
        var copy = new System.Collections.Generic.List<T>(list);
        for (int i = 0; i < count; i++)
        {
            int j = rng.Next(i, copy.Count);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy.GetRange(0, count);
    }

    /// <summary>가중치 기반 선택</summary>
    public static T WeightedPick<T>(System.Random rng, System.Collections.Generic.List<T> list, System.Func<T, float> getWeight)
    {
        if (list == null || list.Count == 0) return default;

        float total = 0f;
        foreach (var item in list) total += getWeight(item);

        float r = Range(rng, 0f, total);
        float cumulative = 0f;
        foreach (var item in list)
        {
            cumulative += getWeight(item);
            if (r <= cumulative) return item;
        }
        return list[list.Count - 1];
    }
}
