using UnityEngine;
using UnityEngine.AI;

namespace pigbrain.core.AI
{
    public class MassForce : MonoBehaviour
    {
        [SerializeField][Range(0.01f, 500)] internal float mass = 1;
    }
}