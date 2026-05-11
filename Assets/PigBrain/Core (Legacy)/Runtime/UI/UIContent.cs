// #pragma warning disable UDR0001
// using System.Collections.Generic;
// using System.Linq;
// using System.Text.RegularExpressions;
// using PigBrain.LegacyCore.Utility;
// using TMPro;
// using Unity.VisualScripting;
// using UnityEngine;
// using UnityEngine.UI;
// using static System.StringComparison;

// namespace PigBrain.LegacyCore.UI
// {
//     public class UIContent : MonoBehaviour
//     {
//         // [SerializeField] internal bool useAlias = true;
//         [SerializeField][BoolButton(nameof(Test))] bool test;
//         void Test()
//         {
//             Debug.Log($"Query Test: {Q("Camera")}");
//         }

//         void OnTransformChildrenChanged()
//         {
//             Debug.Log("UIContent: Hierarchy changes");
//         }

//         #region Query
//         static Dictionary<string, string> aliasCache = new();
//         string GetAlias(Transform transform) => aliasCache.TryGetValue(transform.name, out string alias)
//             ? alias : alias = transform.name.Split("(").Last().Split(")").First();

//         internal string GetName(Transform transform) => GetAlias(transform);

//         public RectTransform Q(string path) => Q<RectTransform>(path);
//         public T Q<T>(string path) where T : UnityEngine.Object =>
//             Q<T>((RectTransform)transform, path);
//         public T Q<T>(RectTransform baseTransform, string path) where T : UnityEngine.Object
//         {
//             if (typeof(T) == typeof(RectTransform))
//                 return Q(baseTransform, path) as T;
//             if (typeof(Component).IsAssignableFrom(typeof(T)))
//                 return Q(baseTransform, path).GetComponentInChildren<T>();
//             else throw new System.Exception($"{typeof(T)} is not implemented!");
//         }

//         RectTransform Q(Transform baseTransform, string path)
//         {
//             var matches = Query(baseTransform, path);
//             if (matches.Count() == 0) Debug.LogError($"Q('{path}') not found!");
//             return matches.FirstOrDefault();
//         }

//         IEnumerable<RectTransform> Query(Transform transform, string path)
//         {
//             string[] parts = path.Split("/");
//             string last = parts.Last();

//             Regex regex = !last.Contains('*') ? null : new Regex($"^{Regex.Escape(last).Replace(@"\*", ".*")}$",
//                 RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

//             IEnumerable<RectTransform> Recursive(Transform transform, int index)
//             {
//                 foreach (Transform child in transform)
//                 {
//                     if (child is not RectTransform rtchild) continue;
//                     var name = GetName(child);

//                     if (name == "-")
//                         foreach (var q in Recursive(child, index)) yield return q;

//                     else if (index < parts.Length - 1)
//                     {
//                         if (Compare(name, parts[index]))
//                             foreach (var q in Recursive(child, index + 1)) yield return q;
//                     }

//                     else if (regex?.IsMatch(name) ?? Compare(name, last))
//                         yield return rtchild;
//                 }
//             }
//             return Recursive(transform, 0);
//         }
//         bool Compare(string s1, string s2) => string.Equals(s1, s2, InvariantCultureIgnoreCase);
//     }
//     #endregion

//     #region Extentions
//     static class UIContentExtensions
//     {
//         static UIContent uiContent;
//         static UIContent UIContent => uiContent ? uiContent : uiContent = Object.FindAnyObjectByType<UIContent>();
//         public static RectTransform Q(string path) => Q<RectTransform>(path);
//         public static T Q<T>(string path) where T : Object => UIContent.Q<T>(path);
//         public static T Q<T>(this RectTransform transform, string path) where T : Component =>
//             transform.GetComponentInParent<UIContent>().Q<T>(transform, path);
//         public static RectTransform Q(this RectTransform transform, string path) =>
//             transform.GetComponentInParent<UIContent>().Q<RectTransform>(transform, path);

//         public static TextMeshProUGUI TMP(this RectTransform transform) => transform.GetComponentInChildren<TextMeshProUGUI>();
//         public static Image Image(this RectTransform transform) => transform.GetComponentInChildren<Image>();
//         public static Button Button(this RectTransform transform) => transform.GetComponentInChildren<Button>();

//         public static void Deactivate(this RectTransform transform) => transform.gameObject.SetActive(false);
//         public static void Activate(this RectTransform transform) => transform.gameObject.SetActive(true);

//         public class ActiveScope : System.IDisposable
//         {
//             readonly RectTransform[] transforms;
//             public ActiveScope(RectTransform transform, bool bubble = false)
//             {
//                 transforms = bubble ? transform.Iterate(rt => rt.parent as RectTransform)
//                     .Where(t => !t.gameObject.activeSelf).ToArray()
//                     : new[] { transform };
//                 transforms.ForEach(rt => rt.gameObject.SetActive(true));
//             }

//             void System.IDisposable.Dispose() =>
//                 transforms.ForEach(rt => rt.gameObject.SetActive(false));

//         }
//     }
//     #endregion
// }

// #region Editor
// #if UNITY_EDITOR
// namespace PigBrain.LegacyCore.UI
// {
//     using UnityEditor;
//     [CustomEditor(typeof(UIContent))]
//     public class Stats_Editor : Editor
//     {
//         UIContent content => target as UIContent;

//         public override void OnInspectorGUI()
//         {
//             base.OnInspectorGUI();
//             using (new LabelWidthScope(EditorGUIUtility.labelWidth * 1.5f))
//             {
//                 foreach (var (path, transform) in GetTree())
//                 {
//                     using (new EditorGUILayout.HorizontalScope())
//                     {
//                         EditorGUILayout.SelectableLabel(path, GUILayout.Height(EditorGUIUtility.singleLineHeight));
//                         EditorGUILayout.ObjectField(transform, typeof(RectTransform), true);
//                     }
//                 }
//             }
//         }

//         IEnumerable<(string path, Transform transform)> GetTree()
//         {
//             IEnumerable<(string path, Transform transform)> Recursive(string path, Transform e)
//             {
//                 foreach (Transform c in e)
//                 {
//                     var name = content.GetName(c);
//                     bool ignore = name == "-";
//                     string childPath = ignore ? path : path == "" ? name : $"{path}/{name}";
//                     if (!ignore) yield return (childPath, c);
//                     foreach (var r in Recursive(childPath, c))
//                         yield return r;
//                 }
//             }
//             return Recursive("", content.transform);
//         }
//     }
// }
// #endif
// #endregion
