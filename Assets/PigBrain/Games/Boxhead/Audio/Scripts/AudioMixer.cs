using System;
using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.UnityObject;
using UnityEngine;
using UnityEngine.Audio;

namespace pigbrain.game.Boxhead.Audio
{
    public class AudioMixer : MonoBehaviourSingleton<AudioMixer>
    {
        [SerializeField] AudioMixerGroup soundMixer;
        [SerializeField] AudioMixerGroup musicMixer;

        const float MUTE_DB = -80f;
        internal Mixer music, sound;
        internal Mixer[] mixers;

        void Start()
        {
            mixers = new Mixer[]
            {
                music = new(this, musicMixer),
                sound = new(this, soundMixer),
            };
        }

        public void SetPitch(float pitch) => mixers.ForEach(m => m.SetPitch(pitch));

        public void MusicDisabled(bool state) =>
            music.SetActive(!state);

        public void SoundDisabled(bool state) =>
            sound.SetActive(!state);

        static float GetMixerVolume(AudioMixerGroup mixer)
        {
            if (!mixer || !mixer.audioMixer) return 0;
            mixer.audioMixer.GetFloat("Volume", out float value);
            return value;
        }

        static void SetMixerVolume(AudioMixerGroup mixer, float value)
        {
            if (!mixer || !mixer.audioMixer) return;
            mixer.audioMixer.SetFloat("Volume", value);
        }

        internal class Mixer
        {
            readonly AudioMixer audioMixer;
            readonly AudioMixerGroup mixer;
            readonly float initialVolume;
            bool enabled = true;

            public Mixer(AudioMixer audioMixer, AudioMixerGroup mixer)
            {
                this.audioMixer = audioMixer;
                this.mixer = mixer;
                mixer.audioMixer.GetFloat("Volume", out initialVolume);
            }

            public void SetActive(bool state)
            {
                enabled = state;
                audioMixer.StartCoroutine(FadeVolume(enabled ? initialVolume : MUTE_DB));
                // mixer.audioMixer.SetFloat("Volume", enabled ? initialVolume : MUTE_DB);
            }

            public void Pause(bool state)
            {
                if (!enabled) return;
                audioMixer.StartCoroutine(FadeVolume(!state ? initialVolume : MUTE_DB));
                // mixer.audioMixer.SetFloat("Volume", state ? initialVolume : MUTE_DB);
            }

            IEnumerator FadeVolume(float target)
            {
                mixer.audioMixer.GetFloat("Volume", out float start);
                yield return new OverTimeUnscaled(0.25f, (t) =>
                    mixer.audioMixer.SetFloat("Volume", Mathf.Lerp(start, target, t)));
            }

            public void SetPitch(float pitch)
            {
                mixer.audioMixer.SetFloat("Pitch", pitch);
            }
        }

        public class PauseScope : IDisposable
        {
            public PauseScope() => Instance.mixers.ForEach(m => m.Pause(true));
            void IDisposable.Dispose() => Instance.mixers.ForEach(m => m.Pause(false));
        }
    }
}