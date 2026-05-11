using System;
using System.Collections;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using UnityEngine;
using static pigbrain.game.Boxhead.Audio.AudioClips;
using static pigbrain.game.Boxhead.Audio.AudioManager;

namespace pigbrain.game.Boxhead.Audio
{
    public class Sequencer : ScriptableObject
    {
        [SerializeField] internal Sequence[] sequences;

        void OnValidate() => sequences?.ForEach(s => s.OnValidate());

        public Coroutine[] Play() =>
            sequences.Select(s => AudioManager.Instance.StartCoroutine(s.PlaySequence())).ToArray();

        public static AudioTrack GetTrack(Clip clip) => SoundTracks.GetTrack(clip);

        [Serializable]
        public class Sequence
        {
            [SerializeField] AudioClip clip;
            [SerializeField] internal float startDuration;
            [SerializeField] internal float repeatDuration;
            [SerializeField][ReadOnly] internal float clipLength;

            internal void OnValidate() => clipLength = clip ? clip.length : -1;

            public IEnumerator PlaySequence()
            {
                double next = AudioSettings.dspTime + startDuration;
                double lookAhead = 0.25; // schedule slightly ahead

                Clip clip = GetClip(this.clip);
                while (true)
                {
                    if (AudioSettings.dspTime + lookAhead >= next)
                    {
                        GetTrack(clip).Play(clip, default, next);
                        next += repeatDuration;
                    }
                    yield return null;
                }
            }
        }
    }
}

#region Editor
#if UNITY_EDITOR
namespace pigbrain.game.Boxhead.Audio
{
    using pigbrain.core.Utility;
    using UnityEditor;

    [CustomEditor(typeof(Sequencer))]
    public class Sequencer_Editor : Editor
    {
        const float QuantizeD = 0.1f;
        Sequencer sequencer => target as Sequencer;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            foreach (var sequence in sequencer.sequences)
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.MaxHeight(8));
                var s = sequence;

                for (int i = 0; i < 100; i++)
                {
                    float t1 = s.startDuration + s.repeatDuration * i, t2 = t1 + s.clipLength;
                    int q1 = QuantizeTime(t1), q2 = QuantizeTime(t2);
                    Rect r = new(q1, rect.y, q2 - q1, rect.height);
                    EditorGUI.DrawRect(r, Color.green);
                    if (q2 > rect.size.x) break;
                }
            }
        }

        int QuantizeTime(float time) =>
            Mathf.RoundToInt(time / QuantizeD);
    }
}
#endif
#endregion
