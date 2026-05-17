#pragma warning disable UDR0001
using UnityEngine;
using UnityEngine.AI;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using System.Collections.Generic;
using pigbrain.core.UnityObject;
using System;

namespace pigbrain.game.Boxhead.Navigation
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshAgentState))]
    public class NavMeshAgentController : MonoBehaviour
    {
        const float MinSpeedSq = 0.00001f;
        [SerializeField, Range(0.5f, 10f)] float cornerProbeDistance = 2f;

        // Vector3 target;
        // bool hasTarget;
        NavMeshAgent agent;
        NavMeshAgentState state;
        IAgentController control;
        Vector3 velocity { get => state.velocity; set => state.velocity = value; }

        public void SetController(IAgentController control) => this.control = control;

        public void SetProbeDistance(float probeDistance) => cornerProbeDistance = probeDistance;
        public float GetProbeDistance() => cornerProbeDistance;
        // public void SetTarget(Vector3 target) { this.target = target; hasTarget = true; }
        // public void ClearTarget() => hasTarget = false;

        void Awake()
        {
            state = GetComponent<NavMeshAgentState>();
            agent = state.agent;
            agent.updatePosition = true;
            agent.updateRotation = true;
            control ??= GetComponent<IAgentController>();
        }

        void Update()
        {
            if (TimeScale.IsPaused) return;

            // Vector3 input = control == null ? GetAxisControl() : control.GetAxisControl();
            IAgentController.Result r = control?.GetAxisControl() ?? default;

            Vector3 newVelocity = default;
            if (r.type == IAgentController.Result.Type.Direction)
            {
                newVelocity = GetMovementDirection(r.value) * agent.speed;
            }
            else if (r.type == IAgentController.Result.Type.Position)
            {
                newVelocity = GetMovementPosition(r.value) * agent.speed;
            }

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
#if UNITY_EDITOR
            gizmoCorners.Clear(); gizmoMarkers.Clear(); gizmoResults.Clear();
#endif
            Vector3 position = transform.position;

            // if (hasTarget) direction = this.target - position;
            direction = direction.normalized;

            Vector3 d = direction * cornerProbeDistance;
            Vector3 sample = position + d;

            if (NavMesh.SamplePosition(sample, out var hit, cornerProbeDistance * 1.01f, state.areaMask))
                sample = hit.position;

            if (agent.CalculatePath(sample, Path))
            {
                if (Path.status != NavMeshPathStatus.PathComplete)
                    return default;

                Vector3[] corners = Path.corners;
                if (corners == null || corners.Length < 2)
                    return default;
#if UNITY_EDITOR
                gizmoCorners.AddRange(corners);
#endif
                for (int i = 1; i < corners.Length; i++)
                {
                    var to = corners[i] - transform.position;
                    if (to.sqrMagnitude > 0.01f)
                    {
#if UNITY_EDITOR
                        gizmoResults.Add(corners[i]);
#endif
                        return to.normalized;
                    }
                }
            }
            return default;
        }
        #endregion

        #region  Movement Direction
        Vector3 GetMovementPosition(Vector3 destination)
        {
            if (!agent.isActiveAndEnabled) return default;
            Path ??= new();

            Vector3 position = transform.position;

            if (agent.CalculatePath(destination, Path))
            {
                if (Path.status != NavMeshPathStatus.PathComplete)
                    return default;

                Vector3[] corners = Path.corners;
                if (corners == null || corners.Length < 2)
                    return default;

                for (int i = 1; i < corners.Length; i++)
                {
                    var to = corners[i] - position;
                    if (to.sqrMagnitude > 0.01f)
                        return to.normalized;
                }
            }
            return default;
        }
        #endregion

        // Vector3 GetAxisControl() => default;

        #region  Gizmos
#if UNITY_EDITOR
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
#endif
        #endregion
    }

    public interface IAgentController
    {
        Result GetAxisControl();

        public struct Result
        {
            public Type type;
            public Vector3 value;
            public enum Type { None, Direction, Position, }
        }
    }
}