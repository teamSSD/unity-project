using Game.Schema.Catalog;
using UnityEngine;

/// <summary>
/// 모든 catalog SO에 대한 정적 액세스 진입점.
/// Managers 씬에 배치된 GameObject에 부착, 인스펙터에서 각 catalog 에셋 드래그 등록.
/// Resources.Load/LoadAll을 대체하는 cross-cutting 인프라.
/// 사용: CatalogProvider.Food.GetById("I001") / CatalogProvider.Food.All
/// </summary>
public class CatalogProvider : SingletonMonoBehaviour<CatalogProvider>
{
    [Header("Cooking Domain")]
    [SerializeField] private FoodCatalogSO food;
    [SerializeField] private RecipeCatalogSO recipe;
    [SerializeField] private IngredientCatalogSO ingredient;
    [SerializeField] private CookingToolCatalogSO cookingTool;

    [Header("Mall / Delivery")]
    [SerializeField] private DeliveryNpcCatalogSO deliveryNpc;
    [SerializeField] private DialogueConfigCatalogSO dialogueConfig;

    // Garden CropData는 ScriptableObject가 아닌 CSV POCO이므로 catalog 별도 처리 (Phase 3 이후)

    public static FoodCatalogSO Food                     => Instance != null ? Instance.food                : null;
    public static RecipeCatalogSO Recipe                 => Instance != null ? Instance.recipe              : null;
    public static IngredientCatalogSO Ingredient         => Instance != null ? Instance.ingredient          : null;
    public static CookingToolCatalogSO CookingTool       => Instance != null ? Instance.cookingTool         : null;
    public static DeliveryNpcCatalogSO DeliveryNpc       => Instance != null ? Instance.deliveryNpc         : null;
    public static DialogueConfigCatalogSO DialogueConfig => Instance != null ? Instance.dialogueConfig      : null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (food == null)           Debug.LogWarning($"[{nameof(CatalogProvider)}] {nameof(food)} not assigned", this);
        if (recipe == null)         Debug.LogWarning($"[{nameof(CatalogProvider)}] {nameof(recipe)} not assigned", this);
        if (ingredient == null)     Debug.LogWarning($"[{nameof(CatalogProvider)}] {nameof(ingredient)} not assigned", this);
        if (cookingTool == null)    Debug.LogWarning($"[{nameof(CatalogProvider)}] {nameof(cookingTool)} not assigned", this);
        if (deliveryNpc == null)    Debug.LogWarning($"[{nameof(CatalogProvider)}] {nameof(deliveryNpc)} not assigned", this);
        if (dialogueConfig == null) Debug.LogWarning($"[{nameof(CatalogProvider)}] {nameof(dialogueConfig)} not assigned", this);
    }
#endif
}
