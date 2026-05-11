using pigbrain.core.Collections;
using UnityEditor;
using UnityEngine;

public static class SimpleTools
{
    [MenuItem("GameObject/Group Selection %g", true)]
    static bool GroupSelectionValidate() =>
        Selection.transforms != null && Selection.transforms.Length > 0;

    [MenuItem("GameObject/Group Selection %g", false, 0)]
    static void GroupSelection()
    {
        Transform[] selection = Selection.transforms;
        Transform parent = selection[0].parent;
        Transform group = new GameObject("group!").transform;
        Undo.RegisterCreatedObjectUndo(group.gameObject, "Group Selection");
        group.transform.SetParent(parent, false);
        selection.ForEach(t => Undo.SetTransformParent(t, group, "Group Selection"));
        Selection.activeGameObject = group.gameObject;
    }


}
