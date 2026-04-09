using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Schema 레이어 검증 테스트
/// BentoSelection, DiaryStateSnapshot의 유효성 검증 로직 테스트
/// </summary>
public class SchemaValidationTest
{
    private FoodData mockMainFood;
    private FoodData mockSideFood1;
    private FoodData mockSideFood2;

    [SetUp]
    public void Setup()
    {
        // Mock FoodData 생성
        mockMainFood = ScriptableObject.CreateInstance<FoodData>();
        mockMainFood.id = "I001";
        mockMainFood.ingredientName = "김치볶음밥";
        mockMainFood.type = FoodType.MAIN;

        mockSideFood1 = ScriptableObject.CreateInstance<FoodData>();
        mockSideFood1.id = "I002";
        mockSideFood1.ingredientName = "계란말이";
        mockSideFood1.type = FoodType.SIDE;

        mockSideFood2 = ScriptableObject.CreateInstance<FoodData>();
        mockSideFood2.id = "I003";
        mockSideFood2.ingredientName = "김";
        mockSideFood2.type = FoodType.SIDE;
    }

    [Test]
    public void BentoSelection_EmptyBento_IsValid()
    {
        // 빈 도시락도 유효함
        var bento = new BentoSelection("도시락 1", 1);
        Assert.IsTrue(bento.IsValid());
    }

    [Test]
    public void BentoSelection_OnlyMain_IsValid()
    {
        var bento = new BentoSelection("도시락 1", 1);
        bento.MainMenu = mockMainFood;
        Assert.IsTrue(bento.IsValid());
    }

    [Test]
    public void BentoSelection_MainPlusSides_IsValid()
    {
        var bento = new BentoSelection("도시락 1", 1);
        bento.MainMenu = mockMainFood;
        bento.SideMenus.Add(mockSideFood1);
        bento.SideMenus.Add(mockSideFood2);
        Assert.IsTrue(bento.IsValid());
    }

    [Test]
    public void BentoSelection_MoreThan3Sides_IsInvalid()
    {
        var bento = new BentoSelection("도시락 1", 1);
        bento.MainMenu = mockMainFood;
        bento.SideMenus.Add(mockSideFood1);
        bento.SideMenus.Add(mockSideFood2);
        bento.SideMenus.Add(mockSideFood1); // 중복 허용
        bento.SideMenus.Add(mockSideFood2); // 4개 -> 초과

        Assert.IsFalse(bento.IsValid());
    }

    [Test]
    public void BentoSelection_DuplicateAllowed_IsValid()
    {
        var bento = new BentoSelection("도시락 1", 1);
        bento.AllowDuplicates = true;
        bento.MainMenu = mockMainFood;
        bento.SideMenus.Add(mockSideFood1);
        bento.SideMenus.Add(mockSideFood1); // 중복

        Assert.IsTrue(bento.IsValid());
    }

    [Test]
    public void BentoSelection_DuplicateNotAllowed_IsInvalid()
    {
        var bento = new BentoSelection("도시락 1", 1);
        bento.AllowDuplicates = false;
        bento.MainMenu = mockMainFood;
        bento.SideMenus.Add(mockSideFood1);
        bento.SideMenus.Add(mockSideFood1); // 중복

        Assert.IsFalse(bento.IsValid());
    }

    [Test]
    public void BentoSelection_MainAndSideDuplicate_NotAllowed_IsInvalid()
    {
        var bento = new BentoSelection("도시락 1", 1);
        bento.AllowDuplicates = false;
        bento.MainMenu = mockMainFood;
        bento.SideMenus.Add(mockMainFood); // 메인과 사이드 중복

        Assert.IsFalse(bento.IsValid());
    }

    [Test]
    public void BentoSelection_HasSelection_WithMain_ReturnsTrue()
    {
        var bento = new BentoSelection("도시락 1", 1);
        bento.MainMenu = mockMainFood;

        Assert.IsTrue(bento.HasSelection());
    }

    [Test]
    public void BentoSelection_HasSelection_WithoutMain_ReturnsFalse()
    {
        var bento = new BentoSelection("도시락 1", 1);
        bento.SideMenus.Add(mockSideFood1);

        Assert.IsFalse(bento.HasSelection());
    }

    [Test]
    public void DiaryStateSnapshot_Empty_IsInvalid()
    {
        // 빈 스냅샷은 도시락 선택이 없으므로 Invalid
        var snapshot = new DiaryStateSnapshot();
        Assert.IsFalse(snapshot.IsValid()); // 최소 1개 도시락 필요
    }

    [Test]
    public void DiaryStateSnapshot_OneValidBento_IsValid()
    {
        var snapshot = new DiaryStateSnapshot();
        snapshot.BentoSelections[0].MainMenu = mockMainFood;

        Assert.IsTrue(snapshot.IsValid());
    }

    [Test]
    public void DiaryStateSnapshot_ThreeValidBentos_IsValid()
    {
        var snapshot = new DiaryStateSnapshot();
        snapshot.BentoSelections[0].MainMenu = mockMainFood;
        snapshot.BentoSelections[1].MainMenu = mockMainFood;
        snapshot.BentoSelections[2].MainMenu = mockMainFood;

        Assert.IsTrue(snapshot.IsValid());
    }

    [Test]
    public void DiaryStateSnapshot_Clone_CreatesDeepCopy()
    {
        var original = new DiaryStateSnapshot();
        original.CurrentPhase = PhaseType.Morning;
        original.BentoSelections[0].MainMenu = mockMainFood;
        original.BentoSelections[0].SideMenus.Add(mockSideFood1);

        var clone = original.Clone();

        // 값은 같음
        Assert.AreEqual(original.CurrentPhase, clone.CurrentPhase);
        Assert.AreEqual(original.BentoSelections[0].MainMenu, clone.BentoSelections[0].MainMenu);

        // 하지만 참조는 다름 (깊은 복사)
        clone.CurrentPhase = PhaseType.Afternoon;
        clone.BentoSelections[0].SideMenus.Add(mockSideFood2);

        Assert.AreEqual(PhaseType.Morning, original.CurrentPhase);
        Assert.AreEqual(1, original.BentoSelections[0].SideMenus.Count);
        Assert.AreEqual(2, clone.BentoSelections[0].SideMenus.Count);
    }
}
