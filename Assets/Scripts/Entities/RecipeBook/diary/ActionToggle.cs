using UnityEngine;
using UnityEngine.UI;

public class ActionToggle : MonoBehaviour
{
    public ActionType actionType;
    public Toggle toggle;
    private ActionState state = ActionState.Disavailable;
    private void Start()
    {
        ChangeState(state);
    }

    public void ChangeState(ActionState newState)
    {
        this.state = newState;

        if (toggle == null || toggle.targetGraphic == null) return;
        switch (newState)
        {
            case ActionState.Done:
                // Done: 회색, 변경 불가
                toggle.interactable = false;
                toggle.targetGraphic.color = Color.gray; 
                break;

            case ActionState.Available:
                // Available: 흰색, 변경 가능
                toggle.interactable = true;
                toggle.targetGraphic.color = Color.white;
                break;

            case ActionState.Disavailable:
                // Disavailable: 흰색, 변경 불가능
                toggle.interactable = false;
                toggle.targetGraphic.color = Color.white;
                break;
        }
    }
}
