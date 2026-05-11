using System;
using System.Collections;
using pigbrain.core.Analysis;
using pigbrain.core.Graphics;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Navigation;
using Unity.AI.Navigation;
using UnityEngine;

namespace pigbrain.game.Boxhead.Environment
{
    public class BootStrap : MonoBehaviourSingleton<BootStrap>
    {
        [Header("Builders")]
        [SerializeField] RoomLayout roomLayout;
        [SerializeField] NavMeshSurface navMeshSurface;
        [SerializeField] NavMap navMap;

        [Header("Behaviours")]
        [SerializeField] ActiveRoom activeRoom;
        [SerializeField] ActivePlayer activePlayer;
        [SerializeField] CullingGroupManager cullingGroupManager;

        public event Action OnComplete;

        void OnValidate()
        {
            if (!roomLayout) roomLayout = GetComponentInChildren<RoomLayout>();
            if (!navMeshSurface) navMeshSurface = GetComponentInChildren<NavMeshSurface>();
            if (!navMap) navMap = GetComponentInChildren<NavMap>();
            if (!activeRoom) activeRoom = GetComponentInChildren<ActiveRoom>();
            if (!activePlayer) activePlayer = GetComponentInChildren<ActivePlayer>();
            if (!cullingGroupManager) cullingGroupManager = GetComponentInChildren<CullingGroupManager>();
        }

        protected override void Awake()
        {
            base.Awake();
            Debug.Log("Bootstrap Start");
            gameObject.SetActive(false);
            if (activePlayer) activePlayer.enabled = false;
            if (activeRoom) activeRoom.enabled = false;
            if (cullingGroupManager) cullingGroupManager.enabled = false;
        }

        public void Activate()
        {
            gameObject.SetActive(true);
            activePlayer.OnPlayerStart += OnPlayerStart;
            StartCoroutine(Run());
        }

        void OnPlayerStart(Player player)
        {
            activePlayer.OnPlayerStart -= OnPlayerStart;
            activeRoom.enabled = true;
            OnComplete?.Invoke();
        }

        IEnumerator Run()
        {
            Debug.Log("Hide Camera");
            var camera = Camera.main;
            int originalMask = camera ? camera.cullingMask : 0;
            if (camera) camera.cullingMask = 0;

            Debug.Log("Boot Strap Start");
            {
                var p = Profiler.Start();
                Debug.Log("Build Dungeon");
                roomLayout.Build();
                Profiler.StopAndLog(p, "Layout/Builder");
                yield return null;
            }

            Debug.Log("Build Nav Map");
            {
                var p = Profiler.Start();
                yield return navMap.RuntimeBake();
                Profiler.StopAndLog(p, "Surface/Map");
            }

            Debug.Log("Activation");
            yield return null;

            cullingGroupManager.enabled = true;
            activePlayer.enabled = true;

            Debug.Log($"Restore Camera {camera}");
            if (camera) camera.cullingMask = originalMask;
        }
    }
}