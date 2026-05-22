using System;
using System.Collections;
using System.Linq;
using System.IO;
using pigbrain.core.Analysis;
using pigbrain.core.Graphics;
using pigbrain.core.Inspector;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Navigation;
using pigbrain.game.Boxhead.Statistic;
using pigbrain.game.Boxhead.UI;
using Unity.AI.Navigation;
using UnityEngine;
using static System.Environment;

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

        [Header("Debug")]
        [ReadOnly] internal ProgressState currentState;

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

        // protected override void OnDestroy()
        // {
        //     base.OnDestroy();
        //     PoolingContainer.ForceDestroy();
        // }

        public void Activate()
        {
            PoolingContainer.ForceScene(gameObject.scene);
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

            if (Load(out currentState))
            {
                // prepare data
                Debug.Log("Data loaded");
            }

            cullingGroupManager.enabled = true;
            activePlayer.enabled = true;

            Debug.Log($"Restore Camera {camera}");
            if (camera) camera.cullingMask = originalMask;
        }

        #region Save
        public static bool HasValidSave(string name) =>
            Persistence.CurrentData.HasKey(name);

        Coroutine saveRoutine;
        AssetIdentity soldierIdentity, bamboIdentity;
        bool pauseSave;
        public static void StartSave()
        {
            Instance.pauseSave = false;
            StopSave();
            Instance.soldierIdentity = Catalog.Q<GameObject>("soldier").GetComponent<AssetIdentity>();
            Instance.bamboIdentity = Catalog.Q<GameObject>("bambo").GetComponent<AssetIdentity>();
            Instance.saveRoutine = Instance.StartCoroutine(Instance.PeriodicSaving());
        }

        public static void PauseSave(bool state)
        {
            Instance.pauseSave = state;
        }

        public static void StopSave()
        {
            if (Instance.saveRoutine == null) return;
            Instance.StopCoroutine(Instance.saveRoutine);
            Instance.saveRoutine = null;
        }

        IEnumerator PeriodicSaving()
        {
            while (true)
            {
                if (!Instance.pauseSave) Save();
                yield return new WaitForSeconds(1);
            }
        }

        static void Save()
        {
            var state = GetProgressState();
            var json = JsonUtility.ToJson(state);
            var name = DungeonSelector.GetDungeon().name;
            Persistence.CurrentData.SetString(name, json);
        }

        public static void ClearDungeonState(LevelData dungeon) =>
            Persistence.CurrentData.Clear(dungeon.name);

        internal static bool Load(out ProgressState state)
        {
            var name = DungeonSelector.GetDungeon().name;
            if (!HasValidSave(name))
            {
                state = null;
                return false;
            }
            var json = Persistence.CurrentData.GetString(name, "");
            state = GetProgressState(json);
            return true;
        }

        public static string SaveKey(params object[] keys) =>
            $"Progress.{DungeonSelector.GetDungeon().name}.{string.Join(".", keys)}";

        [Serializable]
        internal class ProgressState
        {
            public float money, exp;
            public string completedRoom;
            public PlayerState[] playerStates;
            public WeaponState[] weaponStates;
            public ObjectState[] objectStates;

            [Serializable]
            public class PlayerState
            {
                public string name;
                public Vector3 position;
                public Vector3 rotation;
                public float health;
                public bool human;
                // public string room; //??
            }

            [Serializable]
            public class WeaponState
            {
                public string name;
                public bool active;
                public int levelIndex;
            }

            [Serializable]
            public class ObjectState { public string name; }

            public static implicit operator bool(ProgressState empty) => empty != null;
        }

        static ProgressState GetProgressState(string json) =>
            JsonUtility.FromJson<ProgressState>(json);

        static ProgressState GetProgressState() => new()
        {
            completedRoom = ActiveRoom.Instance.GetCompletedRoom() is Room r ? r.name : "",
            money = StatsCatalog.Session.GetValue(Stat.Money),
            exp = StatsCatalog.Session.GetValue(Stat.Exp),
            playerStates = GetPlayerStates(),
            weaponStates = GetWeaponStates(),
            objectStates = GetObjectStates()
        };

        static ProgressState.WeaponState[] GetWeaponStates() =>
            ActivePlayer.Instance.player
                ? ActivePlayer.Instance.player.GetComponent<WeaponCache>().GetWeapons()
                    .Select(w => new ProgressState.WeaponState
                    {
                        name = w.name,
                        active = w.gameObject.activeSelf,
                        levelIndex = w.GetLevel()
                    })
                    .ToArray()
                : new ProgressState.WeaponState[0];

        static ProgressState.ObjectState[] GetObjectStates() => ActiveRoom.CellObjects
            .Where(c => c.tracking && !c.gameObject.activeSelf)
            .Select(c => new ProgressState.ObjectState
            {
                name = c.name,
            })
            .ToArray();

        static ProgressState.PlayerState[] GetPlayerStates()
        {
            var players = ActivePlayer.Instance.player.transform.parent.GetComponentsInChildren<Player>();
            return players.Where(p => p.GetComponent<AssetIdentity>().Equals(Instance.soldierIdentity)
                || p.GetComponent<AssetIdentity>().Equals(Instance.bamboIdentity))
                .Select(c => new ProgressState.PlayerState
                {
                    name = c.name,
                    health = c.GetComponent<Health>().damage,
                    human = !c.aiControl,
                    position = c.transform.position,
                    rotation = c.transform.eulerAngles,
                })
                .ToArray();
        }
        #endregion
    }
}
