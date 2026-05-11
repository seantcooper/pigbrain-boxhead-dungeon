using System;
using System.Collections;
using pigbrain.core.Analysis;
using pigbrain.core.Graphics;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Navigation;
using Unity.AI.Navigation;
using UnityEngine;

namespace pigbrain.game.Boxhead.Environment
{
    public class EnvironmentData : MonoBehaviourSingleton<EnvironmentData>
    {
        [SerializeField] ChainRewardContainer rewards;
        public static ChainRewardContainer GetChainRewardContainer()
        {
            return Instance ? Instance.rewards : null;
        }
    }
}