#pragma warning disable UDR0001
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using pigbrain.game.Boxhead.FiniteStateMachine;
using System.Collections;
using pigbrain.game.Boxhead.Navigation;
using System;
using static UnityEngine.Object;
using pigbrain.core.Geom;
using UnityEngine.InputSystem;
using pigbrain.game.Boxhead.Environment;
using pigbrain.core.Collections;
using pigbrain.core.Graphics;
using System.Collections.Generic;
using pigbrain.core.UnityObject;
using pigbrain.core.Audio;

namespace pigbrain.game.Boxhead
{
    #region State
    public abstract class PlayerState : FSM.State<Player>
    {
        public NavMeshAgentState nmaState => fsm.nmaState;
        // public Animator animator => nmaState.animator;
        public NavMeshAgent agent => nmaState.agent;
    }
    #endregion

    #region Control
    [Serializable]
    public class PlayerControl : PlayerState, IAgentControllerInput
    {
        PlayerInput playerInput;
        internal InputAction move;

        public override void Start()
        {
            base.Start();
            move = fsm.TryGetComponent(out playerInput) ? playerInput.actions["Move"] : Game.Input.GamePlay.Move;
        }
        public override void Enter() => fsm.nmaController.SetController(this);
        public override void Exit() => fsm.nmaController.SetController(null);

        Vector3 IAgentControllerInput.GetAxisControl() =>
            move.ReadValue<Vector2>().X_Y();
    }
    #endregion

    #region Rage
    [Serializable]
    public sealed class PlayerRage : PlayerControl
    {
        [SerializeField] int soldiers = 5;
        [SerializeField][Range(0, 60)] internal int duration = 10;
        [SerializeField] Player soldier;
        [SerializeField] ClipLink enter, exit, during;

        public override void Enter()
        {
            base.Enter();
            if (fsm.TryGetComponent(out Invincible invincible)) invincible.Activate(duration);
            CreateSoldiers();
            enter.Play(default, 0);
            // enter.Play(default, 0.2f);
            during.Play();
        }

        public override void Exit()
        {
            base.Exit();
            if (fsm.TryGetComponent(out Invincible invincible)) invincible.enabled = false;
            enter.StopLast();
            exit.Play(default, 0);
            // exit.Play(default, 0.2f);
            during.StopLast();
        }

        public override IEnumerator Run()
        {
            yield return new WaitForSeconds(duration);
            RemoveSoldiers();
            SetState(fsm.control);
        }

        readonly List<Player> instances = new();
        void CreateSoldiers()
        {
            if (!soldier) return;
            for (int i = 0; i < soldiers; i++)
            {
                instances.Add(soldier.Instantiate(fsm.GetRoom().data.GetRandomPosition()));
                if (instances[^1].TryGetComponent(out Invincible invincible)) invincible.Activate(duration);
                instances[^1].follow.duration = duration;
            }
        }
        void RemoveSoldiers()
        {
            foreach (var inst in instances)
            {
                if (!inst) return;
                instances[^1].follow.Cancel();
            }
            instances.Clear();
        }
    }
    #endregion

    #region Control
    [Serializable]
    public sealed class PlayerDemo : PlayerState
    {
        [SerializeField] int seed = 10001;
        [SerializeField] float moveDuration = 1;
        ActiveRoom activeRoom;
        Rnd rnd;
        public override void Enter()
        {
            activeRoom = ActiveRoom.Instance;
            fsm.nmaController.enabled = false;
            rnd = new Rnd(seed);
            transform.GetComponent<Health>().enabled = false;
        }
        public override void Exit() => fsm.nmaController.enabled = true;

        public override IEnumerator Run()
        {
            if (activeRoom.GetLevelRoom() is Room room)
                agent.destination = room.data.GetRandomPosition(rnd);
            yield return new WaitForSeconds(moveDuration);
        }
    }
    #endregion

