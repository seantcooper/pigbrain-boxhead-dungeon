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
        [ReadOnly] internal ProgressState1_0_0 currentState;

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
        public static bool HasValidSave(string name)
        {
            var version = GetVersion(name);
            return version != "0.0.0";
        }

        static string GetVersion(string name) => Persistence.CurrentData.GetString($"{name}.version", "0.0.0");

        public static void Save()
        {
            var state = GetProgressState();
            var json = JsonUtility.ToJson(state);
            var name = DungeonSelector.GetDungeon().name;
#if UNITY_EDITOR
            File.WriteAllText($"{GetFolderPath(SpecialFolder.Desktop)}/{name}.json", json);
#endif
            Persistence.CurrentData.SetString(name, json);
            Persistence.CurrentData.SetString($"{name}.version", "1.0.0");
        }
        internal static bool Load(out ProgressState1_0_0 state)
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
        internal class ProgressState1_0_0
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
            public class ObjectState
            {
                public string name;
                public bool active;
            }
            public static implicit operator bool(ProgressState1_0_0 empty) => empty != null;
        }

        static ProgressState1_0_0 GetProgressState(string json) =>
            JsonUtility.FromJson<ProgressState1_0_0>(json);

        static ProgressState1_0_0 GetProgressState() => new()
        {
            completedRoom = ActiveRoom.CompletedRoom ? ActiveRoom.CompletedRoom.name : "",
            money = StatsCatalog.Session.GetValue(Stat.Money),
            exp = StatsCatalog.Session.GetValue(Stat.Exp),

            playerStates = GetPlayerStates(),

            weaponStates = (ActivePlayer.Instance.player
                ? ActivePlayer.Instance.player.GetComponent<WeaponCache>().GetWeapons()
                : new Weapon[0])
                .Select(w => new ProgressState1_0_0.WeaponState
                {
                    name = w.name,
                    active = w.gameObject.activeSelf,
                    levelIndex = w.GetLevel()
                })
                .ToArray(),

            objectStates = Instance.GetComponentsInChildren<CellObject>(true)
                .Where(c => c.tracking)
                .Select(c => new ProgressState1_0_0.ObjectState
                {
                    name = c.name,
                    active = c.gameObject.activeSelf,
                })
                .ToArray()
        };

        static ProgressState1_0_0.PlayerState[] GetPlayerStates()
        {
            var soldier = Catalog.Q<GameObject>("soldier");
            var bambo = Catalog.Q<GameObject>("bambo");

            var players = FindObjectsByType<Player>(FindObjectsInactive.Include);
            return players.Where(p => p.GetComponent<AssetIdentity>().Equals(soldier)
                || p.GetComponent<AssetIdentity>().Equals(bambo))
                .Select(c => new ProgressState1_0_0.PlayerState
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