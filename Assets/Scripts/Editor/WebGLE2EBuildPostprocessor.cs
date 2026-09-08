using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Development WebGL 출력에만 Unity instance를 E2E 브라우저 API로 연결한다.</summary>
public sealed class WebGLE2EBuildPostprocessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

    // 모든 WebGL 산출물에서 게임의 Canvas Scaler 기준(1920×1080)과 동일한 16:9 화면을
    // 만든다. Unity는 matchWebGLToCanvasSize 기본값으로 CSS 크기와 내부 렌더 타깃을
    // 동기화하므로, 화면 크기 변경 뒤에도 브라우저 포인터 좌표와 Unity 입력 좌표가 같다.
    // Footer 높이만큼은 게임 영역 계산에서 제외해 세로 스크롤과 잘린 Canvas를 방지한다.
    private const string ResponsiveLayout = @"<style id=""aftertaste-webgl-layout"">
html, body { width: 100%; min-height: 100%; margin: 0; background: #231F20; }
body { min-height: 100vh; display: grid; place-items: center; overflow: hidden; }
#unity-container,
#unity-container.unity-desktop,
#unity-container.unity-mobile {
  position: relative !important;
  left: auto !important;
  top: auto !important;
  transform: none !important;
  width: min(100vw, calc((100vh - 38px) * 1.77777778));
}
#unity-canvas,
.unity-mobile #unity-canvas {
  display: block;
  width: 100% !important;
  height: auto !important;
  aspect-ratio: 16 / 9;
  touch-action: none;
}
</style>";

    // Browser page 쪽에서 먼저 API를 만들기 때문에, Unity가 첫 이벤트를 보낼 때의 초기화 순서에 의존하지 않는다.
    private const string BrowserApi = @"<script>
window.AftertasteE2E = window.AftertasteE2E || (function () {
  var events = [];
  function receive(json) {
    var event = typeof json === 'string' ? JSON.parse(json) : json;
    event.browserReceivedAt = performance.now();
    events.push(event);
    if (events.length > 500) events.splice(0, events.length - 500);
    console.log('[AftertasteE2E]', event);
    return event;
  }
  return {
    version: 1,
    events: events,
    clear: function () { events.length = 0; },
    latest: function () { return events.length ? events[events.length - 1] : null; },
    receive: receive,
    command: function (command) {
      if (!window.AftertasteUnityInstance) throw new Error('E2E bridge is not ready.');
      window.AftertasteUnityInstance.SendMessage('AftertasteE2ETestBridge', 'ReceiveCommand', JSON.stringify(command));
    }
  };
})();
</script>";

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        var outputPath = report.summary.outputPath.Replace('\\', '/');
        bool isE2E = (report.summary.options & BuildOptions.Development) != 0 &&
                     (outputPath.Contains("_e2e/") || outputPath.Contains("_e2e_longrun/"));

        var indexPath = Path.Combine(report.summary.outputPath, "index.html");
        if (!File.Exists(indexPath))
        {
            Debug.LogError($"[E2E] WebGL index.html을 찾을 수 없습니다: {indexPath}");
            return;
        }

        var html = File.ReadAllText(indexPath);

        if (!html.Contains("aftertaste-webgl-layout"))
            html = html.Replace("</head>", ResponsiveLayout + "\n</head>");

        // 일반 빌드는 UI 입력 보정만 포함한다. E2E 전용 산출물에만 관찰 API를 주입한다.
        if (!isE2E)
        {
            File.WriteAllText(indexPath, html);
            return;
        }

        const string anchor = "}).then((unityInstance) => {";
        const string registration = "}).then((unityInstance) => {\n        window.AftertasteUnityInstance = unityInstance;";
        if (html.Contains("window.AftertasteUnityInstance"))
        {
            File.WriteAllText(indexPath, html);
            return;
        }
        if (!html.Contains(anchor))
        {
            Debug.LogError("[E2E] WebGL 템플릿의 Unity instance hook을 찾지 못했습니다. E2E API는 비활성화됩니다.");
            File.WriteAllText(indexPath, html);
            return;
        }

        html = html.Replace("</head>", BrowserApi + "\n</head>");
        File.WriteAllText(indexPath, html.Replace(anchor, registration));
        Debug.Log("[E2E] Development WebGL browser bridge 활성화됨");
    }
}
