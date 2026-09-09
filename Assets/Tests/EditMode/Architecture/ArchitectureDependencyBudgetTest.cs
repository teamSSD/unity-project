using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class ArchitectureDependencyBudgetTest
{
    // Legacy budgets are ceilings, not targets. Lower each value whenever usages are removed.
    private const int SingletonAccessBudget = 264;
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
        new(@"\bstatic\s+bool\s+[A-Za-z_][A-Za-z0-9_]*\s*(?:=|;|=>|\{)", RegexOptions.Compiled);

    private static string RuntimeRoot => Path.Combine(Application.dataPath, "Scripts", "Unity");
    private static string UiRoot => Path.Combine(RuntimeRoot, "UI");
    private static string UiCompositionRoot => Path.Combine(RuntimeRoot, "Common", "UIFlowController.cs");

    [Test]
    public void SingletonAccess_DoesNotExceedLegacyBudget()
    {
        AssertBudget(
            RuntimeRoot,
            SingletonAccess,
            SingletonAccessBudget,
            "singleton .Instance access outside UI composition root",
            UiCompositionRoot);
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

    private static void AssertBudget(
        string root,
        Regex pattern,
        int budget,
        string label,
        params string[] excludedPaths)
    {
        var exclusions = new HashSet<string>(excludedPaths.Select(Path.GetFullPath));
        int count = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !exclusions.Contains(Path.GetFullPath(path)))
            .Sum(path => pattern.Matches(RemoveE2EOnlyCode(File.ReadAllText(path))).Count);

        Assert.LessOrEqual(count, budget,
            $"New {label} detected: {count} > {budget}. Inject the dependency or lower the budget after removing legacy usage.");
    }

    private static string RemoveE2EOnlyCode(string source)
    {
        var output = new StringBuilder(source.Length);
        var conditionals = new Stack<ConditionalFrame>();
        bool include = true;

        using var reader = new StringReader(source);
        while (reader.ReadLine() is { } line)
        {
            string directive = line.TrimStart();
            if (directive.StartsWith("#if "))
            {
                bool controlsE2E = directive.Contains("AFTERTASTE_E2E");
                bool condition = controlsE2E && EvaluateReleaseCondition(directive[4..]);
                conditionals.Push(new ConditionalFrame(include, controlsE2E, condition));
                if (controlsE2E)
                {
                    include = include && condition;
                    continue;
                }
            }
            else if (directive.StartsWith("#elif ") && conditionals.Count > 0 && conditionals.Peek().ControlsE2E)
            {
                var frame = conditionals.Pop();
                bool condition = !frame.BranchMatched && EvaluateReleaseCondition(directive[6..]);
                frame.BranchMatched |= condition;
                conditionals.Push(frame);
                include = frame.ParentIncluded && condition;
                continue;
            }
            else if (directive == "#else" && conditionals.Count > 0 && conditionals.Peek().ControlsE2E)
            {
                var frame = conditionals.Pop();
                bool condition = !frame.BranchMatched;
                frame.BranchMatched = true;
                conditionals.Push(frame);
                include = frame.ParentIncluded && condition;
                continue;
            }
            else if (directive == "#endif" && conditionals.Count > 0)
            {
                var frame = conditionals.Pop();
                include = frame.ParentIncluded;
                if (frame.ControlsE2E) continue;
            }

            if (include) output.AppendLine(line);
        }

        return output.ToString();
    }

    private static bool EvaluateReleaseCondition(string expression) =>
        Regex.IsMatch(expression, @"^\s*!\s*AFTERTASTE_E2E(?:_LONGRUN)?\s*$");

    private sealed class ConditionalFrame
    {
        public ConditionalFrame(bool parentIncluded, bool controlsE2E, bool branchMatched)
        {
            ParentIncluded = parentIncluded;
            ControlsE2E = controlsE2E;
            BranchMatched = branchMatched;
        }

        public bool ParentIncluded { get; }
        public bool ControlsE2E { get; }
        public bool BranchMatched { get; set; }
    }
}
