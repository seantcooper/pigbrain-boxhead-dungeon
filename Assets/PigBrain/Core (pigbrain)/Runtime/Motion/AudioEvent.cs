using pigbrain.core.Inspector;
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
}