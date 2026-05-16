using System;
using System.Linq;
using pigbrain.core.UnityObject;
using pigbrain.game.Boxhead.Environment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.UI
{
    public class DungeonCard : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text money;
        [SerializeField] Toggle toggle;
        [SerializeField] RectTransform content;
        [SerializeField] RectTransform weapons;
        [SerializeField] RectTransform enemies;
        [SerializeField] RectTransform locked;
        [SerializeField] Image progress;
        [SerializeField] Image background;

        public void Select() => toggle.isOn = true;

        public void SetLock(bool state)
        {
            content.TryAddComponent(out CanvasGroup group);
            group.alpha = state ? 0.15f : 1f;
            toggle.interactable = !state;
            locked.gameObject.SetActive(state);
        }

        public void SetBackgroundColor(Color color)
        {
            if (!background) return;
            background.color = color;
        }


        public void SetProgress(float value)
        {
            if (value == 0) progress.SetActive(false);
            else
            {
                if (!progressMaterial) progress.material = progressMaterial = new(progress.material);
                progressMaterial.SetFloat("_Fill", value);
            }
        }

        Material progressMaterial;
        void OnDestroy()
        {
            if (progressMaterial) Destroy(progressMaterial);
        }

        public static DungeonCard CreateInstance(DungeonCard prefab, LevelData levelData, Transform parent, Action<LevelData> onSelect)
        {
            var inst = prefab.Instantiate(parent);
            inst.title.text = levelData.title;
            inst.money.text = $"{levelData.money}";
            inst.toggle.onValueChanged.AddListener((state) => { if (state == true) onSelect(levelData); });
            inst.toggle.group = parent.GetComponent<ToggleGroup>();

            var usedWeapons = levelData.startWeapons.Select(w => w.ToLower()).ToHashSet();
            foreach (RectTransform child in inst.weapons)
                child.gameObject.SetActive(usedWeapons.Contains(child.name.ToLower()));

            var usedEnemies = levelData.levels.SelectMany(d => d.items.Select(i => i.type.ToString().ToLower())).ToHashSet();
            foreach (RectTransform child in inst.enemies)
                child.gameObject.SetActive(usedEnemies.Contains(child.name.ToLower()));

            inst.SetLock(false);
            return inst;
        }
    }
}