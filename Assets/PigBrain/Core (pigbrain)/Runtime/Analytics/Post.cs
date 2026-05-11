using System.Collections;
using System.Collections.Generic;
using System.Text;
using pigbrain.core.Inspector;
using pigbrain.core.Utility;
using UnityEngine;

namespace pigbrain.core.Analytics
{
    public class Post : ScriptableObject
    {
        [SerializeField][Range(0, 5)] float frequency = 2;
        [SerializeField][InlineScriptableObject] protected Query[] queries;

        readonly List<AnalyticsPayload> payloads = new();

        public void Send(AnalyticsPayload[] payloads) { foreach (var payload in payloads) Send(payload); }
        public void Send(AnalyticsPayload payload) => AnalyticsManager.Instance.StartCoroutine(Run(payload));

        public void Queue(AnalyticsPayload payload) => payloads.Add(payload);

        protected virtual IEnumerator Run(AnalyticsPayload payload) { yield break; }

        internal IEnumerator FlushMonitor()
        {
            while (true)
            {
                Flush();
                yield return new WaitForSeconds(frequency);
            }
        }

        void Flush()
        {
            Send(payloads.ToArray());
            payloads.Clear();
        }
    }
}