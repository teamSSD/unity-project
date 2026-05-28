using System;
using NUnit.Framework;
using UnityEngine;
using Game.Schema.Events;

namespace Tests.EditMode
{
    public class SoEventTests
    {
        [Test]
        public void GameEvent_Raise_NotifiesListener()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int callCount = 0;
            Action handler = () => callCount++;

            evt.Subscribe(handler);
            evt.Raise();

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void GameEvent_Subscribe_DedupesSameListener()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int callCount = 0;
            Action handler = () => callCount++;

            evt.Subscribe(handler);
            evt.Subscribe(handler);
            evt.Subscribe(handler);
            evt.Raise();

            Assert.AreEqual(1, callCount, "동일 listener 중복 등록 시 1회만 호출되어야 함");
        }

        [Test]
        public void GameEvent_Unsubscribe_RemovesListener()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            bool called = false;
            Action handler = () => called = true;

            evt.Subscribe(handler);
            evt.Unsubscribe(handler);
            evt.Raise();

            Assert.IsFalse(called);
        }

        [Test]
        public void GameEvent_UnsubscribeDuringRaise_SafeNoException()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int callCount = 0;
            Action handler = null;
            handler = () =>
            {
                callCount++;
                evt.Unsubscribe(handler);
            };

            evt.Subscribe(handler);
            Assert.DoesNotThrow(() => evt.Raise());
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void GameEvent_RaiseWithNoListeners_NoException()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            Assert.DoesNotThrow(() => evt.Raise());
        }

        [Test]
        public void IntEvent_PassesValue()
        {
            var evt = ScriptableObject.CreateInstance<IntEvent>();
            int received = -1;
            evt.Subscribe(v => received = v);

            evt.Raise(42);

            Assert.AreEqual(42, received);
        }

        [Test]
        public void FloatEvent_PassesValue()
        {
            var evt = ScriptableObject.CreateInstance<FloatEvent>();
            float received = 0f;
            evt.Subscribe(v => received = v);

            evt.Raise(3.14f);

            Assert.AreEqual(3.14f, received, 0.0001f);
        }

        [Test]
        public void StringEvent_PassesValue()
        {
            var evt = ScriptableObject.CreateInstance<StringEvent>();
            string received = null;
            evt.Subscribe(v => received = v);

            evt.Raise("hello");

            Assert.AreEqual("hello", received);
        }

        [Test]
        public void IntEvent_MultipleListeners_AllNotified()
        {
            var evt = ScriptableObject.CreateInstance<IntEvent>();
            int total = 0;
            evt.Subscribe(v => total += v);
            evt.Subscribe(v => total += v * 2);

            evt.Raise(10);

            Assert.AreEqual(30, total, "10 + (10*2) = 30");
        }

        [Test]
        public void GameEvent_NullListener_Ignored()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            Assert.DoesNotThrow(() => evt.Subscribe(null));
            Assert.DoesNotThrow(() => evt.Unsubscribe(null));
            Assert.AreEqual(0, evt.ListenerCount);
        }
    }
}
