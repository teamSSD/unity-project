using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GlobalButtonSfxManager : SingletonMonoBehaviour<GlobalButtonSfxManager>
{
    private readonly HashSet<int> _registered = new HashSet<int>();

    protected override void OnSingletonAwake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RegisterButtons(null);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScanNextFrameAsync().Forget();
    }

    private async UniTaskVoid ScanNextFrameAsync()
    {
        await UniTask.Yield(cancellationToken: this.GetCancellationTokenOnDestroy());
        RegisterButtons(null);
    }

    public void RegisterButtons(Transform root)
    {
        Button[] buttons = root != null
            ? root.GetComponentsInChildren<Button>(true)
            : FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var btn in buttons)
        {
            int id = btn.GetInstanceID();
            if (_registered.Contains(id)) continue;
            if (btn.GetComponentInParent<Slider>() != null) continue;

            _registered.Add(id);
            btn.onClick.AddListener(() => UISoundManager.Instance?.PlayButtonClick());
        }
    }
}
