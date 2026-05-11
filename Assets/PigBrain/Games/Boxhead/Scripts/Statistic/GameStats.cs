// using pigbrain.core.UnityObject;
// using UnityEngine;
// using System;
// using pigbrain.core.Inspector;

// namespace pigbrain.game.Boxhead.Statistic
// {
//     [DefaultExecutionOrder(-100)]
//     public class GameStats : MonoBehaviourSingleton<GameStats>
//     {
//         [SerializeField][InlineScriptableObject] internal StatsCollection stats;
//         protected override void Awake()
//         {
//             base.Awake();
//             stats = stats.CreateRuntimeInstance();
//         }

//         public static Stats Session =>
//             Instance.GetComponent<StatsController>().GetStats();
//     }

//     [Serializable]
//     public class StatsLink : AssetLink<Stats>
//     {
//         public Stats GetRuntime() => GameStats.Instance.stats[id];
//         public static Stats GetRuntime(Stats stats) => GameStats.Instance.stats[GetID(stats)];

//         public static implicit operator Stats(StatsLink link) => link.GetRuntime();
//     }
// }