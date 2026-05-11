#pragma warning disable UDR0001
using UnityEngine;
using UnityEngine.AI;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using System.Collections.Generic;
using pigbrain.core.UnityObject;

namespace pigbrain.game.Boxhead.Navigation
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshAgentState))]
    public class NavMeshAgentController : MonoBehaviour
    {
        const float MinSpeedSq = 0.00001f;
        [SerializeField, Range(0.5f, 10f)] float cornerProbeDistance = 2f;

        Vector3 target;
        bool hasTarget;
        NavMeshAgent agent;
        NavMeshAgentState state;
        IAgentControllerInput control;
        Vector3 velocity { get => state.velocity; set => state.velocity = value; }

        public void SetController(IAgentControllerInput control) => this.control = control;

        public void SetProbeDistance(float probeDistance) => cornerProbeDistance = probeDistance;
        public float GetProbeDistance() => cornerProbeDistance;
        public void SetTarget(Vector3 target) { this.target = target; hasTarget = true; }
        public void ClearTarget() => hasTarget = false;

        void Awake()
        {
            state = GetComponent<NavMeshAgentState>();
            agent = state.agent;
            agent.updatePosition = true;
            agent.updateRotation = true;
            control ??= GetComponent<IAgentControllerInput>();
        }

        void Update()
        {
            if (TimeScale.IsPaused) return;
            Vector3 input = control == null ? GetAxisControl() : control.GetAxisControl();

            Vector3 newVelocity = input.sqrMagnitude < MinSpeedSq ? default
                : GetMovementDirection(input) * agent.speed;

            velocity = Vector3.MoveTowards(velocity, newVelocity, agent.acceleration * Time.deltaTime);

            if (agent.isOnNavMesh && velocity.sqrMagnitude > MinSpeedSq)
                agent.Move(velocity * Time.deltaTime);
        }

        #region  Movement Direction
        static NavMeshPath Path;
        Vector3 GetMovementDirection(Vector3 direction)
        {
            if (!agent.isActiveAndEnabled) return default;
            Path ??= new();
            gizmoCorners.Clear(); gizmoMarkers.Clear(); gizmoResults.Clear();
            Vector3 position = transform.position;

            if (hasTarget) direction = this.target - position;
            direction = direction.normalized;

            Vector3 d = direction * cornerProbeDistance;
            Vector3 sample = position + d;

            if (NavMesh.SamplePosition(sample, out var hit, d.magnitude * 1.01f, agent.areaMask))
                sample = hit.position;

            if (agent.CalculatePath(sample, Path))
            {
                Vector3[] corners = Path.corners;
                gizmoCorners.AddRange(corners);
                for (int i = 1; i < corners.Length; i++)
                {
                    var to = corners[i] - transform.position;
                    if (to.sqrMagnitude > 0.01f)
                    {
                        gizmoResults.Add(corners[i]);
                        direction = to.normalized;
                        break;
                    }
                }
            }
            return direction;
        }
        #endregion

        Vector3 GetAxisControl() => default;

        #region  Gizmos
        readonly List<Vector3> gizmoCorners = new(), gizmoMarkers = new(), gizmoResults = new();
        [SerializeField][HideInInspector] bool showGizmos = true;
        [ContextMenu("Show Gizmos")] void ToogleGizmos() => showGizmos = !showGizmos;

        void OnDrawGizmos()
        {
            if (!showGizmos) return;
            Gizmos.color = Color.white;
            if (!gizmoCorners.IsNullOrEmpty())
                GizmosUtility.DrawLines(gizmoCorners, 0.1f);
            Gizmos.color = Color.green;
            if (!gizmoMarkers.IsNullOrEmpty())
                GizmosUtility.DrawPoints(gizmoMarkers, 0.25f);
            Gizmos.color = Color.red;
            if (!gizmoResults.IsNullOrEmpty())
                GizmosUtility.DrawPoints(gizmoResults, 0.2f);

        }
        #endregion
    }

    public interface IAgentControllerInput
    {
        Vector3 GetAxisControl();
    }
}