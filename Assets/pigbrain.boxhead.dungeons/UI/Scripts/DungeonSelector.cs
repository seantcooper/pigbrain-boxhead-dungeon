using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.Inspector;
using pigbrain.core.Statistics;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Environment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static pigbrain.game.Boxhead.UI.Console;

namespace pigbrain.game.Boxhead.UI
{
    public class DungeonSelector : MonoBehaviourSingleton<DungeonSelector>
    {
        [SerializeField][InlineScriptableObject] DungeonCollection dungeons;
        [SerializeField][InlineScriptableObject] DungeonCollection dungeonsDynamic;
        [SerializeField][InlineScriptableObject] LevelData dungeonSelected;
        [SerializeField] DungeonCard cardPrefab;

        readonly List<DungeonCard> cards = new();

        public event Action<LevelData> OnSelectionChanged;

        public LevelData GetSelectedDungeon() => dungeonSelected;
        public static LevelData GetDungeon()
        {
            if (Instance == null) Instance = FindAnyObjectByType<DungeonSelector>(FindObjectsInactive.Include);
            return Instance.dungeonSelected;
        }

        void SelectDungeon(LevelData dungeon)
        {
            dungeonSelected = dungeon;
            Persistence.CurrentData.SetString("DungeonSelector.selected", dungeonSelected.name);
            OnSelectionChanged?.Invoke(dungeon);
        }

        [Inline("LOADALL")]
        public static (UI.Console.Command.Status, string) LoadAll(UI.Console console, string[] args)
        {
            if (Instance.dungeonsDynamic)
                Instance.dungeonsDynamic.ReadRuntime((levelData) =>
                {
                    Instance.cards.Add(Instance.CreateButton(levelData));
                    Instance.cards[^1].SetBackgroundColor(Color.aquamarine);
                });
            return (UI.Console.Command.Status.Success, "");
        }

        [Inline("UNLOCK")]
        public static (UI.Console.Command.Status, string) Unlock(UI.Console console, string[] args)
        {
            UnlockAllAndSave(true);
            return (UI.Console.Command.Status.Success, "");
        }

        public static void UnlockAllAndSave(bool save = true)
        {
            Instance.cards.ForEach(c => c.SetLock(false));
            if (save) Persistence.CurrentData.SetBool("DungeonSelector.Unlock", true);
        }
        static bool IsLocked => Persistence.CurrentData.GetBool("DungeonSelector.Unlock");

        void OnDisable()
        {
            RemoveButtons();
        }
        void OnEnable()
        {
            RemoveButtons();
            CreateButtons();
        }

        void CreateButtons()
        {
            foreach (var levelData in dungeons.levelDatas)
                cards.Add(CreateButton(levelData));
            cards.Skip(1).ForEach(c => c.SetLock(!IsLocked));

            var selected = Persistence.CurrentData.GetString("DungeonSelector.selected", "");
            var selectedDungeon = dungeons.levelDatas.FirstOrDefault(l => l.name == selected);
            if (selectedDungeon == null) selectedDungeon = dungeons.levelDatas.FirstOrDefault();

            int index = Array.IndexOf(dungeons.levelDatas, selectedDungeon);
            cards[index].Select();
        }

        void RemoveButtons()
        {
            cards.ForEach(c => c.DestroyObject());
            cards.Clear();
        }

        DungeonCard CreateButton(LevelData levelData)
        {
            var progress = Persistence.CurrentData.GetFloat($"{levelData.name}.progress");
            Debug.Log($"Progress {progress} {levelData.name}");
            var card = DungeonCard.CreateInstance(cardPrefab, levelData, transform, (ld) => SelectDungeon(ld));
            card.SetProgress(progress);
            return card;

        }

    }
}