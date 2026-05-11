#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace pigbrain.core.Inspector
{
    public class EnumMenuDrawer<T> : PropertyDrawer where T : Enum
    {
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
        {
            var names = prop.enumDisplayNames;
            int curIndex = prop.enumValueIndex;

            if (!GUI.Button(pos, MenuName(names[curIndex]), EditorStyles.popup)) return;

            var menu = new GenericMenu();
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                menu.AddItem(new(MenuName(names[idx])),
                    idx == curIndex,
                    () =>
                    {
                        prop.enumValueIndex = idx;
                        prop.serializedObject.ApplyModifiedProperties();
                    });
            }
            menu.DropDown(pos);
        }

        static string MenuName(string value)
            => value.ToString().Replace('_', '/');
    }
}
#endif
