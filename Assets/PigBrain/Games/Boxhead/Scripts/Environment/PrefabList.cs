using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Inspector;
using UnityEngine;
using static pigbrain.core.Geom.Rnd;

namespace pigbrain.game.Boxhead.Environment
{
    public class PrefabList : ScriptableObject, IEnumerable<PrefabList.Info>
    {
        public PrefabContainer prefabs;
        public Info[] items => prefabs.items;

        public static PrefabList CreateInstance(GameObject[] prefabs)
        {
            PrefabList prefabList = CreateInstance<PrefabList>();
            prefabList.prefabs = new PrefabContainer() { items = prefabs.Select(p => new Info { prefab = p }).ToArray() };
            return prefabList;
        }

        [Serializable]
        public class PrefabContainer : DropBox<Info> { }

        [Serializable]
        public class Info : IWeightedObject
        {
            [DropBoxTarget] public GameObject prefab;
            public int weight = 1;

            object IWeightedObject.GetValue() => prefab;
            int IWeightedObject.GetWeight() => weight;
        }

        static string Normalize(string s) => string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant();

        public GameObject Get(string name)
        {
            if (string.IsNullOrEmpty(name) || prefabs?.items == null) return null;
            name = Normalize(name);
            foreach (var i in prefabs.items)
            {
                GameObject p = i?.prefab;
                if (!p) continue;
                string n = Normalize(p.name);
                if (n == name) return p;

                int a = n.IndexOf('('), b = n.IndexOf(')');
                if (a >= 0 && b > a)
                {
                    var inner = Normalize(n.Substring(a + 1, b - a - 1));
                    if (inner == name) return p;
                }
            }
            return null;
        }

        public T Get<T>(string name) where T : Component => Get(name).GetComponent<T>();

        IEnumerator<Info> IEnumerable<Info>.GetEnumerator() =>
            (prefabs?.items ?? Enumerable.Empty<Info>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            ((IEnumerable<Info>)this).GetEnumerator();
    }

    public static class PrefabListX
    {
        public static bool IsValid(this PrefabList p) =>
            p && p.prefabs != null && p.prefabs.items != null && p.prefabs.items.Count(p => p.prefab) > 0;
    }

}

#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Environment
{
    using UnityEditor;
    using UnityEngine;
    using pigbrain.core.Geom;
    using static pigbrain.core.Inspector.InspectorUtility;

    [CustomEditor(typeof(PrefabList))]
    public class PrefabList_Editor : Editor
    {
        PrefabList prefabList => target as PrefabList;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Select All"))
            {
                var objects = prefabList.prefabs?.items?
                    .Where(i => i != null && i.prefab != null)
                    .Select(i => i.prefab)
                    .Distinct()
                    .Cast<Object>()
                    .ToArray();

                if (objects != null && objects.Length > 0)
                    Selection.objects = objects;
            }
        }
    }

    [CustomPropertyDrawer(typeof(PrefabList.Info), true)]
    public class PrefabList_PrefabInfo_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, prop);
            prop.GetProperties(nameof(PrefabList.Info.prefab), nameof(PrefabList.Info.weight)).DrawPropertyFields(
                pos.WithH(LineHeight).DivideArea(Padding, 0, MiniFieldWidth));
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label) =>
            LineHeight + VerticalSpacing;
    }
}
#endif