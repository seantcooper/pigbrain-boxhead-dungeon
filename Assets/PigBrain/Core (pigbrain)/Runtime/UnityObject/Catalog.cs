#pragma warning disable UDR0001
using System.Collections.Generic;
using System.Linq;
using pigbrain.core.Inspector;
using pigbrain.game.Boxhead.Environment;
using UnityEngine;

namespace pigbrain.core.UnityObject
{
    public class Catalog : MonoBehaviour
    {
        [SerializeField][InlineScriptableObject] ObjectList[] lists;

        static CatalogQuery QueryCache;
        public static CatalogQuery Query => QueryCache = new CatalogQuery();
        public static T Q<T>(string name) where T : Object => Query.Get<T>(name);

        #region Cache
        public class CatalogQuery
        {
            public readonly Dictionary<string, Object[]> lookup;
            public readonly Object[] array;

            public CatalogQuery()
            {
                var lists = FindObjectsByType<Catalog>(FindObjectsInactive.Include).SelectMany(c => c.lists);
                this.array = lists.SelectMany(l => l.objects).ToArray();
                this.lookup = this.array.GroupBy(o => GetName(o))
                    .ToDictionary(g => g.Key, g => g.ToArray());
            }

            public T Find<T>(string name) where T : Object
            {
                return lookup.TryGetValue(name.ToLower(), out var list) ? list[0] as T : null;
            }

            static string GetName(Object o) => o.name.Split("(").Last().Split(")").First().ToLower();

            public Dictionary<string, Object> GetAllObjects() =>
                lookup.ToDictionary(kv => kv.Key, kv => kv.Value[0]);

            public T Get<T>(string name) where T : Object
            {
                if (lookup.TryGetValue(name.ToLower(), out var array))
                    foreach (var o in array)
                        if (o is T t) return t;
                return null;
            }

            public GameObject Get(AssetIdentity id)
            {
                foreach (Object[] o in lookup.Values)
                    foreach (Object item in o)
                        if (item is GameObject g && g.TryGetComponent(out AssetIdentity oid)
                            && oid.GetGuid() == id.GetGuid())
                            return g;
                return null;
            }

        }
        #endregion
    }
}