// using pigbrain.Core.Utility;
// using PigBrain.LegacyCore.Utility;
// using UnityEngine;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.Universal;

// [ExecuteAlways]
// public class TiltShift : MonoBehaviour
// {
//     [SerializeField] Volume volume;
//     [SerializeField] Transform focusTarget;

//     [Header("Tilt-Shift Settings")]
//     [SerializeField][Range(1, 300)] float focalLength = 200f;
//     [SerializeField][Range(1, 32)] float aperture = 1f;
//     [SerializeField][Range(-5, 5)] float yOffset = 2f;
//     [SerializeField][ReadOnly] float distance;

//     DepthOfField dof;

//     void Update()
//     {
//         if (!volume) return;
//         if (!focusTarget) return;
//         if (dof == null && !volume.profile.TryGet(out dof)) return;

//         float distanceToTarget = distance = Vector3.Distance(GetComponent<Camera>().transform.position,
//             focusTarget.position.AddY(yOffset));

//         Debug.Log("DOF CHANGED");
//         dof.mode.value = DepthOfFieldMode.Bokeh;
//         dof.focusDistance.value = distanceToTarget;
//         dof.aperture.value = aperture;
//         dof.focalLength.value = focalLength;
//     }
// }
