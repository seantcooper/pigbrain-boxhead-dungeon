using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public abstract class CommandFilter : ScriptableObject
    {
        public abstract bool Filter(Command command);
    }
}
