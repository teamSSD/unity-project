using System;
using Game.Schema.State;

namespace Game.Domain.Common
{
    /// <summary>
    /// 게임 세션 상태의 소유권 경계. 서비스는 이 Store가 소유한 하위 상태만 주입받는다.
    /// 상태 교체는 모든 서비스가 Store 조회 방식으로 이전된 뒤 추가한다.
    /// </summary>
    public sealed class GameSessionStore
    {
        public GameState State { get; }

        public GameSessionStore(GameState initialState)
        {
            State = initialState ?? throw new ArgumentNullException(nameof(initialState));
        }
    }

    public static class NewGameStateFactory
    {
        public static GameState Create() => new();
    }
}
