/// <summary>
/// BentoToggleList 인터페이스
/// RecipeDataManager를 통한 메뉴 선택 관리
/// </summary>
public interface IBentoToggle
{
    /// <summary>
    /// 메뉴에 음식 추가
    /// </summary>
    void AddFood(FoodData food);

    /// <summary>
    /// 메뉴에서 음식 제거
    /// </summary>
    void RemoveFood(FoodData food);

    /// <summary>
    /// 초기화 (RecipeDataManager는 Singleton이므로 별도 주입 불필요)
    /// </summary>
    void Initialize();
}
