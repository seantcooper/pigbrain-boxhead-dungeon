#pragma warning disable UDR0001
#pragma warning disable UDR0002
using System.Collections.Generic;
using UnityEngine;

namespace pigbrain.core.Utility
{
    public static class ScriptableObjectUtility
    {
        readonly static Dictionary<ScriptableObject, string> snapshot = new();

        public static void RuntimeBackup<T>() where T : ScriptableObject
        {
            Debug.Log($"Backup Scriptable Objects - {typeof(T)}");
            T[] objects = Resources.FindObjectsOfTypeAll<T>();
            foreach (T scriptableObject in objects)
            {
                if (snapshot.TryGetValue(scriptableObject, out string json)) return;
                Debug.Log($"Backup Scriptable Object - {scriptableObject.name} ({scriptableObject.GetType()})");
                snapshot.Add(scriptableObject, JsonUtility.ToJson(scriptableObject));
                if (scriptableObject is IScriptableObjectRuntime b)
                    b.OnBackup();
            }
#if UNITY_EDITOR
            Monitor.OnApplicationExit += EditorRestore;
#endif
        }

        static void RuntimeRestore(this ScriptableObject scriptableObject)
        {
            if (!snapshot.TryGetValue(scriptableObject, out string json)) return;
            Debug.Log($"Restore Scriptable Object - {scriptableObject.name} ({scriptableObject.GetType()})");
            JsonUtility.FromJsonOverwrite(json, scriptableObject);
        }

        public static void RuntimeRestoreAndStart()
        {
            Debug.Log("Runtime Restore Scriptable Objects");
            if (snapshot.Count == 0) return;
            foreach (var scriptableObject in snapshot.Keys)
            {
                RuntimeRestore(scriptableObject);
                if (scriptableObject is IScriptableObjectRuntime b)
                    b.OnStart();
            }
        }

        public static void EditorRestore()
        {
            Debug.Log("Editor Restore Scriptable Objects");
            if (snapshot.Count == 0) return;
            foreach (var scriptableObject in snapshot.Keys)
            {
                RuntimeRestore(scriptableObject);
                if (scriptableObject is IScriptableObjectRuntime b)
                    b.OnQuit();
            }
            snapshot.Clear();
        }
    }

    public interface IScriptableObjectRuntime
    {
        void OnBackup();
        void OnStart();
        void OnQuit(); // only used for the Unity Editor
    }
}