    #region Find
    [Serializable]
    public sealed class PlayerFind : PlayerState
    {
        internal Vector3 startPosition;
        public override void Enter()
        {
            startPosition = transform.position;
            fsm.SetLeader(FindObjectsByType<Player>()
                    .Where(p => p != fsm && p.aiControl == false)
                    .OrderBy(p => (p.transform.position - transform.position).sqrMagnitude)
                    .FirstOrDefault());
            nmaState.ClearLookDirection();
            agent.stoppingDistance = 4;
        }
        public override void Exit() => nmaState.StopMovement();

        public override IEnumerator Run()
        {
            yield return new WaitForSeconds(0.25f);
            while ((fsm.leader.transform.position - transform.position).magnitude > agent.remainingDistance + 1)
            {
                agent.SetDestination(fsm.leader.transform.position);
                while (agent.remainingDistance > agent.stoppingDistance)
                    yield return new WaitForNextUpdate();
                yield return new WaitForNextUpdate();
            }
            SetState(fsm.follow);
        }
    }
    #endregion

    #region Follow
    [Serializable]
    public sealed class PlayerFollow : PlayerState, IAgentControllerInput
    {
        [SerializeField][Range(0.5f, 10)] internal float leaderDistance = 1;
        [SerializeField][Range(5, 20)] internal float lostDistance = 10;
        [SerializeField][Range(0, 120)] internal float duration = 0;
        Player leader => fsm.leader;
        public override void Enter()
        {
            fsm.nmaController.SetController(this);
            if (!leader) fsm.SetLeader(FindObjectsByType<Player>()
                .Where(p => p != fsm && p.aiControl == false)
                .OrderBy(p => (p.transform.position - transform.position).sqrMagnitude)
                .FirstOrDefault());

            nmaState.ClearLookDirection();
        }
        public override void Exit() => fsm.nmaController.SetController(null);

        public override IEnumerator Run()
        {
            if ((duration > 0 && fsm.stateTime > duration) || !leader)
                SetState(fsm.leave);
            yield break;
        }

        public override void Cancel() => SetState(fsm.leave);

        #region AI Control
        Vector3 IAgentControllerInput.GetAxisControl()
        {
            if (!leader) return Vector3.zero;
            Vector3 bestPosition = fsm.GetRankPosition(leaderDistance);
            Vector3 delta = bestPosition - transform.position;
            float distance = delta.magnitude;
            return distance < 0.5f ? Vector3.zero : delta.normalized;
        }
        #endregion
    }
    #endregion

    #region Leave
    [Serializable]
    public sealed class PlayerLeave : PlayerState
    {
        [SerializeField] float expireTime = 30;
        internal Vector3 startPosition;
        public override void Enter()
        {
            Vector3 destination;

            // if (fsm.find.enabled)
            // {
            //     destination = fsm.find.startPosition;
            //     // // Debug.LogWarning("If it did not find the player there is no start position!");
            //     // Destroy(transform.gameObject);
            //     // return;
            // }
            // else
            // {
            //     var room = gameObject.GetComponent<CullingGroupItem>().zone.GetComponent<Room>();
            //     destination = room.data.GetRandomPosition();
            // }

            var room = gameObject.GetComponent<CullingGroupItem>().zone.GetComponent<Room>();
            destination = room.data.GetRandomPosition();

            agent.SetDestination(destination);
            agent.stoppingDistance = 1;
            // Debug.Log($"Leaving!");
        }

        public override IEnumerator Run()
        {
            if ((agent.destination - transform.position).magnitude < agent.stoppingDistance + 1
                || fsm.stateTime > expireTime)
            {
                yield return null;
                Destroy(gameObject);
            }
        }
    }
    #endregion

    #region Dead
    [Serializable]
    public sealed class PlayerDead : PlayerState
    {
        [SerializeField] CommandContainer onDeath;
        [SerializeField] internal GameObject explodeModel;

        public override void Enter()
        {
            agent.isStopped = true;
            agent.enabled = false;
            nmaState.animator.SetTrigger("Dead");
        }

        public override IEnumerator Run()
        {
            onDeath?.Invoke(transform);
            yield return new WaitForSeconds(2);
            if (fsm.aiControl) Destroy(gameObject);
            fsm.enabled = false;
            yield break;
        }
    }
    #endregion

}