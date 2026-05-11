using System;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    [Serializable]
    public abstract class DropBoxBase { }

    [Serializable]
    public class DropBox<T> : DropBoxBase { public T[] items; }
    public sealed class DropBoxTargetAttribute : PropertyAttribute { }
}

#if UNITY_EDITOR
namespace pigbrain.core.Inspector
{
    using System.Linq;
    using System.Reflection;
    using pigbrain.core.Geom;
    using pigbrain.core.Utility;
    using UnityEditor;
    using UnityEngine;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(DropBoxBase), true)]
    public class DropBoxDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            var items = prop.FindPropertyRelative("items");
            HandleDrag(pos.WithH(LineHeight + VerticalSpacing), items);

            EditorGUI.PropertyField(pos, items, label, true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            EditorGUI.GetPropertyHeight(prop.FindPropertyRelative("items"), label, true);

        static Type GetDropBoxElementType(Type type)
        {
            for (; type != null; type = type.BaseType)
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DropBox<>))
                    return type.GetGenericArguments()[0];
            return null;
        }

        void HandleDrag(Rect box, SerializedProperty items)
        {
            var e = Event.current;
            if (!box.Contains(e.mousePosition)) return;
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform) return;
            if (GetDropBoxElementType(fieldInfo.FieldType) is not Type elementType) return;

            if (elementType.GetFields(ReflectionUtility.DefaultBindings)
                .FirstOrDefault(f => Attribute.IsDefined(f, typeof(DropBoxTargetAttribute))) is not FieldInfo targetField)
            {
                Debug.Log("No DropBoxTargetAttribute on Object field, Use [DropBoxTarget] public UnityEngine.Object object");
                return;
            }

            var targetType = targetField.FieldType;
            var objs = DragAndDrop.objectReferences.Where(o => o && targetType.IsAssignableFrom(o.GetType())).ToArray();

            if (objs.Length == 0) return;

            switch (e.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    e.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    for (int i = 0, start = items.arraySize, n = items.arraySize += objs.Length; i < objs.Length; i++)
                    {
                        var elem = items.GetArrayElementAtIndex(start + i);
                        var targetProp = elem.FindPropertyRelative(targetField.Name);
                        if (targetProp != null) targetProp.objectReferenceValue = objs[i];
                    }
                    e.Use();
                    items.serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(items.serializedObject.targetObject);
                    break;
            }
        }
    }
}
#endif