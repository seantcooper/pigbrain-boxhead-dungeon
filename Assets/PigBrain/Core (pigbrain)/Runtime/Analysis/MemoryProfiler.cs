using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Profiling.Memory;
using UnityProfiler = UnityEngine.Profiling.Profiler;

namespace pigbrain.core.Analysis
{
    public static class MemoryProfiler
    {
        public static Stats Start() => Stats.Capture();
        public static void StopAndLog(Stats snapshot, string name = "unnamed")
        {
            Stats result = Stop(snapshot);
            UnityEngine.Debug.Log($"Memory: {name}:");
            UnityEngine.Debug.Log($"  monoUsed:       {FormatBytes(result.monoUsed)}\n");
            UnityEngine.Debug.Log($"  monoHeap:       {FormatBytes(result.monoHeap)}\n");
            UnityEngine.Debug.Log($"  totalAllocated: {FormatBytes(result.totalAllocated)}\n");
            UnityEngine.Debug.Log($"  totalReserved : {FormatBytes(result.totalReserved)}\n");
            UnityEngine.Debug.Log($"  unusedReserved: {FormatBytes(result.totalUnusedReserved)}\n");
            UnityEngine.Debug.Log($"  gfx:            {FormatBytes(result.gfxAllocated)}");
        }
        public static Stats Stop(Stats snapshot)
        {
            return Stats.Capture().Subtract(snapshot);
        }

        public static string FormatBytes(long b) => FormatBytes((ulong)b);
        public static string FormatBytes(ulong b, ulong bSize = 1024)
        {
            ulong kb = bSize, mb = kb * bSize, gb = mb * bSize;
            if (b >= gb) return $"{b / (double)gb:0.0} GB";
            if (b >= mb) return $"{b / (double)mb:0.0} MB";
            if (b >= kb) return $"{b / (double)kb:0.0} KB";
            return $"{b} B";
        }

        static string FormatDelta(long b)
        {
            string s = b > 0 ? "+" : b < 0 ? "-" : "";
            return s + FormatBytes(Math.Abs(b));
        }

        public struct Stats
        {
            public long monoUsed, monoHeap, totalAllocated, totalReserved, totalUnusedReserved, gfxAllocated;
            public static Stats Capture()
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                return new Stats
                {
                    monoUsed = UnityProfiler.GetMonoUsedSizeLong(),
                    monoHeap = UnityProfiler.GetMonoHeapSizeLong(),
                    totalAllocated = UnityProfiler.GetTotalAllocatedMemoryLong(),
                    totalReserved = UnityProfiler.GetTotalReservedMemoryLong(),
                    totalUnusedReserved = UnityProfiler.GetTotalUnusedReservedMemoryLong(),
                    gfxAllocated = UnityProfiler.GetAllocatedMemoryForGraphicsDriver(),
                };
            }

            public Stats Subtract(Stats other)
            {
                Stats result = this;
                result.monoUsed -= other.monoUsed;
                result.monoHeap -= other.monoHeap;
                result.totalAllocated -= other.totalAllocated;
                result.totalReserved -= other.totalReserved;
                result.totalUnusedReserved -= other.totalUnusedReserved;
                result.gfxAllocated -= other.gfxAllocated;
                return result;
            }
        }
    }
}
