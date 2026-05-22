mergeInto(LibraryManager.library, {
    CopyToClipboard: function (ptr) {
        navigator.clipboard.writeText(UTF8ToString(ptr));
    }
});