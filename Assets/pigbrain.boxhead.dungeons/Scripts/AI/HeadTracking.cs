using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public class HeadTracking : MonoBehaviour
    {
        [SerializeField] Transform head;
        [SerializeField] Transform neck;
        [SerializeField] Target targetType = Target.Camera;
        [SerializeField] Vector3 offset;

        enum Target
        {
            Camera, Player
        }

        [SerializeField] float turnSpeed = 10f;
        [SerializeField] float maxYaw = 60f;
        [SerializeField] float maxPitch = 30f;

        public void SetTarget(Transform target) { } // => this.target = target;

        void Start()
        {
        }

        Vector3 GetTargetPosition()
        {
            switch (targetType)
            {
                case Target.Camera: return Camera.main.transform.position;
                case Target.Player: return ActivePlayer.Position;
                default: throw new System.Exception($"Invalid target type '{targetType}'");
            }
        }

        void LateUpdate()
        {
            if (!head) return;

            Vector3 targetPosition = GetTargetPosition() + offset;

            var dir = (targetPosition - head.position).normalized;
            var local = transform.InverseTransformDirection(dir);

            float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -maxYaw, maxYaw);

            float pitch = -Mathf.Asin(local.y) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

            var rot = Quaternion.Euler(pitch, yaw, 0);

            head.localRotation = Quaternion.Slerp(head.localRotation, rot, Time.deltaTime * turnSpeed);

            if (neck)
            {
                var neckRot = Quaternion.Slerp(Quaternion.identity, rot, 0.5f);
                neck.localRotation = Quaternion.Slerp(neck.localRotation, neckRot, Time.deltaTime * turnSpeed);
            }
        }
    }
}