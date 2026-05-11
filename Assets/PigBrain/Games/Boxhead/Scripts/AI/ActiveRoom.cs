using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Graphics;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.core.Audio;
using pigbrain.game.Boxhead.Environment;
using pigbrain.game.Boxhead.Navigation;
using pigbrain.game.Boxhead.Statistic;
using pigbrain.game.Boxhead.UI;
using UnityEngine;
using UnityEngine.AI;
using static pigbrain.core.Collections.CoroutineUtility;
using static pigbrain.game.Boxhead.Environment.CellObject;
using static pigbrain.game.Boxhead.Environment.RoomData;

namespace pigbrain.game.Boxhead
{
    public class ActiveRoom : MonoBehaviourSingleton<ActiveRoom>
    {
        const float FadeSpeed = 0.02f;

        [SerializeField] ClipLink roomStart;
        [SerializeField] ClipLink roomComplete;

        [Header("Messages")]
        [SerializeField][InlineScriptableObject] MessageTickerData roomCompleted;
        [SerializeField][InlineScriptableObject] MessageTickerData dungeonCompleted;

        [Header("Runtime")]
        [SerializeField][ReadOnly] string state;
        [SerializeField][ReadOnly] Room currentRoom;
        [SerializeField][ReadOnly] Room levelRoom;
        [SerializeField][ReadOnly] Room lootRoom;

        public event Action<Room> OnRoomStarted;
        public event Action<Room> OnRoomCompleted;
        public event Action<Room> OnLevelStarted;

        public Room GetCurrentRoom() => currentRoom;
        public Room GetLevelRoom() => levelRoom;
        public Room GetLootRoom() => lootRoom;
        public Player player => ActivePlayer.Instance.player;

        RoomBuilder builder;
        bool forcedComplete;

        #region Start
        void Start()
        {
            builder = GetComponent<RoomBuilder>();
            RunStartRoom();
            StatsCatalog.Session.AddChangeListener(Stat.Track_EnemyActive, OnEnemyActiveChange);
        }

        void OnEnemyActiveChange(Stats.ChangeEvent ev) => AudioManager.SetIntensity(ev.newValue);

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StatsCatalog.Session.RemoveChangeListener(Stat.Track_EnemyActive, OnEnemyActiveChange);
        }

        #endregion

        #region Start Room
        Coroutine running;
        void RunStartRoom() => RunRoom(builder.startRoom.GetComponent<Room>());
        void RunRoom(Room room)
        {
            StopRunRoom();
            levelRoom = room;
            levelRoom.Activate();
            Debug.Log(state = $"Room: {levelRoom}");
            running = StartCoroutine(RunRoom());
        }

        void StopRunRoom()
        {
            if (running == null) return;
            StopCoroutine(running);
            running = null;
        }

        IEnumerator RunRoom()
        {
            while (true)
            {
                currentRoom = levelRoom;

                // Wait for the players to enter
                state = $"Wait for players: {levelRoom}";
                yield return WaitForPlayers(levelRoom);
                OnRoomStarted?.Invoke(levelRoom);

                roomStart.Play();

                // Run Spawners and Wait for them to finish
                Debug.Log(state = $"Run Spawner: {levelRoom}");
                yield return RunSpawner(levelRoom);

                // Complete the Room
                Debug.Log(state = $"Is player dead: {ActivePlayer.Instance.playerIsDead} {levelRoom}");
                if (ActivePlayer.Instance.playerIsDead)
                    yield return new WaitUntil(() => !ActivePlayer.Instance.playerIsDead);
                else yield return new WaitForSeconds(0.25f);

                Debug.Log(state = $"Room Complete: {levelRoom}");
                LockDoors(levelRoom, false);
                Analytics.Post(new Analytics.Level(Analytics.Level.Status.Complete));

                roomComplete.Play();
                roomCompleted.Invoke(("name", levelRoom.data.levelData.title));

                levelRoom.Complete(forcedComplete);
                OnRoomCompleted?.Invoke(levelRoom);
                Debug.Log($"Room Completed: {levelRoom}");

                if (levelRoom.type == Room.Type.Final)
                {
                    state = $"Game Complete: {levelRoom}";
                    yield return new WaitForSeconds(2);
                    dungeonCompleted.Invoke();
                    yield break;
                }

                StartCoroutine(NavMap.Instance.RuntimeBakeResync());

                foreach (var corridor in levelRoom.nextRooms)
                {
                    corridor.Activate();
                    if (!forcedComplete) StartCoroutine(FadeIn(corridor));
                    foreach (var next in corridor.nextRooms)
                    {
                        if (next.data.roomType == Room.Type.Loot) lootRoom = next; else levelRoom = next;
                        next.Activate();
                    }
                }
                Debug.Log(state = $"Next Room: {levelRoom}");
            }
        }

