mergeInto(LibraryManager.library,
{
    GetWasmHeapSize: function()
    {
        return HEAP8.buffer.byteLength;
    }
});