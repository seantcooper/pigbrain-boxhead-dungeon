using System;
using pigbrain.core.Analysis;
using pigbrain.core.Analytics;
using pigbrain.core.UnityObject;
using UnityEditor;
using UnityEngine;

namespace pigbrain.game.Boxhead
{
    public class Analytics : AnalyticsManager
    {
        public static Context CTX => GetContext<Context>();
        void Start()
        {
            Initialize(new Context() { url = JS.URL }, typeof(Analytics));
            Post(new Session());
        }

        [Serializable]
        public class Context : AnalyticsContext
        {
            public string url, levelid, profile;
        }

        public class Payload : AnalyticsPayload
        {
            public string version;
            public string url, levelid, profile;

            public override void ApplyContext(AnalyticsContext context)
            {
                if (context is not Context ctx) throw new Exception("Wrong Context");
                url = ctx.url;
                levelid = ctx.levelid;
                version = Application.version;
            }
        }

        public class Session : Payload
        {
            public string status = "started";
            public Session() { }
        }

        public class System : Payload
        {
            public System() { }
        }

        public class Level : Payload
        {
            public string status;
            public Level() { }
            public Level(Status status) => this.status = $"{status}";
            public enum Status { Started = 0, Complete = 1, }
        }

        [Serializable]
        public class Player : Payload
        {
            public string status;
            public Player() { }
            public Player(Status status) => this.status = $"{status}";
            public enum Status { Died = 0, Respawned = 1, }
        }
    }
}

