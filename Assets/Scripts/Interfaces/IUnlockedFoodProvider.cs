using System.Collections.Generic;

/// <summary>
/// 해금된 음식 데이터를 제공하는 인터페이스
/// DiaryFlowManager가 구현하여 MenuToggleList와 BentoToggleList에 주입
/// </summary>
public interface IUnlockedFoodProvider
{
    /// <summary>
    /// 해금된 메인 메뉴 목록
    /// </summary>
    List<FoodData> GetUnlockedMainFoods();

    /// <summary>
    /// 해금된 사이드 메뉴 목록
    /// </summary>
    List<FoodData> GetUnlockedSideFoods();

    /// <summary>
    /// 특정 음식이 해금되었는지 확인
    /// </summary>
    bool IsUnlocked(string foodId);
}
