namespace Game.Schema.State.Mall
{
    /// <summary>
    /// Mall 도메인 상태 컨테이너.
    /// persistent: NPC별 퀘스트 단계 (디스크 저장)
    /// session 필드는 OrderService 마이그레이션(3-C-3-b)에서 추가됨
    /// </summary>
    public class MallState
    {
        public MallPersistent persistent = new MallPersistent();
    }
}
