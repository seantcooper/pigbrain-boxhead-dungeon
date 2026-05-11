using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class CanvasGroupUtility
{
    public static IDisposable BlockInput(this CanvasGroup canvasGroup)
    {
        var devices = new List<InputDevice>(InputSystem.devices);

        // UI block
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = false;

        // Disable all devices
        foreach (var d in devices)
            InputSystem.DisableDevice(d);

        return new Disposable(() =>
        {
            // Restore UI
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = true;

            // Re-enable devices
            foreach (var d in devices)
                InputSystem.EnableDevice(d);
        });
    }

    public sealed class Disposable : IDisposable
    {
        Action onDispose;
        bool disposed;
        public Disposable(Action onDispose) => this.onDispose = onDispose;
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            onDispose?.Invoke();
        }
    }

}
