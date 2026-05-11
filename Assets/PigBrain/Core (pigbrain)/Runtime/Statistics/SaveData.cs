using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using pigbrain.core.UnityObject;
using UnityEngine;
using static pigbrain.core.Utility.ReflectionUtility;

#region Save Data
namespace pigbrain.core.Statistics
{
    public class SaveData : MonoBehaviour
    {
        public string key;
        [SerializeField] internal Property property;
        void Awake() => Register();
        void OnDestroy() => Persistence.Unregister(this);
        void OnValidate() { if (string.IsNullOrEmpty(key)) key = transform.GetPath(); }
        public void Register()
        {
            property.Resolve(gameObject);
            if (property.component) Persistence.Register(this);
        }
    }
}
#endregion

#region Property
namespace pigbrain.core.Statistics
{
    [Serializable]
    public class Property
    {
        public Component component;
        public string componentType;
        public string propertyPath;
        public Type type => info?.PropertyType;
        internal PropertyInfo info => component ? component.GetType().GetProperty(propertyPath, DefaultBindings) : null;

        public event Action OnValueChange;

        public object value
        {
            get => info?.GetValue(component);
            set { info?.SetValue(component, value); OnValueChange?.Invoke(); }
        }

        readonly static Dictionary<string, Type> TypeCache = new();
        internal void Resolve(GameObject gameObject)
        {
            if (component) return;
            if (!TypeCache.TryGetValue(componentType, out Type type))
                TypeCache[componentType] = type = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(componentType))
                    .FirstOrDefault(t => t != null);
            component = gameObject.GetComponent(type);
        }
    }
}
#endregion

#region Editor
#if UNITY_EDITOR
namespace pigbrain.core.Statistics
{
    using UnityEditor;
    [CustomPropertyDrawer(typeof(Property))]
    public class SaveData_Property_PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var compProp = property.FindPropertyRelative(nameof(Property.component));
            var pathProp = property.FindPropertyRelative(nameof(Property.propertyPath));

            var targets = property.serializedObject.targetObjects
                .OfType<Component>()
                .ToArray();

            if (targets.Length == 0)
            {
                EditorGUI.LabelField(position, "Invalid target");
                EditorGUI.EndProperty();
                return;
            }

            var go = targets[0].gameObject;
            var components = go.GetComponents<Component>();

            List<string> options = new();
            List<(Component comp, string path)> map = new();

            foreach (var comp in components)
            {
                if (!comp) continue;

                var type = comp.GetType();
                var flags = DefaultBindings;

                var props = type.GetProperties(flags);

                foreach (var p in props)
                {
                    if (!p.CanRead || p.SetMethod == null || !p.SetMethod.IsPublic) continue;
                    if (p.GetIndexParameters().Length > 0) continue;
                    var t = p.PropertyType;
                    if (t != typeof(int) && t != typeof(float) && t != typeof(bool) && t != typeof(string) && !t.IsEnum)
                        continue;
                    options.Add($"{type.Name}/{p.Name}");
                    map.Add((comp, p.Name));
                }
            }

            int currentIndex = -1;

            for (int i = 0; i < map.Count; i++)
            {
                if (compProp.objectReferenceValue == map[i].comp &&
                    pathProp.stringValue == map[i].path)
                {
                    currentIndex = i;
                    break;
                }
            }

            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, options.ToArray());

            if (newIndex >= 0 && newIndex < map.Count)
            {
                foreach (SerializedObject so in property.serializedObject.targetObjects
                    .Select(t => new SerializedObject(t)))
                {
                    var root = so.FindProperty(property.propertyPath);
                    var comp = root.FindPropertyRelative(nameof(Property.component));
                    var path = root.FindPropertyRelative(nameof(Property.propertyPath));
                    var compType = root.FindPropertyRelative(nameof(Property.componentType));

                    var targetGO = ((Component)so.targetObject).gameObject;
                    var selected = targetGO.GetComponent(map[newIndex].comp.GetType());

                    comp.objectReferenceValue = selected;
                    path.stringValue = map[newIndex].path;
                    compType.stringValue = selected.GetType().FullName;

                    so.ApplyModifiedProperties();
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
#endregion
