// #pragma warning disable UDR0001
// using System.Collections.Generic;
// using UnityEngine;
// using static UnityEngine.JsonUtility;
// using PigBrain.LegacyCore.Utility;
// using System.Linq;
// using System;

// namespace PigBrain.LegacyCore.Analytics
// {
//     public static class ScriptableObjectMonitor
//     {
//         static readonly Dictionary<ScriptableObject, string> backup = new();

//         public static void Backup(this ScriptableObject so)
//         {
//             if (!Application.isPlaying) return;
//             if (backup.ContainsKey(so)) return;
//             Debug.Log($"ScriptableObjectMonitor::Backup {so.name}");
//             backup[so] = ToJson(so);
//         }

//         public static void Restore(this ScriptableObject so)
//         {
//             if (!backup.ContainsKey(so)) return;
//             var compare = ToJson(so);
//             if (compare == backup[so]) return;
//             FromJsonOverwrite(backup[so], so);
//             if (so is IScriptableObjectRestorable restorable)
//                 restorable.OnRestore();
//             Debug.Log($"ScriptableObjectMonitor::Restore {so.name}");
//         }

//         public static void Restore() =>
//             backup.ForEach(p => Restore(p.Key));

//         public static IEnumerable<T> GetTypes<T>() where T : ScriptableObject =>
//             backup.Keys.Where(p => p.GetType() == typeof(T) || p.GetType().IsSubclassOf(typeof(T))).Cast<T>();

//         public static void Restore<T>() where T : ScriptableObject =>
//             GetTypes<T>().ForEach(p => Restore(p));


//     }

//     public interface IScriptableObjectRestorable
//     {
//         void OnRestore();
//     }
// }