using System.Collections.Generic;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Audio;
using pigbrain.game.Boxhead.Statistic;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [CreateAssetMenu(menuName = "PigBrain/Boxhead/Commands/Stat Modify")]
    public class CommandStatModify : Command
    {
        [Header("Stat Modify")]
        [SerializeField] StatsLink statsOverride;
        [SerializeField] Upgrade.UStat[] mods;

        protected override bool OnInvoke(Transform target)
        {
            Stats GetStats()
            {
                if (statsOverride) return statsOverride;
                if (target.TryGetComponent(out IStats component) && component.GetStats() is Stats stats)
                    return stats;
                return null;
            }

            if (GetStats() is Stats stats)
            {
                sound.Play(target.position);
                stats.AddUpgrades(Upgrade.CreateInstance(name, true, mods));
                return true;
            }
            return false;
        }

    }
}
