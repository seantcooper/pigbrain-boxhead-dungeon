#if UNITY_EDITOR
#pragma warning disable UDR0001
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Utility;
using UnityEditor;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    public static class InspectorUtility
    {
        public static float HeaderHeight => LineHeight + 4;
        public static float FullHeaderHeight => HeaderHeight + VerticalSpacing;

        public static float LineHeight => EditorGUIUtility.singleLineHeight;
        public static float FullLineHeight => LineHeight + VerticalSpacing;

        public static float VerticalSpacing => EditorGUIUtility.standardVerticalSpacing;
        public static float MiniFieldWidth = 50;
        public static float TinyFieldWidth = 30;
        public static float IndentSize = 15;
        public static float FoldoutWidth = IndentSize;
        public static float TotalIndentSize => EditorGUI.indentLevel * IndentSize;
        public static float LabelWidth => EditorGUIUtility.labelWidth;
        public static float Padding = 2;
        public static float Spacing = 8;
        public static float Separation = 8;

        #region "Controls"

        public static void DrawHeader(string name) =>
            DrawHeader(name, new Color(0.2f, 0.4f, 0.8f, 0.5f));

        public static void DrawHeader(string name, Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(rect, color);
            EditorGUI.LabelField(rect, name, EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
        }

        public static void DrawHeader(Rect position, string label, Color color = default)
        {
            EditorGUI.DrawRect(position.AddX(-100).AddW(200), color);
            EditorGUI.LabelField(position, label, EditorStyles.boldLabel);

        }
        #endregion

        #region IterateToEnd
        public static void IterateToEndDraw(this SerializedProperty property, Rect position,
           Action<Rect, SerializedProperty> draw, params string[] ignore)
        {
            using var s = new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel);
            Rect fieldRect = new(position.x, position.y, position.width, 0);
            property.IterateToEnd(ignore).ForEach(p =>
            {
                draw(fieldRect = fieldRect.WithH(EditorGUI.GetPropertyHeight(p, true)), p);
                fieldRect.y += fieldRect.height + VerticalSpacing;
            });
        }

        public static float IterateToEndHeight(this SerializedProperty property, params string[] ignore) =>
            property.IterateToEnd(ignore).Sum(p => EditorGUI.GetPropertyHeight(p, true) + VerticalSpacing);

        static IEnumerable<SerializedProperty> IterateToEnd(this SerializedProperty property, params string[] ignore)
        {
            SerializedProperty current = property.Copy(), end = current.GetEndProperty();
            for (current.NextVisible(true); !SerializedProperty.EqualContents(current, end); current.NextVisible(false))
            {
                if (ignore.Contains(current.name)) continue;
                if (current.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(current, true);
                }
                else yield return current.Copy();
            }
        }
        #endregion

        #region Iterate Properties
        public static IEnumerable<SerializedProperty> IterateProperties(this SerializedProperty start) =>
            IterateProperties(start, start.GetEndProperty());

        public static IEnumerable<SerializedProperty> IterateProperties(this SerializedObject so)
        {
            bool enter = true;
            for (SerializedProperty p = so.GetIterator(); p.NextVisible(enter); enter = false)
                yield return p.Copy();
        }

        public static IEnumerable<SerializedProperty> IterateProperties(this SerializedProperty start, SerializedProperty end)
        {
            bool enter = true;
            for (SerializedProperty p = start.Copy(); p.NextVisible(enter) && !SerializedProperty.EqualContents(p, end); enter = false)
                yield return p.Copy();
        }

        public static IEnumerable<SerializedProperty> IterateArrayProperties(this SerializedProperty property) =>
            Enumerable.Range(0, property.arraySize).Select(i => property.GetArrayElementAtIndex(i));

        #endregion

        #region Inline
        public static void InlineArray(this SerializedProperty property, string label = null)
        {
            if (!string.IsNullOrEmpty(label)) EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0, n = property.arraySize; i < n; i++)
                EditorGUILayout.PropertyField(property.GetArrayElementAtIndex(i), true);
            EditorGUI.indentLevel--;
        }

        public static void InlineObject(this SerializedProperty property, string label = null)
        {
            if (!string.IsNullOrEmpty(label)) EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            var iterator = property.Copy();
            for (bool enter = iterator.NextVisible(true); iterator.NextVisible(enter); enter = false)
                EditorGUILayout.PropertyField(iterator, true);
            EditorGUI.indentLevel--;
        }
        #endregion

        #region Line division
        // Divides a rect by widths (0 gets resized)
        public static IEnumerable<Rect> DivideArea(this Rect rect, float spacing, params float[] widths)
        {
            float division = (rect.width - widths.Sum() - (widths.Length - 1) * spacing) / widths.Count(w => w <= 0);
            foreach (var w in widths)
            {
                yield return rect = rect.WithW(w <= 0 ? division : w);
                rect.x += rect.width + spacing;
            }
        }

        public static IEnumerable<Rect> DivideAreaRatio(this Rect rect, float spacing, params float[] ratios)
        {
            float scale = 1 / ratios.Sum();
            var widths = ratios.Select(r => r * scale * rect.width).ToArray();
            return DivideArea(rect, spacing, widths);
        }

        // draws a list of properties into rects
        public static void DrawPropertyFields(this IEnumerable<SerializedProperty> properties, IEnumerable<Rect> rects) =>
            rects.Zip(properties, (r, p) => (r, p)).ForEach(t => EditorGUI.PropertyField(t.r, t.p, GUIContent.none, true));

        public static void DrawPropertyFields(this IEnumerable<SerializedProperty> properties, Rect pos)
        {
            properties.ForEach(p =>
            {
                pos.height = EditorGUI.GetPropertyHeight(p, true);
                EditorGUI.PropertyField(pos, p, true);
                pos.y = pos.yMax + VerticalSpacing;
            });
        }
        public static float GetPropertyFieldsHeight(this IEnumerable<SerializedProperty> properties) =>
            properties.Sum(p => EditorGUI.GetPropertyHeight(p, true));

        public static IEnumerable<SerializedProperty> GetProperties(this SerializedProperty property, params string[] paths) =>
            paths.Select(p => property.FindPropertyRelative(p));

        public static IEnumerable<SerializedProperty> GetProperties(this SerializedObject serializedObject, params string[] paths) =>
            paths.Select(p => serializedObject.FindProperty(p));

        #endregion

        #region Without Script
        public static IEnumerable<SerializedProperty> IteratePropertiesWithoutScript(this Editor editor)
        {
            SerializedProperty iterator = editor.serializedObject.GetIterator();
            for (bool enter = iterator.NextVisible(true); iterator.NextVisible(enter); enter = false)
                if (iterator.name != "m_Script")
                    yield return iterator.Copy();
        }

        public static bool DrawWithoutScript(this Editor editor, ref Rect rect)
        {
            EditorGUI.BeginChangeCheck();
            editor.serializedObject.Update();
            foreach (var prop in editor.IteratePropertiesWithoutScript())
            {
                EditorGUI.PropertyField(rect = rect.WithH(EditorGUI.GetPropertyHeight(prop, true)), prop, true);
                rect.y += rect.height + VerticalSpacing;
            }
            editor.serializedObject.ApplyModifiedProperties();
            return EditorGUI.EndChangeCheck();
        }

        public static float GetHeightWithoutScript(this Editor editor) =>
            editor.IteratePropertiesWithoutScript().Sum(p => EditorGUI.GetPropertyHeight(p, true) + VerticalSpacing);
        #endregion

        #region Find Property
        public static SerializedProperty FindParentProperty(this SerializedProperty serializedProperty)
        {
            var propertyPaths = serializedProperty.propertyPath.Split('.');
            if (propertyPaths.Length <= 1) return default;

            var parentSerializedProperty = serializedProperty.serializedObject.FindProperty(propertyPaths.First());
            for (var index = 1; index < propertyPaths.Length - 1; index++)
            {
                if (propertyPaths[index] == "Array" && propertyPaths.Length > index + 1 && Regex.IsMatch(propertyPaths[index + 1], "^data\\[\\d+\\]$"))
                {
                    var match = Regex.Match(propertyPaths[index + 1], "^data\\[(\\d+)\\]$");
                    parentSerializedProperty = parentSerializedProperty.GetArrayElementAtIndex(int.Parse(match.Groups[1].Value));
                    index++;
                }
                else parentSerializedProperty = parentSerializedProperty.FindPropertyRelative(propertyPaths[index]);
            }
            return parentSerializedProperty;
        }

        public static bool IsArrayElement(this SerializedProperty prop) =>
            prop.propertyPath.Contains(".Array.data[");

        public static SerializedProperty GetParent(this SerializedProperty prop)
        {
            string path = prop.propertyPath.Replace(".Array.data[", ".Array[");
            int i = path.LastIndexOf('.');
            return i < 0 ? null : prop.serializedObject.FindProperty(path[..i]);
        }

        #endregion

        #region Target Object
        public static object GetOwner(this SerializedProperty property) =>
            property.GetParent()?.GetTargetObject<object>() ?? property.serializedObject.targetObject;

        public static object GetMemberValue(this object owner, string memberName)
        {
            if (!string.IsNullOrEmpty(memberName))
            {
                var type = owner.GetType();
                if (type.GetField(memberName, ReflectionUtility.DefaultBindings) is FieldInfo fi)
                    return fi.GetValue(owner);
                else if (type.GetProperty(memberName, ReflectionUtility.DefaultBindings) is PropertyInfo pi)
                    return pi.GetValue(owner);
            }
            return null;
        }

        public static T GetTargetObject<T>(this SerializedProperty prop)
        {
            if (prop == null) return default;
            UnityEngine.Object target = prop.serializedObject.targetObject;
            string path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = target;
            foreach (var element in path.Split('.'))
            {
                if (element.Contains("["))
                {
                    string elementName = element[..element.IndexOf('[')];
                    int index = Convert.ToInt32(element[(element.IndexOf('[') + 1)..^1]);
                    obj = GetValue_Indexed(obj, elementName, index);
                }
                else obj = GetValue(obj, element);
            }
            return (T)obj;
        }

        static object GetValue(object source, string name)
        {
            const BindingFlags bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            if (source == null) return null;
            for (var t = source.GetType(); t != null; t = t.BaseType)
            {
                if (t.GetField(name, bindings) is FieldInfo f) return f.GetValue(source);
                if (t.GetProperty(name, bindings) is PropertyInfo p) return p.GetValue(source, null);
            }
            return null;
        }

        static object GetValue_Indexed(object source, string name, int index)
        {
            if (GetValue(source, name) is not IEnumerable enumerable) return null;
            IEnumerator enm = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++) if (!enm.MoveNext()) return null;
            return enm.Current;
        }

        public static Type GetElementType(this SerializedProperty prop)
        {
            var fieldType = prop.GetFieldInfo().FieldType;
            if (fieldType.IsArray) fieldType = fieldType.GetElementType();
            else if (typeof(System.Collections.IList).IsAssignableFrom(fieldType) && fieldType.IsGenericType)
                fieldType = fieldType.GetGenericArguments()[0];
            return fieldType;
        }
        public static FieldInfo GetFieldInfo(this SerializedProperty prop)
        {
            var type = prop.serializedObject.targetObject.GetType();
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            FieldInfo field = null;

            foreach (var name in path.Split('.'))
            {
                if (name.Contains("["))
                {
                    var elementName = name[..name.IndexOf("[")];
                    field = type.GetField(elementName, ReflectionUtility.DefaultBindings);
                    type = field.FieldType.GetElementType()
                        ?? field.FieldType.GetGenericArguments()[0];
                }
                else
                {
                    field = type.GetField(name,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    type = field.FieldType;
                }
            }
            return field;
        }

        #endregion


        #region Arrays
        public static IEnumerable<SerializedProperty> GetArrayProperties(this SerializedProperty array)
        {
            for (int i = 0; i < array.arraySize; i++)
                yield return array.GetArrayElementAtIndex(i);
        }
        #endregion

        #region Drawers
        public static void DrawRectWithOutline(this Rect rect, Color outline, int thickness = 1) =>
            DrawRectWithOutline(rect, Color.clear, outline, thickness);
        public static void DrawRectWithOutline(this Rect rect, Color fill, Color outline, int thickness = 1)
        {
            if (fill.a > 0) EditorGUI.DrawRect(rect, fill);
            if (outline.a > 0)
            {
                Rect x = rect.WithY(rect.y + thickness).WithH(rect.height - thickness * 2).WithW(1), y = rect.WithH(1);
                EditorGUI.DrawRect(y, outline);
                EditorGUI.DrawRect(y.WithY(rect.yMax - thickness), outline);
                EditorGUI.DrawRect(x, outline);
                EditorGUI.DrawRect(x.WithX(rect.xMax - thickness), outline);
            }
        }

        public static void LayoutLine(int thickness = 2)
        {
            int s1 = ((int)FullLineHeight - thickness) / 2, s2 = (int)FullLineHeight - s1 - thickness;
            GUILayout.Space(s1);
            var rect = EditorGUILayout.GetControlRect(false, thickness);
            EditorGUI.DrawRect(rect, Color.white.WithA(0.2f));
            GUILayout.Space(s2);
        }

        #endregion

        #region Misc
        public static string GetDisplayName<T>(this T value) where T : Enum
        {
            var name = value.ToString();
            var f = typeof(T).GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var a = f?.GetCustomAttribute<InspectorNameAttribute>();
            return a != null ? a.displayName : ObjectNames.NicifyVariableName(name);
        }

        public static bool HasFoldout(this SerializedProperty prop) =>
             prop.hasVisibleChildren && prop.propertyType == SerializedPropertyType.Generic;
        #endregion

        #region Find Editor
        readonly static Dictionary<Type, Type> CacheEditors = new();
        public static Type FindEditorTypeFor(this UnityEngine.Object target)
        {
            var targetType = target.GetType();
            if (CacheEditors.TryGetValue(targetType, out Type etype)) return etype;

            Type Search()
            {
                (Type editor, int depth) best = (null, int.MaxValue);
                foreach (var editorType in TypeCache.GetTypesDerivedFrom<Editor>())
                {
                    foreach (var attrData in editorType.GetCustomAttributesData())
                    {
                        if (attrData.AttributeType != typeof(CustomEditor)) continue;

                        var inspected = attrData.ConstructorArguments[0].Value as Type;
                        bool editorForChildren = attrData.ConstructorArguments.Count > 1 &&
                            attrData.ConstructorArguments[1].Value is bool b && b;

                        if (inspected == null) continue;
                        if (inspected == targetType) return editorType;
                        if (editorForChildren && inspected.IsAssignableFrom(targetType))
                        {
                            int depth = GetInheritanceDepth(inspected, targetType);
                            if (depth < best.depth) best = (editorType, depth);
                        }
                    }
                }
                return best.editor;
            }
            return CacheEditors[targetType] = Search();
        }

        static int GetInheritanceDepth(Type baseType, Type derived)
        {
            int depth = 0;
            while (derived != null && derived != baseType)
            {
                derived = derived.BaseType;
                depth++;
            }
            return derived == null ? int.MaxValue : depth;
        }
        #endregion

        #region PropertyTypes
        public static readonly Dictionary<SerializedPropertyType, Type> PropertyTypeLookup = new()
        {
            { SerializedPropertyType.Integer, typeof(int) },
            { SerializedPropertyType.Boolean, typeof(bool) },
            { SerializedPropertyType.Float, typeof(float) },
            { SerializedPropertyType.String, typeof(string) },
            { SerializedPropertyType.Color, typeof(Color) },
            { SerializedPropertyType.ObjectReference, typeof(UnityEngine.Object) },
            { SerializedPropertyType.LayerMask, typeof(int) },
            { SerializedPropertyType.Enum, typeof(int) },
            { SerializedPropertyType.Vector2, typeof(Vector2) },
            { SerializedPropertyType.Vector3, typeof(Vector3) },
            { SerializedPropertyType.Vector4, typeof(Vector4) },
            { SerializedPropertyType.Rect, typeof(Rect) },
            { SerializedPropertyType.Bounds, typeof(Bounds) },
            { SerializedPropertyType.Quaternion, typeof(Quaternion) },
            { SerializedPropertyType.AnimationCurve, typeof(AnimationCurve) },
            { SerializedPropertyType.ExposedReference, typeof(UnityEngine.Object) },
            { SerializedPropertyType.FixedBufferSize, typeof(int) },
            { SerializedPropertyType.Vector2Int, typeof(Vector2Int) },
            { SerializedPropertyType.Vector3Int, typeof(Vector3Int) },
            { SerializedPropertyType.RectInt, typeof(RectInt) },
            { SerializedPropertyType.BoundsInt, typeof(BoundsInt) }
        };

        #endregion
    }

    #region GUI Style
    public static class Styles
    {
        public static GUIStyle WithAlignment(this GUIStyle style, TextAnchor alignment) =>
            new(style) { alignment = alignment };

        static Dictionary<string, GUIStyle> CachedStyles = new();
        public static GUIStyle ToolbarDropDown
        {
            get
            {
                // if (CachedStyles.TryGetValue(nameof(ToolbarDropDown), out var style)) return style;
                GUIStyle style = new GUIStyle(EditorStyles.iconButton);
                style.normal.scaledBackgrounds = new Texture2D[0];
                style.alignment = TextAnchor.MiddleCenter;
                style.fontStyle = FontStyle.Bold;
                return CachedStyles[nameof(ToolbarDropDown)] = style;
            }
        }
    }
    #endregion


    #region Scope
    public class BackgroundColorScope : System.IDisposable
    {
        readonly Color color;
        public BackgroundColorScope(Color color)
        { this.color = GUI.backgroundColor; GUI.backgroundColor = color; }

        public void Dispose() => GUI.backgroundColor = color;
    }

    public class GUIColorScope : System.IDisposable
    {
        readonly Color color;
        public GUIColorScope(Color color)
        { this.color = GUI.color; GUI.color = color; }

        public void Dispose() => GUI.color = color;
    }

    public class LabelWidthScope : System.IDisposable
    {
        readonly float width;
        public LabelWidthScope(float width)
        {
            this.width = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = width;
        }
        public void Dispose() => EditorGUIUtility.labelWidth = width;
    }
    #endregion

}
#endif
