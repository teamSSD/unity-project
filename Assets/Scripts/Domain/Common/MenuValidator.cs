using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Validates if bento contents match customer order
/// Provides scoring and feedback for order accuracy
/// </summary>
public static class MenuValidator
{
    /// <summary>
    /// Validation result for an order
    /// </summary>
    public struct ValidationResult
    {
        public bool IsValid;                    // Overall validity
        public bool MainMenuCorrect;            // Main dish matches
        public int CorrectSideCount;            // Number of correct sides
        public int TotalSideCount;              // Total sides provided
        public int ExpectedSideCount;           // Expected number of sides
        public float AccuracyScore;             // 0.0 to 1.0
        public string FeedbackMessage;          // Human-readable feedback

        public ValidationResult(bool isValid, bool mainCorrect, int correctSides, int totalSides, int expectedSides, float score, string feedback)
        {
            IsValid = isValid;
            MainMenuCorrect = mainCorrect;
            CorrectSideCount = correctSides;
            TotalSideCount = totalSides;
            ExpectedSideCount = expectedSides;
            AccuracyScore = score;
            FeedbackMessage = feedback;
        }
    }

    /// <summary>
    /// Validate bento contents against menu order
    /// </summary>
    /// <param name="order">Customer's order</param>
    /// <param name="mainMenu">Main dish in bento</param>
    /// <param name="sideMenus">Side dishes in bento</param>
    /// <returns>Validation result with scoring</returns>
    public static ValidationResult Validate(MenuSchema order, FoodSchema mainMenu, List<FoodSchema> sideMenus)
    {
        if (order == null)
        {
            Debug.LogError("[MenuValidator] Order is null!");
            return new ValidationResult(false, false, 0, 0, 0, 0f, "주문 정보 없음");
        }

        if (mainMenu == null)
        {
            Debug.LogWarning("[MenuValidator] No main menu provided!");
            return new ValidationResult(false, false, 0, 0, 0, 0f, "메인 메뉴 없음");
        }

        // Check main menu
        bool mainCorrect = ValidateMainMenu(order.mainMenu, mainMenu);

        // Check side menus
        int expectedSideCount = order.sideMenus?.Count ?? 0;
        int providedSideCount = sideMenus?.Count ?? 0;
        int correctSideCount = CountCorrectSides(order.sideMenus, sideMenus);

        // Calculate accuracy score
        float score = CalculateAccuracyScore(mainCorrect, correctSideCount, expectedSideCount, providedSideCount);

        // Generate feedback
        string feedback = GenerateFeedback(mainCorrect, correctSideCount, expectedSideCount, providedSideCount);

        // Overall validity: main must be correct, and at least some sides should match
        bool isValid = mainCorrect && (expectedSideCount == 0 || correctSideCount > 0);

        return new ValidationResult(
            isValid,
            mainCorrect,
            correctSideCount,
            providedSideCount,
            expectedSideCount,
            score,
            feedback
        );
    }

    /// <summary>
    /// Validate if main menu matches order
    /// </summary>
    private static bool ValidateMainMenu(FoodData expectedMain, FoodSchema providedMain)
    {
        if (expectedMain == null || providedMain == null) return false;

        return expectedMain.id == providedMain.foodData.id;
    }

    /// <summary>
    /// Count how many side menus are correct
    /// </summary>
    private static int CountCorrectSides(List<FoodData> expectedSides, List<FoodSchema> providedSides)
    {
        if (expectedSides == null || expectedSides.Count == 0) return 0;
        if (providedSides == null || providedSides.Count == 0) return 0;

        // Get IDs of expected sides
        var expectedIds = new HashSet<string>(expectedSides.Select(f => f.id));

        // Count how many provided sides match expected
        int correctCount = 0;
        foreach (var provided in providedSides)
        {
            if (provided?.foodData != null && expectedIds.Contains(provided.foodData.id))
            {
                correctCount++;
            }
        }

        return correctCount;
    }

