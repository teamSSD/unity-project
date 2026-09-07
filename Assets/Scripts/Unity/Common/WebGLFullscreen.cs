using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>WebGL 브라우저의 실제 전체화면 상태를 Unity 입력 정책에서 사용한다.</summary>
public static class WebGLFullscreen
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int IsBrowserFullscreen();
#endif

    public static bool IsActive
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return IsBrowserFullscreen() != 0;
#else
            return Screen.fullScreen;
#endif
        }
    }
}
