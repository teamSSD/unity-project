using UnityEngine;

namespace Game.Schema.Catalog
{
    /// <summary>
    /// 게임 데이터 테이블 CSV 자산 카탈로그.
    /// Resources.Load&lt;TextAsset&gt; 패턴을 SerializeField로 대체.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Catalog/Csv", fileName = "CsvCatalog")]
    public class CsvCatalogSO : ScriptableObject
    {
        [Header("Garden / Shop Upgrade Tables")]
        public TextAsset farmUpgrade;
        public TextAsset cropData;
        public TextAsset storageUpgrade;
        public TextAsset toolUpgrade;

        [Header("Dialogue / Quest Tables")]
        public TextAsset npcCasualDialogue;
        public TextAsset deliveryQuest;
    }
}
