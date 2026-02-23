/// <summary>
/// Phase 2: BentoToggleList 인터페이스
/// 싱글톤 제거를 위한 의존성 주입 인터페이스
/// </summary>
public interface IBentoToggle
{
    /// <summary>
    /// 도시락에 음식 추가
    /// </summary>
    void AddFood(FoodData food);

    /// <summary>
    /// 도시락에서 음식 제거
    /// </summary>
    void RemoveFood(FoodData food);

    /// <summary>
    /// DiaryModel 주입 및 초기화
    /// </summary>
    void Initialize(DiaryModel model);
}
