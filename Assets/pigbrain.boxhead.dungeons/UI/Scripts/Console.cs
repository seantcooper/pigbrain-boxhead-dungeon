#pragma warning disable UDR0001
using UnityEngine;
using System.Collections;
using pigbrain.core.Collections;
using TMPro;
using UnityEngine.InputSystem;
using System;
using System.Linq;
using pigbrain.core.UnityObject;
using System.Collections.Generic;
using UnityEngine.InputSystem.Controls;
using static System.StringComparison;
using UnityEngine.Scripting;
using System.Reflection;
using pigbrain.core.Utility;
using pigbrain.core.Statistics;

namespace pigbrain.game.Boxhead.UI
{
    public class Console : MonoBehaviourSingleton<Console>
    {
        [SerializeField] TMP_InputField input;
        [SerializeField] TMP_Text output;
        [SerializeField] TMP_Text ghost;
        [SerializeField] History history;

        InputActionAsset inputAsset;

        bool state;

        void Start()
        {
            Game.Input.GamePlay.Console.performed += ctx => ToggleState();
            input.onSubmit.AddListener(OnSubmit);
            input.onValueChanged.AddListener(history.OnInputChanged);
            history.OnInputChanged();
            inputAsset = Game.Input.asset;
            history.LoadHistory();
            gameObject.SetActive(false);
        }

        void OnEnable()
        {
            StartCoroutine(KeyRepeater(Keyboard.current.upArrowKey, history.OnKeyUp));
            StartCoroutine(KeyRepeater(Keyboard.current.downArrowKey, history.OnKeyDown));
        }

        #region OnSubmit
        void OnSubmit(string text)
        {
            // text = ghost.text;
            // if (string.IsNullOrEmpty(text)) return;
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && Process(parts))
                history.Add(text);

            history.MoveToEnd();
            input.text = string.Empty;
            input.ActivateInputField();
        }
        #endregion

