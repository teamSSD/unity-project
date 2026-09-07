using Game.Schema.State.Garden;
using Game.Schema.State.Mall;
using Game.Schema.State.Shop;

namespace Game.Schema.State
{
    /// <summary>
    /// 게임 전체 상태의 POCO 컨테이너 (ADR-001 Option B).
    /// GameSessionStore가 소유하고 Composition Root가 하위 상태를 서비스에 주입.
    /// Phase 3-C 진행에 따라 도메인별 sub-state 추가됨.
    /// </summary>
    public class GameState
    {
        public GardenState garden = new GardenState();
        public ShopState shop = new ShopState();
        public MallState mall = new MallState();
        public InventoryState inventory = new InventoryState();

        // 글로벌 게임 상태 (System 카테고리 매니저들이 facade로 접근)
        public BasicStats stats = new BasicStats();
        public PhaseData phase = new PhaseData();
        public TutorialState tutorial = new TutorialState();
    }
}
