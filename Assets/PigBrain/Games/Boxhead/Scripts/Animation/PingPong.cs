// // using pigbrain.core.Voxel;
// using UnityEngine;

// public class PingPong : MonoBehaviour
// {
//     [SerializeField] Axis axis = Axis.Z;
//     [SerializeField][Range(0, 10)] float distance = 1;
//     [SerializeField][Range(0, 10)] float speed = 1;
//     // [SerializeField][Range(0, 1)] float ease = 1;

//     Vector3 startPos;
//     Tween tween;

//     void OnEnable()
//     {
//         startPos = transform.localPosition;

//         Vector3 dir =
//             axis == Axis.X ? Vector3.right :
//             axis == Axis.Y ? Vector3.up :
//             Vector3.forward * distance;

//         tween = transform.DOLocalMove(startPos + dir, 1f / Mathf.Max(0.0001f, speed))
//             .SetEase(Ease.InOutSine)
//             .SetLoops(-1, LoopType.Yoyo);
//     }

//     void OnDisable()
//     {
//         tween?.Kill();
//         transform.localPosition = startPos;
//     }

//     enum Axis { X, Y, Z }
// }
