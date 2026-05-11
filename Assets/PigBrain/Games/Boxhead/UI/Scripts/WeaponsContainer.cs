using UnityEngine;
using pigbrain.game.Boxhead.Environment;
using pigbrain.core.Inspector;
using pigbrain.core.Collections;
using System.Collections.Generic;
using pigbrain.core.UnityObject;
using System;
using System.Linq;

namespace pigbrain.game.Boxhead.UI
{
    public class WeaponsContainer : MonoBehaviourSingleton<WeaponsContainer>
    {
        [SerializeField] internal RectTransform cardContainer;
        [SerializeField][InlineObject()] internal Messages messages;
        [SerializeField] internal WeaponsGrid[] grids;

        void OnValidate() => grids = GetComponentsInChildren<WeaponsGrid>();

        protected override void Awake()
        {
            base.Awake();
            grids.ForEach(g => { g.OnWeaponSlotAdded += OnWeaponSlotAdded; g.OnWeaponSlotRemoved += OnWeaponSlotRemoved; });
        }

        [Serializable]
        internal class Messages
        {
            [InlineScriptableObject] public MessageTickerData weaponOutOfAmmo;
            [InlineScriptableObject] public MessageTickerData weaponUpgraded;
            [InlineScriptableObject] public MessageTickerData weaponAdded;

            public void Invoke(MessageTickerData message, Weapon weapon)
            {
                if (!message && !weapon) return;
                message.Invoke(weapon.transform, ("name", weapon.displayName), ("level", $"{weapon.GetLevel() + 1}"));
            }
            public static implicit operator bool(Messages empty) => empty != null;
        }

        Player player;
        public void Bind(Player player)
        {
            this.player = player;

            ActivePlayer.Instance.OnEnterRoom -= OnEnterRoom;
            ActivePlayer.Instance.OnEnterRoom += OnEnterRoom;
            ActivePlayer.Instance.OnLeaveRoom -= OnLeaveRoom;
            ActivePlayer.Instance.OnLeaveRoom += OnLeaveRoom;

            // Bind grids first
            grids.ForEach(g => g.Bind(player));

            var weapons = player.GetComponent<WeaponCache>();
            weapons.OnWeaponAdded -= OnWeaponAdded;
            weapons.OnWeaponAdded += OnWeaponAdded;
            weapons.OnWeaponRemoved -= OnWeaponRemoved;
            weapons.OnWeaponRemoved += OnWeaponRemoved;
        }

        readonly HashSet<Weapon> weapons = new();
        internal void OnWeaponAdded(Weapon weapon) => weapons.Add(weapon);
        internal void OnWeaponRemoved(Weapon weapon) => weapons.Remove(weapon);

        GameObject currentCard;
        UIItems currentUIItems;

        public static void SetCard(CommandContainer commandContainer)
        {
            var command = commandContainer.commands.FirstOrDefault(c => c is Command.IPrefab);
            if (command && (command as Command.IPrefab).GetPrefab() is GameObject g)
                Instance.SetCard(g.GetComponent<UIItems>());
        }

        void SetCard(UIItems uiItems = null)
        {
            if (currentUIItems == uiItems) return;
            currentUIItems = uiItems;

            if (currentCard) Destroy(currentCard);

            GameObject cardPrefab = uiItems ? uiItems.card : null;
            if (!cardPrefab)
            {
                cardContainer.gameObject.SetActive(false);
                return;
            }

            cardContainer.gameObject.SetActive(true);
            var inst = uiItems.card.Instantiate(cardContainer);

            var rt = inst.GetComponent<RectTransform>();
            rt.offsetMax = rt.offsetMin = rt.anchorMin = default;
            rt.anchorMax = rt.localScale = Vector3.one;
            rt.anchoredPosition = default;
            rt.localRotation = Quaternion.identity;
            currentCard = inst;
        }

        void OnWeaponSlotHover(WeaponSlot weapon, bool state) =>
            SetCard(weapon.weapon.GetComponent<UIItems>());

        void OnWeaponSlotAdded(WeaponSlot weapon) => weapon.OnHover += OnWeaponSlotHover;
        void OnWeaponSlotRemoved(WeaponSlot weapon) => weapon.OnHover -= OnWeaponSlotHover;

        void OnEnterRoom(Room room)
        {
            if (room.data.roomType != Room.Type.Loot) return;
            grids.ForEach(g => g.StartEdit());
            OrthoCamera.Instance.SetHorizontalOffset(3.75f);
        }

        void OnLeaveRoom(Room room)
        {
            if (room.data.roomType != Room.Type.Loot) return;

            grids.ForEach(g => g.StopEdit());
            if (OrthoCamera.Instance) OrthoCamera.Instance.SetHorizontalOffset(0);
        }
    }
}