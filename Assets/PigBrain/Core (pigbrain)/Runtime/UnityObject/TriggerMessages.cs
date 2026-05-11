using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public sealed class TriggerMessages : MonoBehaviour
    {
        [SerializeField] Component target;
        [SerializeField] SendMessageOptions sendMessageOptions = SendMessageOptions.DontRequireReceiver;

        void OnTriggerEnter(Collider other) =>
            Message(nameof(OnTriggerEnter), other, sendMessageOptions);
        void OnTriggerStay(Collider other) =>
           Message(nameof(OnTriggerStay), other, sendMessageOptions);
        void OnTriggerExit(Collider other) =>
         Message(nameof(OnTriggerExit), other, sendMessageOptions);

        void Message(string name, Collider other, SendMessageOptions options)
        {
            if (target) target.SendMessage(name, other, options);
        }
    }

}