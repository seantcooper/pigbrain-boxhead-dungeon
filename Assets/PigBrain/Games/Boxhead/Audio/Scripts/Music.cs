using System;
using System.Collections;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.game.Boxhead.Statistic;
using UnityEngine;
using static pigbrain.game.Boxhead.Audio.AudioClips;

namespace pigbrain.game.Boxhead.Audio
{
    public class Music : ScriptableObject
    {
        [SerializeField] internal StatsLink stats;
        [SerializeField] internal Stat stat;
        [SerializeField][Range(0.1f, 2)] internal float mixDuration = 1;
        [SerializeField] MusicTrack[] tracks;

        void OnValidate()
        {
            tracks.Where(t => t).ForEach(t => t.OnValidate());
        }

        public void Play()
        {
            AudioManager.Instance.StartCoroutine(Track());
        }

        IEnumerator Track()
        {
            tracks.ForEach(t => t.Play());

            MusicTrack currentTrack = null;
            float value;
            while (true)
            {
                value = stats.GetRuntime()[stat];
                MusicTrack newMusicTrack = null;
                foreach (var track in tracks)
                    if (value >= track.value)
                        newMusicTrack = track;

                if (newMusicTrack != currentTrack)
                {
                    currentTrack = newMusicTrack;
                    if (currentTrack) currentTrack.audioTrack.FadeIn(mixDuration);

                    foreach (var track in tracks)
                        if (currentTrack != track) track.audioTrack.FadeOut(mixDuration);
                }

                yield return new WaitForSeconds(mixDuration);
                yield return null;
            }
        }

        [Serializable]
        class MusicTrack
        {
            [SerializeField][HideInInspector] string name;
            [SerializeField] AudioClip audioClip;
            [SerializeField] internal int value;
            internal AudioTrack audioTrack;
            Clip clip;

            internal void OnValidate()
            {
                name = audioClip ? audioClip.name : "<none>";
            }

            public void Play()
            {
                clip = AudioManager.GetClip(audioClip);
                (audioTrack = AudioManager.MusicTracks.GetTrack(clip)).Play(clip, schedule: clip.delay);
                audioTrack.volume = 0;
            }
            public static implicit operator bool(MusicTrack empty) => empty != null;
        }
    }
}