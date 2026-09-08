using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class ScreenSpaceCanvasScalingContractTest
{
    private static readonly Regex Document =
        new(@"^--- !u!\d+ &\d+\r?$.*?(?=^--- !u!|\z)", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex GameObjectId =
        new(@"^  m_GameObject: \{fileID: (\d+)\}", RegexOptions.Multiline | RegexOptions.Compiled);

    [Test]
    public void ProductionScreenSpaceCanvases_UseResponsiveReferenceScaling()
    {
        var assets = EnumerateProductionUiAssets().ToArray();
        var violations = assets.SelectMany(FindViolations).ToArray();

        Assert.That(violations, Is.Empty,
            "Screen Space Overlay UI must use Scale With Screen Size at the shared 1920x1080 reference resolution.\n" +
            string.Join("\n", violations));
    }

    [Test]
    public void MenuSelection_UsesWidthMatchingToKeepThreeSlotsVisible()
    {
        var path = Path.Combine(Application.dataPath, "Bundles", "Prefabs", "recipebook", "MenuSelection", "MenuSelection.prefab");
        var contents = File.ReadAllText(path);

        Assert.That(contents, Does.Contain("m_MatchWidthOrHeight: 0"),
            "The three-column menu selection layout must match viewport width so its outer slots cannot be clipped.");
    }

    [Test]
    public void MallPhaseSelector_CannotAttachToPersistentLoadingCanvas()
    {
        var mallControllerPath = Path.Combine(
            Application.dataPath, "Scripts", "Unity", "Common", "MallSceneController.cs");
        var selectorPath = Path.Combine(
            Application.dataPath, "Scripts", "Unity", "UI", "PhaseActionSelector.cs");

        var mallController = File.ReadAllText(mallControllerPath);
        var selector = File.ReadAllText(selectorPath);

        Assert.That(mallController, Does.Contain("c.gameObject.scene == activeScene"),
            "Scene-owned phase UI must not become a child of DontDestroyOnLoad LoadingCanvas during transitions.");
        Assert.That(selector, Does.Contain("UILockManager.Unlock(UILockManager.Owner.PhaseSelection)"),
            "Destroying phase selection UI must release its global lock even when Hide was skipped.");
    }

    private static IEnumerable<string> EnumerateProductionUiAssets()
    {
        var assetsRoot = Application.dataPath;
        return Directory.EnumerateFiles(Path.Combine(assetsRoot, "Scenes", "ForReal"), "*.unity", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(Path.Combine(assetsRoot, "Bundles", "Prefabs"), "*.prefab", SearchOption.AllDirectories));
    }

    private static IEnumerable<string> FindViolations(string assetPath)
    {
        var documents = Document.Matches(File.ReadAllText(assetPath)).Select(match => match.Value).ToArray();
        var screenSpaceCanvasObjects = documents
            .Where(document => document.StartsWith("--- !u!223 ") && document.Contains("m_RenderMode: 0"))
            .Select(GetGameObjectId)
            .Where(id => id != null)
            .ToHashSet();

        foreach (var scaler in documents.Where(document => document.Contains("UnityEngine.UI::UnityEngine.UI.CanvasScaler")))
        {
            var gameObjectId = GetGameObjectId(scaler);
            if (gameObjectId == null || !screenSpaceCanvasObjects.Contains(gameObjectId))
                continue;

            if (!scaler.Contains("m_UiScaleMode: 1") ||
                !scaler.Contains("m_ReferenceResolution: {x: 1920, y: 1080}") ||
                !scaler.Contains("m_ScreenMatchMode: 0"))
            {
                yield return $"{Path.GetRelativePath(Application.dataPath, assetPath)}";
            }
        }
    }

    private static string GetGameObjectId(string document)
    {
        var match = GameObjectId.Match(document);
        return match.Success ? match.Groups[1].Value : null;
    }
}
