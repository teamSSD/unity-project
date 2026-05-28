using UnityEngine;

namespace Game.Schema.Events
{
    /// <summary>
    /// SO 기반 이벤트의 추상 베이스.
    /// description 필드로 인스펙터에서 이벤트의 발행 시점/구독 가이드를 명시.
    /// 구체 클래스: GameEvent (void), IntEvent, FloatEvent, StringEvent, 도메인별 payload.
    /// </summary>
    public abstract class GameEventBase : ScriptableObject
    {
        [TextArea(2, 4)]
        [Tooltip("이 이벤트가 언제 발행되고, 어떤 구독자가 사용해야 하는지 기록")]
        public string description;
    }
}
