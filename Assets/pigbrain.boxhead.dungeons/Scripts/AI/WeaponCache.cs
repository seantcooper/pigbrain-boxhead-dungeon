using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using static pigbrain.core.Collections.CoroutineUtility;
using pigbrain.core.Collections;
using pigbrain.core.UnityObject;
using Unity.Mathematics;

namespace pigbrain.game.Boxhead
{
    public class WeaponCache : MonoBehaviour
    {
        [SerializeField][Range(1, 10)] int maxWeapons = 6;
        [SerializeField][Range(1, 5)] float holdScale = 2;
        [SerializeField] List<Weapon> weapons = new();

        public event Action<Weapon> OnWeaponAdded;
        public event Action<Weapon> OnWeaponRemoved;
        public event Action<Weapon.ChangeEvent> OnWeaponChanged;

        public Weapon[] GetWeapons() => weapons.ToArray();
        public int maxWeaponSlots => maxWeapons;

        public Transform rightHand;
        public Transform leftHand;

        Targeting targeting;

        void Start()
        {
            // if (TryGetComponent(out Health health))
            //     health.onDeath += OnDeath;
            targeting = GetComponent<Targeting>();
            UpdateWeapons();
        }

        void OnDestroy() => GetWeapons().ForEach(w => Remove(w));

        // void OnDeath() => weapons.ForEach(w => w.gameObject.SetActive(false));

        public bool GetMaxRange(out float range)
        {
            range = 0;
            foreach (var w in weapons)
            {
                float r = w.GetMaxRange();
                if (r > range) range = r;
            }
            return weapons.Count > 0;
        }

        void OnTransformChildrenChanged() =>
            StartCoroutine(DelayFrames(1, () => UpdateWeapons()));

        void Add(Weapon weapon)
        {
            if (weapons.FirstOrDefault(w => w.name == weapon.name) is not Weapon same)
            {
                weapon.transform.ResetLocal();
                weapons.Add(weapon);
                if (weapons.Count(w => w.gameObject.activeInHierarchy) > maxWeapons)
                    weapon.gameObject.SetActive(false);
                OnWeaponAdded?.Invoke(weapon);
                // weapon.AddListener(OnWeaponCarryChange);
                weapon.AddListener(OnWeaponChange);
            }
            else
            {
                Upgrade(same);
                Destroy(weapon.gameObject);
            }
        }

        void Remove(Weapon weapon)
        {
            weapons.Remove(weapon);
            weapon.RemoveListener(OnWeaponChange);
            // weapon.RemoveListener(OnWeaponCarryChange);
            OnWeaponRemoved?.Invoke(weapon);
        }

        readonly HashSet<WeaponCarry> carrying = new();
        const string NoWeapon = "NoWeapon";
        const string RightWeapon = "Weapon1";
        void UpdateCarry(Weapon weapon)
        {
            if (!rightHand) return;

            var weaponCarry = weapon.GetComponentInChildren<WeaponCarry>();
            if (!weaponCarry) return;
            weaponCarry.parentTarget = rightHand;

            if (weapon.isActiveAndEnabled) { if (carrying.Add(weaponCarry)) SetWeaponLayer(RightWeapon); }
            else if (carrying.Remove(weaponCarry) && carrying.Count == 0)
                SetWeaponLayer(NoWeapon);
        }

        void SetWeaponLayer(string trigger) =>
            GetComponentInChildren<Animator>().SetTrigger(trigger);

        void OnAnimatorMove()
        {
            foreach (var carry in carrying)
            {
                carry.transform.SetPositionAndRotation(carry.parentTarget.position, carry.parentTarget.rotation);
                carry.transform.localScale = (float3)holdScale;
            }
        }

        void OnWeaponChange(Weapon.ChangeEvent e)
        {
            UpdateCarry(e.weapon);
            OnWeaponChanged?.Invoke(e);
        }

        void Upgrade(Weapon weapon) => weapon.LevelUp();

        void UpdateWeapons()
        {
            var weapons = GetComponentsInChildren<Weapon>(true);
            this.weapons.Compare(weapons, out List<Weapon> add, out List<Weapon> remove);

            add.ForEach(w => Add(w));
            remove.ForEach(w => Remove(w));

            if (targeting)
                targeting.maxDistance = this.weapons.IsNullOrEmpty() ? 0
                    : this.weapons.Max(w => w.GetMaxRange());
        }
    }
}