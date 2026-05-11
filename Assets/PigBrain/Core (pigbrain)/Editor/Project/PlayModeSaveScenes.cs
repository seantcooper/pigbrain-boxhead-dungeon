using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class PlayModeSaveScenes
{
    static PlayModeSaveScenes() =>
        EditorApplication.playModeStateChanged += OnPlayModeChanged;

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;

        bool anyDirty = false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.isDirty)
            {
                anyDirty = true;
                break;
            }
        }

        if (!anyDirty) return;

        bool save = EditorUtility.DisplayDialog(
            "Unsaved Scenes",
            "One or more open scenes have unsaved changes. Save before entering Play Mode?",
            "Save & Play",
            "Cancel");

        if (save)
        {
            EditorSceneManager.SaveOpenScenes();
        }
        else
        {
            EditorApplication.isPlaying = false;
        }
    }
}
