using pigbrain.core.Inspector;
using UnityEngine;

namespace pigbrain.game.Boxhead.Environment
{
    [InlineButton(nameof(Open), nameof(Close))]
    public class Door : MonoBehaviour
    {
        [SerializeField] Quaternion closed;
        [SerializeField] Quaternion open;

        public void Open()
        {
            transform.localRotation = open;
        }
        public void Close()
        {
            transform.localRotation = closed;
        }
    }
}