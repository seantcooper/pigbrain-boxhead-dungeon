using System;
using System.Collections.Generic;

namespace pigbrain.core.Events
{
    #region Base Event
    public abstract class EventBase
    {
        public object target;
        public bool stopPropagation;
        public void StopPropagation() => stopPropagation = true;
    }
    #endregion

    #region Standard Events
    public sealed class IndexChangeEvent : EventBase
    {
        public int index;
        public IndexChangeEvent(object target, int index)
        {
            this.target = target;
            this.index = index;
        }
    }
    #endregion

    public class EventDispatcher
    {
        readonly Dictionary<Type, List<Delegate>> callbacks = new();

        public void AddListener<T>(Action<T> cb, T initial = null) where T : EventBase
        {
            if (callbacks.TryGetValue(typeof(T), out var list)) list.Add(cb);
            else callbacks[typeof(T)] = list = new() { cb };
            if (initial != null) cb(initial);
        }

        public void RemoveListener<T>(Action<T> cb) where T : EventBase
        {
            if (callbacks.TryGetValue(typeof(T), out var list))
                list.Remove(cb);
        }

        public void Invoke<T>(T evt) where T : EventBase
        {
            if (!callbacks.TryGetValue(typeof(T), out var list)) return;
            for (int i = 0; i < list.Count; i++)
            {
                ((Action<T>)list[i])(evt);
                if (evt.stopPropagation) return;
            }
        }
    }
}