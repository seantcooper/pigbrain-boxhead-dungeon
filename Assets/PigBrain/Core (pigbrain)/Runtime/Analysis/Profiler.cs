using System;
using System.Collections.Generic;

namespace pigbrain.core.Analysis
{
    public static class Profiler
    {
        static readonly System.Diagnostics.Stopwatch StopWatch = System.Diagnostics.Stopwatch.StartNew();
        public static long Start() => StopWatch.ElapsedTicks;
        public static TimeSpan Stop(long ticks) => new(StopWatch.ElapsedTicks - ticks);
        public static void StopAndLog(long ticks, string name = "unnamed") =>
            UnityEngine.Debug.Log($"'{name}' Took {FormatTicks(new TimeSpan(StopWatch.ElapsedTicks - ticks))}");
        public static void SampleAndLog(string name, Action action)
        {
            var p = Start();
            action();
            StopAndLog(p, name);
        }

        readonly static Dictionary<string, Sample> samples = new();
        class Sample
        {
            const long ExpireTicks = TimeSpan.TicksPerSecond * 5;

            public long total;
            public long slowest = long.MinValue;
            public long fastest = long.MaxValue;
            public long average;
            public long current;
            public int count;

            long slowestTime;
            long fastestTime;

            public void Add(long ticks)
            {
                var now = DateTime.UtcNow.Ticks;

                current = ticks;
                total += ticks;
                count++;
                average = total / count;

                if (ticks > slowest || now - slowestTime > ExpireTicks)
                {
                    slowest = ticks;
                    slowestTime = now;
                }

                if (ticks < fastest || now - fastestTime > ExpireTicks)
                {
                    fastest = ticks;
                    fastestTime = now;
                }
            }
        }

        static void AddSample(string name, long start, long end)
        {
            if (!samples.TryGetValue(name, out var sample))
                samples[name] = sample = new Sample();

            sample.Add(end - start);
        }

        public class SampleScope : IDisposable
        {
            readonly string name;
            readonly long start;
            public SampleScope(string name)
            {
                this.name = name;
                start = Start();
            }
            public void Dispose()
            {
                AddSample(name, start, StopWatch.ElapsedTicks);
            }
        }

        static string FormatTicks(TimeSpan span)
        {
            static string Format(double v)
            {
                if (v < 10) return string.Format("{0:0.00}", v);
                else if (v < 100) return string.Format("{0:0.0}", v);
                return string.Format("{0:0}", v);
            }
            if (span.Seconds < 1) return Format(span.TotalMilliseconds) + "ms";
            else if (span.Minutes < 1) return Format(span.TotalSeconds) + "s";
            return Format(span.TotalMinutes) + "m";
        }
    }
}


// readonly static Dictionary<string, Tracked> tracked = new();
// static void AddTracked(string name, long ticks)
// {
//     void Add(string name)
//     {
//         if (!tracked.ContainsKey(name))
//             tracked.Add(name, new Tracked() { name = name });
//         tracked[name].Add(ticks);
//     }

//     Add(name);
//     Add("Main");
// }

// public static void Report(bool clear = false)
// {
//     UnityEngine.Debug.Log($"Profile: {string.Join("\n", tracked.Values.OrderByDescending(v => v.total).Select(v => $"{v}"))}");
//     if (clear) tracked.Clear();
// }

// class Tracked
// {
//     public string name;
//     public float current, min = float.MaxValue, max, total;
//     public int samples;

//     public void Add(long ticks)
//     {
//         float seconds = (float)new TimeSpan(ticks).TotalSeconds;
//         current = seconds;
//         if (current < min) min = current;
//         if (current > max) max = current;
//         total += seconds;
//         samples++;
//     }
//     public override string ToString() => $"{name} - total: {total} average: {total / samples}";
// }

// public class TrackedScoped : IDisposable
// {
//     readonly string name;
//     readonly long start;
//     readonly Action<string> onComplete;
//     public TrackedScoped(string name, Action<string> onComplete = null)
//     {
//         this.name = name;
//         this.onComplete = onComplete;
//         this.start = StopWatch.ElapsedTicks;
//     }
//     public void Dispose()
//     {
//         AddTracked(name, StopWatch.ElapsedTicks - start);
//         onComplete?.Invoke(name);
//     }
// }

