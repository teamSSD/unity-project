mergeInto(LibraryManager.library, {
  AftertastePlayUiSfx: function (relativePathPointer, volume) {
    var relativePath = UTF8ToString(relativePathPointer);
    var audio = document.createElement("audio");
    audio.src = Module.streamingAssetsUrl + "/" + relativePath;
    audio.preload = "auto";
    audio.volume = Math.max(0, Math.min(1, volume));

    var playback = audio.play();
    if (playback) {
      playback.catch(function () {});
    }
  },

  AftertastePlayBgm: function (relativePathPointer, volume) {
    var relativePath = UTF8ToString(relativePathPointer);
    var audio = Module.aftertasteBgmAudio;
    if (!audio) {
      audio = document.createElement("audio");
      audio.loop = true;
      audio.preload = "auto";
      Module.aftertasteBgmAudio = audio;
    }

    var source = Module.streamingAssetsUrl + "/" + relativePath;
    if (audio.getAttribute("data-aftertaste-bgm-path") !== relativePath) {
      audio.src = source;
      audio.setAttribute("data-aftertaste-bgm-path", relativePath);
    }
    audio.volume = Math.max(0, Math.min(1, volume));
    var playback = audio.play();
    if (playback) playback.catch(function () {});
  },

  AftertasteStopBgm: function () {
    if (Module.aftertasteBgmAudio) Module.aftertasteBgmAudio.pause();
  }
});
