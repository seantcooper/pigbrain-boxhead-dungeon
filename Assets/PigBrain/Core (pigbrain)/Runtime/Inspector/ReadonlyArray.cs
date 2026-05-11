using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyArrayAttribute : PropertyAttribute { }

    [Serializable]
    public class ReadOnlyList<T> : IList<T>
    {
        [SerializeField] List<T> items = new();

        public int Count => items.Count;
        public bool IsReadOnly => false;

        public T this[int i] { get => items[i]; set => items[i] = value; }
        public void Add(T item) => items.Add(item);
        public void Clear() => items.Clear();
        public bool Contains(T item) => items.Contains(item);
        public void CopyTo(T[] array, int index) => items.CopyTo(array, index);
        public IEnumerator<T> GetEnumerator() => items.GetEnumerator();
        public int IndexOf(T item) => items.IndexOf(item);
        public void Insert(int index, T item) => items.Insert(index, item);
        public bool Remove(T item) => items.Remove(item);
        public void RemoveAt(int index) => items.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
        public void Sort(IComparer<T> comparer) => items.Sort(comparer);
        public void Sort(Comparison<T> comparison) => items.Sort(comparison);
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using System.Linq;
    using pigbrain.core.Collections;
    using pigbrain.core.Geom;
    using UnityEditor;
    using UnityEngine;
    using static pigbrain.core.Inspector.InspectorUtility;
    using static UnityEditor.EditorGUI;

    [CustomPropertyDrawer(typeof(ReadOnlyArrayAttribute))]
    public class ReadOnlyArray_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            BeginProperty(position, label, property);
            if (property.isExpanded = Foldout(position.WithH(LineHeight), property.isExpanded, label))
            {
                EditorGUI.indentLevel++;
                var arrayProp = property.FindPropertyRelative("items");
                position = position.AddY(LineHeight + VerticalSpacing);
                arrayProp.IterateArrayProperties().ForEach(p =>
                {
                    PropertyField(position = position.WithH(EditorGUI.GetPropertyHeight(p, true)), p, true);
                    position.y += position.height + VerticalSpacing;
                });
                EditorGUI.indentLevel--;
            }
            EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;
            if (property.isExpanded)
                height += property.FindPropertyRelative("items")
                    .IterateArrayProperties().Sum(p => EditorGUI.GetPropertyHeight(p, true) + VerticalSpacing);
            return height + LineHeight + VerticalSpacing;
        }
    }
}
#endif