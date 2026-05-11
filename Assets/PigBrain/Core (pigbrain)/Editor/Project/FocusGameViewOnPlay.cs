#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace pigbrain.core.Project
{

    [InitializeOnLoad]
    static class FocusGameViewOnPlay
    {
        static FocusGameViewOnPlay()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.delayCall += () =>
                {
                    var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                    var window = EditorWindow.GetWindow(type);
                    window?.Focus();
                };
            }
        }
    }
}
#endif