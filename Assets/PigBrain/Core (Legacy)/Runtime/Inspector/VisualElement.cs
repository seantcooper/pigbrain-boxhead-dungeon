// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using PigBrain.LegacyCore.Utility;
// using UnityEngine;
// using UnityEngine.UIElements;

// public static class VisualElementUtility
// {

//     #region Geom
//     public static Rect LocalToLocal(this VisualElement from, VisualElement to, Rect value) =>
//         to.WorldToLocal(from.LocalToWorld(value));

//     public static Vector2 LocalToLocal(this VisualElement from, VisualElement to, Vector2 value) =>
//         to.WorldToLocal(from.LocalToWorld(value));
//     #endregion

//     #region Children
//     public static void Add(this VisualElement element, VisualElement child, params string[] classes)
//     {
//         element.Add(child);
//         classes.ForEach(c => child.AddToClassList(c));
//     }
//     #endregion

//     #region Visibility
//     public static void ToggleVisibility(this VisualElement element) => element.SetVisibility(!element.visible);
//     public static void SetVisibility(this VisualElement element, bool visible) =>
//         element.style.display = (StyleEnum<DisplayStyle>)((element.visible = visible) ?
//             DisplayStyle.Flex : DisplayStyle.None);

//     public static void Show(this VisualElement element) => element.SetVisibility(false);
//     public static void Hide(this VisualElement element) => element.SetVisibility(true);
//     #endregion

//     #region Styling
//     public static void AddToClassList(this VisualElement e, params string[] classNames) =>
//         classNames.ForEach(c => e.AddToClassList(c));
//     public static void RemoveFromClassList(this VisualElement e, params string[] classNames) =>
//         classNames.ForEach(c => e.RemoveFromClassList(c));

//     public static void BorderColor(this VisualElement e, StyleColor color) => e.style.borderBottomColor =
//         e.style.borderTopColor = e.style.borderLeftColor = e.style.borderRightColor = color;
//     public static void BorderWidth(this VisualElement e, float width) => e.style.borderBottomWidth =
//         e.style.borderTopWidth = e.style.borderLeftWidth = e.style.borderRightWidth = width;
//     public static void BorderRadius(this VisualElement e, float radius) => e.style.borderBottomLeftRadius =
//         e.style.borderBottomRightRadius = e.style.borderTopLeftRadius = e.style.borderTopRightRadius = radius;

//     public static void Padding(this VisualElement e, float p = 0) => e.Padding(p, p, p, p);
//     public static void Padding(this VisualElement e, float l, float r, float b, float t)
//     { e.style.paddingLeft = l; e.style.paddingRight = r; e.style.paddingBottom = b; e.style.paddingTop = t; }

//     public static void Margin(this VisualElement e, float m = 0) => e.Margin(m, m, m, m);
//     public static void Margin(this VisualElement e, float l, float r, float b, float t)
//     { e.style.marginLeft = l; e.style.marginRight = r; e.style.marginBottom = b; e.style.marginTop = t; }
//     #endregion

//     // #region "Query"
//     // public static IEnumerable<T> Q<T>(this VisualElement e, string name = null, string className = null,
//     //     bool raiseError = true) where T : VisualElement
//     // {
//     //     var items = e.Query<T>(name, className).Build();
//     //     if (raiseError && items.Count() == 0)
//     //         Debug.Log($"VisualElement::Q nothing found with the name/class {name}/{className}");
//     //     return items;
//     // }
//     // public static IEnumerable<VisualElement> Q(this VisualElement e, string name = null, string className = null,
//     //     bool raiseError = true) =>
//     //     e.Q<VisualElement>(name, className, raiseError);

//     // #endregion
// }
