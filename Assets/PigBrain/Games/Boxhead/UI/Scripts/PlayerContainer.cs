using System;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Statistic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static pigbrain.game.Boxhead.Statistic.Stat;
using static pigbrain.game.Boxhead.Statistic.Stats;
using static pigbrain.game.Boxhead.UI.Console;

namespace pigbrain.game.Boxhead.UI
{
    public class PlayerContainer : MonoBehaviourSingleton<PlayerContainer>
    {
        [SerializeField] internal TMP_Text levelIndex;
        [SerializeField] internal Color[] levelColor;
        [SerializeField] internal Image levelBar;
        [SerializeField] internal RectTransform dead;
        [SerializeField] internal MessageTickerData upgraded;
        [SerializeField][ReadOnly] internal Player player;
        [SerializeField][InlineScriptableObject] StatsCollection playerLevels;

        ControlValue enemykills, exp;

        [ConsoleCommand("EXP")]
        public static (UI.Console.Command.Status, string) ActivateRage(UI.Console console, string[] args)
        {
            Instance.exp.Set(int.Parse(args[0]));
            return (UI.Console.Command.Status.Success, "");
        }

        protected override void Awake()
        {
            base.Awake();
            enemykills = StatsCatalog.Session.TryGetControl(Track_EnemyKills);
            enemykills.AddChangeListener(OnKill);
            exp = StatsCatalog.Session.TryGetControl(Exp);
            exp.AddChangeListener(OnExpChange);
            levelBar.material = levelBar.material.Instantiate();
        }

        void OnEnable()
        {
            if (!ActivePlayer.Instance) return;
            ActivePlayer.Instance.OnPlayerStart += OnPlayerCreated;
            ActivePlayer.Instance.OnPlayerRespawn += OnPlayerCreated;
            ActivePlayer.Instance.OnPlayerDead += OnPlayerDead;
            OnPlayerCreated(ActivePlayer.Instance.player);
        }

        void OnKill(ChangeEvent ev) { }

        void OnExpChange(ChangeEvent ev)
        {
            if (!player) return;
            if (player.SetIndex(Mathf.FloorToInt(ExpToLevelIndex())))
                upgraded.Invoke(player.transform, ("name", "Bambo"), ("level", $"{player.GetIndex() + 1}"));
            UpdateUI();
            this.TryPop();
        }

        void OnPlayerDead(Player player) => UpdateUI();

        void OnPlayerCreated(Player player)
        {
            Debug.Log("Player Created");
            this.player = player;
            player.GetComponent<StatsController>().AddListener((evt) => UpdateUI());
            UpdateUI();
        }

        #region Update UI
        void UpdateUI()
        {
            if (!player) return;

            // dead
            dead.gameObject.SetActive(ActivePlayer.Instance.playerIsDead);

            // level
            float level = ExpToLevelIndex();
            int target = Mathf.FloorToInt(level);

            levelIndex.text = $"{target + 1}";
            int t1 = Math.Min(levelColor.Length - 1, target);
            int t2 = Math.Min(levelColor.Length - 1, target + 1);

            levelBar.material.SetFloat("_Fill", level % 1);
            levelBar.material.SetColor("_Color", levelColor[t2]);
            levelBar.material.SetColor("_BackgroundColor", levelColor[t1]);
        }
        #endregion

        float ExpToLevelIndex()
        {
            if (!player) return 0;
            var lvl = playerLevels;
            float min = 0, index = 0;
            for (int i = 0, n = lvl.count - 1; i <= n; i++)
            {
                float max = i == n ? min : min + lvl[i + 1][AI_Exp];
                if (exp >= min)
                    index = min == max ? i : i + (exp - min) / (max - min);
                min = max;
            }
            return index;
        }
    }
}