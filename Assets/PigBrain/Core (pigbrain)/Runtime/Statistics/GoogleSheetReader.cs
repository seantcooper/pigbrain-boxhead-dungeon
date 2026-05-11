using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using pigbrain.core.Utility;
using UnityEngine;
using UnityEngine.Networking;

namespace pigbrain.core.Statistics
{
    [Serializable]
    public class GoogleSheetReader
    {
        public string id = "1VC23MF7jl22sUPbB9ARQt-k1ZXgbA2oiwb4z0tm079U";
        public string sheet = "0";
        internal string url =>
            $"https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={sheet}";

        public GoogleSheetReader() { id = sheet = ""; }
        public GoogleSheetReader(string id, string sheet)
        {
            this.id = id;
            this.sheet = sheet;
        }

        public void Read(Action onRead, Action onError = null)
        {
#if UNITY_EDITOR
            Unity.EditorCoroutines.Editor.EditorCoroutineUtility
               .StartCoroutineOwnerless(LoadCSV(onRead, onError));
#endif
        }

        public void ReadRuntime(Action onRead, Action onError = null)
        {
            ApplicationMonitor.Instance.StartCoroutine(LoadCSV(onRead, onError));
        }

        [SerializeField] internal Line[] lines;
        internal IEnumerator LoadCSV(Action onRead = null, Action onError = null)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                onError?.Invoke();
            }
            else
            {
                ParseCSV(www.downloadHandler.text);
                onRead?.Invoke();
            }
        }

        void ParseCSV(string csv)
        {
            string[] lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[] keys = Split(lines[0]);
            var parsedLines = lines.Skip(1)
                .Select(txt => new Line(keys.Zip(Split(txt), (k, v) => (k, v)).ToArray()));

            this.lines = parsedLines
                .Where(l => l.groups.Length > 0)
                .ToArray();

            Debug.Log("Data read successfully!");
        }

        string[] Split(string row)
        {
            List<string> list = new();
            string current = "";
            bool inQuotes = false;
            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                if (c == '"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    var v = current.Trim();
                    list.Add(string.IsNullOrEmpty(v) || v == "-" ? "" : v);
                    current = "";
                }
                else current += c;
            }
            {
                var v = current.Trim();
                list.Add(string.IsNullOrEmpty(v) || v == "-" ? "" : v);
            }
            return list.ToArray();
        }

        [Serializable]
        public class Line
        {
            public Group[] groups;

            public Group this[string key] => groups.FirstOrDefault(g => g.key == key);
            public string GetValue(string key, string defaultValue)
            {
                var group = this[key];
                if (group == null) return defaultValue;
                return group.GetValue();
            }
            public string[] GetValues(string key, string[] defaultValue)
            {
                var group = this[key];
                if (group == null) return defaultValue;
                return group.values;
            }

            public T GetValue<T>(string key, T defaultValue) where T : struct
            {
                var group = this[key];
                if (group == null) return defaultValue;
                return group.GetValue<T>();
            }

            public T[] GetValues<T>(string key, T[] defaultValue) where T : struct
            {
                var group = this[key];
                if (group == null) return defaultValue;
                return group.GetValues<T>().ToArray();
            }

            public Line((string key, string value)[] keyValues) =>
                groups = keyValues
                    .GroupBy(kv => kv.key)
                    .Select(g => new Group
                    {
                        key = g.Key,
                        values = g.Select(kv => kv.value)
                                  .Where(v => !string.IsNullOrEmpty(v))
                                  .ToArray()
                    })
                    .Where(g => g.values.Length > 0)
                    .ToArray();


            public class Group
            {
                public string key;
                public string[] values;
                public string this[int i] => i < values.Length ? values[i] : "";
                public string GetValue(int i = 0) => values[i];
                public T GetValue<T>(int i = 0) where T : struct => GetValues<T>().ToArray()[i];
                public IEnumerable<T> GetValues<T>() where T : struct => values.Select(v => ParseValue<T>(v));
                static T ParseValue<T>(string value) where T : struct
                {
                    var type = typeof(T);
                    if (type == typeof(bool))
                        return (T)(object)(bool.TryParse(value, out bool v) ? v : default);
                    if (type == typeof(int))
                        return (T)(object)(int.TryParse(value, out int v) ? v : default);
                    if (type == typeof(float))
                        return (T)(object)(float.TryParse(value, out float v) ? v : default);
                    if (type.IsEnum)
                        return Enum.TryParse(value, true, out T v) ? v : default;
                    throw new NotSupportedException($"Unsupported field type: {type.Name}");
                }

            }
        }
        // [Serializable]
        // public class KeyValue
        // {
        //     public string key;
        //     public string value;
        //     public KeyValue(string key, string value)
        //     {
        //         this.key = key;
        //         this.value = value;
        //     }
        // }
    }

    public class TypeInterface<T>
    {
        public T target;
        public object this[string name]
        {
            get => null;
            set => target.GetType().GetField(name)?.SetValue(target, value);
        }
        public TypeInterface(T target) => this.target = target;
    }

}

#if UNITY_EDITOR
#region Editor
namespace pigbrain.core.Statistics
{
    using System.Linq;
    using pigbrain.core.Geom;
    using UnityEditor;
    using UnityEngine;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(GoogleSheetReader), true)]
    public class GoogleSheetReader_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                float LabelSize = MiniFieldWidth;
                var p = pos.WithH(LineHeight).AddMinX(IndentSize);
                GUI.Label(p.WithW(LabelSize), "GSheet:");
                var r = p.AddMinX(LabelSize);
                prop.GetProperties(nameof(GoogleSheetReader.id), nameof(GoogleSheetReader.sheet))
                    .DrawPropertyFields(r.DivideArea(Padding, 0, MiniFieldWidth * 1.5f).ToArray());
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) => FullLineHeight;
    }
}
#endregion
#endif
