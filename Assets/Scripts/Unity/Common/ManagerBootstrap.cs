using UnityEngine;

/// <summary>
/// 매니저 싱글톤 초기화 유틸리티.
/// 각 씬 컨트롤러에서 중복되던 EnsureXxxManager() 패턴을 통합.
/// </summary>
public static class ManagerBootstrap
{
    /// <summary>
    /// 싱글톤 매니저가 없으면 새 GameObject에 생성.
    /// Instance 프로퍼티가 null을 반환하는 매니저에 사용.
    /// </summary>
    public static T Ensure<T>() where T : MonoBehaviour
    {
        // 이미 존재하면 찾아서 반환
        var existing = Object.FindFirstObjectByType<T>();
        if (existing != null) return existing;

        var go = new GameObject(typeof(T).Name);
        var component = go.AddComponent<T>();
        return component;
    }

    /// <summary>
    /// GameStart에서 호출 — 게임에 필요한 모든 매니저 일괄 생성.
    /// </summary>
    public static void EnsureAll()
    {
        Ensure<UnlockedFoodManager>();
        Ensure<InventoryManager>();
        Ensure<RecipeDataManager>();
        Ensure<RecipeLookupService>();
        Ensure<ActionSelectionManager>();
        Ensure<LoadingManager>();
        Ensure<HUDManager>();
        Ensure<UIManager>();
        Ensure<WeatherSystem>();
        Ensure<SettlementManager>();
        Ensure<ShopUIAdapter>();
        Ensure<SettingsUIManager>();
        EnsureRecipeBookManager();
    }

    /// <summary>
    /// RecipeBookManager는 프리팹에서 로드 (특수 처리, PrefabCatalog 경유)
    /// </summary>
    private static void EnsureRecipeBookManager()
    {
        if (RecipeBookManager.HasInstance) return;
        var prefab = CatalogProvider.Prefabs?.recipeBook;
        if (prefab != null)
        {
            Object.Instantiate(prefab);
        }
        else
        {
            Debug.LogError("[ManagerBootstrap] RecipeBook prefab not in PrefabCatalog");
        }
    }
}
