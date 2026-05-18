using UnityEngine;
using pigbrain.core.Collections;
using pigbrain.core.UnityObject;
using System;
using System.Linq;
using System.Collections;
using pigbrain.core.Motion;

namespace pigbrain.game.Boxhead.UI
{
    #region Grid
    [Serializable]
    public class WeaponsGrid : MonoBehaviour
    {
        [SerializeField] Type type;
        [SerializeField] int count = 6;
        [SerializeField] Transform background;
        [SerializeField] internal Transform content;
        [SerializeField] GameObject emptySlot;
        // [SerializeField][ReadOnly] internal WeaponsContainer container;

        public event Action<WeaponSlot> OnWeaponSlotAdded;
        public event Action<WeaponSlot> OnWeaponSlotRemoved;

        enum Type { Using, Stash }

        internal bool editing = false;
        WeaponCache weapons;

        WeaponsContainer.Messages messages => WeaponsContainer.Instance
            ? WeaponsContainer.Instance.messages : null;

        WeaponSlot[] slots => content ? content.GetComponentsInChildren<WeaponSlot>() : new WeaponSlot[0];
        bool isFull => content.transform.childCount == count;

        public WeaponSlot this[Weapon weapon] => slots.FirstOrDefault(s => s.weapon == weapon);

        void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += PopulateBackground;
#endif
        }

        void Awake()
        {
            gameObject.SetActive(type == Type.Using);
        }

        void PopulateBackground()
        {
            if (!background || !emptySlot) return;
            background.DestroyChildrenImmediate();
            for (int i = 0; i < count; i++) emptySlot.PrefabInstantiate(background);
        }

        internal void Bind(Player player)
        {
            content.DestroyChildrenImmediate();
            weapons = player.GetComponent<WeaponCache>();
            if (!weapons) throw new Exception("No weapon cache found");

            weapons.OnWeaponAdded += OnWeaponAdded;
            weapons.OnWeaponRemoved += OnWeaponRemoved;

            weapons.GetWeapons().ForEach(w => OnWeaponAdded(w));
            // gameObject.SetActive(type == Type.Using);
        }

        #region └Editing
        internal WeaponsGrid moveToGrid =>
            transform.parent.GetComponentsInChildren<WeaponsGrid>().FirstOrDefault(g => g != this);

        internal void StartEdit()
        {
            // Debug.Log($"Start Edit: {editing} {name}");
            if (editing) return;
            editing = true;
            Transition(true);
            slots.ForEach(slot => slot.StartEdit());
        }

        internal void StopEdit()
        {
            // Debug.Log($"Stop Edit: {editing} {name}");
            if (!editing) return;
            editing = false;
            Transition(false);
            slots.ForEach(slot => slot.StopEdit());
        }

        Vector2? defaultPosition;
        Coroutine transition;
        void Transition(bool state)
        {
            if (type != Type.Stash) return;
            if (!transform) return;

            var rectTransform = (RectTransform)transform;
            defaultPosition ??= rectTransform.anchoredPosition;

            gameObject.SetActive(true);
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(Run());

            IEnumerator Run()
            {
                gameObject.TryAddComponent(out CanvasGroup cg);
                Vector2 start = (Vector2)defaultPosition, end = new(0, start.y);
                yield return new OverTimeUnscaled(0.25f, t =>
                {
                    t = Ease.Out(t);
                    float f = state ? 1 - t : t;
                    rectTransform.anchoredPosition = Vector2.Lerp(start, end, f);
                    cg.alpha = Mathf.Lerp(1, 0, f);
                });

                if (!state)
                {
                    gameObject.SetActive(false);
                }
                transition = null;
            }
        }

        void OnSlotMove(WeaponSlot slot)
        {
            if (!this) return;
            if (!slot.weapon) return;
            if (moveToGrid.isFull) return;
            StartCoroutine(TransitionWeapon(slot));
        }

        #region Weapon Transition
        IEnumerator TransitionWeapon(WeaponSlot slot)
        {
            Weapon weapon = slot.weapon;

            // Create a temp object 
            var temp = Instantiate(slot.gameObject);
            temp.transform.SetParent(GetComponentInParent<Canvas>().transform, true);
            temp.transform.position = slot.transform.position;
            temp.transform.localRotation = Quaternion.identity;
            temp.transform.localScale = slot.gameObject.transform.localScale;
            ((RectTransform)temp.transform).sizeDelta = ((RectTransform)slot.transform).sizeDelta;

            // change weapon state to add
            weapon.gameObject.SetActive(moveToGrid.type == Type.Using);
            moveToGrid.OnWeaponAdded(weapon, false);

            // hode both
            slot.icon.canvasGroup.alpha = 0;
            moveToGrid[weapon].icon.canvasGroup.alpha = 0;

            // animate
            Vector3 startPos = temp.transform.position, endPos = moveToGrid[weapon].transform.position;
            yield return new OverTimeUnscaled(0.25f,
                (t) => temp.transform.position = Vector3.Lerp(startPos, moveToGrid[weapon].transform.position, Ease.Out(t, 1)));

            // restore
            OnWeaponRemoved(weapon);
            moveToGrid[weapon].icon.canvasGroup.alpha = 1;
            Destroy(temp);
        }
        #endregion
        #endregion

        #region └Add Weapon
        Type GetGridType(Weapon weapon) => weapon.gameObject.activeSelf ? Type.Using : Type.Stash;

        internal void OnWeaponAdded(Weapon weapon) => OnWeaponAdded(weapon, true);
        internal void OnWeaponAdded(Weapon weapon, bool message = false)
        {
            if (AddWeapon(weapon) && message && messages && !ActivePlayer.IsRespawning)
                messages.Invoke(messages.weaponAdded, weapon);
        }
        bool AddWeapon(Weapon weapon)
        {
            if (GetGridType(weapon) != type) return false;
            if (content.childCount >= count) return false;
            var slot = WeaponSlot.CreateInstance(this, weapon);
            slot.OnMove += OnSlotMove;
            OnWeaponSlotAdded?.Invoke(slot);
            return true;
        }
        #endregion

        #region └Remove Weapon
        internal void OnWeaponRemoved(Weapon weapon)
        {
            if (!weapon) throw new System.Exception($"Weapon should be removed after calling this!");
            if (this[weapon])
            {
                OnWeaponSlotRemoved?.Invoke(this[weapon]);
                Destroy(this[weapon].gameObject);
            }
        }
        #endregion
    }
    #endregion
}