        #region Transition
        void Update()
        {
            if (!state) return;
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (!string.IsNullOrEmpty(ghost.text) && ghost.text.StartsWith(input.text, StringComparison.OrdinalIgnoreCase))
                {
                    input.text = ghost.text;
                    history.MoveToEnd();
                }
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, Mouse.current.position.ReadValue()))
                    ToggleState();
        }
        #endregion

        #region Transition
        void ToggleState()
        {
            Debug.Log("Console Toggle");
            state = !state;
            if (state)
            {
                inputAsset.Disable();
                Game.Input.GamePlay.Console.Enable();
                gameObject.SetActive(true);
                foreach (var pi in PlayerInput.all) pi.DeactivateInput();
                StartCoroutine(TransitionIn());
            }
            else
            {
                inputAsset.Enable();
                foreach (var pi in PlayerInput.all) pi.ActivateInput();
                StartCoroutine(TransitionOut());
            }
        }

        const float TransitionSpeed = 0.1f;
        IEnumerator TransitionIn() { yield return Transition(true); }
        IEnumerator TransitionOut() { yield return Transition(false); gameObject.SetActive(false); }
        IEnumerator Transition(bool enter)
        {
            RectTransform rt = (RectTransform)transform;
            float h = rt.rect.height;
            Vector2 start = Vector2.zero, end = new(0, -h);
            yield return new OverTimeUnscaled(TransitionSpeed, u =>
                rt.anchoredPosition = Vector2.LerpUnclamped(start, end, enter ? 1 - u : u));
            if (enter)
            {
                input.ActivateInputField();
                input.Select();
            }
        }
        #endregion

        #region Commands
        [Preserve]
        public abstract class Command
        {
            public abstract string Help(Console console);
            public abstract (Status status, string message) Parse(Console console, string[] parts);
            public enum Status { Success, Error }
        }

        public class InlineAttribute : Attribute
        {
            public readonly string name;
            public InlineAttribute(string name) => this.name = name;
        }
        [AttributeUsage(AttributeTargets.Class)]
        public class NameAttribute : Attribute
        {
            public readonly string name;
            public NameAttribute(string name) => this.name = name;
        }

        bool Process(string[] parts)
        {
            string cmdName = parts[0].ToUpper();
            string[] args = parts.Skip(1).Select(s => s.ToUpper()).ToArray();

            string text = string.Join(" ", parts);

            if (cmdName == "CLEAR")
            {
                output.text = "";
                return true;
            }
            if (cmdName == "HELP")
            {
                Help();
                return true;
            }

            else if (GetInlineCommands().TryGetValue(cmdName, out MethodInfo method))
                return PrintM(ValidateSignature(method, cmdName, args), text);

            else if (GetTypes().TryGetValue(cmdName, out Type type))
                return PrintM(CreateInstance(type).Parse(this, args), text);

            else Print($"'{cmdName}' was not found!");
            return false;

            static bool PrintM((Command.Status status, string message) state, string text)
            {
                string m = string.IsNullOrEmpty(state.message) ? text : $"{text} - {state.message}";
                Print(state.status == Command.Status.Success ? m : $"Error: {m}");
                return state.status == Command.Status.Success;
            }
        }

        void Help()
        {
            List<string> lines = new() { "Help:" };

            lines.AddRange(GetInlineCommands().Keys);
            lines.AddRange(GetTypes().Values
                .Where(t => t != GetType())
                .Select(t => CreateInstance(t).Help(this)));

            lines = lines.Select(l => $"[ {l} ]").ToList();

            output.richText = true;
            output.text = string.Join(" - ", lines);
        }

        public static void Print(string message)
        {
            Instance.output.text = $"{message}\n{Instance.output.text}";
        }

        internal Dictionary<string, Type> GetTypes()
        {
            var types = new[] { typeof(Console).Assembly }
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(Command).IsAssignableFrom(t) && t != typeof(Command));

            static string GetName(Type type)
            {
                var attr = (InlineAttribute)Attribute.GetCustomAttribute(type, typeof(InlineAttribute));
                return attr != null && !string.IsNullOrEmpty(attr.name) ? attr.name : type.Name;
            }
            return types.ToDictionary(t => GetName(t).ToUpper(), t => t);
        }

        (Command.Status, string) ValidateSignature(MethodInfo method, string cmd, string[] args)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return ((Command.Status, string))method.Invoke(null, null);
            else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Console))
                return ((Command.Status, string))method.Invoke(null, new object[] { this });
            else if (parameters.Length == 2 && parameters[0].ParameterType == typeof(Console)
                && parameters[1].ParameterType == typeof(string[]))
                return ((Command.Status, string))method.Invoke(null, new object[] { this, args });
            return (Command.Status.Error, $"Invalid signature for inline command '{cmd}'");
        }

        Dictionary<string, MethodInfo> GetInlineCommands() => new[] { typeof(Console).Assembly }
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(MonoBehaviour).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods(ReflectionUtility.DefaultBindings | BindingFlags.Static))
            .Select(m => (method: m, attr: m.GetCustomAttribute<InlineAttribute>()))
            .Where(x => x.attr != null)
            .ToDictionary(x => x.attr.name.ToUpper(), x => x.method);

        public Command CreateInstance(Type type) => (Command)Activator.CreateInstance(type);
        #endregion

        #region History
        [Serializable]
        class History
        {
            const string HistoryKey = "console.history";
            [SerializeField] List<string> items = new();
            [SerializeField] int index = -1;
            [SerializeField] string currentDraft;

            public void MoveToEnd()
            {
                currentDraft = "";
                index = items.Count;
                Instance.input.caretPosition = Instance.input.text.Length;
            }

            public bool MoveBack(string current, out string result)
            {
                result = null;
                if (items.Count == 0 || index == 0) return false;
                if (index == items.Count) currentDraft = current;
                result = items[--index];
                return true;
            }

            public bool MoveForward(string current, out string result)
            {
                result = null;
                if (items.Count == 0) return false;
                result = (index = Mathf.Min(items.Count, index + 1)) == items.Count ? currentDraft : items[index];
                return true;
            }

            void CleanupHistory()
            {
                if (items.Count < 2) return;
                List<string> cleaned = new(items.Count);
                string prev = null;
                for (int i = 0; i < items.Count; prev = items[i], i++)
                    if (items[i] != prev) cleaned.Add(items[i]);
                items = cleaned;
            }

            public void Add(string cmd)
            {
                if (string.IsNullOrWhiteSpace(cmd)) return;
                items.Add(cmd);
                CleanupHistory();
                SaveHistory();
                index = items.Count;
                Debug.Log($"historyIndex: {index}/{items.Count}");
            }

            public void SaveHistory() => Persistence.CurrentData.SetString(HistoryKey, string.Join("\n", items));
            public void LoadHistory()
            {
                if (!Persistence.CurrentData.HasKey(HistoryKey)) return;
                items = Persistence.CurrentData.GetString(HistoryKey).Split('\n').Where(s => !string.IsNullOrEmpty(s)).ToList();
                CleanupHistory();
                index = items.Count;
            }

            int GetGhostIndex(string prefix) => prefix == "" ? -1 : items.IndexOf((item) => item.StartsWith(prefix, OrdinalIgnoreCase));
            int MoveGhostBack(string text)
            {
                for (int i = index - 1; i >= 0; --i)
                    if (items[i].StartsWith(text, OrdinalIgnoreCase)) return i;
                return -1;
            }
            int MoveGhostForward(string text)
            {
                for (int i = index + 1; i < items.Count; i++)
                    if (items[i].StartsWith(text, OrdinalIgnoreCase)) return i;
                return -1;
            }

            internal void OnKeyDown()
            {
                if (hasGhostContext)
                {
                    if (MoveGhostForward(Instance.input.text) is int i && i >= 0)
                        Instance.ghost.text = items[index = i];
                }
                else if (MoveForward(Instance.input.text, out var text)) Instance.input.text = text;
            }

            bool hasGhostContext => Instance.input.text != "" && !Instance.input.text.Equals(Instance.ghost.text, OrdinalIgnoreCase);

            internal void OnKeyUp()
            {
                if (hasGhostContext)
                {
                    if (MoveGhostBack(Instance.input.text) is int i && i >= 0)
                        Instance.ghost.text = items[index = i];
                }
                else if (MoveBack(Instance.input.text, out var text)) Instance.input.text = text;
            }

            internal void OnInputChanged(string text = "")
            {
                Instance.ghost.text = GetGhostIndex(text) >= 0 ? items[GetGhostIndex(text)] : "";
            }
        }

        const float RepeatDelay = 0.2f; // initial delay
        const float RepeatRate = 0.1f; // speed

        IEnumerator KeyRepeater(KeyControl key, Action onKey)
        {
            while (true)
            {
                yield return new WaitUntil(() => key.wasPressedThisFrame);
                onKey?.Invoke();
                yield return new WaitForSecondsRealtime(RepeatDelay);
                while (key.isPressed)
                {
                    onKey?.Invoke();
                    yield return new WaitForSecondsRealtime(RepeatRate);
                }
                while (key.isPressed) yield return null;
            }
        }
        #endregion
    }

}
