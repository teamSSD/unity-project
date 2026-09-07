using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// MenuValidator.GenerateFeedback (분해된 메시지 로직) 검증.
/// </summary>
public class MenuValidatorFeedbackTest
{
    private FoodData mainExpected, mainOther, sideA, sideB;
    private MenuSchema order;

    [SetUp]
    public void Setup()
    {
        mainExpected = ScriptableObject.CreateInstance<FoodData>();
        mainExpected.id = "ME"; mainExpected.type = FoodType.MAIN;
        mainOther = ScriptableObject.CreateInstance<FoodData>();
        mainOther.id = "MO"; mainOther.type = FoodType.MAIN;
        sideA = ScriptableObject.CreateInstance<FoodData>();
        sideA.id = "SA"; sideA.type = FoodType.SIDE;
        sideB = ScriptableObject.CreateInstance<FoodData>();
        sideB.id = "SB"; sideB.type = FoodType.SIDE;

        order = new MenuSchema("test", 1, mainExpected, new List<FoodData> { sideA, sideB });
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(mainExpected);
        Object.DestroyImmediate(mainOther);
        Object.DestroyImmediate(sideA);
        Object.DestroyImmediate(sideB);
    }

    private FoodSchema Schema(FoodData fd) => new FoodSchema(fd, 100);

    [Test]
    public void MainWrong_ReturnsWrongMainMessage()
    {
        var result = MenuValidator.Validate(order, Schema(mainOther), new List<FoodSchema>());
        StringAssert.Contains("메인", result.FeedbackMessage);
        Assert.IsFalse(result.MainMenuCorrect);
    }

    [Test]
    public void PerfectMatch_ReturnsPerfectMessage()
    {
        var result = MenuValidator.Validate(order, Schema(mainExpected),
            new List<FoodSchema> { Schema(sideA), Schema(sideB) });
        StringAssert.Contains("완벽", result.FeedbackMessage);
        Assert.AreEqual(1.0f, result.AccuracyScore, 0.0001f);
    }

    [Test]
    public void MissingSide_ReturnsShortageMessage()
    {
        var result = MenuValidator.Validate(order, Schema(mainExpected),
            new List<FoodSchema> { Schema(sideA) });
        StringAssert.Contains("부족", result.FeedbackMessage);
    }

    [Test]
    public void ExtraSide_ReturnsExtraMessage()
    {
        var result = MenuValidator.Validate(order, Schema(mainExpected),
            new List<FoodSchema> { Schema(sideA), Schema(sideB), Schema(sideA) });
        StringAssert.Contains("더 들어있", result.FeedbackMessage);
        Assert.AreEqual(2, result.CorrectSideCount);
    }

    [Test]
    public void AllSidesWrong_ReturnsAllWrongMessage()
    {
        var wrongSide = ScriptableObject.CreateInstance<FoodData>();
        wrongSide.id = "WRONG"; wrongSide.type = FoodType.SIDE;

        var result = MenuValidator.Validate(order, Schema(mainExpected),
            new List<FoodSchema> { Schema(wrongSide), Schema(wrongSide) });
        StringAssert.Contains("모두 틀렸", result.FeedbackMessage);

        Object.DestroyImmediate(wrongSide);
    }

    [Test]
    public void EmptyExpectedSides_NoProvided_ReturnsPerfect()
    {
        var emptyOrder = new MenuSchema("t", 1, mainExpected, new List<FoodData>());
        var result = MenuValidator.Validate(emptyOrder, Schema(mainExpected), new List<FoodSchema>());
        StringAssert.Contains("완벽", result.FeedbackMessage);
    }

    [Test]
    public void EmptyExpectedSides_ExtraProvided_ReturnsExtraOnly()
    {
        var emptyOrder = new MenuSchema("t", 1, mainExpected, new List<FoodData>());
        var result = MenuValidator.Validate(emptyOrder, Schema(mainExpected),
            new List<FoodSchema> { Schema(sideA) });
        StringAssert.Contains("더 들어있", result.FeedbackMessage);
    }

    [Test]
    public void GetGrade_ScoreBoundaries()
    {
        Assert.AreEqual("S", MenuValidator.GetGrade(1.0f));
        Assert.AreEqual("A", MenuValidator.GetGrade(0.9f));
        Assert.AreEqual("B", MenuValidator.GetGrade(0.8f));
        Assert.AreEqual("C", MenuValidator.GetGrade(0.7f));
        Assert.AreEqual("D", MenuValidator.GetGrade(0.6f));
        Assert.AreEqual("F", MenuValidator.GetGrade(0.5f));
    }
}
