using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Game.Domain.Cooking;
using Game.Domain.Mall;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Development WebGL에서만 브라우저 E2E 실행기를 위한 관측/제어 경계.
/// 상태 관측과 여행 단축, 실제 게임 규칙을 호출하는 의미 기반 조작만 제공한다.
/// </summary>
#if AFTERTASTE_E2E
public sealed class AftertasteE2ETestBridge : MonoBehaviour
{
    private const string ObjectName = "AftertasteE2ETestBridge";
    private static bool _created;

    [Serializable]
    private sealed class Command
    {
        public string action;
        public string label;
        public string targetId;
        public string foodId;
        public string sourceToolId;
        public string targetToolId;
    }

    [Serializable]
    private sealed class Snapshot
    {
        public string type;
        public string label;
        public string scene;
        public string phase;
        public int day;
        public int hour;
        public int minute;
        public int phaseEndMinuteOfDay;
        public int remainingPhaseMinutes;
        public int money;
        public int stamina;
        public int inventoryItemCount;
        public int refrigeratorIngredientTypes;
        public int upperShelfIngredientTypes;
        public int lowerShelfIngredientTypes;
        public int cookingFoodObjectCount;
        public int refrigeratorObjectCount;
        public int upperShelfObjectCount;
        public int lowerShelfObjectCount;
        public int bentoFoodCount;
        public int selectedMenuCount;
        public int questStageCount;
        public int activeOrderCount;
        public string firstOrderState;
        public string activeMinigame;
        public int immutableSeed;
        public bool campaignProfile;
        public bool uiLocked;
        public float realtime;
        public int frame;
        public float timeScale;
    }

    [Serializable]
    private sealed class InventoryObservation
    {
        public string foodId;
        public int quantity;
        public int quantityAfterDayAdvance;
    }

    [Serializable]
    private sealed class IngredientStorageObservation
    {
        public string foodId;
        public string storageType;
    }

    [Serializable]
    private sealed class StorageObservation
    {
        public string storageType;
        public int used;
        public int capacity;
        public bool isMax;
        public int nextCost;
    }

    [Serializable]
    private sealed class ShopItemObservation
    {
        public string foodId;
        public int unitPrice;
        public int remainingStock;
        public bool isUnlimited;
        public bool canBuyOne;
        public string refusalKind;
    }

    [Serializable]
    private sealed class OrderObservation
    {
        public string questId;
        public int orderNumber;
        public string state;
        public string npcId;
        public string[] mainFoodIds;
        public string[] sideFoodIds;
    }

    [Serializable]
    private sealed class CookingToolObservation
    {
        public string toolId;
        public bool cookable;
        public string resultFoodId;
        public string[] ingredientFoodIds;
    }

    [Serializable]
    private sealed class BentoObservation
    {
        public string targetId;
        public string[] foodIds;
    }

    [Serializable]
    private sealed class TicketObservation
    {
        public string targetId;
        public bool delivery;
        public string questId;
        public string[] requiredFoodIds;
    }

    [Serializable]
    private sealed class SelectedMenuObservation
    {
        public int slotIndex;
        public string mainFoodId;
        public string[] sideFoodIds;
    }

    [Serializable]
    private sealed class MinigameObservation
    {
        public string name;
        public string nextInput;
        public float currentValue;
        public float targetValue;
    }

    [Serializable]
    private sealed class QuestNpcObservation
    {
        public string npcId;
        public string targetId;
        public string groupId;
        public string prerequisiteGroupId;
        public string stage;
        public bool unlocked;
        public string[] mainFoodIds;
        public string[] sideFoodIds;
    }

    [Serializable]
    private sealed class FarmObservation
    {
        public string targetId;
        public bool locked;
        public string cropId;
        public int passedPhases;
        public int requiredPhases;
        public bool harvestable;
    }

