using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStart : MonoBehaviour
{
    [SerializeField] private Button ContinueButton;
    [SerializeField] private Button NewGameButton;
    [SerializeField] private Button Settings;
    
    void Start()
    {
        ContinueButton.interactable = ProgressSystem.instance.IsLoadable();

        ContinueButton.onClick.AddListener(ProcessContinue);
        NewGameButton.onClick.AddListener(NewGame);
        Settings.onClick.AddListener(OpenSetting);
    }

    private void ProcessContinue()
    {
        StatsSystem.Initialize();
        ProgressSystem.instance.Initialize();
        SceneManager.LoadScene("Cuisine");
    }
    private void NewGame()
    {
        StatsSystem.Initialize();
        StatsSystem.SetMoney(10000);
        StatsSystem.SetStamina(100);
        ProgressSystem.instance.Initialize();
        
        StatsSystem.flush();
        ProgressSystem.instance.flush();
        
        SceneManager.LoadScene("Cuisine");
    }
    private void OpenSetting()
    {
        // 세팅창 열기~
    }
}
