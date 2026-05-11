using pigbrain.core.Geom;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [DefaultExecutionOrder(-1000)]
    public class GameTime : MonoBehaviourSingleton<GameTime>
    {
        public static float time { get; set; }
        public static float timeScale { get; set; }

        void Update()
        {
            time += Time.deltaTime * timeScale;
        }
    }
}