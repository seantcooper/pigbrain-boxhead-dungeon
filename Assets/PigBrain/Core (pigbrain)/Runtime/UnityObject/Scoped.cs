using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public class Scoped<T> : System.IDisposable where T : UnityEngine.Object
    {
        public readonly T target;
        readonly bool immediate;
        public Scoped(T target, bool immediate = true)
        {
            this.target = target;
            this.immediate = immediate;
        }

        void System.IDisposable.Dispose()
        {
            if (immediate) Object.DestroyImmediate(target);
            else Object.Destroy(target);
        }

        public static implicit operator T(Scoped<T> scope) =>
            scope.target;
    }
}