using UnityEngine;
using pigbrain.generated.WeaponIconBase;
using pigbrain.core.UnityObject;
using System;
using System.Collections;
using pigbrain.core.Collections;
using Unity.Mathematics;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace pigbrain.game.Boxhead.UI
{
    [DefaultExecutionOrder(-1)]
    public class WeaponSlot : MonoBehaviour
    {
        public WeaponsGrid grid;
        public Weapon weapon;
        public PrefabView icon;

        public event Action<WeaponSlot> OnMove;
        public event Action<WeaponSlot, bool> OnHover;

        WeaponsContainer.Messages messages => WeaponsContainer.Instance
            ? WeaponsContainer.Instance.messages : null;

        #region Create
        public static WeaponSlot CreateInstance(WeaponsGrid grid, Weapon weapon)
        {
            var slot = weapon.GetComponent<UIItems>().icon.Instantiate(grid.content).AddComponent<WeaponSlot>();
            slot.icon = slot.GetComponent<PrefabView>();
            slot.grid = grid;
            slot.weapon = weapon;
            return slot;
        }

        bool isHovering;

        void Start()
        {
            icon.buttons.transform.gameObject.SetActive(true);
            if (weapon.gameObject.activeSelf) Activate(icon.buttons.buttonRemove.button, icon.buttons.buttonAdd.button);
            else Activate(icon.buttons.buttonAdd.button, icon.buttons.buttonRemove.button);

            icon.buttons.transform.gameObject.SetActive(false);
            if (grid.editing) StartEdit();
            icon.content.reload.image.material = new(icon.content.reload.image.material);
            weapon.AddListener(OnWeaponChanged);
        }

        void Activate(Button buttonActive, Button buttonInactive)
        {
            buttonActive.onClick.AddListener(MoveWeapon);
            buttonActive.transform.gameObject.SetActive(true);
            buttonInactive.transform.gameObject.SetActive(false);

            var trigger = buttonActive.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter },
                exit = new() { eventID = EventTriggerType.PointerExit };

            enter.callback.AddListener(_ => OnHoverState(true));
            exit.callback.AddListener(_ => OnHoverState(false));

            trigger.triggers.Add(enter);
            trigger.triggers.Add(exit);
        }

        void OnHoverState(bool state)
        {
            OnHover?.Invoke(this, state);
        }

        void OnDestroy()
        {
            weapon.RemoveListener(OnWeaponChanged);
            icon.buttons.buttonAdd.button.onClick.RemoveListener(MoveWeapon);
            icon.buttons.buttonRemove.button.onClick.RemoveListener(MoveWeapon);
            Destroy(icon.content.reload.image.material);
        }
        #endregion

        #region └Editing
        void MoveWeapon() => OnMove?.Invoke(this);
        internal void StartEdit()
        {
            if (!weapon || !icon) return;
            icon.buttons.transform.gameObject.SetActive(true);
        }

        internal void StopEdit()
        {
            if (!weapon || !icon) return;
            icon.buttons.transform.gameObject.SetActive(false);
        }
        #endregion

        #region Sync Weapon
        void OnWeaponChanged(Weapon.ChangeEvent e) => Sync(e);
        void Sync(Weapon.ChangeEvent e)
        {
            SetAmmo(e);
            SetLevel(e);
        }

        void Update()
        {
            SetReload();
        }

        void SetReload()
        {
            float f = weapon.isActiveAndEnabled ? 1 - weapon.reloadUnitTime : 0;
            icon.content.reload.image.gameObject.SetActive(f > 0.001f && f < 0.95f);
            if (icon.content.reload.image.isActiveAndEnabled)
                icon.content.reload.image.material.SetFloat("_Fill", f);
        }

        void SetAmmo(Weapon.ChangeEvent e)
        {
            icon.content.ammo.text.gameObject.SetActive(!weapon.infiniteAmmo);
            icon.content.ammoInfinity.transform.gameObject.SetActive(weapon.infiniteAmmo);
            icon.content.ammo.text.text = $"{e.ammo}";
            if (e.lastAmmo != e.ammo && e.ammo == 0)
                if (messages) messages.Invoke(messages.weaponOutOfAmmo, weapon);

            if (isActiveAndEnabled) StartCoroutine(Pop(1.1f, 0.1f));
        }

        void SetLevel(Weapon.ChangeEvent e)
        {
            if (e.lastLevel != e.level) if (messages) messages.Invoke(messages.weaponUpgraded, weapon);
            icon.levels.groupIndex.SetIndex(e.level);
            // icon.levels.level1.transform.gameObject.SetActive(e.level == 0);
            // icon.levels.level2.transform.gameObject.SetActive(e.level == 1);
            // icon.levels.level3.transform.gameObject.SetActive(e.level == 2);
            // icon.levels.level4.transform.gameObject.SetActive(e.level == 3);
            // icon.levels.level5.transform.gameObject.SetActive(e.level == 4);
            if (isActiveAndEnabled) StartCoroutine(Pop());
        }

        IEnumerator Pop(float scale = 2, float duration = 0.25f)
        {
            yield return new OverTimeUnscaled(duration, (t) => ((RectTransform)transform)
                .localScale = Vector3.Lerp((float3)scale, (float3)1, t));
        }
        #endregion
    }
}