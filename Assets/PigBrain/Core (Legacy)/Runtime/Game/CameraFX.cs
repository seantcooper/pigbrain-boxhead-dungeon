// using UnityEngine;

// public class CameraFX : MonoBehaviour
// {
//     [SerializeField] float noiseSpeed = 0;
//     [SerializeField] float noiseAmount = 1f;

//     Vector3 initialRotation;
//     float timeOffset;

//     void Start()
//     {
//         initialRotation = transform.eulerAngles;
//         timeOffset = UnityEngine.Random.value * 100f;
//     }

//     void Update()
//     {
//         float t = Time.time * noiseSpeed + timeOffset;
//         float x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * noiseAmount;
//         float y = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * noiseAmount;

//         transform.rotation = Quaternion.Euler(initialRotation + new Vector3(x, y, 0));
//     }
// }
