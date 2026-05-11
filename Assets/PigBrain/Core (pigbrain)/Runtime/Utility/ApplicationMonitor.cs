using System;
using pigbrain.core.UnityObject;
using UnityEngine;
using static UnityEngine.Object;

namespace pigbrain.core.Utility
{
    public class ApplicationMonitor : MonoBehaviourSingleton<ApplicationMonitor>
    {
        void Start()
        {
        }

        void OnApplicationQuit() => Monitor.OnApplicationQuit();
    }

    public static class Monitor
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void OnInitialize() =>
            DontDestroyOnLoad(new GameObject("Monitor", typeof(ApplicationMonitor)));

        public static event Action OnApplicationExit;

        internal static void OnApplicationQuit()
        {
            OnApplicationExit?.Invoke();
            OnApplicationExit = null;
        }
    }
}