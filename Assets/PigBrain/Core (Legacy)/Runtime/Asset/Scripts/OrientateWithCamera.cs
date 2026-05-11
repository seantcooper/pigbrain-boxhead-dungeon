using Unity.Mathematics;
using UnityEngine;

public class OrientateWithCamera : MonoBehaviour
{
    [SerializeField] bool3 constraint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var cameraRotation = Camera.main.transform.eulerAngles;
        var rotation = transform.eulerAngles;
        if (!constraint.x) rotation.x = cameraRotation.x;
        if (!constraint.y) rotation.y = cameraRotation.y;
        if (!constraint.z) rotation.z = cameraRotation.z;
        transform.eulerAngles = rotation;
    }
}
