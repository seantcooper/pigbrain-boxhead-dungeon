using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using pigbrain.core.Utility;
using pigbrain.game.Boxhead.Environment;
using TMPro;
using UnityEngine;

namespace pigbrain.game.Boxhead.UI
{
    public class TutorialChecklist : MonoBehaviourSingleton<TutorialChecklist>
    {
        const string Key = "CheckLists Completed";

        [Header("Prefabs")]
        [SerializeField] TMP_Text header;
        [SerializeField] TMP_Text message;

        Color GetColor(string html) => ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.magenta;
        Color head => GetColor("#EEEEEE");
        Color kill => GetColor("#FF004B");
        Color loot => GetColor("#FFE600");
        Color level => GetColor("#64B440");
        Color pickup => GetColor("#00C6FF");
        Color buy => GetColor("#AB8700");

        void OnEnable() => StartCoroutine(Run());
        void OnDisable() => StopAllCoroutines();

        IEnumerator Run()
        {
            if (Persistence.CurrentData.GetBool(Key, false))
            {
                gameObject.SetActive(false);
                yield break;
            }
            ActiveRoom activeRoom = null;
            Room currentLevelRoom = null;
            Room currentLootRoom = null;
            Room currentCorridorRoom = null;
            List<Message> messages = new();

            yield return new Sequencer(this)
                .WaitUntil(() => activeRoom = ActiveRoom.Instance)
                .WaitUntil(() => currentLevelRoom = ActiveRoom.Instance.GetLevelRoom())
                .WaitUntil(() => currentLevelRoom.level == 1 && currentLevelRoom.currentState == Room.State.Active)
                .WaitForSeconds(1)
                .Call(() => messages.Add(new(header, "CHECK LIST:", head, transform)))
                .WaitForSeconds(0.25f)
                .Call(() => messages.Add(new(message, "KILL ALL ZOMBIES", kill, transform)))
                .WaitUntil(() => currentLevelRoom.currentState == Room.State.Complete)
                .Call(() => LookDoor(currentLevelRoom))
                .Call(() => messages[^1].Complete())
                .WaitForSeconds(0.25f)
                .WaitUntil(() => (currentLootRoom = ActiveRoom.Instance.GetLootRoom())
                    && (currentCorridorRoom = ActiveRoom.Instance.GetCorridorRoom()))

                .Call(() => messages.Add(new(message, "GOTO LOOT ROOM", loot, transform)))
                .WaitUntil(() => ActivePlayer.Room == currentLootRoom)
                .Call(() => messages[^1].Complete())

                .Call(() => messages.Add(new(message, "BUY SOMETHING", buy, transform)))
                .WaitCounter(() => PadPurchase.PurchaseCounter)
                .Call(() => messages[^1].Complete())

                .Call(() => currentLevelRoom.LockDoors(false, RoomData.Cell.Type.Exit))

                .WaitForSeconds(0.25f)
                .Call(() => messages.Add(new(message, "PROCEED TO WARM UP", level, transform)))
                .WaitUntil(() => ActivePlayer.Room == currentCorridorRoom)
                .Call(() => messages[^1].Complete())
                .WaitForSeconds(1)
                .Call(() =>
                {
                    messages.ForEach(m => m.Remove());
                    messages.Clear();
                })
                .Call(() => currentLevelRoom = activeRoom.GetLevelRoom())
                .WaitUntil(() => ActivePlayer.Room == currentLevelRoom)
                .Call(() => messages.Add(new(header, "CHECK LIST:", head, transform)))
                .WaitForSeconds(0.25f)
                .Branch(sequence =>
                {
                    Message inst = null;
                    sequence
                        .Call(() => messages.Add(inst = new Message(message, "KILL ALL ZOMBIES + RUNNERS", kill, transform)))
                        .WaitUntil(() => currentLevelRoom.currentState == Room.State.Complete)
                        .Call(() => inst.Complete());
                })
                .Branch(sequence =>
                {
                    Message inst = null;
                    sequence
                        // .WaitCounter(() => ChainKills.RewardCreatedCount)
                        .Call(() => messages.Add(inst = new Message(message, "PICKUP BOXES + WHITE SOULS", pickup, transform)))
                        .WaitCounter(() => Pickup.PickupCount)
                        .Call(() => inst.Complete());
                })
                .WaitUntil(() => messages.Count > 0 && messages.Skip(1).All(m => m.completed))
                .WaitForSeconds(1)
                .Call(() =>
                {
                    messages.ForEach(m => m.Remove());
                    messages.Clear();
                })
                .Call(() => Persistence.CurrentData.SetBool(Key, true))
                .Run();
        }

        void LookDoor(Room levelRoom)
        {
            var doors1 = levelRoom.data.Find(RoomData.Cell.Type.Exit, CellObject.GeomType.Door);
            foreach (var corridor in levelRoom.nextRooms) foreach (var next in corridor.nextRooms)
                if (next.data.roomType != Room.Type.Loot)
                {
                    var doors2 = corridor.data.Find(RoomData.Cell.Type.Enter, CellObject.GeomType.Door);
                    var pair = doors1
                        .SelectMany(d1 => doors2.Select(d2 => (d1, d2)))
                        .FirstOrDefault(p => (p.d1.transform.position - p.d2.transform.position).sqrMagnitude < 1);
                    levelRoom.LockDoors(true, pair.d1);
                    break;
                }
        }

        class Message
        {
            readonly TMP_Text text;
            readonly CanvasGroup group;
            readonly RectTransform rt;
            public bool completed;

            public Message(TMP_Text prefab, string text, Color color, Transform parent)
            {
                this.text = prefab.Instantiate(parent);
                this.text.TryAddComponent(out group);
                this.text.text = text;
                this.text.color = color;
                rt = (RectTransform)this.text.transform;
                this.text.StartCoroutine(Transition());
            }

            IEnumerator Transition(float start = -500, float end = 0, bool invert = false)
            {
                yield return new OverTime(0.25f, (t) =>
                {
                    float f = invert ? 1 - t : t;
                    group.alpha = f;
                    rt.anchoredPosition = rt.anchoredPosition.WithX(Mathf.Lerp(-500f, 0, f));
                });
            }

            public void Remove() => text.StartCoroutine(Transition(invert: true));

            public void Complete()
            {
                text.fontStyle |= FontStyles.Strikethrough;
                completed = true;
            }
        }
    }
}