    /// <summary>
    /// Calculate accuracy score (0.0 to 1.0)
    /// Main menu worth 60%, sides worth 40%
    /// </summary>
    private static float CalculateAccuracyScore(bool mainCorrect, int correctSides, int expectedSides, int providedSides)
    {
        float mainScore = mainCorrect ? 0.6f : 0.0f;

        float sideScore = 0.0f;
        if (expectedSides > 0)
        {
            // Perfect match: all expected sides present, no extra
            if (correctSides == expectedSides && providedSides == expectedSides)
            {
                sideScore = 0.4f;
            }
            // Partial match: some correct, some missing or extra
            else
            {
                float correctRatio = (float)correctSides / expectedSides;
                float extraPenalty = Mathf.Max(0, providedSides - expectedSides) * 0.1f;
                sideScore = Mathf.Clamp01(correctRatio * 0.4f - extraPenalty);
            }
        }
        else
        {
            // No sides expected
            if (providedSides == 0)
            {
                sideScore = 0.4f; // Perfect
            }
            else
            {
                sideScore = Mathf.Max(0, 0.4f - providedSides * 0.1f); // Penalty for extra items
            }
        }

        return Mathf.Clamp01(mainScore + sideScore);
    }

    /// <summary>
    /// Generate human-readable feedback message
    /// </summary>
    private static string GenerateFeedback(bool mainCorrect, int correctSides, int expectedSides, int providedSides)
    {
        if (!mainCorrect) return "메인 메뉴가 틀렸습니다!";
        if (expectedSides == 0) return FeedbackForNoExpectedSides(providedSides);
        return FeedbackForSideMatching(correctSides, expectedSides, providedSides);
    }

    private static string FeedbackForNoExpectedSides(int providedSides)
    {
        return providedSides == 0
            ? "완벽합니다!"
            : $"사이드가 {providedSides}개 더 들어있습니다.";
    }

    private static string FeedbackForSideMatching(int correctSides, int expectedSides, int providedSides)
    {
        if (correctSides == expectedSides && providedSides == expectedSides) return "완벽합니다!";
        if (correctSides == expectedSides) return $"정확하지만 {providedSides - expectedSides}개가 더 들어있습니다.";
        if (correctSides == 0) return "사이드 메뉴가 모두 틀렸습니다!";

        int missing = expectedSides - correctSides;
        int extra = Mathf.Max(0, providedSides - correctSides);
        return extra > 0
            ? $"사이드 {missing}개 부족, {extra}개 잘못됨"
            : $"사이드 {missing}개 부족";
    }

    /// <summary>
    /// Get grade based on accuracy score
    /// </summary>
    public static string GetGrade(float accuracyScore)
    {
        if (accuracyScore >= 1.0f) return "S";
        if (accuracyScore >= 0.9f) return "A";
        if (accuracyScore >= 0.8f) return "B";
        if (accuracyScore >= 0.7f) return "C";
        if (accuracyScore >= 0.6f) return "D";
        return "F";
    }

    /// <summary>
    /// 영업 보상 (기획 공식: Aftertaste_Book.md):
    ///   보상 = 총 가격 × 메인 배율 × 사이드 배율
    ///   메인 배율: 일치 1.0 / 불일치 0.7
    ///   사이드 배율: 1.0 + (일치 사이드 × 0.15)  ← 2026-07 튜닝: 5% → 15% (sim 기반)
    /// 총 가격 = 제공된 메인 + 사이드 가격 단순 합 (개별 페널티 없음).
    /// </summary>
    public static int CalculateReward(MenuSchema order, FoodSchema providedMain, List<FoodSchema> providedSides)
    {
        if (order == null || providedMain == null) return 0;

        // 총 가격 = 메인 + 사이드 합 (개별 가격에 페널티 적용 X)
        float totalPrice = providedMain.Price;

        int matchingSidesCount = 0;
        var expectedSideIds = new HashSet<string>(order.sideMenus.Select(s => s.id));

        if (providedSides != null)
        {
            foreach (var side in providedSides)
            {
                if (side == null || side.foodData == null) continue;
                if (expectedSideIds.Contains(side.foodData.id)) matchingSidesCount++;
                totalPrice += side.Price;
            }
        }

        bool mainMatches = order.mainMenu.id == providedMain.foodData.id;
        float mainMultiplier = mainMatches ? 1.0f : 0.7f;
        float sideMultiplier = 1.0f + (matchingSidesCount * 0.15f);

        return Mathf.RoundToInt(totalPrice * mainMultiplier * sideMultiplier);
    }
}
