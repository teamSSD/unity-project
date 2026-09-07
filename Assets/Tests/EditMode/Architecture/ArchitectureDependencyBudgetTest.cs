using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class ArchitectureDependencyBudgetTest
{
    // Legacy budgets are ceilings, not targets. Lower each value whenever usages are removed.
    private const int SingletonAccessBudget = 266;
    private const int GameSessionRootAccessBudget = 165;
    private const int SingletonTypeBudget = 15;
    private const int SceneSearchBudget = 27;
    private const int UiStaticBoolBudget = 3;

    private static readonly Regex SingletonAccess =
        new(@"\b[A-Za-z_][A-Za-z0-9_]*\.Instance\b", RegexOptions.Compiled);

    private static readonly Regex GameSessionRootAccess =
        new(@"\bGameSessionRoot\.Instance\b", RegexOptions.Compiled);

    private static readonly Regex SingletonType =
        new(@":\s*SingletonMonoBehaviour<", RegexOptions.Compiled);

    private static readonly Regex SceneSearch =
        new(@"\b(?:FindObjectOfType|FindObjectsOfType|FindFirstObjectByType|FindAnyObjectByType|FindWithTag|FindGameObjectWithTag|GameObject\.Find|transform\.Find)\b",
            RegexOptions.Compiled);

    private static readonly Regex StaticBool =
        new(@"\bstatic\s+bool\s+[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

    private static string RuntimeRoot => Path.Combine(Application.dataPath, "Scripts", "Unity");
    private static string UiRoot => Path.Combine(RuntimeRoot, "UI");

    [Test]
    public void SingletonAccess_DoesNotExceedLegacyBudget()
    {
        AssertBudget(RuntimeRoot, SingletonAccess, SingletonAccessBudget, "singleton .Instance access");
    }

    [Test]
    public void GameSessionRootAccess_DoesNotExceedLegacyBudget()
    {
        AssertBudget(RuntimeRoot, GameSessionRootAccess, GameSessionRootAccessBudget, "GameSessionRoot.Instance access");
    }

    [Test]
    public void SingletonTypes_DoNotExceedLegacyBudget()
    {
        AssertBudget(RuntimeRoot, SingletonType, SingletonTypeBudget, "SingletonMonoBehaviour type");
    }

    [Test]
    public void SceneSearches_DoNotExceedLegacyBudget()
    {
        AssertBudget(RuntimeRoot, SceneSearch, SceneSearchBudget, "runtime scene search");
    }

    [Test]
    public void UiStaticBoolState_DoesNotExceedLegacyBudget()
    {
        AssertBudget(UiRoot, StaticBool, UiStaticBoolBudget, "UI static bool state");
    }

    private static void AssertBudget(string root, Regex pattern, int budget, string label)
    {
        int count = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Sum(path => pattern.Matches(File.ReadAllText(path)).Count);

        Assert.LessOrEqual(count, budget,
            $"New {label} detected: {count} > {budget}. Inject the dependency or lower the budget after removing legacy usage.");
    }
}
