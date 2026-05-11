using System.Collections.Generic;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.core.Analysis
{
    public class BehaviourDebug : MonoBehaviour
    {
        const int MaxMessages = 25;
        [SerializeField] List<string> messages = new();
        public void AddMessage(string message)
        {
            messages.Add(message);
            if (messages.Count > MaxMessages)
                messages.RemoveAt(0);
        }
    }

    public static class BehaviourDebugX
    {
        public static void LogMessage(this MonoBehaviour behaviour, object message)
        {
            if (!Application.isEditor) return;
            behaviour.TryAddComponent(out BehaviourDebug debug);
            debug.AddMessage($"{message}");
        }
    }
}