#pragma warning disable UDR0001
using Unity.Mathematics;
using System;
using UnityEngine;
using static Unity.Mathematics.math;
using System.Linq;
using System.Collections.Generic;

namespace pigbrain.core.Geom
{
    [Serializable]
    public class Rnd
    {
        #region Seed / Create
        public const uint SEED = 73856093;
        public static Rnd Instance = new(0);

        public uint seed;
        public bool isValid => seed != 0;

        public Rnd(int seed = (int)SEED) : this((uint)seed) { }
        public Rnd(uint seed) => this.seed = seed == 0 ? SEED : seed;
        public Rnd(uint seed, uint index) : this(seed) => seed = GetIndexedSeed(seed, index);
        public Rnd(Rnd r) : this(r.seed) { }
        public Rnd New() => new(NextUInt());
        public static Rnd Time() => new((int)UnityEngine.Time.time * 1000);

        public static Rnd FromUnitySeed() => new(UnityEngine.Random.Range(0xff, 0xfffffff));
        public static uint GetIndexedSeed(uint seed, uint index)
        {
            uint x = seed ^ index;
            x ^= x >> 17; x *= 0xed5ad4bb;
            x ^= x >> 11; x *= 0xac4c1b51;
            x ^= x >> 15; x *= 0x31848bab;
            x ^= x >> 14;
            return x;
        }

        public readonly static Dictionary<UnityEngine.Object, Rnd> ObjectRnd = new();

        public static Rnd GetInstance(Component o, uint seed = 0)
        {
            Debug.LogWarning("Using Component types may introduce a lot of Rnd seeds.", o);
            return GetInstance(o, seed);
        }
        public static Rnd GetInstance(GameObject o, uint seed = 0)
        {
            Debug.LogWarning("Using GameObject types may introduce a lot of Rnd seeds.", o);
            return GetInstance(o, seed);
        }
        public static Rnd GetInstance(UnityEngine.Object o, uint seed = 0)
        {
            if (!ObjectRnd.TryGetValue(o, out Rnd rnd)) ObjectRnd[o] = rnd = new Rnd(seed);
            return rnd;
        }

        uint NextSeed()
        {
            uint last = seed;
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            return last;
        }

        public static uint GetTimeSeed() => (uint)(UnityEngine.Time.time * 1000) * 73856093;

        public static int GetIntervalSeed(int id, float interval)
        {
            var bucket = (int)((UnityEngine.Time.time + id) / interval);
            unchecked { return (bucket * 73856093) ^ (id * 19349663); }
        }
        #endregion

        #region IEnum / Ilist
        public T Next<T>() where T : System.Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(NextInt(0, values.Length));
        }

        // IEnumerable<T>
        public T Next<T>(IEnumerable<T> source) =>
            Next(source, out T value) ? value : default;

        public bool Next<T>(IEnumerable<T> source, out T result)
        {
            int count = 0;
            result = default;
            foreach (var item in source ?? Enumerable.Empty<T>())
                if (NextInt(++count) == 0) result = item;
            return count != 0;
        }

        // IList<T>
        public bool Next<T>(IList<T> list, out T result) =>
            Next((IEnumerable<T>)list, out result);

        public T Next<T>(IList<T> list) =>
            Next(list, out T value) ? value : default;

        #endregion

        #region Weighted
        // IEnumerable<T> with Weights
        public T NextWeighted<T>(IEnumerable<(T value, int weight)> valueWeights)
        {
            int r = NextInt(valueWeights.Sum(t => t.weight)), c = 0;
            foreach (var (value, weight) in valueWeights)
                if (r < (c += weight)) return value;
            throw new Exception("Rnd::Weight failed");
        }

        public T NextWeighted<T>(IEnumerable<IWeightedObject> valueWeights) =>
            NextWeighted<T>(valueWeights, out T value) ? value : default;

        public bool NextWeighted<T>(IEnumerable<IWeightedObject> valueWeights, out T value)
        {
            int sum = valueWeights.Sum(t => t.GetWeight());
            if (sum > 0)
            {
                int r = NextInt(sum), c = 0;
                foreach (var valueWeight in valueWeights)
                    if (r < (c += valueWeight.GetWeight()))
                    {
                        value = (T)valueWeight.GetValue();
                        return true;
                    }
            }
            value = default;
            return false;
        }

        public interface IWeightedObject
        {
            int GetWeight(); object GetValue();
        }
        #endregion

        #region NUMERIC
        public bool NextBool() => (NextSeed() & 1) == 1;
        public int NextSign() => NextBool() ? -1 : +1;

        // max is exclusive : NextInt(5) yields 0,1,2,3,4 
        public int NextInt() => (int)NextSeed() ^ int.MinValue;
        public int NextInt(int max) => (int)((NextSeed() * (ulong)max) >> 32);
        public int NextInt(int min, int max) => NextInt(max - min) + min;
        public int Next(MinMaxInt mm) => NextInt(mm.min, mm.max);

        public int2 NextInt2() => new(NextInt(), NextInt());
        public int2 NextInt2(int2 max) => new(NextInt(max.x), NextInt(max.y));
        public int2 NextInt2(int2 min, int2 max) => new(NextInt(min.x, max.x), NextInt(min.y, max.y));

        public uint NextUInt() => NextSeed() - 1u;
        public uint NextUInt(uint max) => (uint)((NextSeed() * (ulong)max) >> 32);
        public uint NextUInt(uint min, uint max) => (uint)(NextSeed() * (ulong)(max - min) >> 32) + min;

        public float NextFloat() => math.asfloat(0x3f800000 | (NextSeed() >> 9)) - 1.0f;
        public float NextFloat(float max) => NextFloat() * max;
        public float NextFloat(float min, float max) => NextFloat() * (max - min) + min;
        #endregion

        #region DIRECTION
        public Vector2 NextVector2Direction()
        {
            sincos(NextFloat() * PI * 2.0f, out float s, out float c);
            return float2(c, s);
        }

        public Vector3 NextVector3FlatDirection() => NextVector2Direction().X_Y();

        public Quaternion NextQuaternion() =>
            Quaternion.Euler(NextFloat(360), NextFloat(360), NextFloat(360));
        public Quaternion NextQuaternion(float x = 0, float y = 0, float z = 0) =>
            Quaternion.Euler(NextFloat(x), NextFloat(y), NextFloat(z));
        #endregion

        #region SHAPES
        public Vector2 NextInsideCircle(float radius) => NextFloat() * radius * NextVector2Direction();
        public Vector2 NextInsideCircle(float radius, Vector2 direction)
        {
            Vector2 c = NextInsideCircle(radius);
            Vector2 r = new(Mathf.Abs(c.x), c.y);
            Vector2 d = direction.normalized;
            float cos = d.x, sin = d.y;
            return new Vector2(r.x * cos - r.y * sin, r.x * sin + r.y * cos);
        }

        public Vector3 NextInsideBox() => NextInsideBox(Vector3.zero, Vector3.one);
        public Vector3 NextInsideBox(BoxCollider box) =>
            box.transform.TransformPoint(NextInsideBox(Vector3.zero, Vector3.one));
        public Vector3 NextInsideBox(Bounds bounds) => NextInsideBox(bounds.center, bounds.size);
        public Vector3 NextInsideBox(Vector3 size) => NextInsideBox(Vector3.zero, size);
        public Vector3 NextInsideBox(Vector3 center, Vector3 size)
        {
            Vector3 half = size / 2;
            Vector3 local = new(NextFloat(-half.x, half.x), NextFloat(-half.y, half.y), NextFloat(-half.z, half.z));
            return center + local;
        }
        #endregion
    }
}
