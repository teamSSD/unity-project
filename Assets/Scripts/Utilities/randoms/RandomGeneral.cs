using UnityEngine;
using System.Collections.Generic;

public static class RandomGeneral
{
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
}