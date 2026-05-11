using System.IO;
using UnityEngine;

namespace pigbrain.core.Utility
{
    public static class PathUtility
    {
        public static ulong GetDirectorySize(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return 0;

            ulong size = 0;
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                try { size += (ulong)new FileInfo(f).Length; }
                catch { }
            }
            return size;
        }

    }
}
