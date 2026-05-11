using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public sealed class AnimatorMessages : MonoBehaviour
    {
        [SerializeField] Component target;
        [SerializeField] SendMessageOptions sendMessageOptions = SendMessageOptions.DontRequireReceiver;

        void OnAnimatorMove()
        {
            if (target) target.SendMessage(nameof(OnAnimatorMove), sendMessageOptions);
        }

        void OnAnimatorIK()
        {
            if (target) target.SendMessage(nameof(OnAnimatorIK), sendMessageOptions);
        }

    }
}