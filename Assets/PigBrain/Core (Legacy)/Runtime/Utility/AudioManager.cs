// using System.Collections;
// using UnityEngine;
// using UnityEngine.Audio;

// public class AudioManager : MonoBehaviour
// {
//     [SerializeField] AudioMixer masterMixer;
//     [SerializeField][Range(0.0001f, 1)] float lowVolume = 0.1f;

//     // volume is in linear 0-1 range
//     void SetMasterVolume(float volume)
//     {
//         float dB = Mathf.Log10(Mathf.Clamp(volume, lowVolume, 1f)) * 20;
//         masterMixer.SetFloat("MasterVolume", dB);
//     }

//     IEnumerator Start()
//     {
//         while (masterMixer)
//         {
//             SetMasterVolume(Mathf.Lerp(0, 1, Time.timeScale));
//             yield return null;
//         }
//     }
// }
