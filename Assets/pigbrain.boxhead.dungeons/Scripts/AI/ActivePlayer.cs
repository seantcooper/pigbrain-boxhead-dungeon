using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Graphics;
using pigbrain.core.Inspector;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Environment;
using pigbrain.game.Boxhead.Statistic;
using pigbrain.game.Boxhead.UI;
using UnityEngine;
using static pigbrain.core.Collections.CoroutineUtility;

namespace pigbrain.game.Boxhead
{
    public class ActivePlayer : MonoBehaviourSingleton<ActivePlayer>
    {
        [SerializeField] Player playerPrefab;

        [SerializeField][ReadOnly] internal bool playerIsDead;
        [ReadOnly] public Player player;
        [ReadOnly] public Room room;
        [SerializeField][ReadOnly] string currentRoom;
        [SerializeField][ReadOnly] float currentEXP;
        [SerializeField][ReadOnly] float currentMoney;

        public event Action<Player> OnPlayerStart;
        public event Action<Player> OnPlayerRespawning;
        public event Action<Player> OnPlayerRespawn;
        public event Action<Player> OnPlayerDead;
        public event Action<Room> OnLeaveRoom;
        public event Action<Room> OnEnterRoom;

        public static Room Room => Instance ? Instance.room : null;

        static Vector3 LastPosition;
        public static Vector3 Position => Instance.player ? LastPosition =
            Instance.player.transform.position : LastPosition;

        public static bool HasPlayer => Instance && Instance.player;
        public static bool IsRespawning { get; private set; }

        public static Camera GetCamera() => Instance && Instance.player ? Instance.player.GetComponentInChildren<Camera>() : null;

        void Start()
        {
            CreatePlayer();
            OnPlayerStart?.Invoke(player);
            Pickup.OnCreated += OnPickupCreated;
        }

        void OnDisable()
        {
            Pickup.OnCreated -= OnPickupCreated;
        }

        Player InstantiatePlayer(Vector3 position)
        {
            MessageTicker.MuteMessages = true;
            StartCoroutine(Delay(1f, () => MessageTicker.MuteMessages = false));

            player = playerPrefab.Instantiate(position, Quaternion.identity);
            player.GetComponent<Health>().onDeath += OnPlayerDeath;
            player.GetComponent<CullingGroupItem>().OnAddedToZone += OnEnterZone;
            player.GetComponent<CullingGroupItem>().OnRemovedFromZone += OnLeaveZone;
            Debug.Log($"Player Instantiated {player}");
            return player;
        }

        public readonly HashSet<Pickup> pickups = new();
        void OnPickupCreated(Pickup pickup) => pickups.Add(pickup);

        void OnEnterZone(CullingGroupZone zone)
        {
            room = zone.GetComponent<Room>();
            currentRoom = room.name;
            OnEnterRoom?.Invoke(room);
        }
        void OnLeaveZone(CullingGroupZone zone) =>
            OnLeaveRoom?.Invoke(zone.GetComponent<Room>());

        void CreatePlayer()
        {
            Debug.Log(">>>> Creating player");
            var builder = GetComponent<RoomBuilder>();

            if (BootStrap.Instance.currentState && !BootStrap.Instance.currentState.playerStates.IsNullOrEmpty())
            {
                foreach (var p in BootStrap.Instance.currentState.playerStates)
                {
                    Player inst = p.human ? InstantiatePlayer(default)
                            : Catalog.Q<GameObject>(p.name).Instantiate().GetComponent<Player>();
                    inst.GetComponent<Health>().damage = p.health;
                    inst.transform.position = p.position;
                    inst.agent.Warp(p.position);
                    inst.transform.eulerAngles = p.rotation;
                }
            }
            else
            {
                InstantiatePlayer(default);
                StartCoroutine(Delay(0.5f, () =>
                {
                    var soldier = Catalog.Q<GameObject>("soldier");
                    for (int i = 0; i < builder.levelData.soldiers; i++)
                        soldier.Instantiate();
                }));
            }

            // WEAPONS set the initial weapons
            if (BootStrap.Instance.currentState && !BootStrap.Instance.currentState.weaponStates.IsNullOrEmpty())
                BootStrap.Instance.currentState.weaponStates
                    .ForEach(w =>
                    {
                        var inst = Catalog.Query.Get<GameObject>(w.name).Instantiate(player.transform);
                        inst.SetActive(w.active);
                        inst.GetComponent<StatsController>().SetLevelIndex(w.levelIndex);
                    });

            else builder.levelData.startWeapons
                .ForEach(w => Catalog.Query.Get<GameObject>(w).Instantiate(player.transform));

            // MONEY / EXP set the initial money

            float money = BootStrap.Instance.currentState ? BootStrap.Instance.currentState.money : builder.levelData.money;
            float exp = BootStrap.Instance.currentState ? BootStrap.Instance.currentState.exp : builder.levelData.exp;
            StatsCatalog.Session.SetValue(Stat.Exp, exp);
            StatsCatalog.Session.SetValue(Stat.Money, money);
        }

        #region └Death
        readonly Dictionary<bool, List<Weapon>> deadWeapons = new() { { true, new() }, { false, new() } };
        int deadLevelIndex;
        void OnPlayerDeath()
        {
            playerIsDead = true; // enabled = false;
            deadLevelIndex = ((ILevelIndex)player).GetIndex();
            deadWeapons[true].Clear(); deadWeapons[false].Clear();
            foreach (var w in player.GetComponent<WeaponCache>().GetWeapons())
            {
                deadWeapons[w.isActiveAndEnabled].Add(w);
                w.gameObject.SetActive(false);
                w.transform.SetParent(null);
            }
            Debug.Log($"OnPlayerDeath weapons: {deadWeapons[true].Count + deadWeapons[false].Count}");

            OnPlayerDead?.Invoke(player);
            Analytics.Post(new Analytics.Player(Analytics.Player.Status.Died));
        }
        #endregion

        #region └Respawn
        public void RespawnPlayer(Action onComplete)
        {
            IEnumerator Run()
            {
                using var _ = new ScopeState((s) => IsRespawning = s);
                OnPlayerRespawning?.Invoke(player);
                yield return null;
                if (player) Destroy(player.gameObject);
                yield return null;
                InstantiatePlayer(room.data.GetSpawnPosition());
                Analytics.Post(new Analytics.Player(Analytics.Player.Status.Respawned));
                ((ILevelIndex)player).SetIndex(deadLevelIndex);
                // Restore Weapons
                Debug.Log($"RespawnPlayer weapons: {deadWeapons[true].Count + deadWeapons[false].Count}");
                foreach (var w in deadWeapons[true])
                {
                    if (!w)
                    {
                        Debug.LogError("Should not be destroyed?");
                        continue;
                    }
                    w.gameObject.SetActive(true);
                    w.transform.SetParent(player.transform);
                    w.transform.ResetLocal();
                }
                foreach (var w in deadWeapons[false])
                    w.transform.SetParent(player.transform);
                deadWeapons[true].Clear(); deadWeapons[false].Clear();
                yield return null;
                playerIsDead = false;

                // Events and Enabling
                OnPlayerRespawn?.Invoke(player);
                onComplete?.Invoke();
            }
            StartCoroutine(Run());

            pickups.RemoveWhere(p => !p);
            pickups.ForEach(p => Destroy(p.gameObject));
            pickups.Clear();
        }
        #endregion
    }
}
