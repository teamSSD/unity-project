/// <summary>
/// 인벤토리 배치: 동일 시점에 획득한 동일 아이템 묶음.
/// 런타임 모델 (디스크 저장은 InventoryBatchEntry로 변환).
/// </summary>
[System.Serializable]
public class InventoryBatch
{
    public int quantity;
    public int daysRemaining;
}
