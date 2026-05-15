#pragma warning disable UDR0004
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Statistic;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace pigbrain.game.Boxhead.UI
{
    public class TutorialHelp : MonoBehaviourSingleton<TutorialHelp>
    {
        [SerializeField] RectTransform blackout;
        [SerializeField] BamboCard bamboCard;
        [SerializeField] WeaponCard weaponCard;
        [SerializeField] MoneyCard moneyCard;
        [SerializeField] ComboCard comboCard;
        Card[] cards => new Card[] { bamboCard, weaponCard, moneyCard, comboCard };

        void OnValidate() => cards.ForEach(c => c.owner = this);

        protected override void Awake()
        {
            base.Awake();
            cards.ForEach(c => c.Hide());
            ActiveRoom.OnDungeonStarted += OnDungeonStart;
            ActiveRoom.OnDungeonStopped += OnDungeonStopped;
            if (ActiveRoom.Instance) OnDungeonStart();
            else gameObject.SetActive(false);
        }

        void OnDungeonStart() => this.SetActive(true);
        void OnDungeonStopped() => this.SetActive(false);

        void OnEnable() => StartCoroutine(Run());
        void OnDisable() => cards.ForEach(c => c.Stop());

        readonly List<Card> queue = new();
        void QueueCard(Card card)
        {
            if (!isActiveAndEnabled) return;
            card.Stop();
            queue.Add(card);
        }

        IEnumerator Run()
        {
            yield return new WaitForSeconds(1);
            cards.Where(c => !Persistence.CurrentData.GetBool(c.key, false)).ForEach(c => c.Start());
            while (enabled)
            {
                yield return new WaitUntil(() => queue.Count > 0);
                yield return new WaitForSeconds(1);
                var card = queue[0];
                queue.RemoveAt(0);

                var closed = false;
                card.Show(() => closed = true);
                blackout.SetActive(true);
                yield return new WaitUntil(() => closed);
                blackout.SetActive(false);
            }
        }

        [Serializable]
        class Card
        {
            [SerializeField][ReadOnly] internal TutorialHelp owner;
            [SerializeField] internal TutorialCard card;
            public virtual void Start() { }
            public virtual void Stop() { }
            public virtual void Hide() => card.SetActive(false);
            public virtual void Show(Action onClose)
            {
                void Closed(TutorialCard card)
                {
                    card.OnClose -= Closed;
                    onClose?.Invoke();
                }
                card.OnClose += Closed;
                card.SetActive(true);
                Persistence.CurrentData.SetBool(key, true);
            }
            public string key => $"tutorial.{card}";
        }

        [Serializable]
        class BamboCard : Card
        {
            [SerializeField] AssetIdentity pickupId;
            public override void Start() => Pickup.OnPickup += OnPickup;
            public override void Stop() => Pickup.OnPickup -= OnPickup;
            void OnPickup(Pickup pickup, Transform target)
            {
                if (pickup.TryGetComponent(out AssetIdentity id) && id.Equals(pickupId))
                    owner.QueueCard(this);
            }
        }

        [Serializable]
        class WeaponCard : Card
        {
            public override void Start() => WeaponsContainer.OnStartEdit += OnEdit;
            public override void Stop() => WeaponsContainer.OnStartEdit -= OnEdit;
            void OnEdit() =>
                owner.QueueCard(this);
        }

        [Serializable]
        class ComboCard : Card
        {
            public override void Start() => StatsCatalog.Session.AddChangeListener(Stat.Track_EnemyKills, OnChange);
            public override void Stop() => StatsCatalog.Session.RemoveChangeListener(Stat.Track_EnemyKills, OnChange);
            void OnChange(Stats.ChangeEvent ev)
            {
                if (ev.newValue >= 5)
                    owner.QueueCard(this);
            }
        }

        [Serializable]
        class MoneyCard : Card
        {
            public override void Start() => StatsCatalog.Session.AddChangeListener(Stat.Money, OnChange);
            public override void Stop() => StatsCatalog.Session.RemoveChangeListener(Stat.Money, OnChange);
            void OnChange(Stats.ChangeEvent ev)
            {
                Debug.Log($"Money: {ev.newValue}");
                if (ev.delta > 0) owner.QueueCard(this);
            }
        }

    }
}