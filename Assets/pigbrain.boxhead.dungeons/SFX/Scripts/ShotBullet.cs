using UnityEngine;
using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Geom;

namespace pigbrain.game.Boxhead
{
    public class ShotBullet : Shot
    {
        protected override IEnumerator ProjectileUpdate()
        {
            // transform.rotation = Quaternion.identity; //(targetPosition - transform.position).GetRotation();
            if (duration > 0)
            {
                Vector3 startPosition = transform.position;
                yield return new OverTime(duration, (t) =>
                    transform.position = Vector3.Lerp(startPosition, targetPosition, t));

                // for (float t = 0, startTime = Time.time; t < 1;)
                // {
                //     transform.position = Vector3.Lerp(startPosition, targetPosition,
                //         t = Mathf.Clamp01((Time.time - startTime) / duration));
                //     yield return new WaitForNextUpdate();
                // }
            }
        }

    }
}