    [Serializable]
    private sealed class CampaignObservation
    {
        public string type;
        public string label;
        public string scene;
        public string phase;
        public int day;
        public int money;
        public int managementFee;
        public int refreshCount;
        public int refreshCost;
        public bool canRefresh;
        public InventoryObservation[] inventory;
        public IngredientStorageObservation[] ingredientStorage;
        public StorageObservation[] storage;
        public ShopItemObservation[] shopItems;
        public OrderObservation[] orders;
        public RecipeExecutionPlan[] cookingPlans;
        public CookingToolObservation[] tools;
        public BentoObservation[] bentos;
        public TicketObservation[] tickets;
        public SelectedMenuObservation[] selectedMenus;
        public string[] customerSalesMainFoodIds;
        public QuestNpcObservation[] questNpcs;
        public FarmObservation[] farmTiles;
        public string[] configuredQuestGroupIds;
        public MinigameObservation minigame;
        public string[] unlockedMainFoodIds;
        public string[] unlockedSideFoodIds;
    }

    [Serializable]
    private sealed class TargetMap
    {
        public string type;
        public E2EUiTargetRegistry.TargetState[] targets;
    }

    [Serializable]
    private sealed class LogEvent
    {
        public string type;
        public string level;
        public string message;
        public string scene;
        public float realtime;
        public int frame;
    }

    [Serializable]
    private sealed class ActionResult
    {
        public string type;
        public string action;
        public string label;
        public bool success;
        public string message;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (_created) return;
        _created = true;

        var bridge = new GameObject(ObjectName).AddComponent<AftertasteE2ETestBridge>();
        DontDestroyOnLoad(bridge.gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.logMessageReceived += OnLog;
        EmitSnapshot("bridge-ready");
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.logMessageReceived -= OnLog;
    }

    /// <summary>WebGL의 window.AftertasteE2E.command가 SendMessage로 호출한다.</summary>
    public void ReceiveCommand(string json)
    {
        var command = JsonUtility.FromJson<Command>(json);
        if (command == null || string.IsNullOrWhiteSpace(command.action))
        {
            EmitLog("warning", "Invalid E2E command");
            return;
        }

        switch (command.action)
        {
            case "snapshot":
                EmitSnapshot(command.label);
                break;
            case "mark":
                EmitSnapshot(string.IsNullOrWhiteSpace(command.label) ? "marker" : command.label);
                break;
            case "targets":
                EmitTargetMap();
                break;
            case "campaign":
                EmitCampaignObservation(command.label);
                break;
            case "teleport":
                TeleportToRegisteredWorldTarget(command.targetId);
                break;
            case "placeIngredient":
                EmitActionResult(command, PlaceIngredient(command.foodId, command.targetToolId));
                break;
            case "transferTool":
                EmitActionResult(command, TransferTool(command.sourceToolId, command.targetToolId));
                break;
            case "buyItem":
                EmitActionResult(command, ShopUIAdapter.Instance?.E2EBuyOne(command.foodId) ?? false);
                break;
            default:
                EmitLog("warning", $"Unsupported E2E command: {command.action}");
                break;
        }
    }

    private static bool PlaceIngredient(string foodId, string targetToolId)
    {
        var food = UnityEngine.Object.FindObjectsByType<FoodModel>(FindObjectsSortMode.None)
            .FirstOrDefault(model => model.GetFoodData()?.id == foodId);
        var tool = UnityEngine.Object.FindObjectsByType<CookingToolModel>(FindObjectsSortMode.None)
            .FirstOrDefault(model => model.GetToolId() == targetToolId);
        return food != null && food.E2ETransferToTool(tool);
    }

    private static bool TransferTool(string sourceToolId, string targetToolId)
    {
        var tools = UnityEngine.Object.FindObjectsByType<CookingToolModel>(FindObjectsSortMode.None);
        var source = tools.FirstOrDefault(model => model.GetToolId() == sourceToolId);
        var target = tools.FirstOrDefault(model => model.GetToolId() == targetToolId);
        return source != null && source.E2ETransferToTool(target);
    }

    private void EmitActionResult(Command command, bool success)
    {
        EmitJson(JsonUtility.ToJson(new ActionResult
        {
            type = "action-result",
            action = command.action,
            label = command.label ?? string.Empty,
            success = success,
            message = success ? string.Empty : "Gameplay action was rejected by live rules.",
        }));
    }

