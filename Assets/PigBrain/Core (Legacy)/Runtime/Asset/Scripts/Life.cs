using System.Collections;
using UnityEngine;

public class Life : MonoBehaviour
{
    [SerializeField][Range(1, 120)] float duration = 5;

    void Start() => Destroy(this.gameObject, duration);
}
