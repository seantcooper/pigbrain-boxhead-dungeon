using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace pigbrain.core.Collections
{
    [Serializable]
    public class SerializedDictionaryBase { }

    // Better implementation
    //  https://github.com/ayellowpaper/SerializedDictionary/blob/main/Editor/Scripts/SerializedDictionaryDrawer.cs
    //https://github.com/ayellowpaper/SerializedDictionary/blob/main/Runtime/Scripts/SerializedKeyValuePair.cs

    [Serializable]
    public class SerializedDictionary<TKey, TValue> : SerializedDictionaryBase,
        IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] List<TKey> keys = new();
        [SerializeField] List<TValue> values = new();
        Dictionary<TKey, TValue> dict = new();

        #region IDictionary
        public TValue this[TKey key] { get => dict[key]; set => dict[key] = value; }
        public ICollection<TKey> Keys => dict.Keys;
        public ICollection<TValue> Values => dict.Values;

        public int Count => dict.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) => dict.Add(key, value);
        public bool ContainsKey(TKey key) => dict.ContainsKey(key);
        public bool Remove(TKey key) => dict.Remove(key);
        public bool TryGetValue(TKey key, out TValue value) => dict.TryGetValue(key, out value);

        public void Add(KeyValuePair<TKey, TValue> item) => dict.Add(item.Key, item.Value);
        public void Clear() => dict.Clear();
        public bool Contains(KeyValuePair<TKey, TValue> item) =>
            dict.TryGetValue(item.Key, out var v) &&
            EqualityComparer<TValue>.Default.Equals(v, item.Value);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
            ((IDictionary<TKey, TValue>)dict).CopyTo(array, arrayIndex);

        public bool Remove(KeyValuePair<TKey, TValue> item) => dict.Remove(item.Key);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Serialize
        public void OnBeforeSerialize()
        {
            keys = dict.Keys.ToList();
            values = dict.Values.ToList();
        }

        public void OnAfterDeserialize()
        {
            dict = keys.Zip(values, (k, v) => (k, v))
                .GroupBy(t => t.k).ToDictionary(g => g.Key, g => g.First().v);
            keys = dict.Keys.ToList();
            values = dict.Values.ToList();
            // dict.Clear();
            // int count = Math.Min(keys.Count, values.Count);
            // for (int i = 0; i < count; i++)
            // {
            //     var k = keys[i];
            //     if (k == null) continue;
            //     dict[k] = values[i];
            // }
            // Debug.Log()
        }
        #endregion

        #region Operator
        public static implicit operator bool(SerializedDictionary<TKey, TValue> d) => d != null;
        public static implicit operator Dictionary<TKey, TValue>(SerializedDictionary<TKey, TValue> d) => d.dict;
        #endregion
    }
}

#if UNITY_EDITOR
#region Editor
namespace pigbrain.core.Collections
{
    using System.Linq;
    using pigbrain.core.Geom;
    using UnityEditor;
    using UnityEngine;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(SerializedDictionaryBase), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        readonly Dictionary<string, UnityEditorInternal.ReorderableList> lists = new();

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);

            var foldRect = new Rect(pos.x, pos.y, pos.width, LineHeight);
            prop.isExpanded = EditorGUI.Foldout(foldRect, prop.isExpanded, label, true);

            if (prop.isExpanded)
                GetList(prop).DoList(pos.AddY(LineHeight + VerticalSpacing).AddMinX(EditorGUI.indentLevel * 15f));

            // ApplyChanges(prop);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
        {
            float h = LineHeight + VerticalSpacing;
            if (prop.isExpanded) h += GetList(prop).GetHeight();
            return h;
        }

        void ApplyChanges(SerializedProperty prop)
        {
            prop.serializedObject.ApplyModifiedProperties();
            // Debug.Log("Apply Changes!");
        }

        UnityEditorInternal.ReorderableList GetList(SerializedProperty prop)
        {
            var path = prop.propertyPath;
            if (lists.TryGetValue(path, out var list)) return list;

            var keys = prop.FindPropertyRelative("keys");
            var values = prop.FindPropertyRelative("values");

            list = new(prop.serializedObject, keys, true, false, true, true)
            {
                elementHeightCallback = i =>
                {
                    SerializedProperty k = keys.GetArrayElementAtIndex(i), v = values.GetArrayElementAtIndex(i);
                    return Mathf.Max(EditorGUI.GetPropertyHeight(k), EditorGUI.GetPropertyHeight(v)) + 4;
                },

                drawElementCallback = (r, i, a, f) =>
                {
                    SerializedProperty k = keys.GetArrayElementAtIndex(i), v = values.GetArrayElementAtIndex(i);
                    Rect[] areas = r.AddMinY(2).DivideArea(Padding, LabelWidth, 0).ToArray();
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(areas[0], k, GUIContent.none, true);
                    EditorGUI.PropertyField(areas[1], v, GUIContent.none, true);
                    if (EditorGUI.EndChangeCheck()) ApplyChanges(prop);
                },

                onAddCallback = _ =>
                {
                    keys.arraySize++;
                    values.arraySize++;
                    ApplyChanges(prop);
                },

                onRemoveCallback = l =>
                {
                    keys.DeleteArrayElementAtIndex(l.index);
                    values.DeleteArrayElementAtIndex(l.index);
                    ApplyChanges(prop);
                }
            };

            lists[path] = list;
            return list;
        }
    }
}
#endregion
#endif