        #region └Wait Players
        IEnumerator WaitForPlayers(Room room)
        {
            Debug.Log($"Wait for players {room}");

            const float MaxTimeout = 5;
            float timeout = Time.time;

            while (true)
            {
                yield return new WaitUntil(() => ActivePlayer.Instance.player && ActivePlayer.Instance.player.transform.parent);
                if (player.GetRoom() != room) timeout = Time.time;
                else if (Time.time >= timeout + MaxTimeout) break;
                if (PlayersInRoom(room)) break;
                yield return WaitSecondsTenth;
            }

            static bool PlayersInRoom(Room room)
            {
                foreach (Transform child in Instance.player.transform.parent)
                    if (child.gameObject.GetRoom() != room) return false;
                return true;
            }
        }
        #endregion

        #region └Run Spawner
        IEnumerator RunSpawner(Room room)
        {
            forcedComplete = false;
            bool roomComplete = false;
            if (!room.StartSpawner((r) => roomComplete = true))
                yield break;

            OnLevelStarted?.Invoke(room);

            if (room.data.levelData.lockDoor) LockDoors(room, true);

            Analytics.CTX.levelid = room.GetLevelID();
            Analytics.Post(new Analytics.Level(Analytics.Level.Status.Started));

            yield return new WaitUntil(() => roomComplete || forcedComplete);
        }
        #endregion

        #region └Lock Doors
        void LockDoors(Room room, bool locked)
        {
            void Lock() => room.data.Find(Cell.Type.Enter, GeomType.Door)
                .Select(c => c.transform.Find("Lock"))
                .Where(l => l)
                .ForEach(l => l.gameObject.SetActive(locked));
            Lock();
        }
        #endregion
        #endregion

        #region Fade In
        IEnumerator FadeIn(Room start)
        {
            Debug.Log("FadeIn");
            Vector3 p = start.data.Find(Cell.Type.Enter, GeomType.Floor).FirstOrDefault().transform.position;

            float time = Time.time;
            var objects = start.nextRooms.Prepend(start)
                .SelectMany(r => r.zone.visibleObjects
                    .Select(o => (o, d: (o.transform.position - p).magnitude * FadeSpeed)))
                .OrderBy(t => t.d).ToArray();

            objects.ForEach(t => t.o.SetActive(false));

            //ordered reveal
            foreach (var (trans, dist) in objects)
            {
                while (Time.time - time < dist)
                    yield return null;
                trans.SetActive(true);
            }
        }
        #endregion

        #region CHEAT: Set Level
        public bool SetLevel(int level)
        {
            IEnumerator SetRoom()
            {
                var rooms = GetComponentsInChildren<Room>(true);

                StopRunRoom();
                yield return null;

                // Reset all rooms
                rooms.Where(r => r.level > level).ForEach(r => r.ResetRoom());
                yield return null;

                // Activate & Complete all rooms < level
                rooms.Where(r => r.level < level).ForEach(r =>
                {
                    r.Activate();
                    r.Complete(true);
                });

                var targetRoom = rooms.FirstOrDefault(r => r.data.roomType.IsLevelRoom() && r.level == level);
                if (!targetRoom) yield break;

                var spawn = targetRoom.data.parent ? targetRoom.data.parent : targetRoom.data;
                spawn.GetComponent<Room>().zone.SetVisibility(true);
                player.GetComponent<NavMeshAgent>().Warp(player.transform.position = spawn.GetRandomPosition());
                RunRoom(targetRoom);
            }

            StartCoroutine(SetRoom());
            return true;
        }

        public bool SetLevelComplete()
        {
            forcedComplete = true;
            return true;
        }
        #endregion
    }
}
