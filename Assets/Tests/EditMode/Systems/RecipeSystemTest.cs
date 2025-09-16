using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class RecipeSystem_RealRecipeTests
{
    // ---- 공유 인그리디언트(이름은 변수명으로 구분, 실제 비교는 id 기준) ----
    IngredientData 밥, 맨드레이크새싹, 화염장, 용의달걀;
    IngredientData 냄비밥, 채썬맨드레이크, 용달걀프라이, 비빔밥_완성;

    IngredientData 면, 은빛모래물고기;
    IngredientData 삶은면, 육수, 용달걀지단, 잔치국수_완성;

    IngredientData 밀가루, 물, 고기00, 용의꼬리풀;
    IngredientData 반죽, 다진고기00, 볶은다진고기00, 채썬용의꼬리풀, 만두, 만둣국_완성;

    RecipeSystem sys;

    // ---- 유틸: SO 생성 ----
    IngredientData Ingr(int id)
    {
        var x = ScriptableObject.CreateInstance<IngredientData>();
        x.id = id;
        return x;
    }

    RecipeData MakeRecipe(int id, IngredientData output, params IngredientData[] inputs)
    {
        var r = ScriptableObject.CreateInstance<RecipeData>();
        r.id = id;
        r.outputFood = output;
        r.inputFoods = new List<IngredientData>(inputs);
        // r.Minigame = ... // 필요시 더미 세팅
        return r;
    }

    [SetUp]
    public void SetUp()
    {
        // ---- 원재료 / 중간산물 / 완성요리 식별자 부여 ----
        // (id는 테스트 편의를 위한 고유 번호일 뿐, 의미는 변수명으로 표현)
        밥 = Ingr(10);
        맨드레이크새싹 = Ingr(11);
        화염장 = Ingr(12);
        용의달걀 = Ingr(13);
        냄비밥 = Ingr(20);
        채썬맨드레이크 = Ingr(21);
        용달걀프라이 = Ingr(22);
        비빔밥_완성 = Ingr(29);

        면 = Ingr(30);
        은빛모래물고기 = Ingr(31);
        삶은면 = Ingr(32);
        육수 = Ingr(33);             // 여러 레시피에서 같은 '육수'를 재사용
        용달걀지단 = Ingr(34);
        잔치국수_완성 = Ingr(39);

        밀가루 = Ingr(40);
        물 = Ingr(41);
        고기00 = Ingr(42);
        용의꼬리풀 = Ingr(43);
        반죽 = Ingr(44);
        다진고기00 = Ingr(45);
        볶은다진고기00 = Ingr(46);
        채썬용의꼬리풀 = Ingr(47);
        만두 = Ingr(48);
        만둣국_완성 = Ingr(49);

        // ---- 조리 단계(= 레시피) 구성 ----
        var recipes = new List<RecipeData>
        {
            // 메인메뉴 1: 맨드레이크 새싹 나물 비빔밥
            MakeRecipe(101, 냄비밥, 밥),                                 // 밥 → (냄비) → 냄비밥
            MakeRecipe(102, 채썬맨드레이크, 맨드레이크새싹),             // 맨드레이크 → (채썰기) → 채썬 맨드레이크
            MakeRecipe(103, 용달걀프라이, 용의달걀),                     // 용의 달걀 → (프라이팬) → 용 달걀 프라이
            MakeRecipe(104, 비빔밥_완성, 냄비밥, 채썬맨드레이크, 용달걀프라이, 화염장), // 중간산물 3 + 화염장 → (비빔)

            // 메인메뉴 2: 잔치국수
            MakeRecipe(201, 삶은면, 면),                                 // 면 → (냄비) → 삶은 면
            MakeRecipe(202, 육수, 은빛모래물고기),                        // 은빛 모래 물고기 → (냄비) → 육수
            MakeRecipe(203, 용달걀프라이, 용의달걀),                     // (재사용 가능) 달걀 프라이
            MakeRecipe(204, 용달걀지단, 용달걀프라이),                    // 프라이 → (썰기) → 지단
            MakeRecipe(205, 잔치국수_완성, 삶은면, 육수, 용달걀지단, 화염장), // 삶은 면 + 육수 + 지단 + 화염장 → (비빔)

            // 메인메뉴 3: 만둣국
            MakeRecipe(301, 반죽, 밀가루, 물),                            // 밀가루 + 물 → (비빔) → 반죽
            MakeRecipe(302, 다진고기00, 고기00),                          // 00고기 → (다지기) → 다진 00고기
            MakeRecipe(303, 볶은다진고기00, 다진고기00),                  // 다진 00고기 → (볶기) → 볶은 00 다진 고기
            MakeRecipe(304, 채썬용의꼬리풀, 용의꼬리풀),                  // 용의 꼬리풀 → (채썰기) → 채썬 용의 꼬리풀
            MakeRecipe(305, 육수, 은빛모래물고기),                        // 은빛 모래 물고기 → (냄비) → 육수 (동일 출력 재활용 가정)
            MakeRecipe(306, 만두, 반죽, 볶은다진고기00, 채썬용의꼬리풀),  // 반죽 + 볶은 고기 + 채썬 꼬리풀 → (찌기) → 만두
            MakeRecipe(307, 만둣국_완성, 만두, 육수)                      // 만두 + 육수 → (냄비) → 만둣국
        };

        // ---- 시스템 인스턴스 구성 ----
        var go = new GameObject("RecipeSystem_ForRealRecipes");
        sys = go.AddComponent<RecipeSystem>();
        sys.recipes = recipes;
    }

    [TearDown]
    public void TearDown()
    {
        // 간단 정리 (필수는 아님)
        Object.DestroyImmediate(sys?.gameObject);
    }

    // 1) 완전히 일치하는 레시피를 찾아내는지 (순서 무시 + 개수 고려)
    [Test]
    public void ExactMatch_Finds_FinalDish_Recipe()
    {
        // 맨드레이크 새싹 나물 비빔밥 완성 레시피: [냄비밥, 채썬맨드레이크, 용달걀프라이, 화염장]
        var sources = new List<IngredientData> { 화염장, 용달걀프라이, 채썬맨드레이크, 냄비밥 }; // 순서 뒤섞음
        var found = sys.Search(sources);

        Assert.IsNotNull(found, "정확히 일치하는 레시피를 못 찾았습니다.");
        Assert.AreEqual(104, found.id, "비빔밥 최종 레시피가 아닌 다른 레시피를 반환했습니다.");
        Assert.AreEqual(비빔밥_완성.id, found.outputFood.id, "출력 요리가 비빔밥이 아닙니다.");
    }

    // 2) 찾는 레시피가 없다면 null(=쓰레기)을 반환하는지
    [Test]
    public void NotFound_ReturnsNull_WhenNoRecipeMatches()
    {
        // 존재하지 않는 조합: [면, 맨드레이크, 화염장] 같은 건 정의하지 않음
        var sources = new List<IngredientData> { 면, 맨드레이크새싹, 화염장 };
        var found = sys.Search(sources);

        Assert.IsNull(found, "존재하지 않는 조합인데 레시피를 반환했습니다(쓰레기 아님).");
    }

    // 3) 포함(부분집합)인 경우 추천하지 않는지 → null(=쓰레기)
    [Test]
    public void Subset_ShouldReturnNull_NotRecommend_PartialInputs()
    {
        // 잔치국수 최종 레시피는 [삶은면, 육수, 용달걀지단, 화염장]
        // 그중 일부만 넣은 케이스: [삶은면, 육수]
        var sources = new List<IngredientData> { 삶은면, 육수 };
        var found = sys.Search(sources);

        Assert.IsNull(found, "부분 입력만으로 최종 레시피가 매칭되었습니다(권장 X).");
    }

    // 4) 초과(슈퍼셋)인 경우 추천하지 않는지 → null(=쓰레기)
    [Test]
    public void Superset_ShouldReturnNull_NotRecommend_ExtraInputs()
    {
        // 만둣국 최종 레시피는 [만두, 육수]
        // 초과 재료 포함: [만두, 육수, 화염장]
        var sources = new List<IngredientData> { 만두, 육수, 화염장 };
        var found = sys.Search(sources);

        Assert.IsNull(found, "초과 재료가 포함되었는데도 매칭되었습니다(권장 X).");
    }
}
