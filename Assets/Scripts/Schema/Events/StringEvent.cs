using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Schema.Events
{
    /// <summary>
    /// string 값을 전달하는 이벤트. 예: OnUpgradeApplied(key), OnSceneTransitionRequested(name).
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Events/String", fileName = "StringEvent")]
    public class StringEvent : GameEventBase
    {
        private readonly List<Action<string>> _listeners = new List<Action<string>>();

        public void Subscribe(Action<string> listener)
        {
            if (listener == null) return;
            if (!_listeners.Contains(listener)) _listeners.Add(listener);
        }

        public void Unsubscribe(Action<string> listener)
        {
            if (listener == null) return;
            _listeners.Remove(listener);
        }

        public void Raise(string value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (i < _listeners.Count) _listeners[i]?.Invoke(value);
            }
        }

        public int ListenerCount => _listeners.Count;
    }
}
