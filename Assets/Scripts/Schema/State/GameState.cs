using Game.Schema.State.Garden;
using Game.Schema.State.Mall;
using Game.Schema.State.Shop;

namespace Game.Schema.State
{
    /// <summary>
    /// 게임 전체 상태의 POCO 컨테이너 (ADR-001 Option B).
    /// Composition Root(GameSessionRoot)에서 인스턴스화 + 서비스에 주입.
    /// Phase 3-C 진행에 따라 도메인별 sub-state 추가됨.
    /// </summary>
    public class GameState
    {
        public GardenState garden = new GardenState();
        public ShopState shop = new ShopState();
        public MallState mall = new MallState();
    }
}
