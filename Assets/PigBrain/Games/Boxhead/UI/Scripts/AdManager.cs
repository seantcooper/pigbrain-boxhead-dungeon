using System;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.UI
{
    public class AdManager : MonoBehaviourSingleton<AdManager>
    {
        [SerializeField] Screens screens;
        [ToggleObject] public CompletePurchase completePurchase;

        void OnValidate()
        {
            if (!screens) screens = FindAnyObjectByType<Screens>();
        }

        [Serializable]
        public class CompletePurchase
        {
            [SerializeField] GameObject container;
            [SerializeField] float maxAmount = 20;
            [SerializeField][ReadOnly] Status status;
            [SerializeField][ReadOnly] float amount;

            public Status GetStatus() => status;

            public bool CanStart(float amount) => amount < maxAmount;
            public CompletePurchase Start(float amount)
            {
                this.amount = amount;
                status = Status.Active;
                container.SetActive(true);
                container.GetComponentInChildren<Button>().onClick.AddListener(RunAd);
                return this;
            }

            public void Stop()
            {
                container.SetActive(false);
                container.GetComponentInChildren<Button>().onClick.RemoveListener(RunAd);
                status = Status.None;
            }

            void RunAd()
            {
                Instance.screens.game.RunAd(() =>
                {
                    status = Status.Complete;
                    container.SetActive(false);
                });
            }
            public enum Status { None, Active, Complete, Failed }
        }
    }
}
