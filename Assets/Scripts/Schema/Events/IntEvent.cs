using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Schema.Events
{
    /// <summary>
    /// int 값을 전달하는 이벤트. 예: OnMoneyChanged, OnStaminaChanged.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Events/Int", fileName = "IntEvent")]
    public class IntEvent : GameEventBase
    {
        private readonly List<Action<int>> _listeners = new List<Action<int>>();

        public void Subscribe(Action<int> listener)
        {
            if (listener == null) return;
            if (!_listeners.Contains(listener)) _listeners.Add(listener);
        }

        public void Unsubscribe(Action<int> listener)
        {
            if (listener == null) return;
            _listeners.Remove(listener);
        }

        public void Raise(int value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (i < _listeners.Count) _listeners[i]?.Invoke(value);
            }
        }

        public int ListenerCount => _listeners.Count;
    }
}
