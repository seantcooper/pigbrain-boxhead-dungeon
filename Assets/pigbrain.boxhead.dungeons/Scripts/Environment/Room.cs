using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Analytics;
using pigbrain.core.Collections;
using pigbrain.core.Geom;
using pigbrain.core.Graphics;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.UI;
using UnityEngine;
using static pigbrain.game.Boxhead.Environment.CellObject;
using static pigbrain.game.Boxhead.Environment.RoomData;

namespace pigbrain.game.Boxhead.Environment
{
    public class Room : MonoBehaviour, CullingGroupZone.IBounds
    {
        [SerializeField] internal RoomData data;
        [SerializeField] internal State state;

        [SerializeField][ReadOnly] public Spawner spawner;
        [SerializeField][ReadOnly] public CullingGroupZone zone;

        public event Action<IRoomTracker> OnTrackerEnter;
        public event Action<IRoomTracker> OnTrackerLeave;
        public event Action<Room> OnLootRoomEmpty;

        HashSet<IRoomTracker> roomTrackers = new();
        public bool hasTracker => roomTrackers.Count > 0;

        internal int level => data.level;
        internal IEnumerable<Room> nextRooms => data.children.Select(c => c.GetComponent<Room>());
        internal Type type => data.roomType;
        internal State currentState => state;
        RoomItem[] roomItemRemoval => GetComponentsInChildren<RoomItem>(true);

        #region └Lock Doors
        public void LockDoors(bool locked, Cell.Type type = Cell.Type.Enter) =>
            LockDoors(locked, data.Find(type, GeomType.Door).ToArray());

        public void LockDoors(bool locked, params CellObject[] doors) =>
            doors.Where(c => c.transform.Find("Lock"))
                .ForEach(c =>
                {
                    c.transform.Find("Lock").gameObject.SetActive(locked);
                    c.GetLabel().gameObject.SetActive(!locked);
                });
        #endregion

        public string GetLevelID()
        {
            LevelData dungeon = DungeonSelector.GetDungeon();
            return $"{dungeon.title}/{data.levelData.title}/{data.level}";
        }

        void Start()
        {
            zone.OnItemAdded += OnItemAdded;
            zone.OnItemRemoved += OnItemRemoved;
            Deactivate();
        }

        public void Activate()
        {
            zone.Show();
            data.Find(0, GeomType.Label)
                .ForEach(c => c.gameObject.SetActive(false));

            roomItemRemoval.ForEach(ri => ri.gameObject.SetActive(true));
            state = State.Active;
        }

        public bool StartSpawner(Action<Room> onComplete)
        {
            if (state == State.Complete || !spawner) return false;
            void CompleteSpawner()
            {
                spawner.onComplete -= CompleteSpawner;
                onComplete(this);
            }
            spawner.onComplete += CompleteSpawner;
            spawner.Activate();
            return true;
        }

        public void Complete(bool killSpawner = false)
        {
            if (spawner && killSpawner) spawner.gameObject.SetActive(false);
            data.Find(0, GeomType.Label).ForEach(c => c.gameObject.SetActive(true));
            roomItemRemoval.ForEach(ri => ri.gameObject.SetActive(false));
            state = State.Complete;
        }

        public void Deactivate()
        {
            zone.Hide();
            if (spawner) spawner.gameObject.SetActive(false);
        }

        public void ResetRoom()
        {
            zone.Hide();
            if (spawner) spawner.gameObject.SetActive(false);
        }

        void OnItemAdded(CullingGroupItem item)
        {
            if (item.TryGetComponent(out IRoomTracker tracker) && tracker.isTracker)
            {
                roomTrackers.Add(tracker);
                OnTrackerEnter?.Invoke(tracker);
            }

            if (zone && item.TryGetComponent(out OrthoCamera ortho))
                ortho.AddBounds(zone.GetFullBounds());
        }

        void OnItemRemoved(CullingGroupItem item)
        {
            if (item.TryGetComponent(out IRoomTracker tracker) && tracker.isTracker)
            {
                roomTrackers.Remove(tracker);
                OnTrackerLeave?.Invoke(tracker);

                Debug.Log($"Tracker enter room {name} {data.roomType}");
                if (data.roomType == Type.Loot)
                {
                    var objects = data.GetComponentsInChildren<CellObject>().Where(c => c.geomType == GeomType.Loot);
                    Debug.Log($"Loot: {objects.Count()} {string.Join(",", objects.Select(o => o.name))}");
                    if (objects.Count() <= 0)
                    {
                        Debug.Log("LOOT ROOM EMPTY: EVENT");
                        OnLootRoomEmpty?.Invoke(this);
                    }
                }
            }

            if (zone && item.TryGetComponent(out OrthoCamera ortho))
                ortho.RemoveBounds(zone.GetFullBounds());
        }

        #region Bounds
        bool CullingGroupZone.IBounds.GetBounds(Bounds fullBounds, out Bounds[] bounds)
        {
            var ceiling = transform.Find(CeilingContainerName);

            bounds = null;
            if (!ceiling) return false;

            Bounds GetBounds(Bounds bounds)
            {
                bounds.min = bounds.min.WithY(fullBounds.min.y);
                bounds.max = bounds.max.WithY(fullBounds.max.y);
                return bounds;
            }

            bounds = ceiling.GetComponentsInChildren<Collider>(true)
                .Select(c => GetBounds(c.bounds)).ToArray();

            return true;
        }
        #endregion

        public enum Type { None, Start, Room, Final, Loot, Connector }
        public enum State { Inactive, Active, Complete }
    }

    static class RoomX
    {
        public static bool IsLevelRoom(this Room.Type roomType) =>
            !(roomType == Room.Type.Loot || roomType == Room.Type.Connector);

        public static Room GetRoom(this Component component) =>
            component ? component.gameObject.GetRoom() : null;
        public static Room GetRoom(this GameObject gameObject) =>
            gameObject.TryGetComponent(out CullingGroupItem item)
                && item.zone ? item.zone.GetComponent<Room>() : null;

    }

    public interface IRoomTracker
    {
        bool isTracker { get; }
        Room currentRoom { get; }
    }
}