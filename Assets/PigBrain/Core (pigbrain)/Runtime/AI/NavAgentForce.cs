using System.Collections;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using UnityEngine;
using UnityEngine.AI;

namespace pigbrain.core.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavAgentForce : MonoBehaviour
    {
        const float Decay = 0.001f;
        [SerializeField] NavMeshAgent agent;
        [SerializeField][Range(0.01f, 500)] internal float mass = 1;

        Vector3 accumulatedForce;
        Coroutine running;

        public Vector3 linearVelocity => accumulatedForce;

        void OnValidate() => AddAgent();
        void Start() => AddAgent();
        void AddAgent() { if (!agent) agent = GetComponent<NavMeshAgent>(); }

        public void ApplyForce(float mass, Vector3 velocity) => ApplyForce(mass / this.mass * velocity);
        public void ApplyForce(Vector3 force)
        {
            accumulatedForce += force;
            running ??= StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            while (!accumulatedForce.IsZero())
            {
                if (!agent || !agent.isOnNavMesh) yield break;
                agent.Move(accumulatedForce * Time.deltaTime);
                accumulatedForce *= Mathf.Pow(Decay, Time.deltaTime);
                yield return new WaitForNextUpdate();
            }
            accumulatedForce = Vector3.zero;
            running = null;
        }
    }
}