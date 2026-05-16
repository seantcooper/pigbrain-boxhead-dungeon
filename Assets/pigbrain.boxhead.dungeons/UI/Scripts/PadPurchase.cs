using System.Collections;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Statistic;
using pigbrain.game.Boxhead.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using static pigbrain.game.Boxhead.UI.AdManager.CompletePurchase;

namespace pigbrain.game.Boxhead
{
    // public class PadSwitch : PadBase
    public class PadPurchase : PadBase
    {
        [SerializeField] StatsLinkSingle stat;
        [SerializeField] CommandContainer commands;
        [SerializeField] Type type;
        [SerializeField] GameObject upgrade;

        public static int PurchaseCounter;

        enum Type
        {
            Undefined,
            Weapon,
        }

        [HeaderLine("UI")]
        [SerializeField] int price;
        [SerializeField] TextMeshPro tmpPrice;

        [HeaderLine("Debug")]
        [SerializeField][ReadOnly] float value;
        [SerializeField][ReadOnly] float money;

        [Preserve]
        void TriggerAd(bool state = true)
        {
            Debug.Log($"Trigger Ad {state}");
        }

        [Preserve]
        void SetPrice(int price)
        {
            this.price = price;
            UpdateUI();
        }

        void OnEnable()
        {
            StartCoroutine(MonitorPrice());
            UpdateWeapon();
        }

        void OnDisable() => StopAllCoroutines();

        IEnumerator MonitorPrice()
        {
            if (upgrade) upgrade.SetActive(false);
            while (isActiveAndEnabled)
            {
                UpdateWeapon();
                yield return new WaitForSeconds(0.5f);
            }
        }

        void UpdateWeapon()
        {
            if (type != Type.Weapon) return;
            if (!ActivePlayer.HasPlayer) return;

            var statsController = GetComponent<StatsController>();
            if (!statsController) return;

            var purchasingController = ActivePlayer.Instance.player
                .GetComponentsInChildren<StatsController>(true)
                .FirstOrDefault(s => s.id == statsController.id);

            if (purchasingController)
            {
                int newLevel = purchasingController.GetLevelIndex() + 1;
                if (upgrade) upgrade.SetActive(true);

                if (newLevel < statsController.GetLevels().count)
                {
                    if (statsController.GetLevelIndex() != newLevel)
                        statsController.SetLevelIndex(newLevel);
                }
                else gameObject.SetActive(false);
            }
        }

        protected override void UpdateUI() { base.UpdateUI(); UpdatePrice(); }
        void UpdatePrice() => UpdatePrice(price);
        void UpdatePrice(float price) { if (tmpPrice) tmpPrice.text = $"{price}"; }

        protected override void StartFill(Collider other)
        {
            base.StartFill(other);
            // money = stat.GetValue();
            WeaponsContainer.SetCard(commands);
            StartCoroutine(Run(other));
        }

        protected override void EndFill(Collider other)
        {
            base.EndFill(other);
            if (traits.HasFlag(Trait.ResetOnLeave))
            {
                // stat.SetValue(money);
                stat.SetValue(stat.GetValue() + moneyRemoved);
                UpdatePrice();
            }
            StopAllCoroutines();
            AdManager.Instance.completePurchase.Stop();
        }

        protected override void CompleteFill(Collider other)
        {
            base.CompleteFill(other);
            PurchaseCounter++;
            commands?.Invoke(other.transform);
        }

        int moneyRemoved;
        IEnumerator Run(Collider other)
        {
            // var motion = other.GetComponent<TransformMotion>();
            // yield return new WaitUntil(() => motion && motion.isStationary);
            yield return new WaitForSeconds(0.25f);

            moneyRemoved = 0;
            for (value = 0; value < price;)
            {
                var money = stat.GetValue() + moneyRemoved;
                value = Mathf.Min(money, price, value + Time.deltaTime / fillDuration * price);
                SetFill(value / price);

                int v = Mathf.FloorToInt(value);
                moneyRemoved = v;
                stat.SetValue((int)(money - v));

                int remaining = price - v;
                UpdatePrice(remaining);

                yield return new WaitForNextUpdate();

                if (stat.GetValue() == 0 && remaining > 0)
                {
                    var cp = AdManager.Instance.completePurchase;
                    if (cp.CanStart(remaining))
                    {
                        cp.Start(remaining);
                        yield return new WaitUntil(() => cp.GetStatus() != Status.Active);

                        Debug.Log($"PadPurchase: {cp.GetStatus()}");
                        if (cp.GetStatus() == Status.Complete)
                        {
                            yield return CoroutineUtility.WaitSecondsHalf;
                            break;
                        }
                    }
                }
            }
            CompleteFill(other);
        }
    }
}
