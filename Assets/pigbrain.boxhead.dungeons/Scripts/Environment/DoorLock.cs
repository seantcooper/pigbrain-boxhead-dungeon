using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Inspector;
using pigbrain.core.Audio;
using UnityEngine;

namespace pigbrain.game.Boxhead.Environment
{
    public class DoorLock : MonoBehaviour
    {
        [SerializeField] float duration = 0.25f;
        [SerializeField] float height = 10;
        [SerializeField] ClipLink landClip;

        void OnEnable()
        {
            StartCoroutine(Drop());
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        IEnumerator Drop()
        {
            yield return null;
            Vector3 start = transform.position.AddY(height), end = transform.position;
            yield return new OverTime(duration, (t) => transform.position = Vector3.Lerp(start, end, t));
            landClip.Play(transform.position);
        }
    }
}