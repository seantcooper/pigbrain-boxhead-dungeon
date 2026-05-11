using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PigBrain.LegacyCore.Utility
{
    public static class ExceptionUtility
    {
        public static void Trap(Action action)
        {
            try { action(); } catch (Exception x) { UnityEngine.Debug.LogException(x); }
        }
    }
}