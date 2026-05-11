#pragma warning disable UDR0004
using System;
using System.Collections.Generic;
using pigbrain.core.Utility;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.Toolbars;
using UnityEngine;
using static EditorPlayMode;

public static class PlayRecorderButtons
{
    const string MenuPath = "pigbrain/Record";

    static PlayRecorderButtons() { }

    #region Buttons
    [MainToolbarElement(MenuPath, defaultDockPosition = MainToolbarDockPosition.Left)]
    static IEnumerable<MainToolbarElement> NavigationButtons()
    {
        yield return new MainToolbarButton(new(null, Record, "Start Recorder & Play"), PlayAndRecord) { enabled = PlayAndRecordValidate() && CurrentState == PlayModeState.Stopped };
    }
    #endregion

    public static void Refresh() { MainToolbar.Refresh(MenuPath); }

    static bool PlayAndRecordValidate() => true;
    static void PlayAndRecord() { EditorPlayMode.Record(); }

    static Texture2D Record => Resources.Load<Texture2D>("record");
}

[InitializeOnLoad]
class EditorPlayMode
{
    const string RecordKey = "EditorPlayMode/Recording";
    public static PlayModeState CurrentState = PlayModeState.Stopped;

    static bool RecordState
    {
        get => EditorPrefs.GetBool(RecordKey, false);
        set => EditorPrefs.SetBool(RecordKey, value);
    }

    static EditorPlayMode()
    {
        EditorApplication.playmodeStateChanged = OnUnityPlayModeChanged;
        if (RecordState) ApplicationMonitor.OnInstanceCreated += (_) => StartRecording();
    }

    public static event Action<PlayModeState, PlayModeState> OnPlayModeChanged;

    public static void Play() => EditorApplication.isPlaying = true;
    public static void Record() { RecordState = true; Play(); }
    public static void Pause() => EditorApplication.isPaused = true;
    public static void Stop() => EditorApplication.isPlaying = false;

    private static void PlayModeChanged(PlayModeState currentState, PlayModeState changedState)
    {
        OnPlayModeChanged?.Invoke(currentState, changedState);
        PlayRecorderButtons.Refresh();
        Debug.Log($"Changing state: {currentState} to {changedState}");
    }

    private static void OnUnityPlayModeChanged()
    {
        var changedState = PlayModeState.Stopped;
        switch (CurrentState)
        {
            case PlayModeState.Stopped:
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    changedState = PlayModeState.Playing;
                break;
            case PlayModeState.Playing:
                changedState = EditorApplication.isPaused ? PlayModeState.Paused : PlayModeState.Stopped;
                break;
            case PlayModeState.Paused:
                changedState = EditorApplication.isPlayingOrWillChangePlaymode ? PlayModeState.Playing : PlayModeState.Stopped;
                break;
            default: throw new ArgumentOutOfRangeException();
        }

        // Fire PlayModeChanged event.
        PlayModeChanged(CurrentState, changedState);
        CurrentState = changedState;
    }

    static void StartRecording()
    {
        RecordState = false;
        Debug.Log(">>>> START RECORDING <<<<");
        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60.0f;

        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.Enabled = true;

        movie.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };

        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = 3840, OutputHeight = 2160 };
        movie.OutputFile = $"Recordings/boxhead-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        controllerSettings.AddRecorderSettings(movie);

        var rec = new RecorderController(controllerSettings);
        rec.PrepareRecording();
        rec.StartRecording();
    }

    public enum PlayModeState
    {
        Stopped,
        Playing,
        Recording,
        Paused
    }
}