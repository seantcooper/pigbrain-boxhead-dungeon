#pragma warning disable UDR0005
using UnityEngine;

public static class ApplicationQuitting
{
    public static bool IsQuitting { get; private set; }

    [RuntimeInitializeOnLoadMethod]
    static void Init() => Application.quitting += () => IsQuitting = true;
}
