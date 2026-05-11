using System.Collections;
using System.Collections.Generic;
using pigbrain.core.Collections;
using UnityEngine;

namespace pigbrain.game.Boxhead.Environment
{
    public class VolumeBlending : MonoBehaviour
    {
        void Start() => ActivePlayer.Instance.OnPlayerStart += OnPlayerStart;

        void OnPlayerStart(Player player)
        {
            ActivePlayer.Instance.OnEnterRoom += OnEnterRoom;
            ActivePlayer.Instance.OnLeaveRoom += OnLeaveRoom;
        }

        readonly Dictionary<Room, Coroutine> tracking = new();
        void OnEnterRoom(Room room) => StartTransition(room, 1);
        void OnLeaveRoom(Room room) => StartTransition(room, 0);
        void StartTransition(Room room, float end)
        {
            if (tracking.TryGetValue(room, out Coroutine coroutine))
                StopCoroutine(coroutine);
            tracking[room] = StartCoroutine(Transition(room, end));
        }

        IEnumerator Transition(Room room, float end, float duration = 0.5f)
        {
            var volume = room.GetComponent<RoomData>().volume;
            float start = volume.weight, range = Mathf.Abs(end - start);
            yield return new OverTime(duration * range,
                (t) => volume.weight = Mathf.Lerp(start, end, t));
            if (tracking.ContainsKey(room)) tracking.Remove(room);
        }
    }
}