using System.Linq;
using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.game.Boxhead.Environment
{
    public class ObjectList : ScriptableObject
    {
        [FlattenObject] public Object[] objects;

        public Object this[string name] => Find<Object>(name, out Object t) ? t : null;

        public bool Find<T>(string name, out T result) where T : Object => result = objects
            .Select(o => o as T)
            .FirstOrDefault(o => o &&
                o.name.Contains(name, System.StringComparison.InvariantCultureIgnoreCase));
    }
}