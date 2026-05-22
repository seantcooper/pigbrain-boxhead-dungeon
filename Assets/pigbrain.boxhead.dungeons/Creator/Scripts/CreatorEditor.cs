using System;
using System.Linq;
using pigbrain.core.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace pigbrain.game.Boxhead.Creator
{
    public abstract class CreatorEditor : MonoBehaviour
    {
        [SerializeField] protected Toggle[] toggles;
        [SerializeField] protected Button[] buttons;

        void OnValidate()
        {
            if (toggles.IsNullOrEmpty())
                toggles = GetComponentsInChildren<Toggle>().Where(b => b.transform.parent.name == "Layout").ToArray();
            if (buttons.IsNullOrEmpty())
                buttons = GetComponentsInChildren<Button>().Where(b => b.transform.parent.name == "Layout").ToArray();
        }

        public abstract string[] summary { get; }

        protected string GetBracketName(string name) =>
            name.Split("(").Last().Split(")").First().ToLower();

        void Awake() => toggles.ForEach(t => t.onValueChanged.AddListener((b) => OnToggleChanged(t, b)));

        public abstract byte[] Encode();
        public abstract void Decode(byte[] bytes, ref int index);

        protected virtual void OnToggleChanged(Toggle toggle, bool state) { }
        protected T GetIndex<T>(string name) where T : struct, Enum =>
            Enum.Parse<T>(GetBracketName(name), true);

        protected void Refresh()
        {
            GetComponentInParent<DungeonCreator>().OnEditorChange(this);
        }

    }
}