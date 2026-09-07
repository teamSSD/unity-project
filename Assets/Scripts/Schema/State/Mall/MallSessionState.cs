using System.Collections.Generic;

namespace Game.Schema.State.Mall
{
    /// <summary>
    /// Mall 도메인의 런타임 상태. 서비스가 아닌 GameSessionStore가 소유한다.
    /// </summary>
    public sealed class MallSessionState
    {
        public List<DeliveryOrderData> Orders { get; } = new();
    }
}
