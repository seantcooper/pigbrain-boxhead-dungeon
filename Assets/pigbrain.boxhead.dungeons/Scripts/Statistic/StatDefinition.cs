using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace pigbrain.game.Boxhead.Statistic
{
    [CreateAssetMenu(menuName = "Stats/Stat Collection")]
    public class StatDefinition : ScriptableObject
    {
        [SerializeReference]
        public List<Stat> stats = new();

        public event Action<Stat> OnAdd;
        public event Action<Stat> OnRemove;
        public event Action<Stat> OnChange;

        public T Get<T>(string name) where T : Stat => stats.OfType<T>().FirstOrDefault(s => s.name == name);
        public void Set<T>(string name, object value) where T : Stat
        {
            var stat = Get<T>(name);
            switch (stat)
            {
                default: return;
                case StatInt s: s.Set(Convert.ToInt32(value)); break;
                case StatFloat s: s.Set(Convert.ToSingle(value)); break;
                case StatBool s: s.Set(Convert.ToBoolean(value)); break;
                case StatString s: s.Set(Convert.ToString(value)); break;
            }
            OnChange?.Invoke(stat);
        }

        public void Add(Stat stat)
        {
            stats.Add(stat);
            OnAdd?.Invoke(stat);
        }

        public void Remove(Stat stat)
        {
            stats.Remove(stat);
            OnRemove?.Invoke(stat);
        }

        public void Remove(string name)
        {
            for (int i = 0; i < stats.Count; i++)
                if (stats[i].name.Equals(name, StringComparison.OrdinalIgnoreCase))
                { Remove(stats[i]); break; }
        }

        [Serializable]
        public abstract class Stat
        {
            public string name;
            public event Action OnChanged;
            protected void Changed() => OnChanged?.Invoke();
        }

        [Serializable]
        public class StatInt : Stat
        {
            public int value;
            public void Set(int v) { if (value != v) { value = v; Changed(); } }
        }

        [Serializable]
        public class StatFloat : Stat
        {
            public float value;
            public void Set(float v) { if (Math.Abs(value - v) > float.Epsilon) { value = v; Changed(); } }
        }

        [Serializable]
        public class StatBool : Stat
        {
            public bool value;
            public void Set(bool v) { if (value != v) { value = v; Changed(); } }
        }

        [Serializable]
        public class StatString : Stat
        {
            public string value;
            public void Set(string v) { if (value != v) { value = v; Changed(); } }
        }
    }
}