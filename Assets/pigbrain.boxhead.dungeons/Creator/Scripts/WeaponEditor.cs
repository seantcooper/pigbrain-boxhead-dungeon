using System;
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Collections;
using pigbrain.core.UnityObject;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public class WeaponEditor : CreatorEditor
    {
        [SerializeField] RectTransform weaponSelection;
        [SerializeField] GameObject[] prefabs;

        [SerializeField] Weapon[] values = new Weapon[] { new() { index = Indices.Uzi, level = 0 }, new(), new(), new() };

        int currentIndex;
        Button[] selectionButtons;

        public enum Indices : byte { None, AK47, Barrel, Barrier, Grenade, Pistol, Railgun, Rockets, Shield, Shotgun, Turret, Uzi }

        #region Encode / Summary
        public override string[] summary => values
            .Where(w => w.index != Indices.None)
            .Select(w => $"{DungeonCreator.Bullet}{w.index} ({w.level + 1})").Prepend("Weapons:").ToArray();

        public override byte[] Encode() => values.SelectMany(w => new byte[] { (byte)w.index, w.level }).ToArray();
        public override void Decode(byte[] bytes, ref int index)
        {
            var weapons = new Weapon[4];
            for (var i = 0; i < weapons.Length; i++)
                weapons[i] = new Weapon { index = (Indices)bytes[index++], level = bytes[index++] };
            SetValue(weapons);
        }
        #endregion

        void Awake()
        {
            buttons.ForEach((b, i) => AwakeWeaponSlot(b, i));
            selectionButtons = weaponSelection.GetChild(0).GetComponentsInChildren<Button>(true);
            selectionButtons.ForEach((b, i) => b.onClick.AddListener(() => OnSelectWeapon(i)));
            SetValue(values);
            weaponSelection.SetActive(false);
        }

        void AwakeWeaponSlot(Button b, int i)
        {
            values[i].Awake(b, i, this);
            b.onClick.AddListener(() => OnClickWeapon(i));
        }

        public Weapon[] GetValue() => values;
        public void SetValue(params Weapon[] weapons)
        {
            weapons = weapons.Concat(new Weapon[] { new(), new(), new(), new() }).Take(4).ToArray();
            weapons.ForEach((w, i) => SetWeapon(i, w.index, w.level));
        }

        void SetWeapon(int index, Indices weaponIndex, int level)
        {
            values[index].Set(weaponIndex, level);
            Refresh();
        }

        [Preserve]
        void OnCancel() => weaponSelection.SetActive(false);

        void OnClickWeapon(int index)
        {
            currentIndex = index;

            HashSet<Indices> addOnce = values.Select(w => w.index).ToHashSet();
            selectionButtons.ForEach((b, i) =>
            {
                b.TryAddComponent(out CanvasGroup g);
                bool contains = i != 0 && addOnce.Contains((Indices)i);
                g.alpha = contains ? 0.33f : 1;
                g.interactable = !contains;
            });

            weaponSelection.SetActive(true);
        }

        void OnSelectWeapon(int index)
        {
            weaponSelection.SetActive(false);
            SetWeapon(currentIndex, (Indices)index, 0);
        }

        #region Weapon
        [Serializable]
        public class Weapon
        {
            public Indices index;
            public byte level;

            Button[] stars;
            Button button;
            WeaponEditor editor;
            Transform transform => button.transform;
            Transform levels;

            internal void Awake(Button button, int i, WeaponEditor editor)
            {
                this.editor = editor;
                this.button = button;
                levels = button.transform.Find("Level");
                stars = levels.GetComponentsInChildren<Button>(true);
                stars.ForEach((b, i) => b.onClick.AddListener(() => SetLevel(i)));
            }

            public void Set(Indices index, int level)
            {
                this.index = index;
                if (button && transform.childCount > 0) Destroy(transform.GetChild(0).gameObject);
                GameObject prefab = editor.prefabs[(int)index].gameObject;
                GameObject inst = Instantiate(prefab, transform, false);
                inst.transform.SetSiblingIndex(0);
                Destroy(inst.GetComponent<Button>());
                SetLevel(level);
                levels.SetActive(index != Indices.None);
            }

            void SetLevel(int level)
            {
                this.level = (byte)level;
                stars.ForEach((b, i) =>
                {
                    b.TryAddComponent(out CanvasGroup g);
                    g.alpha = i <= level ? 1 : 0.25f;
                });
                editor.Refresh();
            }
        }
        #endregion
    }
}