using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Project;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    [ProjectInterface.Control(ProjectInterface.Filter.Hierarchy, 99)]
    public class Game : MonoBehaviourSingleton<Game>
    {
        void Start()
        {
#if UNITY_EDITOR
            Input.GamePlay.Fast.performed += ctx => Fast(true);
            Input.GamePlay.Fast.canceled += ctx => Fast(false);
#endif
            Persistence.Read();
        }

        TimeScale.Scope timescaleScope;
        void Fast(bool state)
        {
            if (state) timescaleScope = new TimeScale.Scope(4);
            else if (timescaleScope) timescaleScope.Dispose();
        }

        BoxheadInput input;
        public static BoxheadInput Input
        {
            get
            {
                if (!Instance || Instance.isQuitting) return null;
                if (Instance.input == null)
                {
                    Instance.input = new BoxheadInput();
                    Instance.input.Enable();
                }
                return Instance.input;
            }
        }

        protected override void OnDestroy()
        {
            input?.Disable();
            // base.OnDestroy();
        }

        void OnApplicationQuit()
        {
            Persistence.Write();
        }

        Coroutine monitorPlayerCamera;
        bool playerCamera3DToggle = false;
        public bool camera3DToggle
        {
            get => playerCamera3DToggle;
            set
            {
                playerCamera3DToggle = value;
                if (monitorPlayerCamera == null) monitorPlayerCamera = StartCoroutine(MonitorPlayerCamera());
            }
        }

        IEnumerator MonitorPlayerCamera()
        {
            (Player player, Camera camera) monitor = (null, null);
            while (enabled)
            {
                if (ActiveRoom.Instance && monitor.player != ActiveRoom.Instance.player)
                {
                    monitor.player = ActiveRoom.Instance.player;
                    monitor.camera = monitor.player.GetComponentInChildren<Camera>(true);
                }
                if (monitor.camera && monitor.camera.gameObject.activeSelf != playerCamera3DToggle)
                    monitor.camera.gameObject.SetActive(playerCamera3DToggle);

                yield return CoroutineUtility.WaitSecondsQuarter;
            }
        }
    }
}

// #region Colors
// public class Colors
// {
//     public static Color Red = new Color32(255, 0, 75, 255);
//     public static Color Level1 = "#9C9C9C".ToColor(); // white
//     public static Color Level2 = "#17B253".ToColor(); // green
//     public static Color Level3 = "#32B5D9".ToColor(); // blue
//     public static Color Level4 = "#8351D6".ToColor(); // purple
//     public static Color Level5 = "#FF8700".ToColor(); // orange
// }
// #endregion
