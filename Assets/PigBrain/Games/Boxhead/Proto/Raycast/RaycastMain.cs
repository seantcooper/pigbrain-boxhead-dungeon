// using pigbrain.core.UnityObject;
// using PigBrain.Generated;
// using UnityEngine;

// [ExecuteAlways]
// public class RaycastMain : MonoBehaviour
// {
//     [SerializeField] Transform player;
//     [SerializeField] Transform enemy;

//     void OnValidate()
//     {
//         gameObject.TryAddComponent(out R1 r1);
//     }

//     void Update()
//     {
//         var collider = enemy.GetComponent<Collider>();
//         var shooter = enemy.position;
//         var target = player.position;

//         bool result = Physics.SphereCast(target, 0.5f, shooter - target, out RaycastHit hit, (shooter - target).magnitude,
//             1 << enemy.gameObject.layer) && hit.collider == collider;

//         Debug.Log($"{result}");
//     }
// }

// public class SubBehaviour
// {
//     protected bool enabled
//     protected virtual void Awake() { }
//     protected virtual void Start() { }
//     protected virtual void Update() { }
//     protected virtual void LateUpdate() { }
// }

// [AddComponentMenu("Raycast/R1")]
// public class R1 : MonoBehaviour { }
// [AddComponentMenu("Raycast/R2")]
// public class R2 : MonoBehaviour { }
// [AddComponentMenu("Raycast/R3")]
// public class R3 : MonoBehaviour { }
// [AddComponentMenu("Raycast/R4")]
// public class R4 : MonoBehaviour { }
