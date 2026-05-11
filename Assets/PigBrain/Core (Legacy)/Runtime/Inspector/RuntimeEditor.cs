using UnityEngine;

public static class RuntimeEditor
{
    public static void SetDirty(UnityEngine.Object unityObject)
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(unityObject);
#endif
    }

}
