#pragma warning disable UDR0001
using System;
using pigbrain.core.Analysis;
using pigbrain.core.Inspector;
using pigbrain.core.UnityObject;
using UnityEngine;

namespace pigbrain.core.Analytics
{
    public abstract class AnalyticsManager : MonoBehaviourSingleton<AnalyticsManager>
    {
        public static bool IsValidSite =>
            Instance && Instance.sitelock ? Instance.sitelock.isValid : false;
        protected const string SessionIDKey = "Analytics.sessionId";

        [SerializeField][InlineScriptableObject] protected SiteLock sitelock;
        [SerializeField][InlineScriptableObject] protected Post post;
        [SerializeField][ReadOnly] protected string sessionId;

        public static T GetContext<T>() where T : AnalyticsContext => (T)Instance.context;
        public static void SetContext<T>(T context) where T : AnalyticsContext => Instance.context = context;

        internal AnalyticsContext context;
        internal Type payloadType;

        public void Initialize(AnalyticsContext context, Type payloadType)
        {
            this.context = context;
            this.payloadType = payloadType;
        }

        protected override void Awake()
        {
            base.Awake();
            // sessionId = PlayerPrefs.GetString(SessionIDKey, System.Guid.NewGuid().ToString());
            sessionId = System.Guid.NewGuid().ToString();
            StartCoroutine(post.FlushMonitor());
        }

        public static string GetSessionID() => Instance.sessionId;
        static AnalyticsPayload Validate(AnalyticsPayload payload) { payload.ApplyContext(Instance.context); return payload; }
        public static void Post(AnalyticsPayload payload) => Instance.post.Queue(Validate(payload));
        public static void PostImmediate(AnalyticsPayload payload) => Instance.post.Send(Validate(payload));
    }

    [Serializable]
    public class AnalyticsContext
    {
        public Type payloadType;
        public static implicit operator bool(AnalyticsContext empty) => empty != null;
    }

    [Serializable]
    public class AnalyticsPayload
    {
        public virtual void ApplyContext(AnalyticsContext context) { }
        public static implicit operator bool(AnalyticsPayload empty) => empty != null;
    }

}