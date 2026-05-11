using Unity.Mathematics;
using UnityEngine;

public class Rotation : MonoBehaviour
{
    [SerializeField] float speed = 0.1f;
    [SerializeField] Vector3 axis = Vector3.up;
    [SerializeField] Vector3 pivot = Vector3.zero;

    void Update()
    {
        // if (useRigidBody && TryGetComponent(out Rigidbody rb))
        // {
        //     rb.angularVelocity = euler * Mathf.Deg2Rad;
        // }

        transform.RotateAround(pivot, axis.normalized, speed * Time.deltaTime);

    }
}
