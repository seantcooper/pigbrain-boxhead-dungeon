using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

static class ComponentSorter
{
    [MenuItem("CONTEXT/Component/Sort Components Alphabetically (Stable)")]
    static void SortContext(MenuCommand command)
    {
        var go = ((Component)command.context).gameObject;
        Sort(go);
    }

    static void Sort(GameObject go)
    {
        Component[] components = go.GetComponents<Component>();
        var c = components.FirstOrDefault(c => c is not Transform);

        Debug.Log(c);
        for (int i = 8; i > 0; --i)
            UnityEditorInternal.ComponentUtility.MoveComponentDown(c);

        SortComponent(go);
        // var components = go.GetComponents<Component>().Where(c => c is not Transform).ToArray();

        // int Compare(Component c1, Component c2) =>
        //     String.Compare(c1.GetType().Name, c2.GetType().Name);

        // for (int i = 0, n = components.Length - 1; i < n; i++)
        // {
        //     bool swapped = false;
        //     for (int j = i + 1; j < n; j++)
        //         if (Compare(components[i], components[j]) > 0)
        //         {
        //             Component c = components[i];
        //             components[i] = components[j];
        //             components[j] = components[i];
        //             UnityEditorInternal.ComponentUtility.IsDesi
        //             UnityEditorInternal.ComponentUtility.MoveComponentUp(components[i]);
        //             swapped = true;
        //         }
        //     if (!swapped) break;
        // }
        // EditorUtility.SetDirty(go);
    }

    public static void SortComponent(GameObject go = null)
    {
        Component[] componentsArrayOfGameObject = go.GetComponents<Component>();

        List<Component> listSortTypeComponents = new();

        foreach (Component componentToSort in componentsArrayOfGameObject)
        {
            if (componentToSort is Transform) continue;

            listSortTypeComponents.Add(componentToSort);
        }

        listSortTypeComponents.Sort((x, y) => x.GetType().Name.CompareTo(y.GetType().Name));
        for (int i = listSortTypeComponents.Count - 1; i >= 0; i--)
        {
            int positionToGo = componentsArrayOfGameObject.Length - 1 - (listSortTypeComponents.Count - 1 - i); // Beacuse of the transform component that cannot be moved
            int currentPosition = GetComponentPositionInInspector(listSortTypeComponents[i], go);
            int delta = Mathf.Abs(positionToGo - currentPosition);
            for (int j = 0; j < delta; j++)
            {
                if (positionToGo > currentPosition)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentDown(listSortTypeComponents[i]);
                }
                else if (positionToGo < currentPosition)
                {
                    UnityEditorInternal.ComponentUtility.MoveComponentUp(listSortTypeComponents[i]);
                }
                else
                {
                    continue;
                }
            }
        }

        EditorUtility.SetDirty(go);
    }
    private static int GetComponentPositionInInspector(Component componentToCheck, GameObject go)
    {
        Component[] components = go.GetComponents<Component>();
        int index = -1;

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == componentToCheck)
            {
                index = i;
                break;
            }
        }
        return index;
    }

}
