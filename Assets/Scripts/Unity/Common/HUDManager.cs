using UnityEngine;

/// <summary>
/// 글로벌 HUD 매니저.
/// StatUI 프리팹(money + stamina + clock)을 인스턴스화하여 모든 씬에서 유지.
/// </summary>
public class HUDManager : SingletonMonoBehaviour<HUDManager>
{
    private const string StatUIPrefabPath = "Prefabs/ui/StatUI";

    private GameObject statUIInstance;

    protected override void OnSingletonAwake()
    {
        var prefab = Resources.Load<GameObject>(StatUIPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[HUDManager] StatUI prefab not found");
            return;
        }

        statUIInstance = Instantiate(prefab, transform);
        statUIInstance.SetActive(false);
    }

    /// <summary>
    /// StatsSystem 초기화 이후 호출. HUD 활성화.
    /// </summary>
    public void Initialize()
    {
        if (statUIInstance != null)
            statUIInstance.SetActive(true);
    }
}