    // 장시간 이동만 생략한다. 돈·재료·퀘스트·시간은 직접 바꾸지 않고,
    // 도착 뒤의 Space/클릭 상호작용은 브라우저 입력으로 반드시 수행한다.
    private void TeleportToRegisteredWorldTarget(string targetId)
    {
        if (!E2EWorldTargetRegistry.TryGetWorldPosition(targetId, out var position))
        {
            EmitLog("warning", $"Unknown world target: {targetId}");
            return;
        }
        var player = GameObject.FindWithTag(Tags.Player);
        if (player == null) { EmitLog("warning", "Player not found for teleport"); return; }
        player.transform.position = position;
        UnityEngine.Object.FindFirstObjectByType<CameraFollow>()?.SnapToPlayer();
        EmitLog("info", $"Teleported player to {targetId}");
    }

    private void OnSceneLoaded(Scene _, LoadSceneMode __) => EmitSnapshot("scene-loaded");

    private void OnLog(string message, string _, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        EmitJson(JsonUtility.ToJson(new LogEvent
        {
            type = "unity-log",
            level = type.ToString(),
            message = message,
            scene = SceneManager.GetActiveScene().name,
            realtime = Time.realtimeSinceStartup,
            frame = Time.frameCount,
        }));
    }

    private void EmitSnapshot(string label)
    {
        var state = GameStateReporter.CaptureRuntimeState();
        var inventory = GameSessionRoot.Instance?.Inventory;
        var refrigerator = UnityEngine.Object.FindFirstObjectByType<Refrigerator>();
        var upperShelf = UnityEngine.Object.FindFirstObjectByType<UpperShelf>();
        var lowerShelf = UnityEngine.Object.FindFirstObjectByType<LowerShelf>();
        var orders = GameSessionRoot.Instance?.Order?.GetOrders();
        var bento = UnityEngine.Object.FindFirstObjectByType<BentoModel>();
        var phaseEndMinute = GameSessionRoot.Instance?.Progress?.PhaseEndMinutes ?? -1;
        var currentMinute = state.Hour * 60 + state.Minute;
        EmitJson(JsonUtility.ToJson(new Snapshot
        {
            type = "snapshot",
            label = label ?? string.Empty,
            scene = SceneManager.GetActiveScene().name,
            phase = state.Phase ?? string.Empty,
            day = state.Day,
            hour = state.Hour,
            minute = state.Minute,
            phaseEndMinuteOfDay = phaseEndMinute,
            remainingPhaseMinutes = phaseEndMinute >= 0 ? Math.Max(0, phaseEndMinute - currentMinute) : -1,
            money = state.Money,
            stamina = state.Stamina,
            inventoryItemCount = state.InventoryItemCount,
            refrigeratorIngredientTypes = inventory?.LoadIngredientsByCategory(IngredientDisplayCategory.Refrigerator)?.Count ?? -1,
            upperShelfIngredientTypes = inventory?.LoadIngredientsByCategory(IngredientDisplayCategory.UpperShelf)?.Count ?? -1,
            cookingFoodObjectCount = UnityEngine.Object.FindObjectsByType<FoodModel>(FindObjectsSortMode.None).Length,
            refrigeratorObjectCount = refrigerator?.GetCount() ?? -1,
            upperShelfObjectCount = upperShelf?.GetCount() ?? -1,
            lowerShelfObjectCount = lowerShelf?.GetCount() ?? -1,
            bentoFoodCount = bento?.getFoodList()?.Count ?? 0,
            selectedMenuCount = state.SelectedMenuCount,
            questStageCount = state.QuestStageCount,
            activeOrderCount = state.ActiveOrderCount,
            firstOrderState = orders != null && orders.Count > 0 ? orders[0].state.ToString() : string.Empty,
            activeMinigame = UnityEngine.Object.FindFirstObjectByType<MiniGameManager>()?.ActiveMinigame ?? string.Empty,
            immutableSeed = state.ImmutableSeed,
            campaignProfile = AftertasteE2EProfileSetup.IsCampaignProfile,
            uiLocked = UILockManager.IsLocked,
            realtime = Time.realtimeSinceStartup,
            frame = Time.frameCount,
            timeScale = Time.timeScale,
        }));
    }

