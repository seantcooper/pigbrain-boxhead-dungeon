using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.UnityObject;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using UnityEngine;
using UnityEngine.Audio;
using static pigbrain.game.Boxhead.Audio.AudioClips;
using static pigbrain.game.Boxhead.Audio.AudioManager;

namespace pigbrain.game.Boxhead.Audio
{
    public class AudioManager : MonoBehaviourSingleton<AudioManager>
    {
        [SerializeField] AudioMixerGroup soundMixer;
        [SerializeField] AudioMixerGroup musicMixer;
        [SerializeField] internal AudioListener audioListener;
        [SerializeField][InlineScriptableObject] AudioClips clips;
        [SerializeField][InlineScriptableObject] Music music;
        [SerializeField][InlineScriptableObject] Sequencer sequencer;

        Dictionary<string, Clip> lookup;

        // Debug
        [Header("Debug")]
        [SerializeField][ReadOnly] AudioTrack[] soundTracks;
        [SerializeField][ReadOnly] AudioTrack[] musicTracks;

        public static Vector3 ListenerPosition => Instance.audioListener.transform.position;
        public static AudioTrack[] SoundTracks => Instance ? Instance.soundTracks : new AudioTrack[0];
        public static AudioTrack[] MusicTracks => Instance ? Instance.musicTracks : new AudioTrack[0];

        protected override void Awake()
        {
            base.Awake();
            InitializeTracks();
            audioListener = FindAnyObjectByType<AudioListener>();
            lookup = clips.ToDictionary(c => ClipLink.GetID(c.clip), c => c);
        }

        void Start()
        {
            if (sequencer) sequencer.Play();
            if (music) music.Play();
        }

        #region Find Clip
        public static Clip GetClip(AudioClip clip) => GetClip(clip.name);
        public static Clip GetClip(string id) => Instance.lookup.TryGetValue(id, out Clip clip) ? clip :
            throw new Exception($"Clip '{id}' was not found?");
        #endregion

        void InitializeTracks()
        {
            var tracks = GetComponentsInChildren<AudioTrack>();
            soundTracks = tracks.Where(t => t.mixer == soundMixer).ToArray();
            musicTracks = tracks.Where(t => t.mixer == musicMixer).ToArray();
        }

#if UNITY_EDITOR
        public static void PreviewClip(AudioClip clip, float volume, float pitch, bool loop)
        {
            if (clip == null) return;

            // Create a temporary hidden object to act as our "Runtime" player
            GameObject tempGO = new($"playing '{clip.name}'");
            // tempGO.hideFlags = HideFlags.HideAndDontSave; // Keep it out of the Hierarchy

            AudioSource source = tempGO.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.loop = loop;
            source.Play();

            float duration = clip.length / Mathf.Abs(pitch > 0.01f ? pitch : 1f);
            if (source.loop) duration *= 5;
            tempGO.DestroyImmediate(duration);
        }
#endif
    }

    [Serializable]
    public class ClipLink : AssetLink<AudioClip>
    {
        AudioTrack track;
        public void Play(Vector3 position = default, float delay = 0)
        {
            if (string.IsNullOrEmpty(id)) return;
            var clip = GetClip(id);
            if (!clip) return;
            var track = SoundTracks.GetTrack(clip);
            if (!track) return;
            this.track = track;
            track.Play(clip, position == default ? ListenerPosition : position, delay > 0 ? clip.delay : 0);
        }

        public void StopLast()
        {
            if (!track) return;
            track.Stop();
        }
    }
}


#region Editor
#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Audio
{
    using pigbrain.core.Collections;
    using UnityEditor;

    [CustomEditor(typeof(AudioManager))]
    public class AudioManager_Editor : Editor
    {
        const float QuantizeD = 0.1f;
        AudioManager audioManager => target as AudioManager;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            if (!Application.isPlaying) return;

            if (GUILayout.Button("Fire"))
            {
                var clip = GetClip("M16_Shot1");
                var track = SoundTracks.GetTrack(clip);
                track.Play(clip, UnityEngine.Random.insideUnitSphere * 10);
            }

            if (SoundTracks.IsNullOrEmpty()) return;

            void DrawTrack(AudioTrack track)
            {
                var rect = EditorGUILayout.GetControlRect();
                if (track.isPlaying)
                {
                    EditorGUI.DrawRect(rect, Color.green.WithA(0.1f));
                    GUI.Label(rect, track.clip.clip.name);
                }
                else EditorGUI.DrawRect(rect, Color.red.WithA(0.1f));
            }


            GUILayout.Label("Sound tracks");
            SoundTracks.ForEach(DrawTrack);

            GUILayout.Label("music tracks");
            MusicTracks.ForEach(DrawTrack);
            Repaint();
        }
    }
}
#endif
#endregion
