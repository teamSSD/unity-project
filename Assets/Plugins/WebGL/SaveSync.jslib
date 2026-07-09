mergeInto(LibraryManager.library, {
  // Unity WebGL의 IDBFS 캐시를 IndexedDB에 flush.
  // File.WriteAllText 후 이 함수 호출 안 하면 브라우저 강제 종료 시 저장 유실.
  SyncFiles: function () {
    FS.syncfs(false, function (err) {
      if (err) console.error("[SaveSync] IDBFS syncfs failed:", err);
    });
  }
});
