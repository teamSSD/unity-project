using UnityEngine;
using UnityEngine.UI;

public class ToggleList : MonoBehaviour
{
    public ToggleGroup toggleGroup;
    public ActionType selectedAction = ActionType.None;
    private void Awake()
    {
        foreach (Toggle t in toggleGroup.GetComponentsInChildren<Toggle>())
        {
            t.onValueChanged.AddListener((isOn) => OnToggleChanged(t, isOn));
        }
    }

    private void OnToggleChanged(Toggle toggle, bool isOn)
    {
        if (!isOn) return;

        ActionToggle actionToggle = toggle.GetComponent<ActionToggle>();
        selectedAction = actionToggle.actionType;
    }
}
