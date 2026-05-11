#pragma warning disable UDR0001
using pigbrain.core.Inspector;
using pigbrain.core.Geom;
using UnityEngine;
using UnityEngine.AI;
using pigbrain.core.AI;
using pigbrain.core.UnityObject;

namespace pigbrain.game.Boxhead.Navigation
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshAgentState : MonoBehaviour
    {
        const float MinSpeedSq = 0.00001f;
        [SerializeField][ReadOnly] internal Vector3 velocity;

        [Header("Animation")]
        [SerializeField] string animatorSpeed = "Speed";

        [Header("Repath")]
        [SerializeField][MinMaxRange(0.1f, 10f)] MinMaxFloat repathDistance = new(3, 10);
        [SerializeField][MinMaxRange(0.1f, 2f)] MinMaxFloat repathInterval = new(0.1f, 1);

        [Header("Debug")]
        [SerializeField][ReadOnly] float speed;
        [SerializeField][ReadOnly] Vector3 lookDirection;
        [SerializeField][ReadOnly] bool hasPath;
        [SerializeField][ReadOnly] bool onNavMesh;
        [SerializeField][ReadOnly] bool navMeshError;

        [HideInInspector] public NavMeshAgent agent;
        [HideInInspector] public AnimationController animator;

        void OnValidate()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<AnimationController>();
        }

        void Start() => agent.TryWarp(transform.position);
        void Update() { if (!TimeScale.IsPaused) UpdateMovement(); }
        void LateUpdate() { if (!TimeScale.IsPaused) UpdateRotation(); }

        // #region Physics
        // public bool HasLOS(Vector3 position) =>
        //     !agent.Raycast(position, out var hit);
        // #endregion

        #region Rotation
        public Vector3 GetLookDirection() =>
            agent.updateRotation ? lookDirection : default;

        public void SetLookDirection(Vector3 direction)
        {
            agent.updateRotation = false;
            lookDirection = direction.normalized;
        }

        public void ClearLookDirection()
        {
            agent.updateRotation = true;
            lookDirection = Vector3.zero;
        }

        void UpdateRotation()
        {
            Vector3 direction = agent.updateRotation ? velocity : lookDirection;
            if (direction.sqrMagnitude < MinSpeedSq) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, direction.GetRotation(),
                agent.angularSpeed * Time.deltaTime);

        }
        #endregion

        #region Movement
        (Vector3 position, float time) lastRepath;
        public void SetDestination(Vector3 destination, float distanceThreshold = 0.3f)
        {
            float dist = Vector3.Distance(agent.transform.position, destination);
            float t = Mathf.InverseLerp(repathDistance.min, repathDistance.max, dist);
            float interval = Mathf.Lerp(repathInterval.min, repathInterval.max, t);

            if (Time.time < lastRepath.time + interval) return;

            if (agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete)
                Debug.Log("Path is not complete!");

            if (!agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1, agent.areaMask))
                    agent.Warp(hit.position);
                else
                {
                    navMeshError = true;
                    return;
                }
            }

            navMeshError = false;
            agent.SetDestination(destination);
            lastRepath = (destination, Time.time);
        }

        void UpdateMovement()
        {
            onNavMesh = agent.isOnNavMesh;
            hasPath = agent.hasPath;
            if (agent.hasPath) velocity = agent.velocity;
            animator.SetFloat(animatorSpeed,
                this.speed = velocity.magnitude / agent.speed);
        }

        public void StopMovement()
        {
            agent.isStopped = true;
            agent.ResetPath();
            velocity = agent.velocity = Vector3.zero;
            lastRepath.time = 0;
        }

        public void StartMovement()
        {
            agent.isStopped = false;
        }
        #endregion
    }

}