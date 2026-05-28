using UnityEngine;

/// <summary>
/// 싱글톤 베이스 클래스.
/// Managers 씬에 배치되어 영구 존재 (DontDestroyOnLoad 불필요).
/// 상속 후 OnSingletonAwake()에서 초기화 로직을 구현하세요.
/// </summary>
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
{
    private static T _instance;
    public static T Instance => _instance;

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = (T)this;
        OnSingletonAwake();
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>
    /// Singleton 초기화 직후 호출. Awake 대신 이 메서드를 오버라이드하세요.
    /// </summary>
    protected virtual void OnSingletonAwake() { }
}
