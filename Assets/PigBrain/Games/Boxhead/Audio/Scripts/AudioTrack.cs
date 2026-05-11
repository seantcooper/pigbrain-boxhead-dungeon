using System;
using System.Collections;
using System.Collections.Generic;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using UnityEngine;
using UnityEngine.Audio;
using static pigbrain.game.Boxhead.Audio.AudioClips;

namespace pigbrain.game.Boxhead.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class AudioTrack : MonoBehaviour
    {
        public AudioMixerGroup mixer;
        [SerializeField] AudioSource source;
        [SerializeField] bool locked;
        [SerializeField] double scheduledEnd;
        [SerializeField] internal double startTime;
        [SerializeField][ReadOnly] internal Clip clip;

        public float volume { get => source.volume; set => source.volume = value; }

        void OnValidate()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.outputAudioMixerGroup = mixer;
        }

        public void Lock(bool state) => locked = state;
        public bool isScheduled => AudioSettings.dspTime < scheduledEnd;
        public bool isPlaying => AudioSettings.dspTime < endTime;
        public bool isBusy => isPlaying || locked;
        public double endTime => clip ? (source.loop ? double.MaxValue : startTime + clip.length) : 0;
        public AudioClip audioClip => source.clip;

        internal void Stop()
        {
            source.Stop();
            source.loop = false;
            source.clip = null;
            locked = false;
            scheduledEnd = 0;
            clip = null;
        }

        internal bool Schedule(double scheduled, Clip clip, Vector3 position)
        {
            if (isPlaying) return false;
            scheduledEnd = scheduled + clip.length;
            Play(clip, position, () => source.PlayScheduled(scheduled));
            this.startTime = (float)scheduled;
            return true;
        }

        internal bool InternalPlay(Clip clip, Vector3 position)
        {
            Play(clip, position, () => source.Play());
            return true;
        }

        void Play(Clip clip, Vector3 position, Action action)
        {
            this.clip = clip;
            this.startTime = AudioSettings.dspTime;
            // Debug.Log($"Play {clip.name} loop: {clip.loop}");
            source.loop = clip.loop;
            source.clip = clip.clip;
            source.volume = clip.volume.Rnd();
            source.pitch = clip.pitch.Rnd();
            source.transform.position = position;
            source.spatialBlend = clip.spatial; // position.sqrMagnitude <= 0.0000001f ? 0 : 1;
            action.Invoke();
        }

        IEnumerator Fade(float duration, float v1, float v2)
        { yield return new OverTime(duration, (t) => volume = Mathf.Lerp(v1, v2, t)); }

        public void FadeIn(float duration) => StartCoroutine(Fade(duration, volume, clip.volume.max));
        public void FadeOut(float duration) => StartCoroutine(Fade(duration, volume, 0));

        public static implicit operator bool(AudioTrack empty) => empty != null;
    }

    public static class AudioTrackX
    {
        public static float Rnd(this MinMaxFloat mmf) =>
            mmf.min == mmf.max ? mmf.min : UnityEngine.Random.Range(mmf.min, mmf.max);

        public static void Play(this AudioTrack track, Clip clip, Vector3 position = default, double schedule = 0)
        {
            if (track == null) return;
            if (schedule > 0) track.Schedule(schedule, clip, position);
            else track.InternalPlay(clip, position);
        }

        public static AudioTrack GetTrack(this IList<AudioTrack> tracks, Clip clip)
        {
            AudioTrack oldest = null;
            AudioTrack oldestMatch = null;
            AudioTrack free = null;
            int count = 0;

            foreach (var track in tracks)
            {
                void Oldest(ref AudioTrack oldest) { if (!oldest || track.startTime < oldest.startTime) oldest = track; }

                if (track.isPlaying)
                {
                    if (track.clip == clip)
                    {
                        if (!clip.important) Oldest(ref oldestMatch);
                        count++;
                    }
                    else if (!track.clip.important) Oldest(ref oldest);
                }
                else Oldest(ref free);
            }

            AudioTrack result = null;
            if (count >= clip.count) result = oldestMatch;
            else if (free) result = free;
            else if (oldest) result = oldest;

            if (result && result.isPlaying)
                result.Stop();

            return result;

            // AudioTrack track = null;
            // var result = tracks.GetTracks(clip);
            // if (result.Count >= clip.count) track = result.GetOldestTrack(true);
            // else track = tracks.GetFreeTrack();
            // if (track && track.isPlaying) track.Stop();
            // return track;
        }

        // static AudioTrack GetOldestTrack(this IList<AudioTrack> tracks, bool isPlaying = false)
        // {
        //     (AudioTrack track, double startTime) result = (null, double.MaxValue);
        //     foreach (var track in tracks)
        //         if (track.isPlaying == isPlaying && track.startTime < result.startTime)
        //             result = (track, track.startTime);
        //     return result.track;
        // }

        // static List<AudioTrack> GetTracks(this IList<AudioTrack> tracks, Clip clip)
        // {
        //     List<AudioTrack> list = new(tracks.Count);
        //     foreach (var track in tracks)
        //         if (track.isBusy && track.clip == clip) list.Add(track);
        //     return list;
        // }

        // static AudioTrack GetFreeTrack(this IList<AudioTrack> tracks, bool useOldest = true)
        // {
        //     foreach (var track in tracks)
        //         if (!track.isPlaying) return track;
        //     return useOldest ? tracks.GetOldestTrack(true) : null;
        // }



    }
}