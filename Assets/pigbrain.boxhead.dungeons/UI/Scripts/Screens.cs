using System.Collections;
using UnityEngine.SceneManagement;
using pigbrain.core.Inspector;
using pigbrain.game.Boxhead.FiniteStateMachine;
using UnityEngine;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Statistic;

namespace pigbrain.game.Boxhead.UI
{
    public class Screens : FSM
    {
        // [Header("Screens")]
        // [SerializeField] public Stats playerStats;

        [Header("States")]
        [ToggleObject] public TitleScreen title;
        [ToggleObject] public MainScreen main;
        [ToggleObject] public LoadingScreen loading;
        [ToggleObject] public GameScreen game;
        [ToggleObject] public PauseScreen pause;
        [ToggleObject] public WastedScreen wasted;
        [ToggleObject] public ADScreen ad;
        [ToggleObject] public OutroScreen outro;

        void Awake() => GetComponentInParent<Canvas>().worldCamera
            .gameObject.SetActive(true);

        public Stats.ControlValue money;

        public void Open(string url) => Application.OpenURL(url);

        // void LateUpdate()
        // {
        //     float t = Time.unscaledTime;

        //     // tweak these
        //     float rotAmount = 0.5f;   // degrees
        //     float scaleAmount = 0.005f;
        //     float speed = 1.2f;

        //     // smooth noise
        //     float n1 = Mathf.PerlinNoise(t * speed, 0f) - 0.5f;
        //     float n2 = Mathf.PerlinNoise(0f, t * speed) - 0.5f;

        //     // rotation (z only for UI)
        //     transform.localRotation = Quaternion.Euler(0, 0, n1 * rotAmount);

        //     // scale (uniform)
        //     float s = 1f + n2 * scaleAmount;
        //     transform.localScale = new Vector3(s, s, 1f);
        // }

        IEnumerator Start()
        {
            UnloadScenes();
            yield return null;
            money = StatsCatalog.Session.TryGetControl(Stat.Money);
            if (enabled) StartFSM(title);
        }

        void UnloadScenes()
        {
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.buildIndex != 0)
                    SceneManager.UnloadSceneAsync(scene);
            }
        }

        TimeScale.Scope timescaleScope;
        public void SetTimeScaleFast(bool state)
        {
            if (state) timescaleScope = new TimeScale.Scope(2);
            else if (timescaleScope) timescaleScope.Dispose();
        }
    }
}