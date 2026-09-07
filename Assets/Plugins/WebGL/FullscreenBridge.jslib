mergeInto(LibraryManager.library, {
  IsBrowserFullscreen: function () {
    return document.fullscreenElement ? 1 : 0;
  }
});
