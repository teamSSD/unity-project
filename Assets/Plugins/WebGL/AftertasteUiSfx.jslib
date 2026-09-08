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
  },

  AftertastePlayLoopSfx: function (relativePathPointer, volume) {
    var relativePath = UTF8ToString(relativePathPointer);
    var audio = Module.aftertasteLoopSfxAudio;
    if (!audio) {
      audio = document.createElement("audio");
      audio.loop = true;
      audio.preload = "auto";
      Module.aftertasteLoopSfxAudio = audio;
    }

    if (audio.getAttribute("data-aftertaste-loop-path") !== relativePath) {
      audio.src = Module.streamingAssetsUrl + "/" + relativePath;
      audio.setAttribute("data-aftertaste-loop-path", relativePath);
    }
    audio.volume = Math.max(0, Math.min(1, volume));
    var playback = audio.play();
    if (playback) playback.catch(function () {});
  },

  AftertasteStopLoopSfx: function () {
    if (!Module.aftertasteLoopSfxAudio) return;
    Module.aftertasteLoopSfxAudio.pause();
    Module.aftertasteLoopSfxAudio.currentTime = 0;
  },

  AftertasteSetLoopSfxVolume: function (volume) {
    if (Module.aftertasteLoopSfxAudio)
      Module.aftertasteLoopSfxAudio.volume = Math.max(0, Math.min(1, volume));
  }
});
