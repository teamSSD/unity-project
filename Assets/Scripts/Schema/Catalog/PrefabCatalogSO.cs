using UnityEngine;

namespace Game.Schema.Catalog
{
    /// <summary>
    /// 동적으로 인스턴스화되는 프리팹의 카탈로그.
    /// Resources.Load&lt;GameObject&gt;를 대체하는 SerializeField 모음.
    /// 새 프리팹 추가: 이 SO의 필드 + 인스펙터 드래그.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Catalog/Prefab", fileName = "PrefabCatalog")]
    public class PrefabCatalogSO : ScriptableObject
    {
        [Header("UI Prefabs")]
        public GameObject recipeBook;
        public GameObject settings;
        public GameObject ingredientDescription;
        public GameObject cookingToolDescription;
        public GameObject statUI;

        [Header("Shop Prefabs")]
        public GameObject shopBook;
        public GameObject shopRow;
    }
}
