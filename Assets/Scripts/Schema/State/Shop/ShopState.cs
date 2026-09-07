namespace Game.Schema.State.Shop
{
    /// <summary>
    /// Shop 도메인 상태 컨테이너.
    /// </summary>
    public class ShopState
    {
        public ShopPersistent persistent = new ShopPersistent();
        public ShopSessionState session = new ShopSessionState();
    }
}
