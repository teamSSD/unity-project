mergeInto(LibraryManager.library, {
  // Unity WebGL의 IDBFS 캐시를 IndexedDB에 flush.
  // File.WriteAllText 후 이 함수 호출 안 하면 브라우저 강제 종료 시 저장 유실.
  SyncFiles: function () {
    // Unity performs its own initial IDBFS population. A save can arrive while that
    // sync is still active, and Emscripten warns (and does redundant work) when a
    // second syncfs overlaps it. Coalesce callers and wait for every in-flight sync.
    Module.aftertasteSaveSyncQueued = true;
    if (Module.aftertasteSaveSyncPumpActive) return;
    Module.aftertasteSaveSyncPumpActive = true;

    var pump = function () {
      if (!Module.aftertasteSaveSyncQueued) {
        Module.aftertasteSaveSyncPumpActive = false;
        return;
      }
      if (FS.syncFSRequests > 0) {
        setTimeout(pump, 50);
        return;
      }

      Module.aftertasteSaveSyncQueued = false;
      FS.syncfs(false, function (err) {
        if (err) console.error("[SaveSync] IDBFS syncfs failed:", err);
        pump();
      });
    };
    pump();
  }
});
