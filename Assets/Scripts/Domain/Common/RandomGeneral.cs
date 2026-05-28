using UnityEngine;
using System.Collections.Generic;
using System;

public static class RandomGeneral
{
    private static System.Random rand = new System.Random();
    
    public static T Pick<T>(List<T> list)
    {
        if (list == null || list.Count == 0) 
        {
            Debug.LogWarning("리스트가 비어있어 랜덤 추출에 실패했습니다.");
            return default;
        }

        int randomIndex = UnityEngine.Random.Range(0, list.Count);
        
        return list[randomIndex];
    }

    // 대충 Gemini로 코드 짜둠, 나중에 수정하셔도 되요 정상 작동하면 주석 지워주시구요
    public static List<T> Pick<T>(List<T> list, int n)
    {
        if (list == null || list.Count == 0 || n <= 0)
        {
            Debug.LogWarning("리스트가 비어있거나 유효하지 않은 개수(n)입니다.");
            return new List<T>();
        }

        int countToPick = Mathf.Min(n, list.Count);
        
        List<T> copyList = new List<T>(list);

        for (int i = 0; i < countToPick; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, copyList.Count);
            
            T temp = copyList[i];
            copyList[i] = copyList[randomIndex];
            copyList[randomIndex] = temp;
        }

        return copyList.GetRange(0, countToPick);
    }

    // 대충 Gemini로 코드 짜둠, 나중에 수정하셔도 되요 정상 작동하면 주석 지워주시구요
    public static float getRandomNormal(float minVal, float maxVal)
    {
        double mean = (minVal + maxVal) / 2.0;
        double stdDev = (maxVal - minVal) / 6.0;

        double u1 = 1.0 - rand.NextDouble(); 
        double u2 = 1.0 - rand.NextDouble();
        
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        double val = mean + stdDev * randStdNormal;

        return UnityEngine.Mathf.Clamp((float)val, minVal, maxVal);
    }
}