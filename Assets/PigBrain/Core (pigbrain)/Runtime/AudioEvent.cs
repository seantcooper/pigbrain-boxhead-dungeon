using System;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Audio;
using UnityEngine;

namespace pigbrain.core.Motion
{
    public class AudioEvent : MonoBehaviour
    {
        [SerializeField] ClipLink onStart;
        [SerializeField] ClipLink onEnable;
        [SerializeField] ClipLink onDisable;

        void Play(ClipLink clip)
        {
            if (!clip) return;
            clip.Play(transform.position);
        }

        void Start() =>
            Play(onStart);
        void OnEnable() =>
            Play(onEnable);
        void OnDisable() =>
            Play(onDisable);
    }

    // [Serializable]
    // public class ClipLink : AssetLink<AudioClip>
    // {
    //     AudioTrack track;
    //     public void Play(Vector3 position = default, float delay = 0)
    //     {
    //         if (string.IsNullOrEmpty(id)) return;
    //         var clip = GetClip(id);
    //         if (!clip) return;
    //         var track = SoundTracks.GetTrack(clip);
    //         if (!track) return;
    //         this.track = track;
    //         track.Play(clip, position == default ? ListenerPosition : position, delay > 0 ? clip.delay : 0);
    //     }

    //     public void StopLast()
    //     {
    //         if (!track) return;
    //         track.Stop();
    //     }
    // }

}