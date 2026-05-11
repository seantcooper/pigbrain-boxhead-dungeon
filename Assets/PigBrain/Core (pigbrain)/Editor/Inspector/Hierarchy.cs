using UnityEditor;
using UnityEngine;

public static class Hierarchy
{
    [MenuItem("Tools/Reveal Hidden Objects")]
    static void Reveal()
    {
        foreach (var g in Resources.FindObjectsOfTypeAll<GameObject>())
            if (g.hideFlags == HideFlags.HideInHierarchy)
                g.hideFlags = HideFlags.None;
    }
}
