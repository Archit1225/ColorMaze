mergeInto(LibraryManager.library, {
    GetUrlSearchQuery: function () {
        var search = window.location.search;
        var bufferSize = lengthBytesUTF8(search) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(search, buffer, bufferSize);
        return buffer;
    }
});