    /// <summary>
    /// Emits a read-only, catalog-derived view for the state-driven macro. Recipe IDs,
    /// ingredients, tools, and order contents all come from the live campaign; no scenario
    /// identifiers are selected here and no game state is changed.
    /// </summary>
    private void EmitCampaignObservation(string label)
    {
        var root = GameSessionRoot.Instance;
        var orderData = root?.Order?.GetOrders() ?? Array.Empty<DeliveryOrderData>();
        var planner = new RecipePlanService(
            CatalogProvider.Food?.All ?? Array.Empty<FoodData>(),
            root?.RecipeLookup?.GetAllRecipes() ?? new List<RecipeData>());

        var orders = orderData.Select(order => new OrderObservation
        {
            questId = order.questId ?? string.Empty,
            orderNumber = order.orderNumber,
            state = order.state.ToString(),
            npcId = order.npcId ?? string.Empty,
            mainFoodIds = order.menuSchema?.mainMenus?
                .Where(food => food != null).Select(food => food.id).ToArray() ?? Array.Empty<string>(),
            sideFoodIds = order.menuSchema?.sideMenus?
                .Where(food => food != null).Select(food => food.id).ToArray() ?? Array.Empty<string>(),
        }).ToArray();

        var questNpcs = UnityEngine.Object.FindObjectsByType<DeliveryNpcView>(FindObjectsSortMode.None)
            .Where(view => !string.IsNullOrWhiteSpace(view.NpcId) && !string.IsNullOrWhiteSpace(view.GroupId))
            .OrderBy(view => view.GroupId)
            .ThenBy(view => view.NpcId)
            .Select(view =>
            {
                var menu = root?.QuestMenus?.GetByGroupId(view.GroupId);
                var prerequisite = view.PrerequisiteGroupId ?? string.Empty;
                return new QuestNpcObservation
                {
                    npcId = view.NpcId,
                    targetId = $"npc.{view.NpcId}",
                    groupId = view.GroupId,
                    prerequisiteGroupId = prerequisite,
                    stage = (root?.DeliveryQuest?.GetStage(view.GroupId) ?? DeliveryQuestStage.FirstMeet).ToString(),
                    unlocked = string.IsNullOrEmpty(prerequisite)
                        || root?.DeliveryQuest?.GetStage(prerequisite) == DeliveryQuestStage.Completed,
                    mainFoodIds = menu?.mainMenus?
                        .Where(food => food != null).Select(food => food.id).ToArray() ?? Array.Empty<string>(),
                    sideFoodIds = menu?.sideMenus?
                        .Where(food => food != null).Select(food => food.id).ToArray() ?? Array.Empty<string>(),
                };
            }).ToArray();

        var targetFoodIds = orders
            .Where(order => order.state == DeliveryOrderState.Ordered.ToString()
                || order.state == DeliveryOrderState.Cooking.ToString())
            .SelectMany(order => order.mainFoodIds.Concat(order.sideFoodIds))
            .Concat(root?.UnlockedFood?.GetUnlockedMainFoods()
                .Where(food => food != null).Select(food => food.id) ?? Enumerable.Empty<string>())
            .Concat(root?.UnlockedFood?.GetUnlockedSideFoods()
                .Where(food => food != null).Select(food => food.id) ?? Enumerable.Empty<string>())
            .Concat(questNpcs.Where(quest => quest.unlocked)
                .SelectMany(quest => quest.mainFoodIds.Concat(quest.sideFoodIds)))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToArray();

        var inventory = root?.Inventory?.GetSaveData()?.items?
            .Select(item => new InventoryObservation
            {
                foodId = item.foodId ?? string.Empty,
                quantity = item.batches?.Sum(batch => batch.quantity) ?? 0,
                quantityAfterDayAdvance = item.batches?
                    .Where(batch => batch.daysRemaining > 1)
                    .Sum(batch => batch.quantity) ?? 0,
            })
            .Where(item => item.quantity > 0)
            .OrderBy(item => item.foodId)
            .ToArray() ?? Array.Empty<InventoryObservation>();

        var ingredientStorage = (CatalogProvider.Food?.All ?? Array.Empty<FoodData>())
            .Where(food => food?.ingredient != null)
            .Select(food => new IngredientStorageObservation
            {
                foodId = food.id,
                storageType = StorageTypeFromCategory(food.ingredient.display),
            })
            .Where(item => !string.IsNullOrEmpty(item.storageType))
            .OrderBy(item => item.foodId)
            .ToArray();

        var storage = (root?.StorageUpgrade?.GetAllTypes() ?? Enumerable.Empty<string>())
            .OrderBy(type => type)
            .Select(type =>
            {
                var category = CategoryFromStorageType(type);
                var current = root.StorageUpgrade.GetCurrentData(type);
                var next = root.StorageUpgrade.GetNextData(type);
                return new StorageObservation
                {
                    storageType = type,
                    used = category.HasValue
                        ? root.Inventory?.LoadIngredientsByCategory(category.Value)?.Count ?? 0
                        : 0,
                    capacity = current?.value ?? int.MaxValue,
                    isMax = next == null,
                    nextCost = next?.cost ?? 0,
                };
            })
            .ToArray();

        var shopAdapter = ShopUIAdapter.Instance;
        var liveShopItems = shopAdapter != null
            ? shopAdapter.E2EGetCurrentItems()
            : Array.Empty<ShopUIAdapter.E2EItemState>();
        var shopItems = liveShopItems
            .Select(item =>
            {
                var refusal = root?.Purchase?.GetRefusalReason(item.Food, item.UnitPrice);
                return new ShopItemObservation
                {
                    foodId = item.Food.id,
                    unitPrice = item.UnitPrice,
                    remainingStock = item.RemainingStock,
                    isUnlimited = item.IsUnlimited,
                    canBuyOne = item.RemainingStock > 0 && !refusal.HasValue,
                    refusalKind = refusal?.Kind.ToString() ?? string.Empty,
                };
            })
            .OrderBy(item => item.foodId)
            .ToArray();

        var tools = UnityEngine.Object.FindObjectsByType<CookingToolModel>(FindObjectsSortMode.None)
            .OrderBy(tool => tool.GetToolId())
            .Select(tool => new CookingToolObservation
            {
                toolId = tool.GetToolId(),
                cookable = tool.E2EIsCookable,
                resultFoodId = tool.E2EResultFoodId,
                ingredientFoodIds = tool.E2EIngredientFoodIds,
            }).ToArray();

        var bentos = UnityEngine.Object.FindObjectsByType<BentoModel>(FindObjectsSortMode.None)
            .Select(bento => new BentoObservation
            {
                targetId = $"world.cooking.bento.{bento.GetInstanceID()}",
                foodIds = bento.getFoodList()?
                    .Where(food => food?.foodData != null).Select(food => food.foodData.id).ToArray()
                    ?? Array.Empty<string>(),
            }).ToArray();

        var tickets = UnityEngine.Object.FindObjectsByType<OrderTicketModel>(FindObjectsSortMode.None)
            .Select(ticket => new TicketObservation
            {
                targetId = ticket.E2ETargetId,
                delivery = ticket.IsDelivery,
                questId = ticket.QuestId ?? string.Empty,
                requiredFoodIds = ticket.E2ERequiredFoodIds,
            }).ToArray();
        var selectedMenus = Enumerable.Range(0, 3)
            .Select(slotIndex =>
            {
                var menu = root?.MenuSelection?.GetMenu(slotIndex);
                return new SelectedMenuObservation
                {
                    slotIndex = slotIndex,
                    mainFoodId = menu?.MainMenu?.id ?? string.Empty,
                    sideFoodIds = menu?.SideMenus?
                        .Where(food => food != null).Select(food => food.id).ToArray()
                        ?? Array.Empty<string>(),
                };
            }).ToArray();
        var customerManager = UnityEngine.Object.FindFirstObjectByType<CustomerManager>();
        var minigameManager = UnityEngine.Object.FindFirstObjectByType<MiniGameManager>();
        var farmTiles = UnityEngine.Object.FindObjectsByType<Farm>(FindObjectsSortMode.None)
            .OrderBy(farm => farm.farmIndex)
            .Select(farm => new FarmObservation
            {
                targetId = farm.E2ETargetId,
                locked = farm.IsLocked,
                cropId = farm.E2ECropId,
                passedPhases = farm.E2EPassedPhases,
                requiredPhases = farm.E2ERequiredPhases,
                harvestable = farm.E2EIsHarvestable,
            }).ToArray();

        EmitJson(JsonUtility.ToJson(new CampaignObservation
        {
            type = "campaign-observation",
            label = label ?? string.Empty,
            scene = SceneManager.GetActiveScene().name,
            phase = GameStateReporter.CaptureRuntimeState().Phase ?? string.Empty,
            day = GameStateReporter.CaptureRuntimeState().Day,
            money = root?.Stats?.GetMoney() ?? 0,
            managementFee = SettlementService.ManagementFee,
            refreshCount = root?.Purchase?.RefreshCount ?? 0,
            refreshCost = root?.Purchase?.GetRefreshCost() ?? 0,
            canRefresh = root?.Purchase?.CanRefresh() ?? false,
            inventory = inventory,
            ingredientStorage = ingredientStorage,
            storage = storage,
            shopItems = shopItems,
            orders = orders,
            cookingPlans = targetFoodIds.Select(planner.Build).ToArray(),
            tools = tools,
            bentos = bentos,
            tickets = tickets,
            selectedMenus = selectedMenus,
            customerSalesMainFoodIds = customerManager?.E2ESalesMainFoodIds ?? Array.Empty<string>(),
            questNpcs = questNpcs,
            farmTiles = farmTiles,
            configuredQuestGroupIds = root?.QuestMenus?.GetAllGroupIds()
                .Where(groupId => !string.IsNullOrWhiteSpace(groupId))
                .OrderBy(groupId => groupId)
                .ToArray() ?? Array.Empty<string>(),
            minigame = new MinigameObservation
            {
                name = minigameManager?.ActiveMinigame ?? string.Empty,
                nextInput = minigameManager?.E2ENextInput ?? string.Empty,
                currentValue = minigameManager?.E2ECurrentValue ?? 0f,
                targetValue = minigameManager?.E2ETargetValue ?? 0f,
            },
            unlockedMainFoodIds = root?.UnlockedFood?.GetUnlockedMainFoods()
                .Where(food => food != null).Select(food => food.id).OrderBy(id => id).ToArray()
                ?? Array.Empty<string>(),
            unlockedSideFoodIds = root?.UnlockedFood?.GetUnlockedSideFoods()
                .Where(food => food != null).Select(food => food.id).OrderBy(id => id).ToArray()
                ?? Array.Empty<string>(),
        }));
    }

