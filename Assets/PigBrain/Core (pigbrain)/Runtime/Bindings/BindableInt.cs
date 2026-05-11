using System;
using UnityEngine;

namespace pigbrain.core.Bindings
{
    [Serializable]
    public class BindableInt
    {
        [SerializeField][InspectorName("value")] int internalValue;
        public event Action<int> Changed;

        public int value
        {
            get => internalValue;
            set { this.internalValue = value; Changed?.Invoke(value); }
        }

        public void Bind(Action<int> setter)
        {
            Changed += setter;
            setter(internalValue);
        }

        public static implicit operator int(BindableInt b) => b.internalValue;
    }
}

#if UNITY_EDITOR
namespace pigbrain.core.Bindings
{
    using UnityEditor;
    using static pigbrain.core.Inspector.InspectorUtility;
    [CustomPropertyDrawer(typeof(BindableInt))]
    public class BindableInt_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect p, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(p, label, property);
            var valueProp = property.FindPropertyRelative("internalValue");
            EditorGUI.PropertyField(p, valueProp, label);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => FullLineHeight;
    }
}
#endif
