using System.Linq;
using pigbrain.core.Geom;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [CreateAssetMenu(menuName = "PigBrain/Boxhead/Commands/Random")]
    public class CommandRnd : Command
    {
        [Header("Rnd")]
        [SerializeField] uint overrideSeed = 0;
        [SerializeField] CommandWeightedContainer commands;

        public override bool IsValid(Transform target) =>
            base.IsValid(target) && commands.items.Length > 0;

        protected override bool OnInvoke(Transform target)
        {
            uint seed = overrideSeed == 0 ? Rnd.GetTimeSeed() : overrideSeed;

            var items = commands.items.Select(i => (i.command, i.weight));

            Rnd.GetInstance(this, seed).NextWeighted(items).Invoke(target);

            // Rnd.GetInstance(this, seed).NextWeighted<Command>(commands.items).Invoke(target);
            return true;
        }
    }
}

