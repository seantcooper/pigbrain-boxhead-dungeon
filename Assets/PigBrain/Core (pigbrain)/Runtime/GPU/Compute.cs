using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace pigbrain.core.Geom
{
    public static class ComputeX
    {
        #region Buffer
        public static ComputeBuffer GetBuffer<T>(this List<T> items) where T : unmanaged
        {
            ComputeBuffer buffer = new(items.Count, Marshal.SizeOf<T>());
            buffer.SetData(items);
            return buffer;
        }

        public static ComputeBuffer GetBuffer<T>(this T[] items) where T : unmanaged
        {
            ComputeBuffer buffer = new(items.Length, Marshal.SizeOf<T>());
            buffer.SetData(items);
            return buffer;
        }

        public static T[] GetArray<T>(this ComputeBuffer buffer) where T : unmanaged =>
            buffer.GetArray<T>(buffer.count);

        public static T[] GetArray<T>(this ComputeBuffer buffer, int count) where T : unmanaged
        {
            var array = new T[count];
            buffer.GetData(array, 0, 0, count);
            return array;
        }
        #endregion
    }

    public class ScopedComputeBuffer : IDisposable
    {
        readonly ComputeBuffer buffer;
        public ScopedComputeBuffer(ComputeBuffer buffer) => this.buffer = buffer;
        void IDisposable.Dispose() => buffer.Release();

        public static implicit operator ComputeBuffer(ScopedComputeBuffer b) => b?.buffer ?? null;
    }
}