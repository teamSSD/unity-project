using System.IO;
using NUnit.Framework;
using UnityEngine;

public class WebGLE2ETestBridgeContractTest
{
    [Test]
    public void E2EBridge_IsDevelopmentOnly_AndLimitsSemanticGameplayCommands()
    {
        var path = Path.Combine(Application.dataPath, "Scripts", "Unity", "Dev", "E2E", "AftertasteE2ETestBridge.cs");
        var source = File.ReadAllText(path);

        Assert.That(source, Does.Contain("#if AFTERTASTE_E2E"));
        Assert.That(source, Does.Contain("case \"snapshot\""));
        Assert.That(source, Does.Contain("case \"mark\""));
        Assert.That(source, Does.Contain("case \"campaign\""));
        Assert.That(source, Does.Contain("case \"placeIngredient\""));
        Assert.That(source, Does.Contain("case \"transferTool\""));
        Assert.That(source, Does.Contain("case \"buyItem\""));
        Assert.That(source, Does.Not.Contain("case \"move\""));
        Assert.That(source, Does.Not.Contain("case \"click\""));
        Assert.That(source, Does.Not.Contain("case \"timeScale\""));
        Assert.That(source, Does.Contain("E2ETransferToTool"));
    }

    [Test]
    public void CampaignObservation_ExposesLiveQuestMetadataWithoutMutatingQuestState()
    {
        var path = Path.Combine(Application.dataPath, "Scripts", "Unity", "Dev", "E2E", "AftertasteE2ETestBridge.cs");
        var source = File.ReadAllText(path);

        Assert.That(source, Does.Contain("QuestNpcObservation"));
        Assert.That(source, Does.Contain("prerequisiteGroupId"));
        Assert.That(source, Does.Contain("questNpcs = questNpcs"));
        Assert.That(source, Does.Contain("configuredQuestGroupIds"));
        Assert.That(source, Does.Contain("QuestMenus?.GetAllGroupIds()"));
        Assert.That(source, Does.Not.Contain("case \"questStage\""));
    }

    [Test]
    public void CampaignObservation_ExposesStorageCapacityForRealShopUpgradeDecisions()
    {
        var bridgePath = Path.Combine(Application.dataPath, "Scripts", "Unity", "Dev", "E2E", "AftertasteE2ETestBridge.cs");
        var bridge = File.ReadAllText(bridgePath);
        var shopPath = Path.Combine(Application.dataPath, "Scripts", "Unity", "Shop", "ShopUIAdapter.cs");
        var shop = File.ReadAllText(shopPath);

        Assert.That(bridge, Does.Contain("IngredientStorageObservation"));
        Assert.That(bridge, Does.Contain("StorageObservation"));
        Assert.That(bridge, Does.Contain("root.StorageUpgrade.GetCurrentData(type)"));
        Assert.That(bridge, Does.Contain("root.Inventory?.LoadIngredientsByCategory"));
        Assert.That(bridge, Does.Contain("quantityAfterDayAdvance"));
        Assert.That(bridge, Does.Contain("batch.daysRemaining > 1"));
        Assert.That(bridge, Does.Contain("ShopItemObservation"));
        Assert.That(bridge, Does.Contain("canBuyOne"));
        Assert.That(bridge, Does.Contain("refreshCost"));
        Assert.That(bridge, Does.Contain("managementFee"));
        Assert.That(shop, Does.Contain("E2EGetCurrentItems"));
        Assert.That(shop, Does.Contain("E2EBuyOne"));
        Assert.That(shop, Does.Contain("shop.storage.{storage.Type}"));
    }

    [Test]
    public void CampaignObservation_ExposesLiveFarmStateForRealHarvestDecisions()
    {
        var bridgePath = Path.Combine(Application.dataPath, "Scripts", "Unity", "Dev", "E2E", "AftertasteE2ETestBridge.cs");
        var bridge = File.ReadAllText(bridgePath);
        var farmPath = Path.Combine(Application.dataPath, "Scripts", "Unity", "Garden", "Farm.cs");
        var farm = File.ReadAllText(farmPath);

        Assert.That(bridge, Does.Contain("FarmObservation"));
        Assert.That(bridge, Does.Contain("farmTiles = farmTiles"));
        Assert.That(farm, Does.Contain("E2EIsHarvestable"));
        Assert.That(farm, Does.Contain("tile?.IsHarvestable()"));
    }

    [Test]
    public void DevelopmentWebGLBuild_RegistersInstance_AndKeepsBoundedEventLog()
    {
        var pluginPath = Path.Combine(Application.dataPath, "Plugins", "WebGL", "AftertasteE2E.jslib");
        var plugin = File.ReadAllText(pluginPath);
        var postprocessorPath = Path.Combine(Application.dataPath, "Scripts", "Editor", "WebGLE2EBuildPostprocessor.cs");
        var postprocessor = File.ReadAllText(postprocessorPath);

        Assert.That(plugin, Does.Contain("AftertasteE2EEmit"));
        Assert.That(postprocessor, Does.Contain("BuildOptions.Development"));
        Assert.That(postprocessor, Does.Contain("_e2e/"));
        var buildAutomationPath = Path.Combine(Application.dataPath, "Editor", "BuildAutomation.cs");
        var buildAutomation = File.ReadAllText(buildAutomationPath);
        Assert.That(buildAutomation, Does.Contain("AFTERTASTE_E2E"));
        Assert.That(postprocessor, Does.Contain("window.AftertasteE2E"));
        Assert.That(postprocessor, Does.Contain("window.AftertasteUnityInstance = unityInstance"));
        Assert.That(postprocessor, Does.Contain("window.AftertasteUnityInstance.SendMessage"));
        Assert.That(postprocessor, Does.Contain("var events = []"));
        Assert.That(postprocessor, Does.Contain("events.length > 500"));
        Assert.That(postprocessor, Does.Contain("event.browserSequence = ++sequence"));
        Assert.That(postprocessor, Does.Contain("var errors = []"));
        Assert.That(postprocessor, Does.Contain("errorCount += 1"));
        Assert.That(postprocessor, Does.Contain("errorCount: function ()"));
    }

    [Test]
    public void WebGLSaveSync_CoalescesCalls_AndWaitsForInflightIdbfsWork()
    {
        var path = Path.Combine(Application.dataPath, "Plugins", "WebGL", "SaveSync.jslib");
        var source = File.ReadAllText(path);

        Assert.That(source, Does.Contain("aftertasteSaveSyncQueued"));
        Assert.That(source, Does.Contain("FS.syncFSRequests > 0"));
        Assert.That(source, Does.Contain("aftertasteSaveSyncPumpActive"));
    }
}
