using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.core.Analytics
{
    [InlineButton(nameof(Fetch))]
    public abstract class Query : ScriptableObject
    {
        public abstract void Fetch();
    }
}