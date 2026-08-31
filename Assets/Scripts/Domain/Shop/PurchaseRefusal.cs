namespace Game.Domain.Shop
{
    public enum PurchaseRefusalKind
    {
        Money,
        Storage,
    }

    /// <summary>
    /// 구매 실패 사유 + 컨텍스트. Storage 종류일 때 카테고리/사용량/상한 채워짐.
    /// </summary>
    public readonly struct PurchaseRefusal
    {
        public readonly PurchaseRefusalKind Kind;
        public readonly IngredientDisplayCategory Category;
        public readonly int Used;
        public readonly int Max;

        public PurchaseRefusal(PurchaseRefusalKind kind, IngredientDisplayCategory category, int used, int max)
        {
            Kind = kind;
            Category = category;
            Used = used;
            Max = max;
        }
    }
}
