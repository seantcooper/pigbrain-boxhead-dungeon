using System.Runtime.InteropServices;

namespace pigbrain.core.Analysis
{
    public static class JS
    {
        [DllImport("__Internal")] static extern int GetWasmHeapSize();

        public static int WasmHeapSize
        {
            get
            {
                int mb = 0;
#if UNITY_WEBGL && !UNITY_EDITOR
                mb = GetWasmHeapSize();
#endif
                return mb;
            }
        }

        [DllImport("__Internal")] static extern System.IntPtr GetURL();
        public static string URL
        {
            get
            {
                string url = "";
#if UNITY_WEBGL && !UNITY_EDITOR
                url = Marshal.PtrToStringAnsi(GetURL());
#endif
                return url == "" ? "local" : url;
            }
        }
    }
}