

using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Statistic;
using TMPro;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public class PadSwitch : PadBase
    {
        protected override void StartFill(Collider other)
        {
            base.StartFill(other);
            StartCoroutine(Run(other));
        }

        protected override void EndFill(Collider other)
        {
            base.EndFill(other);
            StopAllCoroutines();
        }

        IEnumerator Run(Collider other)
        {
            yield return new WaitForSeconds(0.25f);
            var motion = other.GetComponent<TransformMotion>();

            while (enabled)
            {
                float dt = fillDuration > 0.0001f ? Time.deltaTime / fillDuration : 1f;
                if (!(motion && !motion.isStationary) && SetFill(fill + dt))
                {
                    CompleteFill(other);
                    yield break;
                }
                yield return new WaitForNextUpdate();
            }
        }
    }
}
