using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Schema.Events
{
    /// <summary>
    /// 값 없는(void) 이벤트. Subscribe로 listener 등록, Raise로 발행.
    /// 같은 listener를 두 번 Subscribe해도 1회만 호출됨 (중복 방지).
    /// Raise 중 listener가 Unsubscribe해도 안전 (역순 순회).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Events/Void", fileName = "GameEvent")]
    public class GameEvent : GameEventBase
    {
        private readonly List<Action> _listeners = new List<Action>();

        public void Subscribe(Action listener)
        {
            if (listener == null) return;
            if (!_listeners.Contains(listener)) _listeners.Add(listener);
        }

        public void Unsubscribe(Action listener)
        {
            if (listener == null) return;
            _listeners.Remove(listener);
        }

        public void Raise()
        {
            // 역순: listener가 Raise 도중 Unsubscribe해도 인덱스 안전
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (i < _listeners.Count) _listeners[i]?.Invoke();
            }
        }

        public int ListenerCount => _listeners.Count;
    }
}
