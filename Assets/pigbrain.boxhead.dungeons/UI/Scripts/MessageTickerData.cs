using UnityEngine;

namespace pigbrain.game.Boxhead.UI
{
    public class MessageTickerData : ScriptableObject
    {
        [SerializeField] string text = "<enter text>";
        [SerializeField] internal Sprite icon;
        [SerializeField] internal Color color = Color.white;
        [SerializeField] internal Priority priority = Priority.Medium;

        public string runtimeText { get; internal set; }
        public void ResetText() => runtimeText = text;
        public bool canMessage => priority != Priority.Off && MessageTicker.IsAvailable;
        internal void InvokeInternal(Transform owner) =>
            MessageTicker.Instance.CreateMessage(this, owner);
        internal enum Priority { Off, Low, Medium, High }
    }

    public static class MessageTickerDataX
    {
        public static void Invoke(this MessageTickerData message, Transform owner)
        {
            if (message && message.canMessage)
            {
                message.ResetText();
                message.InvokeInternal(owner);
            }
        }

        public static void Invoke(this MessageTickerData message, params (string a, string b)[] replace) =>
            message.Invoke(null, replace);

        public static void Invoke(this MessageTickerData message, Transform owner, params (string a, string b)[] replace)
        {
            if (message && message.canMessage)
            {
                message.ResetText();
                foreach (var (a, b) in replace)
                    message.runtimeText = message.runtimeText.Replace($"{{{a}}}", b);
                message.InvokeInternal(owner);
            }
        }
    }
}
