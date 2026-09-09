mergeInto(LibraryManager.library, {
  AftertasteE2EEmit: function (jsonPointer) {
    var json = UTF8ToString(jsonPointer);
    var api = window.AftertasteE2E;
    if (!api || typeof api.receive !== "function") {
      console.error("[AftertasteE2E] Browser API was not initialized.");
      return;
    }

    try {
      api.receive(json);
    } catch (error) {
      console.error("[AftertasteE2E] Invalid event payload", error, json);
    }
  }
});
