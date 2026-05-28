using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Schema.Events
{
    /// <summary>
    /// float 값을 전달하는 이벤트. 예: OnTimeChanged, OnProgressUpdated.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Events/Float", fileName = "FloatEvent")]
    public class FloatEvent : GameEventBase
    {
        private readonly List<Action<float>> _listeners = new List<Action<float>>();

        public void Subscribe(Action<float> listener)
        {
            if (listener == null) return;
            if (!_listeners.Contains(listener)) _listeners.Add(listener);
        }

        public void Unsubscribe(Action<float> listener)
        {
            if (listener == null) return;
            _listeners.Remove(listener);
        }

        public void Raise(float value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                if (i < _listeners.Count) _listeners[i]?.Invoke(value);
            }
        }

        public int ListenerCount => _listeners.Count;
    }
}