    private static string StorageTypeFromCategory(IngredientDisplayCategory category) => category switch
    {
        IngredientDisplayCategory.Refrigerator => "refrigerator",
        IngredientDisplayCategory.UpperShelf => "upperShelf",
        IngredientDisplayCategory.LowerShelf => "lowerShelf",
        _ => string.Empty,
    };

    private static IngredientDisplayCategory? CategoryFromStorageType(string storageType) => storageType switch
    {
        "refrigerator" => IngredientDisplayCategory.Refrigerator,
        "upperShelf" => IngredientDisplayCategory.UpperShelf,
        "lowerShelf" => IngredientDisplayCategory.LowerShelf,
        _ => null,
    };

    private void EmitLog(string level, string message)
    {
        EmitJson(JsonUtility.ToJson(new LogEvent
        {
            type = "bridge-log",
            level = level,
            message = message,
            scene = SceneManager.GetActiveScene().name,
            realtime = Time.realtimeSinceStartup,
            frame = Time.frameCount,
        }));
    }

    private void EmitTargetMap()
    {
        EmitJson(JsonUtility.ToJson(new TargetMap
        {
            type = "target-map",
            targets = CaptureTargets(),
        }));
    }

    private static E2EUiTargetRegistry.TargetState[] CaptureTargets()
    {
        var targets = E2EUiTargetRegistry.Capture();
        targets.AddRange(E2EWorldTargetRegistry.Capture());
        return targets.ToArray();
    }

    private static void EmitJson(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        AftertasteE2EEmit(json);
#else
        Debug.Log($"[AftertasteE2E] {json}");
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void AftertasteE2EEmit(string json);
#endif
}
#endif
