using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SettingsSaveData
{
    public float effectVolume = 1f;
    public float musicVolume = 1f;
    public int resolutionIndex = 2;
    public bool fullScreen = true;
}

public class SettingsController : MonoBehaviour
{
    private static string SavePath => Application.persistentDataPath + "/saves/settings";

    [SerializeField] private Slider effectSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private ScreenResolutionSelector resolutionSelector;
    [SerializeField] private FullScreenSelector fullScreenSelector;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;

    void OnEnable()
    {
        var data = DataSaveUtil.LoadData(new SettingsSaveData(), SavePath);

        effectSlider.SetValueWithoutNotify(data.effectVolume);
        musicSlider.SetValueWithoutNotify(data.musicVolume);
        SoundManager.Instance?.SetSFXVolume(data.effectVolume);
        SoundManager.Instance?.SetBGMVolume(data.musicVolume);
        resolutionSelector.SetIndex(data.resolutionIndex);
        fullScreenSelector.SetIndex(data.fullScreen);

        effectSlider.onValueChanged.AddListener(OnVolumeChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        resolutionSelector.OnChanged += SaveSettings;
        fullScreenSelector.OnChanged += SaveSettings;
        closeButton.onClick.AddListener(Close);
        resetButton.onClick.AddListener(Reset);
    }

    void OnDisable()
    {
        effectSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        resolutionSelector.OnChanged -= SaveSettings;
        fullScreenSelector.OnChanged -= SaveSettings;
        closeButton.onClick.RemoveAllListeners();
        resetButton.onClick.RemoveAllListeners();
    }

    void OnVolumeChanged(float v)
    {
        SoundManager.Instance?.SetSFXVolume(v);
        SaveSettings();
    }

    void OnMusicChanged(float v)
    {
        SoundManager.Instance?.SetBGMVolume(v);
        SaveSettings();
    }

    void SaveSettings()
    {
        DataSaveUtil.SaveData(new SettingsSaveData
        {
            effectVolume = effectSlider.value,
            musicVolume = musicSlider.value,
            resolutionIndex = resolutionSelector.CurrentIndex,
            fullScreen = fullScreenSelector.IsFullScreen
        }, SavePath);
    }

    void Close()
    {
        SettingsUIManager.Instance?.Close();
    }

    void Reset()
    {
        var d = new SettingsSaveData();
        effectSlider.SetValueWithoutNotify(d.effectVolume);
        musicSlider.SetValueWithoutNotify(d.musicVolume);
        SoundManager.Instance?.SetSFXVolume(d.effectVolume);
        SoundManager.Instance?.SetBGMVolume(d.musicVolume);
        resolutionSelector.SetIndex(d.resolutionIndex);
        fullScreenSelector.SetIndex(d.fullScreen);
        SaveSettings();
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
