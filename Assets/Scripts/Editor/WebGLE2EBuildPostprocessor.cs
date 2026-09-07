using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Development WebGL 출력에만 Unity instance를 E2E 브라우저 API로 연결한다.</summary>
public sealed class WebGLE2EBuildPostprocessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 1000;

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
        if (report.summary.platform != BuildTarget.WebGL ||
            (report.summary.options & BuildOptions.Development) == 0)
            return;

        // 일반 Development 빌드는 사람의 수동 디버깅 용도다. E2E 전용 산출물만 브라우저 API를 가진다.
        if (!report.summary.outputPath.Replace('\\', '/').Contains("_e2e/"))
            return;

        var indexPath = Path.Combine(report.summary.outputPath, "index.html");
        if (!File.Exists(indexPath))
        {
            Debug.LogError($"[E2E] WebGL index.html을 찾을 수 없습니다: {indexPath}");
            return;
        }

        const string anchor = "}).then((unityInstance) => {";
        const string registration = "}).then((unityInstance) => {\n        window.AftertasteUnityInstance = unityInstance;";
        var html = File.ReadAllText(indexPath);
        if (html.Contains("window.AftertasteUnityInstance")) return;
        if (!html.Contains(anchor))
        {
            Debug.LogError("[E2E] WebGL 템플릿의 Unity instance hook을 찾지 못했습니다. E2E API는 비활성화됩니다.");
            return;
        }

        html = html.Replace("</head>", BrowserApi + "\n</head>");
        File.WriteAllText(indexPath, html.Replace(anchor, registration));
        Debug.Log("[E2E] Development WebGL browser bridge 활성화됨");
    }
}
