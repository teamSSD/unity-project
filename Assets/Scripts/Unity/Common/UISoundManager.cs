using UnityEngine;

public class UISoundManager : SingletonMonoBehaviour<UISoundManager>
{
    [SerializeField] private AudioClip uiBookSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    public void PlayUIBook()      => SoundManager.Instance?.Play2DSFX(uiBookSfx);
    public void PlayButtonClick() => SoundManager.Instance?.Play2DSFX(buttonClickSfx);
}
