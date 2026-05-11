using System;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    [Serializable]
    public class AssetLink { }
    [Serializable]
    public class AssetLink<T> : AssetLink, ISerializationCallbackReceiver where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        [SerializeField] internal T target;
#endif
        [SerializeField] internal string id;
        internal static string GetID(T target) =>
            target ? $"{target.name}" : string.Empty;

        public void OnAfterDeserialize() { }
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            id = GetID(target);
#endif
        }

        public static implicit operator bool(AssetLink<T> empty) =>
            empty != null && !string.IsNullOrEmpty(empty.id);
    }
}

#region Editor
#if UNITY_EDITOR
namespace pigbrain.core.UnityObject
{
    using pigbrain.core.Geom;
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomPropertyDrawer(typeof(AssetLink), true)]
    public class AssetLink_Drawer : PropertyDrawer
    {
        protected virtual Type targetType => typeof(UnityEngine.Object);
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var clipProp = property.FindPropertyRelative("target");
            // var idProp = property.FindPropertyRelative("id");
            var line = position.WithH(LineHeight);

            EditorGUI.BeginChangeCheck();

            // EditorGUI.PropertyField(line, clipProp, label);
            EditorGUI.ObjectField(line, clipProp, clipProp.GetFieldInfo().FieldType, label);
            // if (EditorGUI.EndChangeCheck())
            // {
            //     idProp.stringValue = AssetLink.GetID(clipProp.objectReferenceValue);
            //     property.serializedObject.ApplyModifiedProperties();
            // }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            FullLineHeight;
    }
}
#endif
#endregion