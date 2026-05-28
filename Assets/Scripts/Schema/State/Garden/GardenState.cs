namespace Game.Schema.State.Garden
{
    /// <summary>
    /// Garden 도메인 상태 컨테이너. persistent + 세션 휘발성으로 분리.
    /// </summary>
    public class GardenState
    {
        public GardenPersistent persistent = new GardenPersistent();
    }
}
