using UnityEngine;
using UnityEngine.UI;

public class TestClickMenuSelect : MonoBehaviour
{
    void Start()
    {
        Invoke("Click", 2.0f); // Wait for initialization
    }

    public void Click()
    {
        var toggles = FindObjectsByType<ActionToggle>(FindObjectsSortMode.None);
        foreach (var t in toggles)
        {
            if (t.phase == PhaseType.Preparation && t.actionType == ActionType.MenuSelect)
            {
                Debug.Log($"[TestClick] Found MenuSelect Toggle on {t.name}. IsInteractable: {t.toggle.interactable}");
                t.toggle.isOn = true;
                return;
            }
        }
        Debug.LogWarning("[TestClick] Could not find MenuSelect toggle");
    }
}